\ FILE module tests (ANS file words over CBDOS). Requires tester.fs.
\ Runs on the throwaway test card - creates and deletes FTEST* files.

marker ---testfile---

include file

decimal

variable fd
create wbuf 32 allot
create rbuf 32 allot
: fill-w 26 0 do 'A' i + wbuf i + c! loop ;
fill-w

cr .( testfile: create / write / close / open / read ) cr
T{ s" ftest1" w/o create-file swap fd ! -> 0 }T
T{ wbuf 26 fd @ write-file -> 0 }T
T{ fd @ close-file -> 0 }T
T{ s" ftest1" r/o open-file swap fd ! -> 0 }T
T{ rbuf 32 fd @ read-file -> 26 0 }T
T{ rbuf c@ rbuf 25 + c@ -> 'A' 'Z' }T
T{ rbuf 32 fd @ read-file -> 0 0 }T        \ at EOF: nothing, no error
T{ fd @ close-file -> 0 }T

cr .( testfile: file-size / reposition / file-position ) cr
T{ s" ftest1" r/o open-file swap fd ! -> 0 }T
T{ fd @ file-size -> 26 0 0 }T
T{ 10 0 fd @ reposition-file -> 0 }T
T{ rbuf 5 fd @ read-file -> 5 0 }T
T{ rbuf c@ -> 'K' }T                       \ offset 10 = K
T{ fd @ file-position -> 15 0 0 }T
T{ fd @ close-file -> 0 }T

cr .( testfile: write-line / read-line ) cr
T{ s" ftest2" w/o create-file swap fd ! -> 0 }T
T{ s" HELLO" fd @ write-line -> 0 }T
T{ s" WORLD" fd @ write-line -> 0 }T
T{ fd @ close-file -> 0 }T
T{ s" ftest2" r/o open-file swap fd ! -> 0 }T
T{ rbuf 32 fd @ read-line -> 5 -1 0 }T
T{ rbuf c@ rbuf 4 + c@ -> 'H' 'O' }T
T{ rbuf 32 fd @ read-line -> 5 -1 0 }T
T{ rbuf c@ -> 'W' }T
T{ rbuf 32 fd @ read-line -> 0 0 0 }T      \ EOF: empty, flag false
T{ fd @ close-file -> 0 }T

cr .( testfile: status / rename / delete ) cr
T{ s" ftest1" file-status nip -> 0 }T
T{ s" nosuch" file-status nip 0<> -> -1 }T
T{ s" nosuch" r/o open-file nip 0<> -> -1 }T
T{ s" ftest2" s" ftest3" rename-file -> 0 }T
T{ s" ftest3" file-status nip -> 0 }T
T{ s" ftest2" file-status nip 0<> -> -1 }T
T{ s" ftest1" delete-file -> 0 }T
T{ s" ftest3" delete-file -> 0 }T
T{ s" ftest1" file-status nip 0<> -> -1 }T

cr .( testfile: r/w modify mode ) cr
T{ s" ftest4" r/w create-file swap fd ! -> 0 }T
T{ wbuf 10 fd @ write-file -> 0 }T
T{ 2 0 fd @ reposition-file -> 0 }T
T{ rbuf 3 fd @ read-file -> 3 0 }T
T{ rbuf c@ -> 'C' }T
T{ 0 0 fd @ reposition-file -> 0 }T
T{ s" x" fd @ write-file -> 0 }T           \ overwrite the first byte
T{ 0 0 fd @ reposition-file -> 0 }T
T{ rbuf 2 fd @ read-file -> 2 0 }T
T{ rbuf c@ rbuf 1+ c@ -> 'x' 'B' }T
T{ fd @ close-file -> 0 }T
T{ s" ftest4" delete-file -> 0 }T

cr .( testfile: include-file ) cr
T{ s" ftest5" w/o create-file swap fd ! -> 0 }T
T{ s" : incw 42 ;" fd @ write-line -> 0 }T
T{ fd @ close-file -> 0 }T
T{ s" ftest5" r/o open-file swap fd ! -> 0 }T
T{ fd @ include-file incw -> 42 }T
T{ fd @ close-file -> 0 }T
T{ s" ftest5" delete-file -> 0 }T

cr .( testfile: autorun hook ) cr
T{ (autorun) depth -> 0 }T                 \ no AUTORUN on the card: silent no-op
T{ s" autorun" w/o create-file swap fd ! -> 0 }T
T{ s" : arok 77 ;" fd @ write-line -> 0 }T
T{ fd @ close-file -> 0 }T
(autorun)                                  \ boot hook finds + includes it now
T{ arok -> 77 }T
T{ s" autorun" delete-file -> 0 }T

cr .( testfile: unsupported / no-op / pool clean ) cr
T{ 1 0 5 resize-file -> -1 }T
T{ 5 flush-file -> 0 }T
T{ fmap @ -> 0 }T                          \ every fileid returned to the pool

cr .( testfile ok ) cr

---testfile---
