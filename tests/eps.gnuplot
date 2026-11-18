set encoding utf8
set terminal postscript eps enhanced color size 6,4 font ",10" fontscale 2 linewidth 0.75 pointscale 1.5
set key off
$var1<<EOF
1	2
2	4
EOF
set datafile separator tab
plot \
  $var1 using 1:2 axes x1y1 with points pt 6 linecolor "0x000000" notitle, \
  keyentry with points pt 6 ps 1.5 linecolor "0x000000"  title "Series"
