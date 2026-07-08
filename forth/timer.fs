\ jiffy clock timer

code start ( -- clk )
w stx,      \ save forth stack pointer
$ffde jsr,  \ RDTIM -> a=low x=mid y=high jiffy
pha,        \ save low byte
txa,        \ mid byte
w ldx,      \ restore forth stack pointer
dex,
msb sta,x   \ high result byte = mid jiffy
pla,
lsb sta,x   \ low result byte = low jiffy
rts, end-code

\ stop & print elapsed time
: stop ( clk -- )
start swap - base @ swap decimal
#60 /mod s>d <# '.' hold #s #> type
#1000 #60 */ s>d <# # # # #> type
base ! ;

( : timertest ." $1000 loops..."
start $1000 0 do loop stop ." s" cr ;
timertest )
