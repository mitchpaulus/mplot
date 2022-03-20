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
  ;

file      : ('f' | 'file') STRING+ ;
font      : ('font' | 'fo') STRING ;
dpi       : ('dpi' | 'd') INTEGER ;
separator : 'sep' STRING ;
series    : 's' STRING ;
skip      : 'skip' INT ;
title     : 'ct' STRING ;
titles    : 't' STRING STRING STRING ;
xlabel    : 'xl' STRING ;
xcol      : 'x' INT+ ;
labels    : 'l' STRING STRING ;
ylabel    : 'yl' STRING ;
ycol      : 'y' INT+ ;
width     : 'w' INT ;
height    : 'h' INT ;

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

grid : GRID ;

GRID : 'grid' | 'g' ;

hexColor : HEXCOLOR ;
HEXCOLOR : '#' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT ;
RGB : 'rgb' ;
rgbColor : RGB INT INT INT ;

fragment HEX_DIGIT : '0'..'9' | 'a'..'f' | 'A'..'F' ;

INT : [0-9]+ ;

WS : [ \t\n]+ -> skip ;

STRING : '"' (ESC|.)*? '"' ;
fragment ESC : '\\"' | '\\\\' ;
