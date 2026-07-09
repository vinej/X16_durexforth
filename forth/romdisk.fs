\ ROMDISK - on-demand modules from the cartridge ROM.        NEEDS GRAPHIC
\
\ build-cart.sh packs forth/mod/*.fs into cart ROM bank 40+ (build/mkmods.py):
\   [1: name length, 0 = end] [name] [2: size LE] [source, CR line ends] ...
\ NEEDS finds the named module, copies its source into the HIGHEST RAM bank
\ (DATABANK, window $A000) with ROM>MEM and EVALUATEs it there, so no low-RAM
\ staging is used and only the compiled words cost dictionary space.
\ Notes: the staging bank is clobbered; the module source must be < 8 KB and
\ must not switch RAM banks at load time (EVALUATE reads it in place).
\ Uses only core words - baked into both cartridges (useless without a cart).

40 constant modbank

variable md-bank  variable md-off  variable md-size  variable md-nlen
create md-name 32 allot

\ keep the scan offset inside one 16 KB bank window
: md-norm ( -- )
  begin md-off @ $4000 < 0= while
    $4000 negate md-off +!  1 md-bank +! repeat ;

\ read u bytes at the scan position into dst, advance the position
: mread ( dst u -- )
  md-norm dup >r md-bank @ md-off @ 2swap rom>mem r> md-off +! ;

\ read the next directory header; 0 at end of store.  A name length over 31
\ means there is no module store there at all (no cartridge: the empty ROM
\ bank reads as $C0), so treat it as end instead of overrunning md-name.
: entry? ( -- flag )
  md-name 1 mread  md-name c@ dup md-nlen !
  dup 31 > if drop 0 exit then
  0<> dup if
    md-name 1+ md-nlen @ mread
    md-size 2 mread then ;             \ 2 LE bytes = a cell variable

: lower ( c -- c ) dup $41 $5b within if $20 or then ;

\ case-insensitive: does a/u match the current entry's name?
: mod= ( a u -- a u flag )
  dup md-nlen @ <> if 0 exit then
  dup 0 ?do
    over i + c@ lower  md-name 1+ i + c@ lower
    <> if unloop 0 exit then
  loop -1 ;

: needs ( "name" -- )
  parse-name  modbank md-bank !  0 md-off !
  begin entry? while
    mod= if
      2drop databank setbank            \ stage in the highest RAM bank
      $a000 md-size @ mread
      $a000 md-size @ evaluate
      0 setbank exit
    then
    md-size @ md-off +!                 \ skip this module's data
  repeat
  type ."  module?" abort ;
