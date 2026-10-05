Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.ComTypes

''' <summary>
''' Creates Windows .lnk shortcut files through the shell's own IShellLink COM object
''' (the same one Explorer uses), so no scripting host or extra reference is needed.
''' </summary>
Friend NotInheritable Class ShellShortcut

    Private Sub New()
    End Sub

    <ComImport, Guid("00021401-0000-0000-C000-000000000046")>
    Private Class CShellLink
    End Class

    ' Method order must match the COM vtable exactly.
    <ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")>
    Private Interface IShellLinkW
        Sub GetPath(<Out, MarshalAs(UnmanagedType.LPWStr)> pszFile As System.Text.StringBuilder, cch As Integer, pfd As IntPtr, fFlags As UInteger)
        Sub GetIDList(ByRef ppidl As IntPtr)
        Sub SetIDList(pidl As IntPtr)
        Sub GetDescription(<Out, MarshalAs(UnmanagedType.LPWStr)> pszName As System.Text.StringBuilder, cch As Integer)
        Sub SetDescription(<MarshalAs(UnmanagedType.LPWStr)> pszName As String)
        Sub GetWorkingDirectory(<Out, MarshalAs(UnmanagedType.LPWStr)> pszDir As System.Text.StringBuilder, cch As Integer)
        Sub SetWorkingDirectory(<MarshalAs(UnmanagedType.LPWStr)> pszDir As String)
        Sub GetArguments(<Out, MarshalAs(UnmanagedType.LPWStr)> pszArgs As System.Text.StringBuilder, cch As Integer)
        Sub SetArguments(<MarshalAs(UnmanagedType.LPWStr)> pszArgs As String)
        Sub GetHotkey(ByRef pwHotkey As Short)
        Sub SetHotkey(wHotkey As Short)
        Sub GetShowCmd(ByRef piShowCmd As Integer)
        Sub SetShowCmd(iShowCmd As Integer)
        Sub GetIconLocation(<Out, MarshalAs(UnmanagedType.LPWStr)> pszIconPath As System.Text.StringBuilder, cch As Integer, ByRef piIcon As Integer)
        Sub SetIconLocation(<MarshalAs(UnmanagedType.LPWStr)> pszIconPath As String, iIcon As Integer)
        Sub SetRelativePath(<MarshalAs(UnmanagedType.LPWStr)> pszPathRel As String, dwReserved As UInteger)
        Sub Resolve(hwnd As IntPtr, fFlags As UInteger)
        Sub SetPath(<MarshalAs(UnmanagedType.LPWStr)> pszFile As String)
    End Interface

    ''' <summary>Writes a shortcut at <paramref name="lnkPath"/> that opens <paramref name="targetPath"/>.</summary>
    Public Shared Sub Create(lnkPath As String, targetPath As String, workingDirectory As String, description As String)
        Dim link As IShellLinkW = CType(New CShellLink(), IShellLinkW)
        Try
            link.SetPath(targetPath)
            link.SetWorkingDirectory(workingDirectory)
            link.SetDescription(description)
            CType(link, IPersistFile).Save(lnkPath, True)
        Finally
            Marshal.ReleaseComObject(link)
        End Try
    End Sub

End Class
