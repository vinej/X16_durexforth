\ VRAM<->disk round-trip test. Requires tester.fs.

marker ---testvramdisk---

include vramdisk

decimal

cr .( testvramdisk: tile save/load round-trip ) cr
\ write 8 known bytes to VRAM bank 1 $2000
1 $2000 vaddr  10 v! 20 v! 30 v! 40 v! 50 v! 60 v! 70 v! 80 v!
s" vtest" $2000 8 tilesave        \ save to disk
1 $2000 vaddr  0 v! 0 v! 0 v! 0 v! 0 v! 0 v! 0 v! 0 v!   \ clobber
s" vtest" $2000 tileload          \ load back
1 $2000 vaddr
T{ v@ v@ v@ v@ v@ v@ v@ v@ -> 10 20 30 40 50 60 70 80 }T

cr .( testvramdisk: generic VSAVE / VLOAD, dev + bank args ) cr
\ VERA bank 0 this time, explicit device 8 (VRAM $8000+ is unused in text mode)
0 $8200 vaddr  21 v! 22 v! 23 v! 24 v! 25 v! 26 v! 27 v! 28 v!
s" vtest2" 8 0 $8200 8 vsave       \ VSAVE ( c-addr u dev bank vaddr len )
0 $8200 vaddr  0 v! 0 v! 0 v! 0 v! 0 v! 0 v! 0 v! 0 v!
s" vtest2" 8 0 $8200 vload          \ VLOAD ( c-addr u dev bank vaddr )
0 $8200 vaddr
T{ v@ v@ v@ v@ v@ v@ v@ v@ -> 21 22 23 24 25 26 27 28 }T

cr .( testvramdisk ok ) cr

---testvramdisk---
