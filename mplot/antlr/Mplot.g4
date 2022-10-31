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
  | xrange
  | yrange
  | knownColor
  | lineWidth
  | font
  | fontSize
  | output
  ;

file       : ('f' | 'file') STRING+ ;
font       : ('font' | 'fo') STRING ;
fontSize   : ('fs' | 'fontSize') INT ;
dpi        : ('dpi' | 'd') INT ;
separator  : ('sep' | 'separator') STRING ;
series     : ('s' | 'series') STRING+ ;
skip       : 'skip' INT ;
title      : ('ct' | 'charttitle') STRING ;
titles     : 't' STRING STRING STRING ;
xlabel     : ('xl' | 'xlabel') STRING ;
xcol       : ('x' | 'xcol') (INT | STRING)  ;
xformat    : 'xf' STRING ;
xrange     : ('xr' | 'xrange') range ;
yrange     : ('yr' | 'yrange') range ;
labels     : ('l' | 'labels') STRING STRING ;
lineWidth  : ('lw' | 'linewidth') INT+ ;
ylabel     : ('yl' | 'ylabel') STRING ;
ycol       : ('y' | 'ycol') (INT | STRING) ;
width      : ('w' | 'width') (rational | INT) ;
height     : ('h' | 'height') (rational | INT) ;
output     : ('out' | 'output') STRING ;
timeAxis   : TIME ;
timeFormat : 'tf' STRING ;

range : lowLimit=rational? (COLON | TO) highLimit=rational? ;
COLON : ':' ;
TO : 'to' ;

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

RED : 'red' ;
GREEN : 'green' ;
BLUE : 'blue' ;

knownColor : RED | GREEN | BLUE ;

fragment HEX_DIGIT : '0'..'9' | 'a'..'f' | 'A'..'F' ;

INT : [0-9]+ ;

rational
  : numerator=INT '/' denominator=INT  # Fraction
  | integer=INT '-' numerator=INT '/' denominator=INT  # MixedFraction
  | INT # Integer
  ;

WS : [ \t\n]+ -> skip ;

COMMENT : '{-' .*? '-}' -> skip ;

STRING : '"' (ESC|.)*? '"' ;
fragment ESC : '\\"' | '\\\\' ;
