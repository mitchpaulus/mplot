grammar Mplot;

mplot : element* EOF ;

element
  : xlabel
  | ylabel
  | separator
  | series
  | title
  | skip
  | xcol
  | ycol
  | width
  | height
  | file
  | marker
  | COLOR
  ;

file      : ('f' | 'file') STRING ;
separator : 'sep' STRING ;
series    : 's' STRING ;
skip      : 'skip' INT ;
title     : 't' STRING ;
xlabel    : 'xl' STRING ;
xcol      : 'x' INT ;
ylabel    : 'yl' STRING ;
ycol      : 'y' INT ;
width     : 'w' INT ;
height    : 'h' INT ;

marker : 'o' | '+' | '-' ;

separator_type : COMMA_CHAR | TAB_CHAR ;

COMMA_CHAR : 'c' ;
TAB_CHAR   : 'tab' ;

COLOR : '#' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT | 'rgb' INT INT INT ;

fragment HEX_DIGIT : '0'..'9' | 'a'..'f' | 'A'..'F' ;

INT : [0-9]+ ;

WS : [ \t\n]+ -> skip ;

STRING : '"' (ESC|.)*? '"' ;
fragment ESC : '\\"' | '\\\\' ;
