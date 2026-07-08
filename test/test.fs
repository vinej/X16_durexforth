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

parse-name compat included
parse-name tester included
parse-name testcore included
parse-name testcoreplus included
parse-name testcoreext included
parse-name testexception included
parse-name testx16 included

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
