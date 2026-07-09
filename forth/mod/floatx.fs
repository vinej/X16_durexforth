\ FLOATX - extended floating-point set (Forth-2012 FLOATING-EXT flavour).
\ Composes the FLOAT module's words - load FLOAT first:
\   cart: NEEDS FLOAT  NEEDS FLOATX      card: INCLUDE FLOAT  INCLUDE FLOATX
\ Floats are 5-byte packed MFLPT, ~9 significant digits.

\ (If FLOAT is not loaded, the first line below stops with "s>f?".)

decimal

\ --- constants ----------------------------------------------------------------
1 s>f fatan 4 s>f f*  fconstant fpi      \ pi   = 4*atan(1)
1 s>f fatan 2 s>f f*  fconstant fpi2     \ pi/2 = 2*atan(1)
10 s>f fln            fconstant fln10    \ ln(10), for base-10 log/antilog

\ --- float memory / sizing (packed floats are 5 bytes; no alignment) ----------
: float+   ( f-addr -- f-addr' )  5 + ;
: floats   ( n -- n*5 )           5 * ;
: falign   ( -- ) ;                       \ no-op: floats are byte-addressed
: faligned ( addr -- addr ) ;             \ no-op

\ --- float-stack shuffles ------------------------------------------------------
fvariable frtmp
: frot ( F: r1 r2 r3 -- r2 r3 r1 )  frtmp f! fswap frtmp f@ fswap ;
: fsincos ( F: r -- sin cos )       fdup fsin fswap fcos ;

\ --- log / exponential family ---------------------------------------------------
: flog   ( F: r -- log10 )   fln fln10 f/ ;
: falog  ( F: r -- 10^r )    fln10 f* fexp ;
: flnp1  ( F: r -- ln 1+r )  1 s>f f+ fln ;
: fexpm1 ( F: r -- e^r - 1 ) fexp 1 s>f f- ;

\ --- hyperbolic ------------------------------------------------------------------
: fsinh ( F: r -- sinh )  fdup fexp fswap fnegate fexp f- 2 s>f f/ ;
: fcosh ( F: r -- cosh )  fdup fexp fswap fnegate fexp f+ 2 s>f f/ ;
: ftanh ( F: r -- tanh )  fdup fsinh fswap fcosh f/ ;

\ --- inverse trig (built on FATAN; FASIN/FACOS undefined at |r| = 1) -------------
: fasin ( F: r -- asin )  fdup fdup f* 1 s>f fswap f- fsqrt f/ fatan ;
: facos ( F: r -- acos )  fasin fpi2 fswap f- ;

fvariable ft2y   fvariable ft2x
: fatan2 ( F: y x -- angle )   \ full-quadrant, result in (-pi, pi]
  ft2x f! ft2y f!
  ft2x f@ f0= if                        \ x = 0 : straight up / down
    ft2y f@ f0< if fpi2 fnegate else fpi2 then exit
  then
  ft2y f@ ft2x f@ f/ fatan              \ base = atan(y/x)
  ft2x f@ f0< if                        \ x < 0 : shift into the right quadrant
    ft2y f@ f0< if fpi f- else fpi f+ then
  then ;

\ --- approximate comparison --------------------------------------------------
\ F~ ( F: r1 r2 r3 -- ) ( -- flag ) : r3>0 absolute |r1-r2|<r3 ; r3=0 exact ;
\                                     r3<0 relative |r1-r2| < |r3|*(|r1|+|r2|)
fvariable f~tol
: f~ ( -- flag ) ( F: r1 r2 r3 -- )
  fdup f0= if fdrop f- f0= exit then
  fdup f0< if
    fabs f~tol f!
    fover fover fabs fswap fabs f+
    f~tol f@ f* f~tol f!
  else
    f~tol f!
  then
  f- fabs f~tol f@ f< ;

\ --- BASIC-style aliases ( F: r -- f(r) ):   2 S>F SQR F.  ->  1.41421356 ----
: sqr   fsqrt  ;
: sin   fsin   ;
: cos   fcos   ;
: tan   ftan   ;
: atn   fatan  ;
: log   fln    ;     \ BASIC LOG = natural log
: exp   fexp   ;
