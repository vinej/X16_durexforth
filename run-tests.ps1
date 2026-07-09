<#
  run-tests.ps1 - assemble durexForth, boot it with a test bootstrap, and
  report PASS/FAIL from the emulator's echoed output.  Windows / PowerShell
  equivalent of run-tests.sh (no Git Bash needed).

  Usage:  powershell -ExecutionPolicy Bypass -File .\run-tests.ps1
          (add -Python "C:\path\python.exe" if python isn't on PATH)
#>
param(
    [string]$Python = "python",
    [int]$TimeoutSeconds = 90
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$ACME = ".\acme\acme.exe"
$SEED = "release\sdcard.img"          # committed FAT32 card, used as a seed
$IMG  = "build\testcard.img"          # throwaway copy so tests don't dirty it

$FORTH = "wordlist labels doloop debug ls require open accept asm turnkey compat see io dos rnd timer audio loadsave vramdisk".Split(' ')
$TESTS = "tester testcore testcoreplus testcoreext testexception testx16 testvideo testsprite testtile testpalfx testinput testcoreadd testaudio testbank testvramdisk testloadsave test 1".Split(' ')

New-Item -ItemType Directory -Force build | Out-Null

Write-Host "==> assembling kernel"
[System.IO.File]::WriteAllText("$PSScriptRoot\build\version.asm", "!text `"durexforth x16`"`n")
& $ACME -I asm asm\durexforth.asm; if ($LASTEXITCODE) { throw "acme failed" }
Copy-Item build\durexforth.prg emulator\durexforth.prg -Force

Write-Host "==> building test bootstrap (base -> include test)"
$base = (Get-Content forth\base.fs -Raw) -replace "(?m)^save-pack durexfth$", "include test"
[System.IO.File]::WriteAllText("$PSScriptRoot\build\base.fs", $base)

Write-Host "==> writing sources + tests to $IMG (copy of $SEED)"
Copy-Item $SEED $IMG -Force
$files = @("build\base.fs") + ($FORTH | ForEach-Object { "forth\$_.fs" }) + ($TESTS | ForEach-Object { "test\$_.fs" })
& $Python build\mkcard.py $IMG @files | Out-Null; if ($LASTEXITCODE) { throw "mkcard failed" }

Write-Host "==> running suite in x16emu (warp, up to $TimeoutSeconds s)"
# A normal (non-minimized) window is required - x16emu's -run keystroke
# injection does not reach a hidden window, so durexForth would never start.
$emuDir = Join-Path $PSScriptRoot "emulator"
$log = "build\test.log"
Remove-Item $log -ErrorAction SilentlyContinue
$p = Start-Process -FilePath (Join-Path $emuDir "x16emu.exe") -WorkingDirectory $emuDir -PassThru `
     -RedirectStandardOutput $log `
     -ArgumentList @("-sdcard","..\build\testcard.img","-prg","durexforth.prg","-run","-echo","-warp")
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 500
    if ($p.HasExited) { break }
    $hit = Select-String -Path $log -Pattern "ALL TESTS PASSED|INCORRECT RESULT|WRONG NUMBER" -ErrorAction SilentlyContinue
    if ($hit) { Start-Sleep -Milliseconds 300; break }
}
if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force; $p.WaitForExit() }

$txt = Get-Content $log -Raw
Write-Host "-------------------- emulator output (tail) --------------------"
Get-Content $log -Tail 20 | ForEach-Object { $_ -replace '[^\x20-\x7e]', '.' }
Write-Host "----------------------------------------------------------------"
if ($txt -match "ALL TESTS PASSED") {
    Write-Host "==> RESULT: PASS" -ForegroundColor Green
    exit 0
} else {
    Write-Host "==> RESULT: FAIL" -ForegroundColor Red
    ($txt -split "`n") | Select-String -Pattern "INCORRECT RESULT|WRONG NUMBER|EMPTY" | Select-Object -First 5 |
        ForEach-Object { $_.Line -replace '[^\x20-\x7e]', '.' }
    exit 1
}
