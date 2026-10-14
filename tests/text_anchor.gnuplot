set encoding utf8
set terminal pngcairo size 2304,1536 font ",10" fontscale 4 pointscale 4
set label 1 "plain" at 1,1
set label 2 "a" at 2,2 center offset 0,0.6
set label 3 "b" at 3,3 left offset 0,0.6
set label 4 "c" at 4,4 right offset 0,-0.6
set label 5 "d" at 5,5 center
set label 6 "e" at 6,6 left
set key off
set ylabel "Series 1"
$var1<<EOF
0	0
10	10
EOF
set datafile separator tab
plot $var1 using 1:2 axes x1y1 with points pt 6 linecolor "0x000000"  title "Series 1"
