\ GRAPHIC - 320x240x256 bitmap drawing (GRAPHIC.TXT).  Cart: NEEDS GRAPHIC
\ SD card: INCLUDE GRAPHIC.  Ported from ForthX16's GFX.FTH toolkit; draws to
\ an 8bpp bitmap at VRAM $00000 on VERA layer 0, filled rows go through the
\ native FX-FILL (VERA cache writes), so no floating point and fast fills.
\   GINIT GCLS ( x y c ) PSET ( x1 y1 x2 y2 c ) LINE FRAME RECT RING OVAL
\   ( x y c a u ) GTEXT ( x y r c ) CIRCLE FCIRCLE
\ Pen API: n GCOLOR then PLOT DRAW BOX FBOX ELL FELL CIRC DISC SAY
\ Low-level: BPSET BHLINE BVLINE BLINE BFILL BRECT BCLS BSEEK ISQRT

decimal

\ --- integer square root (largest r with r*r <= u), bit-by-bit ---------------
variable sq-n  variable sq-r  variable sq-b
: isqrt ( u -- r )
  sq-n !  0 sq-r !  $4000 sq-b !
  begin sq-b @ while
    sq-r @ sq-b @ +  dup sq-n @ swap < 0= if
      sq-n @ swap - sq-n !  sq-r @ 2/ sq-b @ + sq-r !
    else drop sq-r @ 2/ sq-r ! then
    sq-b @ 2 rshift sq-b !
  repeat sq-r @ ;

\ --- VERA layer-0 setup ------------------------------------------------------
$9f25 constant vctrl              \ bit1 = DCSEL
$9f29 constant vdcvid             \ DCSEL=0: bit4 = layer 0 enable
$9f2d constant vl0cfg
$9f2f constant vl0tile
$9f30 constant vl0hs
: ginit ( -- )                    \ full-screen 320x240x256 bitmap, layer 0
  128 screen
  $07 vl0cfg c!                   \ layer 0 = bitmap, 8bpp
  $00 vl0tile c!                  \ bitmap data at $00000, 320 wide
  $00 vl0hs c!                    \ palette offset 0
  vctrl c@ $fd and vctrl c!       \ DCSEL = 0
  vdcvid c@ $10 or vdcvid c! ;    \ enable layer 0

\ --- bitmap addressing: pixel = y*320 + x (17-bit VRAM address) --------------
: bxy>ba ( x y -- bank addr )  320 um* rot 0 d+ swap ;
: bseek  ( x y -- )  bxy>ba vaddr ;

\ --- low-level drawing -------------------------------------------------------
: bpset ( x y color -- )  >r bseek r> v! ;
: bhline ( x y len color -- )                 \ fast row fill via FX-FILL
  swap >r >r bxy>ba r> -rot r> fx-fill ;

variable bf-x  variable bf-w  variable bf-c
: bfill ( x y w h color -- )                  \ filled rectangle
  bf-c ! >r bf-w ! swap bf-x ! r> over + swap
  ?do bf-x @ i bf-w @ bf-c @ bhline loop ;

variable bv-x  variable bv-c
: bvline ( x y len color -- )                 \ vertical run of pixels
  bv-c ! >r swap bv-x ! r> over + swap
  ?do bv-x @ i bv-c @ bpset loop ;

\ Bresenham line between two points.
variable ln-dx  variable ln-dy  variable ln-sx  variable ln-sy
variable ln-err variable ln-x   variable ln-y
variable ln-x2  variable ln-y2  variable ln-c
: bline ( x1 y1 x2 y2 color -- )
  ln-c ! ln-y2 ! ln-x2 ! ln-y ! ln-x !
  ln-x2 @ ln-x @ - abs ln-dx !
  ln-x @ ln-x2 @ < if 1 else -1 then ln-sx !
  ln-y2 @ ln-y @ - abs ln-dy !
  ln-y @ ln-y2 @ < if 1 else -1 then ln-sy !
  ln-dx @ ln-dy @ - ln-err !
  begin
    ln-x @ ln-y @ ln-c @ bpset
    ln-x @ ln-x2 @ <>  ln-y @ ln-y2 @ <>  or
  while
    ln-err @ 2*
    dup ln-dy @ negate > if ln-dy @ negate ln-err +! ln-sx @ ln-x +! then
    ln-dx @ < if ln-dx @ ln-err +! ln-sy @ ln-y +! then
  repeat ;

variable br-x variable br-y variable br-w variable br-h variable br-c
: brect ( x y w h color -- )                  \ rectangle outline
  br-c ! br-h ! br-w ! br-y ! br-x !
  br-x @ br-y @ br-w @ br-c @ bhline
  br-x @  br-y @ br-h @ 1- +  br-w @ br-c @ bhline
  br-x @ br-y @ br-h @ br-c @ bvline
  br-x @ br-w @ 1- +  br-y @ br-h @ br-c @ bvline ;

: bcls ( color -- )               \ 76800 bytes = 2 x 38400 (16-bit counts)
  dup 0 0 38400 fx-fill  0 38400 38400 fx-fill ;

\ --- drawing vocabulary (GRAPHIC.TXT names/signatures) -----------------------
: gcls ( -- )                    0 bcls ;
: pset ( x y color -- )          bpset ;
: line ( x1 y1 x2 y2 color -- )  bline ;

variable gx1  variable gy1  variable gx2  variable gy2
: >xywh ( x1 y1 x2 y2 -- x y w h )       \ sort corners -> x,y,width,height
  gy2 ! gx2 ! gy1 ! gx1 !
  gx1 @ gx2 @ 2dup > if swap then over - 1+
  gy1 @ gy2 @ 2dup > if swap then over - 1+
  >r swap r> ;
: frame ( x1 y1 x2 y2 color -- )  >r >xywh r> brect ;
: rect  ( x1 y1 x2 y2 color -- )  >r >xywh r> bfill ;

\ Filled ellipse: row half-width = rx*isqrt(ry^2-dy^2)/ry, rows via bhline.
variable ov-cx variable ov-cy variable ov-rx variable ov-ry variable ov-c
: oval ( x1 y1 x2 y2 color -- )
  >r >xywh r> ov-c !
  1- 2/ ov-ry !  1- 2/ ov-rx !
  ov-ry @ + ov-cy !  ov-rx @ + ov-cx !
  ov-ry @ 1+  ov-ry @ negate  ?do
    ov-ry @ dup *  i i *  -  isqrt
    ov-rx @ *  ov-ry @ ?dup if / else drop ov-rx @ then
    dup 2* 1+  swap  ov-cx @ swap -  ov-cy @ i +  rot  ov-c @ bhline
  loop ;

\ Ellipse outline: rows give left/right edges, columns give top/bottom edges.
variable ri-cx variable ri-cy variable ri-rx variable ri-ry variable ri-c
: ring ( x1 y1 x2 y2 color -- )
  >r >xywh r> ri-c !
  1- 2/ ri-ry !  1- 2/ ri-rx !
  ri-ry @ + ri-cy !  ri-rx @ + ri-cx !
  ri-ry @ 1+  ri-ry @ negate  ?do
    ri-ry @ dup *  i i *  -  isqrt
    ri-rx @ *  ri-ry @ ?dup if / else drop ri-rx @ then
    dup  ri-cx @ swap -  ri-cy @ i +  ri-c @ bpset
    ri-cx @ +  ri-cy @ i +  ri-c @ bpset
  loop
  ri-rx @ 1+  ri-rx @ negate  ?do
    ri-rx @ dup *  i i *  -  isqrt
    ri-ry @ *  ri-rx @ ?dup if / else drop ri-ry @ then
    dup  ri-cx @ i +  swap  ri-cy @ swap -  ri-c @ bpset
    ri-cx @ i +  swap  ri-cy @ +  ri-c @ bpset
  loop ;

\ GTEXT: 8x8 glyphs from the charset at VRAM $1F000 (ISO: index = raw byte).
variable gt-x  variable gt-y  variable gt-c  variable gt-x0
create gt-rows 8 allot
: >scr ( c -- sc )  dup $20 $7f within if exit then drop $20 ;
: glyph ( sc -- )  8 * $f000 +  1 swap vaddr ;
: gtext ( x y color c-addr u -- )
  >r >r  gt-c ! gt-y ! gt-x !  r> r>
  0 ?do
    dup i + c@ >scr glyph
    8 0 do v@ gt-rows i + c! loop
    gt-x @ i 8 * + gt-x0 !
    8 0 do
      gt-rows i + c@
      8 0 do
        dup $80 i rshift and if
          gt-x0 @ i +  gt-y @ j +  gt-c @ bpset
        then
      loop
      drop
    loop
  loop drop ;

\ Circles by centre + radius, over the ellipse words.
variable ci-x  variable ci-y  variable ci-r
: circle  ( x y r color -- )
  >r ci-r ! ci-y ! ci-x !
  ci-x @ ci-r @ -  ci-y @ ci-r @ -  ci-x @ ci-r @ +  ci-y @ ci-r @ +  r> ring ;
: fcircle ( x y r color -- )
  >r ci-r ! ci-y ! ci-x !
  ci-x @ ci-r @ -  ci-y @ ci-r @ -  ci-x @ ci-r @ +  ci-y @ ci-r @ +  r> oval ;

\ --- pen API: set the colour once, then draw without repeating it ------------
variable pen   1 pen !
: gcolor ( n -- )           pen ! ;
: plot  ( x y -- )          pen @ bpset ;
: draw  ( x1 y1 x2 y2 -- )  pen @ line ;
: box   ( x1 y1 x2 y2 -- )  pen @ frame ;
: fbox  ( x1 y1 x2 y2 -- )  pen @ rect ;
: ell   ( x1 y1 x2 y2 -- )  pen @ ring ;
: fell  ( x1 y1 x2 y2 -- )  pen @ oval ;
: circ  ( x y r -- )        pen @ circle ;
: disc  ( x y r -- )        pen @ fcircle ;
: say   ( x y c-addr u -- ) 2>r pen @ 2r> gtext ;
