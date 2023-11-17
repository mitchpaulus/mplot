set terminal pngcairo size 2304,1536 font ",10" fontscale 4 pointscale 4
set xlabel "X label"
set ylabel "Y label"
set key 
set datafile separator comma
plot \
  "test1.txt" using 1:2 with points pt 6  linecolor "0xFF0000" title "Series 1", \
  "test1.txt" using 1:3 with lines  linecolor "0x00FF00" title "Series 2", \
  "test1.txt" using 1:4 with lines  linecolor "0xFF0000" title "Series 3"
