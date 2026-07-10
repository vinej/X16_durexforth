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

\ --- high-RAM bank streaming (BANK.TXT) ---------------------------------------
\ Channel words inlined (as in io.fs / the file module) instead of `require io`:
\ a nested include here would deepen the source-file chain past the 4 CBDOS
\ FAT32 contexts and break loading this lib from inside test/user includes.
\ The Forth stack pointer is saved on the hardware stack across KERNAL calls.
code chkin ( file# -- ior )
txa, pha,
lsb lda,x tax,
$ffc6 jsr,
+branch bcs,
0 lda,#
:+
tay, pla, tax, tya,
lsb sta,x
0 lda,# msb sta,x
rts, end-code

code chkout ( file# -- ior )
txa, pha,
lsb lda,x tax,
$ffc9 jsr,
+branch bcs,
0 lda,#
:+
tay, pla, tax, tya,
lsb sta,x
0 lda,# msb sta,x
rts, end-code

code clrchn ( -- )
txa, pha,
$ffcc jsr,
pla, tax,
rts, end-code

\ put the interpreter's input channel back after channel work (an INCLUDEd
\ source file stays CHKIN'd between lines - same discipline as the core's loadb)
: (la>) ( -- ) source-id dup 0 > if chkin drop else drop clrchn then ;

\ Load a PRG file into high RAM at bank:$A000.  Files over 8 KB spill into
\ bank+1, bank+2, ... (the KERNAL LOAD advances the bank register itself).
\ The $00 bank register is restored afterwards.   S" LEVEL.DAT" 8 3 BANKLOAD
: bankload ( c-addr u dev bank -- )
  0 c@ >r  setbank device
  0 0 $a000 (kload) drop
  r> setbank ;

variable bk-b  variable bk-o  variable bk-n
: (bk-c@+) ( -- c )                   \ next byte of bank:off, crossing banks
  bk-b @ bk-o @ b@
  1 bk-o +!  bk-o @ 8192 = if 0 bk-o !  1 bk-b +! then ;

\ Save a len-byte slice of high RAM (from bank:off, crossing bank boundaries)
\ to a PRG file with an $A000 header, so BANKLOAD reads it back.  KERNAL SAVE
\ cannot cross banks, so the bytes are streamed through a write channel.
\   S" LEVEL.DAT" 8 3 0 $3000 BANKSAVE
: banksave ( c-addr u dev bank off len -- )
  bk-n ! bk-o ! bk-b ! device
  dup 26 > if 2drop exit then         \ name too long for the pad buffer
  '@' pad c!  ':' pad 1+ c!           \ "@:name,S,W" = create / overwrite
  dup >r pad 2 + swap cmove
  s" ,S,W" pad r@ 2 + + swap cmove    \ mode letters MUST be uppercase: CBDOS
                                      \ parses them case-sensitively, and a
                                      \ ",s,w" file opens for READ - CHKOUT
                                      \ then wedges the whole IEC state
  pad r> 6 +  13 13 open ioabort
  13 chkout ioabort
  0 emit $a0 emit                     \ PRG header: load address $A000
  bk-n @ begin dup while (bk-c@+) emit 1- repeat drop
  clrchn 13 close (la>) ;
