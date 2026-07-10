\ FILE - ANS file word set over the X16 CBDOS (FILE.TXT).
\ Cart: NEEDS FILE      SD card: INCLUDE FILE
\
\ fileid = KERNAL logical file 2..7 (secondary address = fileid), so up to 6
\ files open at once.  Positioning uses the CBDOS command channel: "P" (seek,
\ sd2iec style) and "T" (tell: position + size as hex dwords) - FAT32 files
\ only.  R/W opens use the ",S,M" modify mode (the file must exist;
\ CREATE-FILE with R/W first creates it, then reopens in modify mode).
\ Self-contained: brings its own channel code words (io/dos libraries are not
\ resident on the core cartridge; redefining them is harmless).
\ Not supported: RESIZE-FILE (ior -1).  FLUSH-FILE is a no-op (CBDOS flushes
\ on close).  INCLUDE-FILE reads the rest of the file (max 8 KB) into the
\ highest RAM bank and EVALUATEs it there - do not nest it.
\ READ-LINE ends a line at CR or LF (a CRLF file yields empty in-between lines).

decimal

\ --- channel primitives (same as io.fs; the Forth stack pointer is saved on
\ the hardware stack - X16 kernal channel calls clobber zp workspace) ---------
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

code readst ( -- status )
txa, pha,
$ffb7 jsr,
tay, pla, tax, dex, tya,
lsb sta,x
0 lda,# msb sta,x
rts, end-code

code chrin ( -- chr )
txa, pha,
$ffcf jsr,
tay, pla, tax, dex, tya,
lsb sta,x
0 lda,# msb sta,x
rts, end-code

\ --- access methods ------------------------------------------------------------
0 constant r/o   1 constant w/o   2 constant r/w
: bin ( fam -- fam ) ;
create (fmode) 'R' c, 'W' c, 'M' c,

\ --- fileid pool (2..7) with per-file EOF flags ---------------------------------
\ (durexForth VARIABLEs are NOT zero-initialised - the cell is just allotted,
\ and after a marker-forget cycle it holds old dictionary bytes.)
variable fmap   0 fmap !
variable feof   0 feof !
: (fbit)  ( fid -- mask ) 1 swap lshift ;
: (falloc) ( -- fid | 0 )
  8 2 do fmap @ i (fbit) and 0= if i unloop exit then loop 0 ;
: (fmark) ( fid -- ) (fbit) fmap @ or fmap ! ;
: (ffree) ( fid -- ) (fbit) invert dup fmap @ and fmap !  feof @ and feof ! ;
: (eof?)  ( fid -- flag ) (fbit) feof @ and 0<> ;
: (eof+)  ( fid -- ) (fbit) feof @ or feof ! ;
: (eof-)  ( fid -- ) (fbit) invert feof @ and feof ! ;

\ --- DOS command / file name building -------------------------------------------
create fnbuf 48 allot   variable fnlen
: (fn0)  ( -- ) 0 fnlen ! ;
: (fn+c) ( c -- ) fnbuf fnlen @ + c!  1 fnlen +! ;
: (fn+s) ( addr u -- ) dup 32 > if 2drop exit then
  tuck fnbuf fnlen @ + swap cmove fnlen +! ;

\ After any channel work, put the interpreter's input channel back: an
\ INCLUDEd source file stays CHKIN'd between lines, and stealing the channel
\ garbles the include (same discipline as loadb / (kload) in the core).
: (la>) ( -- ) source-id dup 0 > if chkin drop else drop clrchn then ;

\ send fnbuf as a DOS command, read the status code (codes < 20 -> ior 0)
variable (dsn)
: (drain) ( -- ) begin chrin drop readst until ;
: (dos#) ( -- ior )
  fnbuf fnlen @ $f $f open ?dup if exit then
  $f chkin ?dup if $f close (la>) exit then
  chrin '0' - 10 * chrin '0' - + (dsn) !
  (drain) clrchn $f close (la>)
  (dsn) @ dup 20 < if drop 0 then ;

\ --- open / create / close -------------------------------------------------------
variable f@?                          \ nonzero: prefix the name with "@:"
: (fopen) ( addr u fam -- fileid ior )
  (falloc) ?dup 0= if 2drop drop 0 -1 exit then >r
  (fn0) f@? @ if '@' (fn+c) ':' (fn+c) then
  -rot (fn+s)
  ',' (fn+c) 'S' (fn+c) ',' (fn+c) (fmode) + c@ (fn+c)
  fnbuf fnlen @ r@ dup open ?dup if r> drop 0 swap exit then
  (fn0) (dos#) ?dup if r@ close r> drop 0 swap exit then
  r@ (fmark) r@ (eof-) r> 0 ;

: open-file ( addr u fam -- fileid ior ) 0 f@? ! (fopen) ;
: close-file ( fileid -- ior ) dup close (ffree) (la>) 0 ;

variable cf-a  variable cf-u
: create-file ( addr u fam -- fileid ior )
  >r 2dup cf-u ! cf-a ! 2drop r>
  r/w = if
    1 f@? ! cf-a @ cf-u @ w/o (fopen) ?dup if exit then
    close-file drop                    \ created; reopen in modify mode
    0 f@? ! cf-a @ cf-u @ r/w (fopen)
  else
    1 f@? ! cf-a @ cf-u @ w/o (fopen)
  then ;

\ --- read ---------------------------------------------------------------------
variable rf-a  variable rf-n  variable rf-fid  variable rf-cnt  variable rf-ior
: (rf-st) ( -- )                      \ digest READST after a byte
  readst ?dup if
    dup $40 and if rf-fid @ (eof+) then
    $bf and ?dup if rf-ior ! then then ;

: read-file ( addr u fileid -- u2 ior )
  rf-fid ! rf-n ! rf-a !  0 rf-cnt !  0 rf-ior !
  rf-fid @ (eof?) if 0 0 exit then
  rf-fid @ chkin ?dup if (la>) 0 swap exit then
  begin rf-cnt @ rf-n @ <  rf-ior @ 0=  and  rf-fid @ (eof?) 0=  and while
    chrin rf-a @ rf-cnt @ + c!  1 rf-cnt +!  (rf-st)
  repeat clrchn (la>) rf-cnt @ rf-ior @ ;

variable rl-t                         \ line terminator seen?
: read-line ( addr u fileid -- u2 flag ior )
  rf-fid ! rf-n ! rf-a !  0 rf-cnt !  0 rf-ior !  0 rl-t !
  rf-fid @ (eof?) if 0 0 0 exit then
  rf-fid @ chkin ?dup if (la>) 0 0 rot exit then
  begin rf-cnt @ rf-n @ <  rf-ior @ 0=  and
        rf-fid @ (eof?) 0=  and  rl-t @ 0=  and while
    chrin dup 13 = over 10 = or if drop 1 rl-t !
    else rf-a @ rf-cnt @ + c!  1 rf-cnt +! then
    (rf-st)
  repeat clrchn (la>)
  rf-cnt @
  rl-t @ rf-cnt @ 0<> or  rf-n @ rf-cnt @ = or  0<>
  rf-ior @ ;

\ --- write --------------------------------------------------------------------
: write-file ( addr u fileid -- ior )
  chkout ?dup if nip nip (la>) exit then
  over + swap ?do i c@ emit loop
  readst $bf and clrchn (la>) ;

create (nl) 13 c,
: write-line ( addr u fileid -- ior )
  dup >r write-file ?dup if r> drop exit then
  (nl) 1 r> write-file ;

\ --- position / size via the DOS command channel --------------------------------
: (hexd) ( c -- n ) dup '9' > if $20 or 'a' - 10 + else '0' - then ;
: (hex8) ( -- ud ) 0 0 8 0 do d2* d2* d2* d2* chrin (hexd) 0 d+ loop ;

: (tell) ( fileid -- dpos dsize ior )  \ "T"+chr(fid): 07,PPPPPPPP SSSSSSSS
  (fn0) 'T' (fn+c) (fn+c)
  fnbuf fnlen @ $f $f open ?dup if 0 0 0 0 4 roll exit then
  $f chkin ?dup if $f close (la>) 0 0 0 0 4 roll exit then
  chrin '0' - 10 * chrin '0' - +
  dup 7 = if
    drop chrin drop (hex8) chrin drop (hex8) (drain) clrchn $f close (la>) 0
  else
    >r (drain) clrchn $f close (la>) 0 0 0 0 r>
  then ;

: file-position ( fileid -- ud ior )  (tell) >r 2drop r> ;
: file-size     ( fileid -- ud ior )  (tell) >r 2swap 2drop r> ;

: reposition-file ( ud fileid -- ior )
  dup (eof-)
  (fn0) 'P' (fn+c) (fn+c)             \ "P" chr(fid) offset[0..3]
  swap dup $ff and (fn+c) 8 rshift (fn+c)
  dup $ff and (fn+c) 8 rshift (fn+c)
  (dos#) ;

\ --- named-file operations -------------------------------------------------------
: delete-file ( addr u -- ior ) (fn0) 'S' (fn+c) ':' (fn+c) (fn+s) (dos#) ;
: rename-file ( a1 u1 a2 u2 -- ior )  \ rename a1/u1 -> a2/u2 : "R:new=old"
  (fn0) 'R' (fn+c) ':' (fn+c) (fn+s) '=' (fn+c) (fn+s) (dos#) ;
: file-status ( addr u -- x ior )     \ x = 0; ior = 0 when the file exists
  r/o open-file dup 0= if swap close-file then ;
: resize-file ( ud fileid -- ior ) drop 2drop -1 ;
: flush-file  ( fileid -- ior ) drop 0 ;

\ --- interpret an open file (rest of it, max 8 KB, staged in the top RAM bank) --
: include-file ( fileid -- )
  databank setbank
  $a000 8191 rot read-file
  ?dup if ." include-file err " . cr abort then
  $a000 swap evaluate  0 setbank ;

\ --- X16 directory navigation ----------------------------------------------------
: cd ( addr u -- )                    \ S" SUBDIR" CD ;  S" .." CD ;  S" /" CD
  (fn0) 'C' (fn+c) 'D' (fn+c) ':' (fn+c) (fn+s)
  (dos#) ?dup if ." cd err " . cr then ;
: dir ( -- ) s" $" here loadb if here rdir then ;
