# durexforth (Commander X16 port)

A Commander X16 port of durexForth, a fast, [Forth 2012](http://forth-standard.org/standard/words) core-compatible Forth for 6502-family machines.

This is the **first-version, plain-Forth core**: everything C64-specific has been
removed, leaving the interpreter/compiler, the 6502/65C02 assembler (`asm`), the
decompiler (`see`), disk/file words, and standard utility libraries. Hardware
features (VIC-II graphics, SID/MML sound, sprites, the vi editor) are gone and
will be re-added on top using X16 facilities (VERA, etc.). Upstream durexForth
(C64) lives at <https://github.com/jkotlinski/durexforth>.

## Prebuilt binaries

Don't want to build? Ready-made binaries are in **`release/`**:

| File | Run with | Notes |
|------|----------|-------|
| `durexforth_full.crt` | `x16emu -cart durexforth_full.crt` | everything resident; **no SD card needed** |
| `durexforth.crt` | `x16emu -cart durexforth.crt -sdcard sdcard.img` | core cart; `INCLUDE AUDIO` etc. from the card |
| `durexforth.prg` | `x16emu -prg durexforth.prg -sdcard sdcard.img` | RAM program |
| `sdcard.img` | (mount as the SD card) | FAT32 card with the Forth libraries; where `EDIT` saves and `INCLUDE` loads |

On Windows, double-click **`run-cart.bat`** to launch the cart with the SD card mounted.

## Building

Requirements (relative to the repo root): `acme/acme.exe`, `emulator/x16emu.exe`
(+ `rom.bin`, `makecart.exe`), the committed FAT32 `release/sdcard.img`, and Python 3.

**Windows — PowerShell (no Git Bash needed):**

```powershell
# build all distributables into release\ (both carts, the prg, and the sdcard image)
powershell -ExecutionPolicy Bypass -File .\make-release.ps1
# add -Python "C:\path\to\python.exe" if python isn't on PATH

.\run-cart.bat            # launch the freshly built cart + SD card
```

**Git Bash / Linux:**

```bash
./build.sh            # assemble with ACME and populate release/sdcard.img
./build.sh run        # ...and launch the emulator
./build-cart.sh       # build a bootable core cartridge (release .crt)
./build-cart.sh full  # ...with audio/graphics/vramdisk/see baked in
./make-release.sh     # build every distributable into release/
```

On boot the kernel (`build/durexforth.prg`, load address `$0801`) loads `base`
from the SD card, compiles the core, and saves the turnkey image `durexfth`
back to the card. durexForth runs in the X16 ISO charset (standard ASCII);
source files are kept as plain ASCII.

## Testing

```powershell
powershell -ExecutionPolicy Bypass -File .\run-tests.ps1   # Windows
./run-tests.sh                                             # Git Bash / Linux
```

Both assemble, boot the kernel with `base` rewired to `include test` instead of saving,
runs the Forth 2012 core / core-ext / core-plus / exception conformance suites
plus `test/testx16.fs` (port-specific: number parsing, the relocated zero-page
stack, the golden-RAM buffers, and the inline assembler), and checks the
emulator's echoed output for the pass banner. `test/testsee.fs` is omitted (it
scrapes VIC-II screen RAM, which the X16 lacks).


see the original site of durexforth here: 

https://github.com/jkotlinski/durexforth

I am not the creator of durexforth, see the contributor section into the durexforth site

All the coding for the X16 version is done by Claude Opus 4.8

Thanks