\ EXTRAS module tests. Requires tester.fs.

marker ---testextras---

include extras

decimal

cr .( testextras: structures ) cr
begin-structure point
  field:  p.x
  field:  p.y
  cfield: p.c
end-structure
T{ point -> 5 }T
T{ 100 p.x 100 p.y 100 p.c -> 100 102 104 }T
begin-structure mix
  4 +field m.name
  field:  m.val
end-structure
T{ mix -> 6 }T
T{ 0 m.val -> 4 }T
create pt1 point allot
T{ 7 pt1 p.y !  pt1 p.y @ -> 7 }T

cr .( testextras: defer / is / defer@ / action-of ) cr
defer dtest
T{ ' dup is dtest  1 dtest -> 1 1 }T
T{ ' dtest defer@ ' dup = -> -1 }T
T{ action-of dtest ' dup = -> -1 }T
: aot action-of dtest ;
T{ aot ' dup = -> -1 }T
: dset ['] drop is dtest ;
dset
T{ 1 2 dtest -> 1 }T
' dup is dtest

cr .( testextras: ahead / ?comp / ?stack ) cr
: tah ahead 99 . then 42 ;
T{ tah -> 42 }T
T{ ' ?comp catch -> -14 }T                 \ interpreting -> throws
: tqi ?comp 5 postpone literal ; immediate \ guard passes while compiling
: tqc tqi ;
T{ tqc -> 5 }T
T{ ?stack depth -> 0 }T

cr .( testextras: compile / [compile] / comma-quote ) cr
: t1 compile dup ; immediate
: t2 t1 ;
T{ 5 t2 -> 5 5 }T
: imm5 5 postpone literal ; immediate
: t3 [compile] imm5 ; immediate
: t4 t3 ;
T{ t4 -> 5 }T
create cs1 ," HI"
T{ cs1 count nip -> 2 }T
T{ cs1 1+ c@ -> 'H' }T

cr .( testextras: forget ) cr
: fg1 11 ;
: fg2 22 ;
forget fg1
T{ s" fg1" find-name s" fg2" find-name or -> 0 }T
: fg3 33 ;
T{ fg3 -> 33 }T

cr .( testextras ok ) cr

---testextras---
