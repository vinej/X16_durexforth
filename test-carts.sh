#!/usr/bin/env bash
# Boot the built cartridges and exercise them through the emulated KEYBOARD
# (x16emu -bas injects keystrokes): NEEDS-load every module from cart ROM with
# a probe each, check the dictionary restored sanely, and (full cart) check the
# baked-in libraries.  This is the test the ordinary suite cannot do - it runs
# the real cartridge boot path (boot stub, packed-image restore, module store).
#
#   ./test-carts.sh          test build/durexforth.crt + build/durexforth_full.crt
#
# Run after ./build-cart.sh / ./make-release.sh.  PASS/FAIL per cart.
set -euo pipefail
cd "$(dirname "$0")"

EMU="${EMU:-x16emu.exe}"
TIMEOUT="${TIMEOUT:-120}"

SMOKE_MARKS="M1= -1|M2= -1|M3= -1|M4= -1|M5= -1|M6= -1|M7= -1|M8= -1|M9= -1|M10= -1|DICT= -1|CART-SMOKE-DONE"
FULL_MARKS="F1= -1|F2= -1|F3= -1|F4= -1|F5-OK|CART-FULL-DONE"

# a throwaway card copy so a stray emulator lock or writes never touch release/
# (a running emulator locks release/sdcard.img - fall back to the test card)
if ! cp release/sdcard.img build/carttest.img 2>/dev/null; then
  echo "==> release/sdcard.img is locked (emulator running?) - using build/testcard.img"
  cp build/testcard.img build/carttest.img
fi

FAIL=0
for MODE in core full; do
  if [ "$MODE" = full ]; then
    CRT=build/durexforth_full.crt
    cat test/cart-smoke.txt test/cart-full-extra.txt > build/carttyped.txt
    MARKS="$SMOKE_MARKS|$FULL_MARKS"
  else
    CRT=build/durexforth.crt
    cp test/cart-smoke.txt build/carttyped.txt
    MARKS="$SMOKE_MARKS"
  fi
  [ -f "$CRT" ] || { echo "==> $MODE: $CRT missing (run ./build-cart.sh first)"; FAIL=1; continue; }

  echo "==> booting $CRT (typed module smoke test, up to ${TIMEOUT}s)"
  OUT=$(cd emulator && MSYS_NO_PATHCONV=1 timeout "$TIMEOUT" "./$EMU" \
          -cart "../$CRT" -sdcard ../build/carttest.img \
          -bas ../build/carttyped.txt -echo -warp </dev/null 2>&1 | tr -d '\r') || true

  MISSING=""
  IFS='|' read -ra MM <<< "$MARKS"
  for m in "${MM[@]}"; do
    echo "$OUT" | grep -qF "$m" || MISSING="$MISSING [$m]"
  done
  if [ -z "$MISSING" ]; then
    echo "==> CART $MODE: PASS"
  else
    echo "==> CART $MODE: FAIL - missing:$MISSING"
    echo "$OUT" | tail -15
    FAIL=1
  fi
done

rm -f emulator/*.nvram
exit $FAIL
