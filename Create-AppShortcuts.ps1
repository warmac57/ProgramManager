<#
  Create-AppShortcuts.ps1
  Opens shell:AppsFolder (the "All Apps" virtual folder) and creates a folder in
  Documents containing a shortcut to every item in it.

  Program Manager runs this from the "Create App Shortcuts..." button on the
  All Links tab, passing -Destination.

  Shortcuts point straight at the AppsFolder item (the same kind of shortcut Windows
  makes when you right-click an app > Create shortcut), so each one shows the app's
  real icon - including Store/UWP apps.

  Run:  powershell -ExecutionPolicy Bypass -File .\Create-AppShortcuts.ps1
  Optional: -Destination "D:\My Apps"  -OpenFolder
  Default destination: Documents\All Apps Shortcuts
#>
param(
    [string]$Destination = (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'All Apps Shortcuts'),
    [switch]$OpenFolder
)

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

public static class AppLink
{
    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    class CShellLink { }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
     Guid("000214F9-0000-0000-C000-000000000046")]
    interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder f, int cch, IntPtr fd, uint flags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder s, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string s);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder s, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string s);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder s, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string s);
        void GetHotkey(out short k);
        void SetHotkey(short k);
        void GetShowCmd(out int c);
        void SetShowCmd(int c);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder s, int cch, out int i);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string s, int i);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string s, uint r);
        void Resolve(IntPtr hwnd, uint flags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string s);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHParseDisplayName(string name, IntPtr bindCtx, out IntPtr pidl, uint sfgaoIn, out uint sfgaoOut);

    public static void Create(string parsingName, string lnkPath, string description)
    {
        IntPtr pidl; uint attrs;
        int hr = SHParseDisplayName(parsingName, IntPtr.Zero, out pidl, 0, out attrs);
        if (hr != 0) Marshal.ThrowExceptionForHR(hr);
        try
        {
            var link = (IShellLinkW)new CShellLink();
            link.SetIDList(pidl);
            if (!string.IsNullOrEmpty(description)) link.SetDescription(description);
            ((IPersistFile)link).Save(lnkPath, true);
            Marshal.ReleaseComObject(link);
        }
        finally { Marshal.FreeCoTaskMem(pidl); }
    }
}
'@

$dest = $Destination
New-Item -ItemType Directory -Force -Path $dest | Out-Null
# Clear shortcuts from a previous run so re-running gives a fresh set
Get-ChildItem -LiteralPath $dest -Filter *.lnk -File | Remove-Item -Force

$shell = New-Object -ComObject Shell.Application
$apps  = $shell.NameSpace('shell:AppsFolder')
if (-not $apps) { throw 'Could not open shell:AppsFolder' }

$invalid = [IO.Path]::GetInvalidFileNameChars()
$created = 0; $failed = 0

foreach ($item in $apps.Items()) {
    $name = $item.Name
    foreach ($c in $invalid) { $name = $name.Replace([string]$c, '_') }
    $name = $name.Trim()
    if (-not $name) { continue }

    # Avoid overwriting when two apps share a display name
    $lnk = Join-Path $dest "$name.lnk"
    $i = 2
    while (Test-Path -LiteralPath $lnk) { $lnk = Join-Path $dest "$name ($i).lnk"; $i++ }

    try {
        [AppLink]::Create("shell:AppsFolder\$($item.Path)", $lnk, $item.Name)
        $created++
    } catch {
        Write-Warning "Failed: $($item.Name) - $($_.Exception.Message)"
        $failed++
    }
}

Write-Host "Created $created shortcuts in '$dest'" -ForegroundColor Green
if ($failed) { Write-Host "$failed failed" -ForegroundColor Yellow }

if ($OpenFolder) { Start-Process explorer.exe $dest }
