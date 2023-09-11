set terminal pngcairo size 2304,1536 font ",10" fontscale 4 pointscale 4
set xlabel "X label"
set ylabel "Y label"
set datafile separator comma
set key 
plot \
  "data.txt" using 1:2 with points  linecolor "0xFF0000" title "Series 1", \
  "data.txt" using 1:3 with lines  linecolor "0x00FF00" title "Series 2", \
  "data.txt" using 1:4 with lines  linecolor "0xFF0000" title "Series 3"
