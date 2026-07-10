# Boot the built cartridges and exercise them through the emulated KEYBOARD
# (x16emu -bas injects keystrokes): NEEDS-load every module from cart ROM with
# a probe each, check the dictionary restored sanely, and (full cart) check the
# baked-in libraries.  PowerShell twin of test-carts.sh.
#
#   powershell -ExecutionPolicy Bypass -File .\test-carts.ps1
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$EMU     = "x16emu.exe"
$TIMEOUT = 120

$SMOKE = @("M1= -1","M2= -1","M3= -1","M4= -1","M5= -1","M6= -1","M7= -1",
           "M8= -1","M9= -1","M10= -1","DICT= -1","CART-SMOKE-DONE")
$FULL  = @("F1= -1","F2= -1","F3= -1","F4= -1","F5-OK","CART-FULL-DONE")

try { Copy-Item release\sdcard.img build\carttest.img -Force }
catch {
    Write-Host "==> release\sdcard.img is locked (emulator running?) - using build\testcard.img"
    Copy-Item build\testcard.img build\carttest.img -Force
}

$fail = 0
foreach ($mode in @('core','full')) {
    if ($mode -eq 'full') {
        $crt = "build\durexforth_full.crt"
        Get-Content test\cart-smoke.txt, test\cart-full-extra.txt |
            Set-Content build\carttyped.txt -Encoding ascii
        $marks = $SMOKE + $FULL
    } else {
        $crt = "build\durexforth.crt"
        Copy-Item test\cart-smoke.txt build\carttyped.txt -Force
        $marks = $SMOKE
    }
    if (-not (Test-Path $crt)) { Write-Host "==> ${mode}: $crt missing"; $fail = 1; continue }

    Write-Host "==> booting $crt (typed module smoke test, up to $TIMEOUT s)"
    Push-Location emulator
    $p = Start-Process -FilePath ".\$EMU" -PassThru -RedirectStandardOutput "..\build\cartout.txt" `
         -ArgumentList "-cart","..\$crt","-sdcard","..\build\carttest.img",`
                       "-bas","..\build\carttyped.txt","-echo","-warp" -WindowStyle Hidden
    if (-not $p.WaitForExit($TIMEOUT * 1000)) { $p.Kill() }
    Pop-Location

    $out = Get-Content build\cartout.txt -Raw
    $missing = @()
    foreach ($m in $marks) { if (-not $out.Contains($m)) { $missing += $m } }
    if ($missing.Count -eq 0) {
        Write-Host "==> CART ${mode}: PASS"
    } else {
        Write-Host "==> CART ${mode}: FAIL - missing: $($missing -join ', ')"
        ($out -split "`n") | Select-Object -Last 15
        $fail = 1
    }
}

Remove-Item emulator\*.nvram -ErrorAction SilentlyContinue
exit $fail
