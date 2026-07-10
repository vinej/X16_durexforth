@echo off
REM ---------------------------------------------------------------------------
REM  Launch durexForth as a RAM program (.prg) with the SD card mounted.
REM  The kernel loads BASE from the card, compiles the core, saves the turnkey
REM  image DUREXFTH back to the card, and drops you at the Forth prompt.
REM  No cartridge: NEEDS is unavailable here - load modules from the card:
REM     INCLUDE GRAPHIC      INCLUDE FLOAT      INCLUDE ADVSND   ...
REM
REM  Rebuild first (in Git Bash) if you changed the sources:   ./build.sh
REM ---------------------------------------------------------------------------
setlocal
set HERE=%~dp0
set PRG=%HERE%build\durexforth.prg
set IMG=%HERE%release\sdcard.img

if not exist "%PRG%" (
    echo Kernel not found: %PRG%
    echo Build it first from Git Bash:  ./build.sh
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
copy /y "%PRG%" durexforth.prg >nul
echo Launching durexForth (RAM program) with SD card...
x16emu.exe -prg durexforth.prg -run -sdcard "%IMG%" %*

endlocal
