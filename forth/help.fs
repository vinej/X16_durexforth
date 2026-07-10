\ HELP - browse the help pages (help/helpdoc on the SD card).
\   HELP           the topic index (INDEX.TXT)
\   HELP STRING    one topic page (STRING.TXT), paged with MORE

: help ( "topic" -- )
  parse-name ?dup 0= if drop s" index" then
  dup 20 > if 2drop exit then
  dup >r pad swap cmove              \ "topic.txt" built at pad
  s" .txt" pad r@ + swap cmove
  pad r> 4 +
  here loadb ?dup 0= if ."  no help page" cr exit then
  cr here ?do
    i c@ dup 13 = if drop cr more else emit then
  loop ;
