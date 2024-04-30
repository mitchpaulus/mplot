# Mplot

A better plotting language.

# Commands

```antlr
file       : ('f' | 'file') STRING+ ;
font       : ('font' | 'fo') STRING ;
dpi        : ('dpi' | 'd') INT ;
separator  : 'sep' STRING ;
series     : 's' STRING ;
skip       : 'skip' INT ;
title      : 'ct' STRING ;
titles     : 't' STRING STRING STRING ;
xlabel     : 'xl' STRING ;
xcol       : 'x' (INT | STRING)  ;
xformat    : 'xf' STRING ;
labels     : 'l' STRING STRING ;
ylabel     : 'yl' STRING ;
ycol       : 'y' (INT | STRING) ;
width      : 'w' (rational | INT) ;
height     : 'h' (rational | INT) ;
timeAxis   : TIME ;
timeFormat : 'tf' STRING ;
```

# Series Styling

Basically a choice between scatter or lines.

For points, `point`, `scatter`, or `xy`.
For lines, `line`.
