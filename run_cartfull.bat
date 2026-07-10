@echo off
REM ---------------------------------------------------------------------------
REM  Launch durexForth from the FULL cartridge (everything baked in) with the
REM  SD card mounted.  Boots straight into Forth; compat/io/dos/rnd/timer/
REM  audio/loadsave/vramdisk are already resident, and the on-demand modules
REM  load from cart ROM:
REM     NEEDS GRAPHIC      NEEDS FLOAT      NEEDS ADVSND   ...
REM  The SD card is only needed for HELP pages, EDIT/INCLUDE of your files
REM  and AUTORUN.
REM
REM  Rebuild the .crt first (in Git Bash) if you changed the sources:
REM     ./build-cart.sh full
REM ---------------------------------------------------------------------------
setlocal
set HERE=%~dp0
set CRT=%HERE%build\durexforth_full.crt
set IMG=%HERE%release\sdcard.img

if not exist "%CRT%" (
    echo Cartridge not found: %CRT%
    echo Build it first from Git Bash:  ./build-cart.sh full
    pause
    exit /b 1
)
if not exist "%IMG%" (
    echo SD card image not found: %IMG%
    pause
    exit /b 1
)

REM x16emu looks for rom.bin in its own folder, so run from there.
cd /d "%HERE%emulator"
echo Launching durexForth FULL cart (everything resident) with SD card...
x16emu.exe -cart "%CRT%" -sdcard "%IMG%" %*

endlocal
