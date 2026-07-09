\ VRAM (VERA bank 1) <-> disk, staged through free RAM at HERE.
\ Generic vsave/vload + the tile / tilemap / sprite wrappers.
\ Load with:  include vramdisk

marker ---vramdisk---

variable vd-a  variable vd-len

\ Save 'len' bytes of VRAM at bank-1 'vaddr' to the named file.
: vsave ( c-addr u vaddr len -- )
  vd-len ! vd-a !
  1 vd-a @ vaddr                          \ VERA read: bank1:vaddr, auto-increment
  here vd-len @ 0 ?do v@ over c! 1+ loop  \ copy VRAM -> RAM at here
  here swap 2swap saveb ;                 \ saveb( here, here+len, name )

\ Load the named file into VRAM at bank-1 'vaddr'.
: vload ( c-addr u vaddr -- )
  vd-a !
  here loadb dup 0= if drop exit then     \ load to RAM at here; end addr (0=fail)
  1 vd-a @ vaddr                          \ VERA write: bank1:vaddr
  here ?do i c@ v! loop ;                 \ stream RAM -> VRAM

\ Tilesets / tilemaps (layer-1 map defaults to the 128x64 text map at $1b000).
: tilesave ( c-addr u vaddr len -- ) vsave ;
: tileload ( c-addr u vaddr -- ) vload ;
: tmapsave ( c-addr u -- ) $b000 $4000 vsave ;
: tmapload ( c-addr u -- ) $b000 vload ;

\ Sprite image pixel data. Reads the sprite's graphics address from its
\ attribute (bank 1) and moves a full 64x64x4bpp block (2 KB).
: sprite-vaddr ( sprite -- vaddr )
  8 * $fc00 + 1 swap vaddr
  v@ v@ $0f and 8 lshift or 5 lshift ;
: sprite-save ( c-addr u sprite -- ) sprite-vaddr $800 vsave ;
: sprite-load ( c-addr u sprite -- ) sprite-vaddr vload ;
