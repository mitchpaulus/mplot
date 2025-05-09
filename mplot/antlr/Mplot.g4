grammar Mplot;

mplot : plot (PLOTSEP plot)* EOF ;

plot : element* ;

element
  : xlabel
  | ylabel
  | y2label
  | labels
  | separator
  | separator_type
  | series
  | title
  | skip
  | xcol
  | xexp
  | ycol
  | yexp
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
  | xmin
  | xmax
  | yrange
  | y2range
  | ymin
  | ymax
  | knownColorCycleAsterisk
  | lineWidth
  | font
  | fontSize
  | output
  | isoDate
  | legendOption
  | legendCols
  | legendRows
  | legend
  | noLegend
  | seriesStyle
  | xtics
  | xticAdd
  | xticAdd2
  | xticlabels
  | ytics
  | yticAdd
  | yticAdd2
  | gnuplotLiteral
  | dpi
  | commandSource
  | ignore
  | ticsOut
  | binsize
  | colorData
  | lmargin
  | rmargin
  | dash
  | axisSide
  ;

file       : ('f' | 'file') stringArray ;
inlineData : INLINEDATA ;
font       : ('font' | 'fo') STRING ;
fontSize   : ('fs' | 'fontSize') INT ;
dpi        : ('dpi' | 'd') INT ;
separator  : ('sep' | 'separator') stringArray ;
series     : ('s' | 'series') stringArray ;
skip       : 'skip' intArray ;
title      : ('ct' | 'charttitle' | 'title' | 't') argString ;
xlabel     : ('xl' | 'xlabel') argString ;
xcol       : ('x' | 'xcol') (intCount | stringCount | CYCLE | ASTERISK)+ ;
xexp       : 'xexp' ASTERISK? expression ;
yexp       : 'yexp' ASTERISK? expression ;
xformat    : ('xf' | 'xticformat' | 'xformat') STRING ;
xrange     : ('xr' | 'xrange' | 'xlim' | 'xlimit') range ;
yrange     : ('yr' | 'yrange' | 'ylim' | 'ylimit') range ;
y2range     : ('yr2' | 'yrange2' | 'ylim2' | 'ylimit2' | 'y2r' | 'y2range' | 'y2lim' | 'y2limit') range ;
xmin       : ('xmin' | 'xlow') rangeItem ;
xmax       : ('xmax' | 'xhigh') rangeItem ;
ymin       : ('ymin' | 'ylow') rangeItem ;
ymax       : ('ymax' | 'yhigh') rangeItem ;
labels     : ('l' | 'labels') STRING STRING ;
lineWidth  : ('lw' | 'linewidth' | 'lt' | 'linethickness') intArray ;
ylabel     : ('yl' | 'ylabel') argString ;
y2label     : ('yl2' | 'y2l' | 'ylabel2' | 'y2label') argString ;
ycol       : ('y' | 'ycol') (intCount | stringCount | CYCLE | ASTERISK)+ ;
width      : ('w' | 'width') rational ;
height     : ('h' | 'height') rational ;
output     : ('out' | 'output') STRING ;
timeAxis   : TIME_KEYWORD ;
timeFormat : ('tf' | 'timeformat') STRING ;
isoDate : 'isodate' ;
seriesStyle : seriesStyleItem (count | CYCLE | ASTERISK)? ;
gnuplotLiteral : 'gnuplot' STRING+ ;
ignore : 'ignore' (ASTERISK | CYCLE)? expression ;
lmargin : ('lmargin' | 'lm') rational ;
rmargin : ('rmargin' | 'rm') rational ;
axisSide : side=('y1' | 'y2') (count | CYCLE | ASTERISK)? ;

stdinInput : STDIN ASTERISK? ;
STDIN : 'stdin' ;

STRING_ARG : 'sarg' [0-9]+ ;
argString : STRING
       | LPAREN STRING_ARG STRING? RPAREN  // This is for a default to be able to be specified like (sarg1 "mydefault")
       | STRING_ARG
       ;

noLegend : 'nokey' | 'nolegend' ;

// Force a legend
legend : 'key' | 'legend' ;

firstAsterisk : ASTERISK ;

// xtics min step max
xtics : 'xtics' rational (COMMA_CHAR | COLON)? rational (COMMA_CHAR | COLON)? rational ;
ytics : 'ytics' rational (COMMA_CHAR | COLON)? rational (COMMA_CHAR | COLON)? rational ;

xticlabels : 'xticlabels' stringArray ;

ticsOut : TICS_OUT ;

xticAdd : ('xticadd' | 'xtickadd' | 'addxtic' | 'addxtick') (rational STRING? | STRING rational) ;
yticAdd : ('yticadd' | 'ytickadd' | 'addytic' | 'addytick') (rational STRING? | STRING rational) ;

xticAdd2 : ('xticadd2' | 'xtickadd2' | 'addxtic2' | 'addxtick2') (rational STRING? | STRING rational) ;
yticAdd2 : ('yticadd2' | 'ytickadd2' | 'addytic2' | 'addytick2') (rational STRING? | STRING rational) ;

LPAREN : '(' ;
RPAREN : ')' ;

TICS_OUT : 'ticsout' | 'outtics' | 'ticksout' | 'outticks' ;

seriesStyleItem : pointStyle | lineStyle | hist | fstep ;

pointStyle : 'point' | 'scatter' | 'xy' ;
lineStyle : 'line' | 'lines';

AUTO : 'auto' ;
range : lowLimit=rangeItem (COLON | TO)? highLimit=rangeItem ;
rangeItem : rational | dateTime | ASTERISK | AUTO ;

COLON : ':' ;
TO : 'to' ;

count : POUND INT ;

intCount : INT count? ;
stringCount : argString count? ;

marker : MARKER? (ASTERISK | CYCLE)? markerTypeCount (ASTERISK | CYCLE)? ;

markerTypeCount : markerType count? ;

markerType
  : ('o' | 'circle') # CircleMarker
  | 'plus' # PlusMarker
  | 'square' # SquareMarker
  | ('filledcircle' | 'fcircle' | 'fillcircle' | 'filled') # FilledCircleMarker
  | ('filledsquare' | 'fsquare' | 'fillsquare') # FilledSquareMarker
  | ('nomarker' | 'nomark') # NoMarker
  ;

dash : dashType (count | ASTERISK | CYCLE)? ;

dashType
  : ('dashed' | 'dash') # DashedLine
  | ('nodash') # SolidLine
  ;

MARKER : 'marker' | 'm' ;

stringArray : (argString | ASTERISK | CYCLE)+ ;
intArray : (INT | ASTERISK | CYCLE)+ ;
rationalArray : (rational | ASTERISK | CYCLE)+ ;
colorArray : (hexColor | rgbColor | knownColor | ASTERISK | CYCLE)+ ;

legendCols : ('legendcols' | 'keycols' | 'cols') INT ;
legendRows : ('legendrows' | 'keyrows' | 'rows') INT ;

separator_type
  : commaSeparator ASTERISK? # CommaSep
  | TAB_CHAR ASTERISK?  # TabSep
  | WHITESPACESEP ASTERISK? # WhitespaceSep
  ;

CYCLE : 'cycle' ;
ASTERISK : '*' ;
INLINEDATA : START .*? END ;
fragment START : ('start' | 'Start' | 'START' | 'DATA') [ \t*]* '\n';
fragment END : ('end' | 'End' | 'END') [ \t*]* '\n';

CMD : 'cmd' | 'command' ;

commandSource : CMD ASTERISK? STRING+ ;

commaSeparator : COMMA_CHAR | COMMA_WORD | CSV ;

COMMA_CHAR : ',' ;
COMMA_WORD : 'comma' ;
CSV : 'CSV' | 'csv' ;
TAB_CHAR   : 'tab' ;
WHITESPACESEP : 'ws' | 'whitespace' ;

TIME_KEYWORD : 'time' ;

hist : HIST ;
HIST : 'hist' | 'histogram' ;
BINSIZE : 'binsize' | 'bs' | 'binwidth' ;

fstep : 'fstep' ;

binsize : BINSIZE ASTERISK? rational ASTERISK? ;

legendOption
  : TOP  # TopLegend
  | RIGHT # RightLegend
  | UPPER_RIGHT # UpperRightLegend
  | UPPER_LEFT  # UpperLeftLegend
  | BOTTOM_RIGHT # BottomRightLegend
  | BOTTOM_LEFT  # BottomLeftLegend
  | ABOVERIGHT   # AboveRightLegend
  ;

TOP : 'top' | 'above' ;
RIGHT : 'right' ;
UPPER_RIGHT : 'ur' | 'upperright' ;
UPPER_LEFT : 'ul' | 'upperleft' ;
BOTTOM_RIGHT : 'br' | 'bottomright' ;
BOTTOM_LEFT : 'bl' | 'bottomleft' ;

ABOVERIGHT : 'aboveright' ;

grid : GRID ;

GRID : 'grid' | 'g' ;

hexColor : HEXCOLOR (ASTERISK | CYCLE)? ;
HEXCOLOR : '#' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT ;
RGB : 'rgb' | 'RGB' ;
rgbColor : RGB INT INT INT (ASTERISK | CYCLE)? ;

RED : 'red' ;
GREEN : 'green' ;
BLUE : 'blue' ;
BLACK : 'black' ;

CCLLC_BLUE : 'ccblue' | 'ccllcblue' ;
CCLLC_GRAY : 'ccgray' | 'ccllcgray' ;
CCLLC_MAROON : 'ccmaroon' | 'ccllcmaroon';
CCLLC_GREEN  : 'ccgreen' | 'ccllcgreen' ;
CCLLC_YELLOW : 'ccyellow' | 'ccllcyellow' ;

colorData : 'colordata' ;

knownColor
  : RED          # RedColor
  | GREEN        # GreenColor
  | BLUE         # BlueColor
  | BLACK        # BlackColor
  | CCLLC_BLUE   # CcllcBlueColor
  | CCLLC_GRAY   # CcllcGray
  | CCLLC_MAROON # CcllcMaroon
  | CCLLC_GREEN  # CcllcGreen
  | CCLLC_YELLOW # CcllcYellow
  ;

knownColorCycleAsterisk : knownColor (ASTERISK | CYCLE)? ;

fragment HEX_DIGIT : '0'..'9' | 'a'..'f' | 'A'..'F' ;

// Must be above INT for lexing priority
DAY : [0-9][0-9][0-9][0-9]'-'[0-9][0-9]'-'[0-9][0-9] ;
TIME : [0-9][0-9]':'[0-9][0-9] ;

INT : [0-9]+ ;

rational
  : negate=MINUS? numerator=INT '/' denominator=INT  # Fraction
  | negate=MINUS? integer=INT MINUS numerator=INT '/' denominator=INT  # MixedFraction
  | negate=MINUS? INT # Integer
  ;

WS : [ \t\n]+ -> skip ;

COMMENT : '{-' .*? '-}' -> skip ;
LINE_COMMENT : '//'.*? '\n' -> skip ;

STRING : '"' (ESC|.)*? '"' ;
fragment ESC : '\\"' | '\\\\' ;

LESSTHAN : '<' ;
GREATERTHAN : '>' ;
LESSOREQUAL : '<=' ;
GREATEROREQUAL : '>=' ;
EQUAL : '=' ;
NOTEQUAL : '!=' ;
FORWARDSLASH : '/' ;
PLUS : '+' ;
MINUS : '-' ;
POUND : '#' ;

AND : 'and' ;
OR : 'or' ;

PLOTSEP : '---' '-'* ;

EXPRESSIONCOL : '$' INT ;
EXPRESSIONCOLX : '$x' ;
EXPRESSIONCOLY : '$y' ;


dateTime : DAY TIME? ;

expression : expressionItem+ ;

FLOAT : [0-9]+ '.' [0-9]+ ;

EXPRESSIONIDENTIFIER : '\\' [a-zA-Z_]+ ;

expressionItem
    : INT # IntExp
    | FLOAT # FloatExp
    | argString # StringExp
    | EXPRESSIONCOL # ExpressionColExp
    | EXPRESSIONCOLX # ExpressionColXExp
    | EXPRESSIONCOLY # ExpressionColYExp
    | PLUS # PlusExp
    | MINUS # MinusExp
    | ASTERISK # AsteriskExp
    | FORWARDSLASH # ForwardSlashExp
    | LESSTHAN # LessThanExp
    | GREATERTHAN # GreaterThanExp
    | LESSOREQUAL # LessOrEqualExp
    | GREATEROREQUAL # GreaterOrEqualExp
    | EQUAL # EqualExp
    | NOTEQUAL # NotEqualExp
    | AND # AndExp
    | OR # OrExp
    | EXPRESSIONIDENTIFIER # ExpressionIdentifierExp
    | CYCLE # CycleExp
    ;
