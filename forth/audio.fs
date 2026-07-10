\ X16 audio: thin wrappers over the ROM audio driver (bank 10) via BCALL,
\ plus direct VERA PCM. Load with:  include audio
\ (PSG note API + FM note API to follow once each ROM routine's register
\  ABI is confirmed; the raw YM path and PCM are done here.)

marker ---audio---

10 constant audiobank

\ --- raw YM2151 register access (AUDIOYM) ---
\ ym_write: .A = value, .X = register   ($c5cd)
\ ym_read : .X = register -> .A = value  ($c697)
: ym! ( value reg -- ) 0 audiobank $c5cd bcall drop drop drop ;
: fmpoke ( value reg -- ) ym! ;
: ym@ ( reg -- value ) 0 swap 0 audiobank $c697 bcall drop drop ;

\ --- VERA PCM FIFO (AUDIOPCM, direct registers) ---
: pcmctrl ( n -- ) $9f3b c! ;            \ AUDIO_CTRL (bit7 write = reset FIFO)
: pcmrate ( n -- ) $9f3c c! ;            \ AUDIO_RATE (0 = stop .. 128 = 48 kHz)
: pcm! ( byte -- ) $9f3d c! ;            \ push one sample byte
: pcmfull? ( -- flag ) $9f3b c@ $80 and 0<> ;
: pcm-write ( addr count -- ) 0 do dup c@ pcm! 1+ loop drop ;

\ setter helper: ROM routine( .A=target, .X=value ), from ( value target ).
: (a2) ( value target routine -- ) >r swap 0 audiobank r> bcall drop drop drop ;

\ --- VERA PSG note API (direct VERA registers, VRAM $1f9c0, 4 bytes/voice) ---
\ The ROM psg_* routines manage their own ROM banking, which conflicts with
\ BCALL, so the PSG is driven directly.  +0/+1 freq, +2 vol|pan, +3 waveform.
: psginit ( -- ) 1 $f9c0 vaddr 64 0 do 0 v! loop ;
: psgfreq ( freq voice -- ) 4 * $f9c0 + 1 swap vaddr  dup $ff and v!  8 rshift v! ;
: psgvol ( vol voice -- )   4 * $f9c2 +  swap $3f and $c0 or  1 -rot vpoke ;
: psgpan ( pan voice -- )   4 * $f9c2 + swap 6 lshift $c0 and
                            over 1 swap vaddr v@ $3f and or  1 -rot vpoke ;
: psgwav ( wave voice -- )  4 * $f9c3 + swap 6 lshift $3f or 1 -rot vpoke ;

\ --- YM2151 FM note API (AUDIOFM: FM* words) ---
: fminit ( -- ) 0 0 0 audiobank $c83f bcall drop drop drop ;
: fmnote ( note channel -- ) $c823 (a2) ;              \ ym_playnote
: fmvol ( vol channel -- ) $c6be (a2) ;                \ ym_setatten
: fmpan ( pan channel -- ) $c928 (a2) ;                \ ym_setpan
: fmdrum ( drum channel -- ) $c833 (a2) ;              \ ym_playdrum
: fminst ( inst channel -- ) $c766 (a2) ;              \ ym_loadpatch 0-162
: fmvib ( speed depth -- )                             \ YM LFO: freq $18, PM depth $19
  $80 or $19 ym!  $18 ym! ;
\ fmfreq: play a raw frequency (Hz, ~17..4434) on a YM channel, like BASIC
\ FMFREQ.  ROM bas_fmfreq ($c000: .A=ch .X=Hzlo .Y=Hzhi) converts Hz -> KC/KF
\ via notecon_freq2fm and triggers the note; 0 Hz releases (silences) the channel.
: fmfreq ( freq channel -- )
  swap dup $ff and swap 8 rshift        \ channel Hzlo Hzhi
  audiobank $c000 bcall drop drop drop ;

\ PSG note -> frequency table (octave 0, semitones C..B), octave shifts left.
create psgtab 22 , 23 , 25 , 26 , 28 , 29 , 31 , 33 , 35 , 37 , 39 , 41 ,
: psgnote ( note voice -- )   \ note = (octave<<4) | 1..12 ; 0 = silence
  over 0= if nip 0 swap psgvol exit then
  swap                              \ voice note
  dup 4 rshift                      \ voice note octave
  swap $0f and 1- cells psgtab + @  \ voice octave freq0
  swap lshift                       \ voice freq
  swap psgfreq ;                    \ freq voice

\ --- minimal play-string: letters A-G (octave 4), each held ~8 jiffies ---
\ semitone for A..G:
create abcmap 10 c, 12 c, 1 c, 3 c, 5 c, 6 c, 8 c,
: letter>note ( ch -- note|0 )
  dup [char] a [char] g 1+ within
  if [char] a - abcmap + c@ $40 or else drop 0 then ;
: fmplay ( c-addr u channel -- )
  -rot over + swap do
    i c@ letter>note ?dup if over fmnote 8 sleep then
  loop drop ;
: psgplay ( c-addr u voice -- )
  dup 63 swap psgvol
  -rot over + swap do
    i c@ letter>note ?dup if over psgnote 8 sleep then
  loop drop ;
\ chords: each note sounds at once on successive channels / voices.
: fmchord ( c-addr u channel -- )
  -rot over + swap do
    i c@ letter>note ?dup if over fmnote 1+ then
  loop drop ;
: psgchord ( c-addr u voice -- )
  -rot over + swap do
    i c@ letter>note ?dup if 63 2 pick psgvol over psgnote 1+ then
  loop drop ;
