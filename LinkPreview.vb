Imports System.Drawing.Imaging
Imports System.Net
Imports System.Net.Http
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading

''' <summary>
''' Web links: recognises http(s) URLs (typed, dropped from a browser, or inside a .url file)
''' and fetches a preview picture for them, much like a bookmark manager does. The picture is
''' the page's Open Graph / Twitter preview image, else its icon, and is cached in the Links
''' folder next to the executable, named from a hash of the URL, so it is only downloaded once.
''' </summary>
Friend NotInheritable Class LinkPreview

    Private Sub New()
    End Sub

    ''' <summary>Folder that holds the cached preview pictures (portable, next to the .exe).</summary>
    Public Shared ReadOnly PreviewFolder As String = IO.Path.Combine(Application.StartupPath, "Links")

    ' Largest size a preview is stored at; the grid cell zooms it to fit.
    Private Const MaxPreviewWidth As Integer = 480
    Private Const MaxPreviewHeight As Integer = 320

    ' Only the page's <head> matters, and images over this size aren't worth the download.
    Private Const MaxHtmlBytes As Integer = 1024 * 1024
    Private Const MaxImageBytes As Integer = 10 * 1024 * 1024

    ' Some sites refuse requests that don't look like they come from a browser.
    Private Const BrowserUserAgent As String =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36"

    Private Shared ReadOnly Http As HttpClient = CreateHttpClient()

    ' A layout full of new links shouldn't open dozens of connections at once.
    Private Shared ReadOnly FetchSlots As New SemaphoreSlim(4)

    Private Shared Function CreateHttpClient() As HttpClient
        Dim handler As New HttpClientHandler() With {
            .AutomaticDecompression = DecompressionMethods.All,
            .AllowAutoRedirect = True
        }
        Dim client As New HttpClient(handler) With {.Timeout = TimeSpan.FromSeconds(15)}
        client.DefaultRequestHeaders.UserAgent.ParseAdd(BrowserUserAgent)
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9")
        Return client
    End Function

    ' --- Recognising web links ---

    ''' <summary>True if the text is an absolute http:// or https:// URL.</summary>
    Public Shared Function IsWebUrl(text As String) As Boolean
        If String.IsNullOrWhiteSpace(text) Then Return False
        Dim uri As Uri = Nothing
        Return Uri.TryCreate(text.Trim(), UriKind.Absolute, uri) AndAlso
               (uri.Scheme = Uri.UriSchemeHttp OrElse uri.Scheme = Uri.UriSchemeHttps)
    End Function

    ''' <summary>
    ''' The web address a link opens: the path itself when it is a URL, or the URL= line of an
    ''' Internet Shortcut (.url) file. Nothing for anything else (files, folders, other schemes).
    ''' </summary>
    Public Shared Function WebUrlOf(path As String) As String
        If IsWebUrl(path) Then Return path.Trim()
        If path Is Nothing OrElse Not path.EndsWith(".url", StringComparison.OrdinalIgnoreCase) Then Return Nothing
        Try
            If Not IO.File.Exists(path) Then Return Nothing
            For Each line As String In IO.File.ReadLines(path)
                If line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase) Then
                    Dim url As String = line.Substring(4).Trim()
                    Return If(IsWebUrl(url), url, Nothing)
                End If
            Next
        Catch
        End Try
        Return Nothing
    End Function

    ''' <summary>Name shown for a URL link with no custom name: the site, without "www.".</summary>
    Public Shared Function SiteName(url As String) As String
        Dim uri As Uri = Nothing
        If Not Uri.TryCreate(url, UriKind.Absolute, uri) Then Return url
        Dim host As String = uri.Host
        If host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) Then host = host.Substring(4)
        Return host
    End Function

    ' --- Links dragged out of a browser ---

    ''' <summary>True if the drag carries a web link (a browser link, address bar, or URL text).</summary>
    Public Shared Function HasDroppedUrl(data As IDataObject) As Boolean
        Return DroppedUrl(data) IsNot Nothing
    End Function

    ''' <summary>
    ''' The URL a browser drag carries, or Nothing. Browsers offer it as UniformResourceLocator(W);
    ''' plain URL text (e.g. from a text editor) is accepted too.
    ''' </summary>
    Public Shared Function DroppedUrl(data As IDataObject) As String
        If data Is Nothing Then Return Nothing
        Dim candidates As String() = {
            ReadDropString(data, "UniformResourceLocatorW", Encoding.Unicode),
            ReadDropString(data, "UniformResourceLocator", Encoding.Default),
            TryCast(SafeGetData(data, DataFormats.UnicodeText), String),
            TryCast(SafeGetData(data, DataFormats.Text), String)
        }
        For Each text As String In candidates
            If IsWebUrl(text) Then Return text.Trim()
        Next
        Return Nothing
    End Function

    ''' <summary>
    ''' The page title a browser offers with a dragged link (as the name of the virtual
    ''' "Title.url" file it would create), or "" if there isn't one.
    ''' </summary>
    Public Shared Function DroppedTitle(data As IDataObject) As String
        Try
            Dim stream As IO.MemoryStream = TryCast(SafeGetData(data, "FileGroupDescriptorW"), IO.MemoryStream)
            If stream Is Nothing Then Return ""
            ' FILEGROUPDESCRIPTORW: a UInt32 item count, then FILEDESCRIPTORW entries whose
            ' 260-character cFileName starts 72 bytes in.
            Const NameOffset As Integer = 4 + 72
            Dim bytes As Byte() = stream.ToArray()
            If bytes.Length < NameOffset + 2 Then Return ""
            Dim name As String = Encoding.Unicode.GetString(bytes, NameOffset, Math.Min(520, bytes.Length - NameOffset))
            Dim nul As Integer = name.IndexOf(ChrW(0))
            If nul >= 0 Then name = name.Substring(0, nul)
            If name.EndsWith(".url", StringComparison.OrdinalIgnoreCase) Then name = name.Substring(0, name.Length - 4)
            Return name.Trim()
        Catch
            Return ""
        End Try
    End Function

    Private Shared Function ReadDropString(data As IDataObject, format As String, encoding As Encoding) As String
        Dim stream As IO.MemoryStream = TryCast(SafeGetData(data, format), IO.MemoryStream)
        If stream Is Nothing Then Return Nothing
        Dim text As String = encoding.GetString(stream.ToArray())
        Dim nul As Integer = text.IndexOf(ChrW(0))
        Return If(nul >= 0, text.Substring(0, nul), text)
    End Function

    Private Shared Function SafeGetData(data As IDataObject, format As String) As Object
        Try
            Return If(data.GetDataPresent(format), data.GetData(format), Nothing)
        Catch
            Return Nothing   ' some drag sources fail to render a format they advertise
        End Try
    End Function

    ' --- Cached preview pictures ---

    ''' <summary>File name (without extension) a URL's preview is cached under.</summary>
    Private Shared Function CacheKey(url As String) As String
        Dim hash As Byte() = System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes(url.Trim()))
        Return Convert.ToHexString(hash, 0, 8).ToLowerInvariant()
    End Function

    ''' <summary>Full path of the cached preview for a URL, or Nothing if there isn't one yet.</summary>
    Public Shared Function CachedImagePath(url As String) As String
        Dim key As String = CacheKey(url)
        For Each ext As String In {".jpg", ".png"}
            Dim path As String = IO.Path.Combine(PreviewFolder, key & ext)
            If IO.File.Exists(path) Then Return path
        Next
        Return Nothing
    End Function

    ''' <summary>Loads the cached preview for a URL (without locking the file), or Nothing.</summary>
    Public Shared Function LoadCachedImage(url As String) As Bitmap
        Dim path As String = CachedImagePath(url)
        If path Is Nothing Then Return Nothing
        Try
            Using fs As IO.FileStream = IO.File.OpenRead(path)
                Using img As Image = Image.FromStream(fs)
                    Return New Bitmap(img)
                End Using
            End Using
        Catch
            Return Nothing
        End Try
    End Function

    ''' <summary>Deletes the cached preview for a URL, if any.</summary>
    Public Shared Sub DeleteCachedImage(url As String)
        Try
            Dim path As String = CachedImagePath(url)
            If path IsNot Nothing Then IO.File.Delete(path)
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Stores a picture as the preview for a URL, shrunk to preview size. Pictures that may be
    ''' transparent (site icons, PNG/GIF files) are kept as PNG, everything else is saved as JPG.
    ''' </summary>
    Public Shared Sub SaveImage(url As String, picture As Image, keepTransparency As Boolean)
        IO.Directory.CreateDirectory(PreviewFolder)
        Dim key As String = CacheKey(url)
        Dim hasAlpha As Boolean = keepTransparency
        Dim path As String = IO.Path.Combine(PreviewFolder, key & If(hasAlpha, ".png", ".jpg"))
        Dim otherPath As String = IO.Path.Combine(PreviewFolder, key & If(hasAlpha, ".jpg", ".png"))

        Using thumb As Bitmap = Shrink(picture)
            If hasAlpha Then
                thumb.Save(path, ImageFormat.Png)
            Else
                Dim jpeg As ImageCodecInfo = ImageCodecInfo.GetImageEncoders().First(Function(c) c.FormatID = ImageFormat.Jpeg.Guid)
                Using params As New EncoderParameters(1)
                    params.Param(0) = New EncoderParameter(Imaging.Encoder.Quality, 90L)
                    thumb.Save(path, jpeg, params)
                End Using
            End If
        End Using
        If IO.File.Exists(otherPath) Then IO.File.Delete(otherPath)
    End Sub

    ''' <summary>A copy of the picture no larger than the preview size (never enlarged).</summary>
    Private Shared Function Shrink(picture As Image) As Bitmap
        Dim scale As Double = Math.Min(1.0, Math.Min(MaxPreviewWidth / picture.Width, MaxPreviewHeight / picture.Height))
        Dim w As Integer = Math.Max(1, CInt(picture.Width * scale))
        Dim h As Integer = Math.Max(1, CInt(picture.Height * scale))
        Dim thumb As New Bitmap(w, h, PixelFormat.Format32bppArgb)
        Using g As Graphics = Graphics.FromImage(thumb)
            g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
            g.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality
            g.DrawImage(picture, 0, 0, w, h)
        End Using
        Return thumb
    End Function

    ' --- Fetching a preview from the web ---

    ''' <summary>
    ''' Downloads the page, finds its best preview picture and caches it. Returns True if a
    ''' picture was saved. Never throws; an offline PC or an unhelpful site just returns False.
    ''' </summary>
    Public Shared Async Function FetchAsync(url As String) As Task(Of Boolean)
        Await FetchSlots.WaitAsync().ConfigureAwait(False)
        Try
            Dim pageUri As New Uri(url)
            Dim candidates As New List(Of (Href As Uri, IsIcon As Boolean))()
            Try
                Dim page As (Html As String, FinalUri As Uri) = Await DownloadHtmlAsync(pageUri).ConfigureAwait(False)
                candidates.AddRange(FindImageCandidates(page.Html, page.FinalUri))
                pageUri = page.FinalUri
            Catch
                ' The page itself couldn't be read; the site's favicon may still work.
            End Try
            candidates.Add((New Uri(pageUri, "/favicon.ico"), True))

            For Each candidate In candidates.Distinct()
                Using picture As Image = Await DownloadImageAsync(candidate.Href).ConfigureAwait(False)
                    If picture Is Nothing Then Continue For
                    SaveImage(url, picture, candidate.IsIcon)
                    Return True
                End Using
            Next
            Return False
        Catch
            Return False
        Finally
            FetchSlots.Release()
        End Try
    End Function

    Private Shared Async Function DownloadHtmlAsync(pageUri As Uri) As Task(Of (Html As String, FinalUri As Uri))
        Using request As New HttpRequestMessage(HttpMethod.Get, pageUri)
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml;q=0.9,*/*;q=0.8")
            Using response As HttpResponseMessage = Await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(False)
                response.EnsureSuccessStatusCode()
                Dim finalUri As Uri = If(response.RequestMessage?.RequestUri, pageUri)
                Dim bytes As Byte() = Await ReadLimitedAsync(response, MaxHtmlBytes).ConfigureAwait(False)
                Return (Encoding.UTF8.GetString(bytes), finalUri)
            End Using
        End Using
    End Function

    ''' <summary>Downloads and decodes one picture; Nothing if it fails or isn't a format Windows can read.</summary>
    Private Shared Async Function DownloadImageAsync(imageUri As Uri) As Task(Of Image)
        Try
            Using response As HttpResponseMessage = Await Http.GetAsync(imageUri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(False)
                If Not response.IsSuccessStatusCode Then Return Nothing
                Dim bytes As Byte() = Await ReadLimitedAsync(response, MaxImageBytes).ConfigureAwait(False)
                If bytes.Length = 0 Then Return Nothing

                Using ms As New IO.MemoryStream(bytes)
                    If bytes.Length > 4 AndAlso bytes(0) = 0 AndAlso bytes(1) = 0 AndAlso bytes(2) = 1 AndAlso bytes(3) = 0 Then
                        ' .ico: take the largest frame rather than GDI+'s first (often 16x16) one.
                        Using ico As New Icon(ms, 256, 256)
                            Return ico.ToBitmap()
                        End Using
                    End If
                    ' Image.FromStream needs its stream for the image's lifetime, so copy it out.
                    ' WebP and SVG aren't supported by GDI+ and end up in the Catch.
                    Using img As Image = Image.FromStream(ms)
                        If img.Width < 16 OrElse img.Height < 16 Then Return Nothing   ' tracking pixels
                        Return New Bitmap(img)
                    End Using
                End Using
            End Using
        Catch
            Return Nothing
        End Try
    End Function

    Private Shared Async Function ReadLimitedAsync(response As HttpResponseMessage, limit As Integer) As Task(Of Byte())
        Using stream As IO.Stream = Await response.Content.ReadAsStreamAsync().ConfigureAwait(False)
            Using ms As New IO.MemoryStream()
                Dim buffer(81919) As Byte
                Do
                    Dim read As Integer = Await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, limit - CInt(ms.Length))).ConfigureAwait(False)
                    If read <= 0 Then Exit Do
                    ms.Write(buffer, 0, read)
                Loop While ms.Length < limit
                Return ms.ToArray()
            End Using
        End Using
    End Function

    ' --- Reading the page's preview tags ---

    Private Shared ReadOnly TagPattern As New Regex("<(meta|link)\s[^>]*>", RegexOptions.IgnoreCase)
    Private Shared ReadOnly AttributePattern As New Regex(
        "([\w:-]+)\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s""'>]+))", RegexOptions.IgnoreCase)

    ''' <summary>
    ''' Picture URLs a page offers, best first: og:image, twitter:image, then its icons
    ''' (apple-touch-icon, then the largest declared icon). IsIcon marks the icons.
    ''' </summary>
    Private Shared Function FindImageCandidates(html As String, baseUri As Uri) As List(Of (Href As Uri, IsIcon As Boolean))
        Dim previews As New List(Of Uri)()
        Dim twitter As New List(Of Uri)()
        Dim touchIcons As New List(Of Uri)()
        Dim icons As New List(Of (Size As Integer, Href As Uri))()

        ' Tags after </head> are body content, not the page's own preview.
        Dim headEnd As Integer = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase)
        If headEnd > 0 Then html = html.Substring(0, headEnd)

        For Each tag As Match In TagPattern.Matches(html)
            Dim attrs As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            For Each attr As Match In AttributePattern.Matches(tag.Value)
                Dim value As String = attr.Groups(2).Value & attr.Groups(3).Value & attr.Groups(4).Value
                attrs(attr.Groups(1).Value) = WebUtility.HtmlDecode(value).Trim()
            Next

            If tag.Groups(1).Value.Equals("meta", StringComparison.OrdinalIgnoreCase) Then
                Dim key As String = If(attrs.GetValueOrDefault("property"), attrs.GetValueOrDefault("name"))
                Dim href As Uri = ResolveUri(baseUri, attrs.GetValueOrDefault("content"))
                If key Is Nothing OrElse href Is Nothing Then Continue For
                Select Case key.ToLowerInvariant()
                    Case "og:image", "og:image:url", "og:image:secure_url"
                        previews.Add(href)
                    Case "twitter:image", "twitter:image:src"
                        twitter.Add(href)
                End Select
            Else
                Dim rel As String = If(attrs.GetValueOrDefault("rel"), "").ToLowerInvariant()
                Dim href As Uri = ResolveUri(baseUri, attrs.GetValueOrDefault("href"))
                If href Is Nothing Then Continue For
                If rel.Contains("apple-touch-icon") Then
                    touchIcons.Add(href)
                ElseIf rel.Split(" "c).Contains("icon") Then
                    icons.Add((DeclaredSize(attrs.GetValueOrDefault("sizes")), href))
                End If
            End If
        Next

        Dim result As New List(Of (Href As Uri, IsIcon As Boolean))()
        result.AddRange(previews.Concat(twitter).Select(Function(u) (u, False)))
        result.AddRange(touchIcons.Concat(icons.OrderByDescending(Function(i) i.Size).Select(Function(i) i.Href)).Select(Function(u) (u, True)))
        Return result
    End Function

    ''' <summary>Width from a sizes="32x32" attribute (0 if absent or "any").</summary>
    Private Shared Function DeclaredSize(sizes As String) As Integer
        If String.IsNullOrEmpty(sizes) Then Return 0
        Dim m As Match = Regex.Match(sizes, "(\d+)x\d+", RegexOptions.IgnoreCase)
        Return If(m.Success, Integer.Parse(m.Groups(1).Value), 0)
    End Function

    Private Shared Function ResolveUri(baseUri As Uri, href As String) As Uri
        If String.IsNullOrWhiteSpace(href) OrElse href.StartsWith("data:", StringComparison.OrdinalIgnoreCase) Then Return Nothing
        Dim result As Uri = Nothing
        If Not Uri.TryCreate(baseUri, href, result) Then Return Nothing
        Return If(result.Scheme = Uri.UriSchemeHttp OrElse result.Scheme = Uri.UriSchemeHttps, result, Nothing)
    End Function

End Class
