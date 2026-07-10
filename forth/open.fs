\ Open a logical file
\ ioresult is 0 on success, kernal
\ error # on failure.
\ The Forth stack pointer is saved on
\ the hardware stack, NOT in zeropage:
\ X16 kernal OPEN far-calls into CBDOS
\ and clobbers zp workspace (a C64
\ "w stx," here cost a debug session).
( nameaddr namelen file# sa --
  ioresult )
code open
txa, pha,
lsb 1+ lda,x \ a = file #
lsb ldy,x \ y = sec. address
$0292 ldx, \ x = device (X16 fa)
$ffba jsr, \ SETLFS
pla, tax, pha, \ x = sp (keep saved)
lsb 2+ lda,x pha, \ a = namelen
msb 3 + ldy,x
lsb 3 + lda,x tax, pla, \ xy = nameptr
$ffbd jsr, \ SETNAM
$ffc0 jsr, \ OPEN
+branch bcs, \ carry set = error
0 lda,# \ A is only valid on error
:+
tay, pla, tax, tya,
inx, inx, inx,
lsb sta,x
0 lda,# msb sta,x
rts, end-code

\ Close a logical file
code close ( file# -- )
txa, pha,
lsb lda,x \ x = file#
$ffc3 jsr, \ CLOSE
pla, tax, inx,
rts, end-code
