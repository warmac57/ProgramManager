Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

''' <summary>
''' Watches for the keyboard "Prt Sc" key while the application is running and
''' saves a full-screen capture to the user's Pictures\Screens folder.
''' Uses a low-level (global) keyboard hook so it works even when Program
''' Manager is not the focused window. The hook lives only as long as this
''' object is alive, so monitoring stops when the app closes.
''' </summary>
Public Class ScreenshotMonitor
    Implements IDisposable

    ' --- Win32 keyboard hook interop ---
    Private Const WH_KEYBOARD_LL As Integer = 13
    Private Const WM_KEYUP As Integer = &H101
    Private Const WM_SYSKEYUP As Integer = &H105
    Private Const VK_SNAPSHOT As Integer = &H2C   ' "Prt Sc" key

    <StructLayout(LayoutKind.Sequential)>
    Private Structure KBDLLHOOKSTRUCT
        Public vkCode As UInteger
        Public scanCode As UInteger
        Public flags As UInteger
        Public time As UInteger
        Public dwExtraInfo As IntPtr
    End Structure

    Private Delegate Function LowLevelKeyboardProc(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SetWindowsHookEx(idHook As Integer, lpfn As LowLevelKeyboardProc,
                                             hMod As IntPtr, dwThreadId As UInteger) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function UnhookWindowsHookEx(hhk As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function CallNextHookEx(hhk As IntPtr, nCode As Integer,
                                           wParam As IntPtr, lParam As IntPtr) As IntPtr
    End Function

    <DllImport("kernel32.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function GetModuleHandle(lpModuleName As String) As IntPtr
    End Function

    ' Keep a reference to the delegate so the GC never collects it while hooked.
    Private ReadOnly _proc As LowLevelKeyboardProc
    Private _hookId As IntPtr = IntPtr.Zero
    Private _disposed As Boolean = False

    ' Destination: <Pictures>\Screens
    Private ReadOnly _saveFolder As String =
        IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Screens")

    ''' <summary>Raised on a background thread after a screenshot is saved.</summary>
    Public Event ScreenshotSaved As EventHandler

    Public Sub New()
        _proc = AddressOf HookCallback
    End Sub

    ''' <summary>Installs the global keyboard hook. Safe to call more than once.</summary>
    Public Sub StartMonitoring()
        If _hookId <> IntPtr.Zero Then Return

        Using curProcess As Process = Process.GetCurrentProcess()
            Using curModule As ProcessModule = curProcess.MainModule
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                                           GetModuleHandle(curModule.ModuleName), 0)
            End Using
        End Using
    End Sub

    ''' <summary>Removes the keyboard hook. Safe to call more than once.</summary>
    Public Sub StopMonitoring()
        If _hookId <> IntPtr.Zero Then
            UnhookWindowsHookEx(_hookId)
            _hookId = IntPtr.Zero
        End If
    End Sub

    Private Function HookCallback(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
        ' Prt Sc reliably reports on key-up through a low-level hook, so we act
        ' on KEYUP/SYSKEYUP to capture exactly once per physical press.
        If nCode >= 0 Then
            Dim msg As Integer = wParam.ToInt32()
            If msg = WM_KEYUP OrElse msg = WM_SYSKEYUP Then
                Dim data As KBDLLHOOKSTRUCT = Marshal.PtrToStructure(Of KBDLLHOOKSTRUCT)(lParam)
                If data.vkCode = VK_SNAPSHOT Then
                    ' Capture off the hook thread: a low-level hook callback must
                    ' return quickly (~300ms) or Windows silently unhooks it, and
                    ' encoding a large/multi-monitor JPEG can exceed that.
                    Threading.Tasks.Task.Run(AddressOf CaptureAndSave)
                End If
            End If
        End If

        Return CallNextHookEx(_hookId, nCode, wParam, lParam)
    End Function

    Private Sub CaptureAndSave()
        Try
            IO.Directory.CreateDirectory(_saveFolder)

            ' Capture the entire virtual desktop (all monitors).
            Dim bounds As Rectangle = SystemInformation.VirtualScreen
            Using bmp As New Bitmap(bounds.Width, bounds.Height)
                Using g As Graphics = Graphics.FromImage(bmp)
                    g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size)
                End Using
                bmp.Save(BuildUniquePath(), ImageFormat.Jpeg)
            End Using

            RaiseEvent ScreenshotSaved(Me, EventArgs.Empty)
        Catch
            ' Never let a capture failure crash the app or the keyboard hook.
        End Try
    End Sub

    ''' <summary>
    ''' Returns Screens\screens_yyyy-MM-dd_HH-mm-ss.jpg, appending a counter if a
    ''' file with that timestamp already exists (two captures within one second).
    ''' </summary>
    Private Function BuildUniquePath() As String
        Dim stamp As String = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
        Dim path As String = IO.Path.Combine(_saveFolder, $"screens_{stamp}.jpg")

        Dim counter As Integer = 2
        While IO.File.Exists(path)
            path = IO.Path.Combine(_saveFolder, $"screens_{stamp}_{counter}.jpg")
            counter += 1
        End While

        Return path
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        If Not _disposed Then
            StopMonitoring()
            _disposed = True
        End If
    End Sub
End Class
