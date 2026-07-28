set encoding utf8
set terminal svg size 2304,1536 font ",10" fontscale 4
set key off
$var1<<EOF
1	2
2	4
EOF
set datafile separator tab
plot $var1 using 1:2 axes x1y1 with points pt 6 linecolor "0x000000"  title "Series"
