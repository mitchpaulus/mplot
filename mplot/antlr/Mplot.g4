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
  | inlineData
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
  | isoDate
  | legendOption
  | noLegend
  | seriesStyle
  | xtics
  | ytics
  | gnuplotLiteral
  | dpi
  ;

file       : ('f' | 'file') STRING+ ;
inlineData : INLINEDATA ;
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
timeFormat : ('tf' | 'timeformat') STRING ;
isoDate : 'isodate' ;
seriesStyle : pointStyle | lineStyle ;
gnuplotLiteral : 'gnuplot' STRING+ ;

noLegend : 'nokey' | 'nolegend' ;

xtics : 'xtics' rational rational rational ;
ytics : 'ytics' rational rational rational ;

pointStyle : 'point' | 'scatter' | 'xy' ;
lineStyle : 'line' | '-' ;

range : lowLimit=rational? (COLON | TO) highLimit=rational? ;
COLON : ':' ;
TO : 'to' ;

marker
  : 'o' # CircleMarker
  | '+' # PlusMarker
  | 'square' # SquareMarker
  ;

stringArray : (STRING | ASTERISK | CYCLE)+ ;
intArray : (INT | ASTERISK | CYCLE)+ ;
rationalArray : (rational | ASTERISK | CYCLE)+ ;
colorArray : (hexColor | rgbColor | knownColor | ASTERISK | CYCLE)+ ;

separator_type
  : commaSeparator # CommaSep
  | TAB_CHAR   # TabSep
  ;

CYCLE : 'cycle' ;
ASTERISK : '*' ;
INLINEDATA : START .*? END ;
fragment START : ('start' | 'Start' | 'START') [ \t]* '\n';
fragment END : ('end' | 'End' | 'END') [ \t]* '\n';

commaSeparator : COMMA_CHAR | COMMA_WORD | CSV ;

COMMA_CHAR : ',' ;
COMMA_WORD : 'comma' ;
CSV : 'CSV' | 'csv' ;
TAB_CHAR   : 'tab' ;

TIME : 'time' ;

legendOption
  : TOP  # TopLegend
  | RIGHT # RightLegend
  | UPPER_RIGHT # UpperRightLegend
  | UPPER_LEFT  # UpperLeftLegend
  ;

TOP : 'top' ;
RIGHT : 'right' ;
UPPER_RIGHT : 'ur' | 'upperright' ;
UPPER_LEFT : 'ul' | 'upperleft' ;

grid : GRID ;

GRID : 'grid' | 'g' ;

hexColor : HEXCOLOR ;
HEXCOLOR : '#' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT ;
RGB : 'rgb' ;
rgbColor : RGB INT INT INT ;

RED : 'red' ;
GREEN : 'green' ;
BLUE : 'blue' ;

CCLLC_BLUE : 'ccblue' ;
CCLLC_GRAY : 'ccgray' ;
CCLLC_MAROON : 'ccmaroon' ;
CCLLC_GREEN  : 'ccgreen' ;
CCLLC_YELLOW : 'ccyellow' ;

knownColor
  : RED          # RedColor
  | GREEN        # GreenColor
  | BLUE         # BlueColor
  | CCLLC_BLUE   # CcllcBlueColor
  | CCLLC_GRAY   # CcllcGray
  | CCLLC_MAROON # CcllcMaroon
  | CCLLC_GREEN  # CcllcGreen
  | CCLLC_YELLOW # CcllcYellow
  ;

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
