\ VRAM (VERA) <-> disk, staged through free RAM at HERE.
\ Generic VLOAD / VSAVE (any VERA bank, explicit device - the LOADSAVE.TXT
\ signatures) plus the bank-1 / current-device tile, tilemap and sprite
\ shortcuts.  Self-contained; file words (BLOAD/SAVE/LOAD/...) live in loadsave.
\ Load with:  include vramdisk

marker ---vramdisk---

variable vd-a  variable vd-bank  variable vd-len

: (dev) $0292 c@ ;                        \ current device (KERNAL FA at $0292)

\ Save 'len' bytes of VRAM 'bank':'vaddr' to a PRG file on 'dev' (read by VLOAD).
: vsave ( c-addr u dev bank vaddr len -- )
  vd-len ! >r >r device r> r> vaddr        \ set FA, VERA read addr bank:vaddr
  here vd-len @ 0 ?do v@ over c! 1+ loop    \ copy VRAM -> RAM at HERE
  here swap 2swap saveb ;                   \ saveb( HERE, HERE+len, name )

\ Load a PRG file from 'dev' into VRAM 'bank':'vaddr' (its 2-byte header skipped).
: vload ( c-addr u dev bank vaddr -- )
  vd-a ! vd-bank ! device
  here loadb dup 0= if drop exit then       \ PRG -> RAM at HERE; end addr (0=fail)
  vd-bank @ vd-a @ vaddr                     \ VERA write bank:vaddr
  here ?do i c@ v! loop ;                    \ stream RAM -> VRAM

\ Tilesets / tilemaps on VERA bank 1, current device (layer-1 text map is $1b000).
: tilesave ( c-addr u vaddr len -- ) >r >r (dev) 1 r> r> vsave ;
: tileload ( c-addr u vaddr -- )     >r     (dev) 1 r>    vload ;
: tmapsave ( c-addr u -- ) $b000 $4000 tilesave ;
: tmapload ( c-addr u -- ) $b000 tileload ;

\ Sprite image pixel data. Reads the sprite's graphics address from its
\ attribute (bank 1) and moves a full 64x64x4bpp block (2 KB).
: sprite-vaddr ( sprite -- vaddr )
  8 * $fc00 + 1 swap vaddr
  v@ v@ $0f and 8 lshift or 5 lshift ;
: sprite-save ( c-addr u sprite -- ) sprite-vaddr $800 tilesave ;
: sprite-load ( c-addr u sprite -- ) sprite-vaddr tileload ;
