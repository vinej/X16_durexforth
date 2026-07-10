\ STRING - characters and strings (STRING.TXT).
\ Cart: NEEDS STRING      SD card: INCLUDE STRING
\
\ S\" (escape literals), C", COMPARE, PLACE/+PLACE, SLITERAL, plus the
\ BASIC-flavoured helpers (STR VAL NHEX NBIN ASC CHR LEN LEFT RIGHT MID RPT)
\ and LINPUT / GETKEY.  Strings built at run time (S\" interpreted, C"
\ interpreted, CHR, RPT) live in PAD and stay valid until the next such call;
\ they are capped at 189 characters.

decimal

\ --- counted strings -----------------------------------------------------------
: place  ( addr u dst -- )            \ store as a counted string at dst
  2dup c! 1+ swap move ;
variable +p
: +place ( addr u dst -- )            \ append to the counted string at dst
  +p !  +p @ c@ >r
  dup r@ + 189 > if r> drop 2drop exit then
  dup r@ + +p @ c!                    \ new count
  +p @ 1+ r> +  swap move ;           \ append at dst + 1 + old count

\ --- comparison ------------------------------------------------------------------
variable cpa  variable cpb
: compare ( a1 u1 a2 u2 -- n )        \ lexicographic: -1 / 0 / 1
  swap cpb ! rot cpa !
  2dup min 0 ?do
    cpa @ i + c@  cpb @ i + c@  - ?dup if
      0< if -1 else 1 then nip nip unloop exit then
  loop
  - dup if 0< if -1 else 1 then then ;

\ --- string literals ---------------------------------------------------------------
: sliteral ( addr u -- )              \ compile ( -- addr u ) into the definition
  postpone lits dup c, tuck here swap move allot ; immediate

: (c") ( -- c-addr ) r> 1+ dup dup c@ + >r ;
: c" ( "ccc<">" -- c-addr )           \ counted-string literal
  '"' parse state @ if
    postpone (c") dup c, tuck here swap move allot
  else pad place pad then ; immediate

\ --- S\" : string literal with escapes ----------------------------------------------
variable sb-n
: (sb+) ( c -- ) sb-n @ 189 < if pad sb-n @ + c!  1 sb-n +! else drop then ;
: (s\c) ( -- c true | false )         \ next raw source char, consumed
  source >in @ /string dup 0= if 2drop 0 exit then
  drop c@  1 >in +!  -1 ;
: (hexd) ( c -- n ) dup '9' > if $20 or 'a' - 10 + else '0' - then ;
: (esc) ( c -- c' )                   \ single-char escapes; others = themselves
  dup 'a' = if drop 7 exit then
  dup 'b' = if drop 8 exit then
  dup 'e' = if drop 27 exit then
  dup 'f' = if drop 12 exit then
  dup 'l' = if drop 10 exit then
  dup 'n' = if drop 13 exit then
  dup 'q' = if drop '"' exit then
  dup 'r' = if drop 13 exit then
  dup 't' = if drop 9 exit then
  dup 'v' = if drop 11 exit then
  dup 'z' = if drop 0 exit then ;
: (s\parse) ( -- addr u )             \ parse ccc" translating escapes into PAD
  0 sb-n !
  begin (s\c) while
    dup '"' = if drop pad sb-n @ exit then
    dup '\' = if drop
      (s\c) 0= if pad sb-n @ exit then
      dup 'm' = if drop 13 (sb+) 10 (sb+) else
      dup 'x' = if drop
        (s\c) if (hexd) else 0 then 16 *
        (s\c) if (hexd) else 0 then + (sb+)
      else (esc) (sb+) then then
    else (sb+) then
  repeat pad sb-n @ ;
: s\" ( "ccc<">" -- addr u )          \ escapes: \n \t \" \\ \e \xAB \m ...
  (s\parse) state @ if
    postpone lits dup c, tuck here swap move allot then ; immediate

\ --- numbers <-> strings (current BASE unless stated) --------------------------------
: str  ( n -- addr u )  dup abs 0 <# #s rot sign #> ;
: nhex ( u -- addr u )  base @ >r hex     0 <# #s #> r> base ! ;
: nbin ( u -- addr u )  base @ >r 2 base ! 0 <# #s #> r> base ! ;
variable vl-s
: val ( addr u -- n )                 \ string to number, current BASE
  0 vl-s !
  over c@ '-' = if 1 /string 1 vl-s ! then
  over + swap 0 -rot ?do              \ accumulator stays under the loop
    base @ * i c@ (hexd) +
  loop vl-s @ if negate then ;

\ --- BASIC-flavoured slicing ----------------------------------------------------------
: asc  ( addr u -- code ) drop c@ ;
: chr  ( code -- addr 1 ) pad c! pad 1 ;
: len  ( addr u -- u )    nip ;
: left ( addr u n -- addr n2 )  min ;
: right ( addr u n -- addr2 n2 )  over min dup >r - + r> ;
: mid  ( addr u start len -- addr2 len2 )  >r 1- /string 0 max r> min ;
: rpt  ( char n -- addr n )           \ char repeated n times, in PAD
  189 min 0 max dup >r pad swap rot fill pad r> ;

\ --- input ------------------------------------------------------------------------------
: linput ( addr +n -- +n2 ) accept ;  \ read an edited line from the keyboard
: getkey ( -- char ) key ;            \ block until a key, return its code
