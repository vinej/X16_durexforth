#!/usr/bin/env bash
# Assemble durexForth, boot it in x16emu with a test bootstrap that runs the
# Forth test suite (test/test.fs), and report PASS/FAIL from the echoed output.
#
# The kernel normally loads base.fs, which ends by saving the turnkey image.
# For a test run we swap that last step for `include test`, so the suite runs
# automatically at boot with no keyboard interaction.
set -euo pipefail
cd "$(dirname "$0")"

PY="${PYTHON:-/c/Users/jyv/AppData/Local/Programs/Python/Python312/python.exe}"
IMG=sdcard/sdcard.img
TIMEOUT="${TIMEOUT:-75}"

# Forth core libraries (everything base.fs pulls in), minus base itself.
FORTH="wordlist labels doloop debug ls require open accept asm turnkey \
       compat see io dos rnd timer"
# Test-suite files.
TESTS="tester testcore testcoreplus testcoreext testexception testx16 test 1"

echo "==> assembling kernel"
mkdir -p build
printf '!text "durexforth x16"\n' > build/version.asm
acme/acme.exe -I asm asm/durexforth.asm
cp durexforth.prg emulator/durexforth.prg

echo "==> building test bootstrap (base -> include test)"
sed 's/^save-pack durexfth$/include test/' forth/base.fs > build/base.fs

echo "==> writing sources + tests to $IMG"
FILES="build/base.fs"
for n in $FORTH; do FILES="$FILES forth/$n.fs"; done
for n in $TESTS; do FILES="$FILES test/$n.fs"; done
"$PY" build/mkcard.py "$IMG" $FILES >/dev/null

echo "==> running suite in x16emu (warp, up to ${TIMEOUT}s)"
OUT=$(cd emulator && MSYS_NO_PATHCONV=1 timeout "$TIMEOUT" ./x16emu.exe \
        -sdcard ../sdcard/sdcard.img -prg durexforth.prg -run \
        -echo -warp </dev/null 2>&1 | tr -d '\r') || true

echo "-------------------- emulator output (tail) --------------------"
echo "$OUT" | tail -25
echo "----------------------------------------------------------------"
if echo "$OUT" | grep -qi "ALL TESTS PASSED"; then
  echo "==> RESULT: PASS"
else
  echo "==> RESULT: FAIL"
  echo "$OUT" | grep -iE "incorrect result|wrong number|empty|\?$" | head -10 || true
  exit 1
fi
