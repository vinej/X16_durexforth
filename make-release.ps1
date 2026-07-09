<#
  make-release.ps1 - build the distributable durexForth binaries into release\
  Native Windows / PowerShell build; no Git Bash needed.

  Requires (paths relative to this script):
    acme\acme.exe          ACME assembler
    emulator\x16emu.exe    x16 emulator (+ rom.bin, makecart.exe)
    release\sdcard.img     a pre-formatted FAT32 image (the committed seed)
    Python 3 on PATH       (or pass -Python "C:\path\to\python.exe")

  Produces:
    release\durexforth_full.crt   bootable cart, everything baked in (no SD)
    release\durexforth.crt        bootable cart, core (libs from the SD card)
    release\durexforth.prg        RAM program (x16emu -prg, needs the SD card)
    release\sdcard.img            FAT32 card: Forth libraries + storage

  Usage:   powershell -ExecutionPolicy Bypass -File .\make-release.ps1
#>
param(
    [string]$Python = "python",
    [int]$BootSeconds = 40          # how long to let the emulator run to save durexfth
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$ACME     = ".\acme\acme.exe"
$MAKECART = "makecart.exe"         # invoked from emulator\
$IMG      = "release\sdcard.img"

$CORE = @("wordlist","labels","doloop","debug","ls","require","open","accept","asm","turnkey")
$OPT  = @("compat","see","io","dos","rnd","timer","audio","loadsave","vramdisk","romdisk")
$MODS = @("graphic","float","floatx")    # forth\mod\ on-demand modules -> cart ROM bank 40+

New-Item -ItemType Directory -Force build, release | Out-Null

function Invoke-Native {                       # run an exe and fail on non-zero exit
    param([string]$Exe, [string[]]$ArgList, [string]$Cwd)
    if ($Cwd) { Push-Location $Cwd }
    try { & $Exe @ArgList; if ($LASTEXITCODE) { throw "$Exe exited $LASTEXITCODE" } }
    finally { if ($Cwd) { Pop-Location } }
}

function Assemble-Kernel {
    [System.IO.File]::WriteAllText("$PSScriptRoot\build\version.asm", "!text `"durexforth x16`"`n")
    Invoke-Native $ACME @("-I","asm","asm\durexforth.asm")
    Copy-Item build\durexforth.prg emulator\durexforth.prg -Force
}

function Populate-Card([string[]]$Files) {
    Invoke-Native $Python (@("build\mkcard.py", $IMG) + $Files) | Out-Null
}

function Boot-ToSaveImage {
    # Run the emulator (warp) long enough for durexForth to compile base.fs and
    # save the packed turnkey image to the card, then stop it.
    # NOTE: do NOT minimize/hide the window - x16emu's -run keystroke injection
    # needs the (normal) SDL window, or durexForth never starts.
    $emuDir = Join-Path $PSScriptRoot "emulator"
    $p = Start-Process -FilePath (Join-Path $emuDir "x16emu.exe") -WorkingDirectory $emuDir `
         -PassThru `
         -ArgumentList @("-sdcard","..\release\sdcard.img","-prg","durexforth.prg","-run","-warp")
    $p | Wait-Process -Timeout $BootSeconds -ErrorAction SilentlyContinue
    if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force; $p.WaitForExit() }
}

function Build-Cart([string]$Mode) {
    Write-Host "==> [$Mode] cartridge"
    Assemble-Kernel
    Invoke-Native $ACME @("asm\cartboot.asm")

    if ($Mode -eq 'full') {
        $bake = "include compat`ninclude io`ninclude dos`ninclude rnd`ninclude timer`ninclude audio`ninclude loadsave`ninclude vramdisk`ninclude romdisk`n"
        $out = "durexforth_full.crt"
    } else {
        $bake = "include romdisk`n"   # the core cart still gets the NEEDS loader
        $out = "durexforth.crt"
    }
    $base = (Get-Content forth\base.fs -Raw) -replace "(?m)^\.\( save new durexforth\.\.\)", ($bake + ".( save new durexforth..)")
    [System.IO.File]::WriteAllText("$PSScriptRoot\build\base.fs", $base)

    $files = @("build\base.fs") + (($CORE + $OPT) | ForEach-Object { "forth\$_.fs" })
    Populate-Card $files
    Write-Host "    booting emulator to save durexfth (up to $BootSeconds s)..."
    Boot-ToSaveImage
    Invoke-Native $Python @("build\extract-durexfth.py", $IMG, "build\durexfth.raw")

    # boot image at bank 32, zero-pad to bank 40, then the on-demand module store
    $stub = [System.IO.File]::ReadAllBytes("$PSScriptRoot\build\cartboot.bin")
    $img  = [System.IO.File]::ReadAllBytes("$PSScriptRoot\build\durexfth.raw")
    $mods = [System.IO.File]::ReadAllBytes("$PSScriptRoot\build\mods.bin")
    $used = $stub.Length + $img.Length
    if ($used -gt 131072) { throw "boot image spills past bank 39" }
    $comb = New-Object byte[] (131072 + $mods.Length)
    [Array]::Copy($stub, 0, $comb, 0, $stub.Length)
    [Array]::Copy($img, 0, $comb, $stub.Length, $img.Length)
    [Array]::Copy($mods, 0, $comb, 131072, $mods.Length)
    [System.IO.File]::WriteAllBytes("$PSScriptRoot\build\cartfull.bin", $comb)

    Invoke-Native ".\$MAKECART" @("-desc","durexForth X16","-author","durexForth","-version",$Mode,
        "-fill","0","-rom_file","32","..\build\cartfull.bin","-o","..\build\$out") "emulator"

    $banks = [math]::Ceiling($used / 16384)
    $free = $banks * 16384 - $used
    Write-Host ("    build\{0}: stub {1} + image {2} = {3} B -> {4} bank(s), {5} B free in last bank; modules {6} B at bank 40" `
                -f $out, $stub.Length, $img.Length, $used, $banks, $free, $mods.Length)
}

# ---- modules, both carts, then the plain prg + populated sdcard, collect ----
Write-Host "==> packing on-demand modules (ROM bank 40+)"
Invoke-Native $Python (@("build\mkmods.py","build\mods.bin") + ($MODS | ForEach-Object { "forth\mod\$_.fs" }))

Build-Cart core
Build-Cart full

Write-Host "==> prg + sdcard image"
Assemble-Kernel
Populate-Card (@("forth\base.fs") + (($CORE + $OPT) | ForEach-Object { "forth\$_.fs" }) + ($MODS | ForEach-Object { "forth\mod\$_.fs" }))

Write-Host "==> collecting into release\"
Copy-Item build\durexforth.crt       release\durexforth.crt      -Force
Copy-Item build\durexforth_full.crt  release\durexforth_full.crt -Force
Copy-Item build\durexforth.prg       release\durexforth.prg      -Force
# release\sdcard.img is already the populated card.

@"
durexForth for the Commander X16 - prebuilt binaries
====================================================

Fastest start - nothing else needed:
    x16emu -cart durexforth_full.crt
  Boots straight into Forth with audio, graphics, VRAM disk, SEE, etc. resident.

Core cartridge - smaller; load libraries from the SD card as needed:
    x16emu -cart durexforth.crt -sdcard sdcard.img
  then e.g.   INCLUDE AUDIO      INCLUDE VRAMDISK

Both cartridges carry on-demand modules in ROM (no SD card needed for them):
    NEEDS GRAPHIC      ( 320x240x256 bitmap drawing - HELP GRAPHIC )
    NEEDS FLOAT        ( floating point + literals  - HELP FLOAT )
    NEEDS FLOATX       ( extended float set, after FLOAT )

As a RAM program (compiles the core from the card on boot):
    x16emu -prg durexforth.prg -sdcard sdcard.img

sdcard.img is a FAT32 image with the Forth source libraries and is where EDIT
saves and INCLUDE loads your own .FS files.  On real hardware write it to an
SD card and load a .crt via a cartridge (or run the .prg).
"@ | Out-File -Encoding ascii release\README.txt

Write-Host "==> release\ ready:"
Get-ChildItem release | Format-Table Name, Length -AutoSize
