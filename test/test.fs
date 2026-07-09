\ durexForth X16 test runner.
\ Booted by build/run-tests.sh (which makes the kernel `include test`
\ instead of saving the turnkey image). On the first failed assertion the
\ Hayes tester prints "INCORRECT RESULT" / "WRONG NUMBER" and QUITs, so
\ reaching the banner below means every test passed.
\
\ testsee.fs (the C64 screen-scraping decompiler test) is intentionally
\ omitted: it compares VIC-II screen RAM at $0400, which the X16 does not
\ have (text lives in VERA). `see` is instead smoke-tested by testx16.

marker ---test---

page parse-name compat included
page parse-name tester included
page parse-name testcore included
page parse-name testcoreplus included
page parse-name testcoreext included
page parse-name testexception included
page parse-name testx16 included
page parse-name testvideo included
page parse-name testsprite included
page parse-name testtile included
page parse-name testpalfx included
page parse-name testinput included
page parse-name testcoreadd included
page parse-name testaudio included
page parse-name testbank included
page parse-name testvramdisk included
page parse-name testloadsave included

\ include-mechanism smoke test (loads the file "1")
:noname s" include 1 2" evaluate
2 <> abort" include failed"
1 <> abort" include failed" ; execute

---test---

decimal cr cr
.( ============================) cr
.( +++ ALL TESTS PASSED +++) cr
.( ============================) cr
0 1 s" ok" saveb

\ Stop here instead of dropping into the REPL, so the emulator's -run
\ autostart keystroke residue is never interpreted (the trailing "ç?").
: ---halt--- begin again ;
---halt---
