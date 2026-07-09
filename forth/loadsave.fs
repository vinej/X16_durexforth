\ LOADSAVE - device-aware file & VRAM load/save (LOADSAVE.TXT).
\ Load with:  include loadsave      (baked into the full cartridge).
\
\ BLOAD / SAVE are thin wrappers over the native LOADB / SAVEB.  LOAD / BVLOAD /
\ BVERIFY use the native (kload) primitive, which drives the KERNAL LOAD with an
\ explicit secondary address (header / relocate / headerless) and mode
\ (RAM / verify / VRAM bank).  PRG-to-VRAM save/load (VLOAD / VSAVE) live in the
\ vramdisk module; loadsave does not define those names.

marker ---loadsave---

\ Load a raw/relocatable binary file from 'dev' to 'addr'.
\   S" LEVEL.BIN" 8 $A000 BLOAD
: bload ( c-addr u dev addr -- )
  swap device loadb drop ;

\ Save memory 'start'..'end' (end exclusive) to a file on 'dev'.
\   S" DUMP.BIN" 8 $2000 $3000 SAVE
: save ( c-addr u dev start end -- )
  >r >r device r> r> 2swap saveb ;

\ Load a PRG file to the load address stored in its own 2-byte header.
\   S" SPRITES.PRG" 8 LOAD
: load ( c-addr u dev -- )
  device 1 0 0 (kload) drop ;

\ Load a headerless binary file straight into VRAM 'bank':'vaddr' (no 2-byte
\ header is skipped).   S" TILES.BIN" 8 0 $F000 BVLOAD
: bvload ( c-addr u dev bank vaddr -- )
  >r 2 + swap device 2 swap r> (kload) drop ;

\ Verify a headerless file on 'dev' against memory at 'addr'.
\ Returns true when every byte matches.   S" TILES.BIN" 8 $A000 BVERIFY
: bverify ( c-addr u dev addr -- flag )
  >r device 2 1 r> (kload) 0<> ;
