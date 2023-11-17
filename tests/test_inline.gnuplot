set terminal pngcairo size 2304,1536 font ",10" fontscale 4 pointscale 4
set key 
set xrange [0:8]
set yrange [0:9]
$var1<<EOF
1 2
3 4
EOF
$var2<<EOF
5 6
7 8
EOF
set datafile separator " "
plot \
  $var1 using 1:2 with points pt 6  linecolor "0x000000" title "series1", \
  $var2 using 1:2 with points pt 6  linecolor "0x000000" title "series1"
