set terminal pngcairo size 2304,1536 font ",10" fontscale 4 pointscale 4
set output "expressions.png"
set key off
$var1<<EOF
1 2 3
4 5 6
7 8 9
EOF
set datafile separator whitespace
plot $var1 using 1:($2 + $3) with points pt 6  linecolor "0x000000" title "My Series"
