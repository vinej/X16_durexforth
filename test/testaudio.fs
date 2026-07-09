\ AUDIO tests (AUDIOYM raw round-trip + AUDIOPCM). Requires tester.fs.

marker ---testaudio---

include audio

decimal

cr .( testaudio: YM raw round-trip ) cr
T{ $c7 $20 ym! $20 ym@ -> $c7 }T
T{ $3a $28 ym! $28 ym@ -> $3a }T
T{ $11 $20 fmpoke $20 ym@ -> $11 }T

cr .( testaudio: PCM FIFO ) cr
T{ $80 pcmctrl pcmfull? -> 0 }T          \ reset -> empty -> not full

cr .( testaudio: PSG note API ) cr
: pvp ( addr -- b ) 1 swap vaddr v@ ;    \ VERA bank-1 peek (voice regs $1f9c0)
psginit
T{ $2a 5 psgvol   $f9d6 pvp $3f and -> $2a }T    \ voice5 +2 volume bits
T{ $1234 5 psgfreq  $f9d4 pvp  $f9d5 pvp -> $34 $12 }T
T{ 2 5 psgwav   $f9d7 pvp $c0 and -> $80 }T      \ voice5 +3 waveform bits

cr .( testaudio: FM note API ) cr
fminit
T{ $4a 1 fmnote   $29 ym@ -> $4a }T              \ ch1 note -> KC reg $29
0 0 fmvol   3 0 fmpan   0 0 fminst   64 0 fmdrum \ setters just run
30 20 fmvib                                     \ LFO regs not shadowed by ym_read

cr .( testaudio: FM frequency in Hz ) cr
\ 440 Hz = A4 -> KC $4a on ch0 reg $28; key-on shadow reg $08 = $78|ch
T{ 440 0 fmfreq   $28 ym@  $08 ym@ -> $4a $78 }T
T{ 880 0 fmfreq   $28 ym@ -> $5a }T             \ octave up: high nybble +1
T{ 440 3 fmfreq   $2b ym@  $08 ym@ -> $4a $7b }T \ channel routing (reg $28+ch)
T{ 0 3 fmfreq     $08 ym@ -> 3 }T               \ 0 Hz -> release (ops off, ch only)

cr .( testaudio: PSG note ) cr
\ note $41 = octave 4, semitone 1 (C) -> freq 22<<4 = $160 on voice 3
T{ $41 3 psgnote  $f9cc pvp  $f9cd pvp -> $60 $01 }T
T{ 0 3 psgnote  $f9ce pvp $3f and -> 0 }T        \ note 0 -> volume 0

cr .( testaudio: play-strings run ) cr
s" ceg" 0 fmplay
s" ceg" 1 psgplay
s" ceg" 0 fmchord
s" ceg" 1 psgchord
psginit fminit                     \ silence every voice the chords left sounding

cr .( testaudio ok ) cr

---testaudio---
