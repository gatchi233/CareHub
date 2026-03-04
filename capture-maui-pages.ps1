param(
    [string]$ExePath = "D:\CareHub\CareHub.Desktop\bin\Debug\net8.0-windows10.0.19041.0\win10-x64\CareHub.Desktop.exe",
    [string]$OutputDir = "D:\CareHub\_captures\maui-manual"
)

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$code = @'
using System;
using System.Runtime.InteropServices;
public static class Win32Capture {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
}
'@

Add-Type $code

function Get-MauiWindow {
    $proc = Get-Process CareHub.Desktop -ErrorAction SilentlyContinue |
        Sort-Object StartTime -Descending |
        Select-Object -First 1

    if ($null -eq $proc -or $proc.MainWindowHandle -eq 0) {
        return $null
    }

    return $proc
}

function Capture-Window {
    param(
        [IntPtr]$Handle,
        [string]$Path
    )

    $rect = New-Object Win32Capture+RECT
    [Win32Capture]::GetWindowRect($Handle, [ref]$rect) | Out-Null

    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top

    if ($width -le 0 -or $height -le 0) {
        throw "Invalid window size: ${width}x${height}"
    }

    $bmp = New-Object System.Drawing.Bitmap $width, $height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen(
        [System.Drawing.Point]::new($rect.Left, $rect.Top),
        [System.Drawing.Point]::Empty,
        [System.Drawing.Size]::new($width, $height)
    )
    $bmp.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
}

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
}

$proc = Get-MauiWindow

if ($null -eq $proc) {
    if (-not (Test-Path $ExePath)) {
        throw "MAUI executable not found: $ExePath"
    }

    $proc = Start-Process -FilePath $ExePath -PassThru
    Start-Sleep -Milliseconds 1200

    for ($i = 0; $i -lt 60; $i++) {
        $current = Get-Process -Id $proc.Id -ErrorAction SilentlyContinue
        if ($null -ne $current -and $current.MainWindowHandle -ne 0) {
            $proc = $current
            break
        }
        Start-Sleep -Milliseconds 250
    }
}

if ($null -eq $proc -or $proc.MainWindowHandle -eq 0) {
    throw "Could not detect a visible MAUI window."
}

[Win32Capture]::ShowWindow($proc.MainWindowHandle, 9) | Out-Null
[Win32Capture]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 400

$pages = @(
    "login",
    "home",
    "floor-plan",
    "residents",
    "inventory",
    "staff",
    "help"
)

Write-Host "Output directory: $OutputDir"
Write-Host "Bring the CareHub MAUI app to the requested page, then press Enter."

foreach ($name in $pages) {
    Read-Host "Open page '$name' and press Enter to capture"

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $file = Join-Path $OutputDir "$timestamp-$name.png"
    Capture-Window -Handle $proc.MainWindowHandle -Path $file
    Write-Host "Captured: $file"
}

Write-Host "Done."
