\ FLOAT - floating point over the X16 ROM Math Library (FLOAT.TXT).
\ Cart: NEEDS FLOAT      SD card: INCLUDE FLOAT      extended set: FLOATX
\
\ 5-byte MFLPT floats (~9 digits) on a separate 16-deep float stack.  All ROM
\ calls go through the Math Library's DOCUMENTED jump table in ROM bank 4
\ ($FE00-$FE99, stable across ROM revisions) via the native BCALL - pure
\ Forth, no assembler, no BASIC zeropage use (the library's own zp is
\ $A9-$D3, clear of durexForth).  >FLOAT is parsed in Forth (the ROM VAL
\ route needs BASIC's zp CHRGET, which cart boots never initialise).
\ After loading, float literals work: 3.14  -2.5e-3  1e6

decimal

\ --- float stack: 16 x 5 bytes, growing downward -----------------------------
80 buffer: fstk   fstk 80 + constant fstk0   variable fsp
create ftmp 5 allot
: fclear ( -- ) fstk0 fsp ! ;  fclear
: fdepth ( -- n ) fstk0 fsp @ - 5 / ;
: fdrop ( F: r -- ) 5 fsp +! ;
: (fpush) ( -- addr ) fsp @ 5 - dup fsp ! ;

\ --- FAC <-> memory over the jump table (BCALL: a x y bank addr -- a' x' y') -
: (a-y) ( addr -- a x y ) dup 255 and 0 rot 8 rshift ;   \ A=lo Y=hi
: (x-y) ( addr -- a x y ) 0 over 255 and rot 8 rshift ;  \ X=lo Y=hi
: fac! ( addr -- ) (a-y) 4 $fe63 bcall drop 2drop ;      \ MOVFM: mem -> FAC
: fac@ ( addr -- ) (x-y) 4 $fe66 bcall drop 2drop ;      \ MOVMF: FAC -> mem

\ --- memory access, stack shuffles -------------------------------------------
: f@ ( f-addr -- ) ( F: -- r )  (fpush) 5 cmove ;
: f! ( f-addr -- ) ( F: r -- )  fsp @ swap 5 cmove fdrop ;
: fdup  ( F: r -- r r )        fsp @ (fpush) 5 cmove ;
: fover ( F: a b -- a b a )    fsp @ 5 + (fpush) 5 cmove ;
: fswap ( F: a b -- b a )
  fsp @ ftmp 5 cmove  fsp @ 5 + fsp @ 5 cmove  ftmp fsp @ 5 + 5 cmove ;
: fnip  ( F: a b -- b )        fswap fdrop ;

\ --- conversion ---------------------------------------------------------------
: s>f ( n -- ) ( F: -- r )                        \ GIVAYF: A=hi Y=lo signed
  dup 8 rshift 0 rot 255 and 4 $fe03 bcall drop 2drop
  (fpush) fac@ ;
: f>s ( -- u ) ( F: r -- )                        \ GETADR: 0 <= r < 65536
  fsp @ fac! fdrop 0 0 0 4 $fe0c bcall nip swap 8 lshift or ;

\ --- arithmetic: FAC = top, mem operand = second, result replaces both -------
: (fbin) ( romaddr -- ) ( F: r1 r2 -- op )
  >r fsp @ fac!  fsp @ 5 + (a-y) 4 r> bcall drop 2drop
  fdrop fsp @ fac@ ;
: f+ ( F: r1 r2 -- r1+r2 ) $fe18 (fbin) ;
: f- ( F: r1 r2 -- r1-r2 ) $fe12 (fbin) ;         \ FSUB = mem - FAC
: f* ( F: r1 r2 -- r1*r2 ) $fe1e (fbin) ;
: f/ ( F: r1 r2 -- r1/r2 ) $fe24 (fbin) ;         \ FDIV = mem / FAC

: (funa) ( romaddr -- ) ( F: r -- op )
  >r fsp @ fac!  0 0 0 4 r> bcall drop 2drop  fsp @ fac@ ;
: fsqrt ( F: r -- ) $fe30 (funa) ;
: fln   ( F: r -- ) $fe2a (funa) ;
: fexp  ( F: r -- ) $fe3c (funa) ;
: fcos  ( F: r -- ) $fe3f (funa) ;
: fsin  ( F: r -- ) $fe42 (funa) ;
: ftan  ( F: r -- ) $fe45 (funa) ;
: fatan ( F: r -- ) $fe48 (funa) ;

\ --- sign ops on the packed top (byte 0 = exponent, byte 1 bit 7 = sign) -----
: fnegate ( F: r -- -r )
  fsp @ c@ if fsp @ 1+ dup c@ 128 xor swap c! then ;
: fabs ( F: r -- |r| ) fsp @ 1+ dup c@ 127 and swap c! ;

\ --- tests and comparison -----------------------------------------------------
: f0= ( -- flag ) ( F: r -- ) fsp @ c@ 0=  fdrop ;
: f0< ( -- flag ) ( F: r -- )
  fsp @ c@ 0<>  fsp @ 1+ c@ 128 and 0<> and  fdrop ;
: f<  ( -- flag ) ( F: r1 r2 -- ) f- f0< ;
: fmax ( F: a b -- max ) fover fover f< if fnip else fdrop then ;
: fmin ( F: a b -- min ) fover fover f< if fdrop else fnip then ;

\ --- defining words -----------------------------------------------------------
: fvariable ( "name" -- ) create 5 allot ;
: fconstant ( "name" -- ) ( F: r -- ) create here f! 5 allot does> f@ ;

\ --- output: ROM FOUT renders FAC as a zero-terminated string ----------------
: (f$) ( -- z-addr ) ( F: r -- )
  fsp @ fac! fdrop 0 0 0 4 $fe06 bcall nip 8 lshift or ;
: f. ( F: r -- ) (f$) begin dup c@ ?dup while emit 1+ repeat drop space ;

\ --- power --------------------------------------------------------------------
: fpow ( F: x y -- x^y ) fswap fln f* fexp ;      \ x > 0
: f** fpow ;

\ --- integer square root (no float stack use) ---------------------------------
variable sq-n  variable sq-r  variable sq-b
: isqrt ( u -- root )
  sq-n !  0 sq-r !  16384 sq-b !
  begin sq-b @ while
    sq-r @ sq-b @ +  dup sq-n @ swap < 0= if
      sq-n @ swap - sq-n !  sq-r @ 2/ sq-b @ + sq-r !
    else drop sq-r @ 2/ sq-r ! then
    sq-b @ 2 rshift sq-b !
  repeat sq-r @ ;

\ --- string -> float (pure Forth, jump-table ops only) -------------------------
fvariable f-ten   10 s>f f-ten f!
: (f10^*) ( n -- ) ( F: r -- r*10^n )
  dup 0< if negate 0 ?do f-ten f@ f/ loop
  else 0 ?do f-ten f@ f* loop then ;

variable >fa  variable >fn  variable >fok  variable >fdp  variable >fng
: (f>f+) ( -- ) 1 >fa +!  -1 >fn +! ;
: (fdigit) ( -- n true | false )       \ consume one digit if present
  >fn @ if >fa @ c@ dup '0' '9' 1+ within
    if '0' - (f>f+) -1 exit then drop then 0 ;
: (fdigits) ( -- ) ( F: acc -- acc' )  \ digits into the float accumulator
  begin (fdigit) while
    f-ten f@ f* s>f f+  1 >fok !
  repeat ;
: (fchar?) ( c -- flag )               \ consume the char if it is next
  >fn @ if >fa @ c@ = if (f>f+) -1 exit then else drop then 0 ;

: >float ( c-addr u -- flag ) ( F: -- r | )
  >fn ! >fa !  0 >fok !  0 >fdp !  0 >fng !
  0 s>f
  '-' (fchar?) if 1 >fng ! else '+' (fchar?) drop then
  (fdigits)
  '.' (fchar?) if
    >fn @ (fdigits) >fn @ - >fdp ! then       \ count of fraction digits
  0
  'e' (fchar?) 'E' (fchar?) or >fok @ and if
    1 swap                                    \ ( esgn e=0 )
    '-' (fchar?) if swap negate swap else '+' (fchar?) drop then
    0 >fok !
    begin (fdigit) while swap 10 * + 1 >fok ! repeat
    * then                                    \ e * esgn
  >fn @ 0<>  >fok @ 0=  or if drop fdrop 0 exit then
  >fdp @ - (f10^*)
  >fng @ if fnegate then  -1 ;

\ --- float literals: hook the interpreter's not-found vector -------------------
: (flit) ( F: -- r )  r> 1+ dup 5 + 1- >r  f@ ;
: fliteral ( F: r -- ) ['] (flit) compile,  here 5 allot f! ;
: (fnum) ( c-addr u -- )
  2dup >float if 2drop state @ if fliteral then exit then
  notfound ;
' (fnum) 'notfound !
