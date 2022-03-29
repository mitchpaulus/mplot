grammar Mplot;

mplot : element* EOF ;

element
  : xlabel
  | ylabel
  | labels
  | separator
  | separator_type
  | series
  | title
  | titles
  | skip
  | xcol
  | ycol
  | width
  | height
  | file
  | marker
  | hexColor
  | rgbColor
  | grid
  | timeAxis
  | timeFormat
  | xformat
  ;

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

marker
  : 'o' # CircleMarker
  | '+' # PlusMarker
  | '-' # LinesMarker
  ;

separator_type
  : COMMA_CHAR # CommaSep
  | TAB_CHAR   # TabSep
  ;

COMMA_CHAR : ',' ;
TAB_CHAR   : 'tab' ;

TIME : 'time' ;

grid : GRID ;

GRID : 'grid' | 'g' ;

hexColor : HEXCOLOR ;
HEXCOLOR : '#' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT ;
RGB : 'rgb' ;
rgbColor : RGB INT INT INT ;

fragment HEX_DIGIT : '0'..'9' | 'a'..'f' | 'A'..'F' ;

INT : [0-9]+ ;

rational
  : numerator=INT '/' denominator=INT  # Fraction
  | integer=INT '-' numerator=INT '/' denominator=INT  # MixedFraction
  ;

WS : [ \t\n]+ -> skip ;

STRING : '"' (ESC|.)*? '"' ;
fragment ESC : '\\"' | '\\\\' ;
