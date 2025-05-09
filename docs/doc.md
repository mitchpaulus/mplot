# `mplot`

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

# Tic Marks

```
xtics min step max
xticadd value "label"
ticsout {- tics out instead of inside by default -}
```

# Ignoring data

Ignoring data is done with a very simple concatenative stack-based language.

The final result on the stack should be a boolean. If the value is true, the row is ignored.

```
ignore expression
```

You can reference columns by index or by 'x' or 'y', prefixed with a `$`, like `$x`, `$y`, or `$2`.

Other expression items:

```
124 # integer
1.24 # float
"string" # string
+ - * / # arithmetic
< > <= >= = != # comparison
and or # logical
```

# Example

```mplot
// Ignore when column "a" is less than 0 and column "b" is greater than 10
ignore "a" 0 < "b" 10 > and
```

# Time Series

Need to set the *time format*, using `tf "string"`.

```
tf "%Y-%m-%d %H:%M:%S"
x "Timestamp
y "Sensor Value"
```

# Gnuplot Direct

If there's something that not directly supported right now, you can inject text directly into the gnuplot script.

```
gnuplot "set xtics rotate by -45"
```

# Legend/Key Location

Keywords for legend position:

```
TOP : 'top' ;
RIGHT : 'right' ;
UPPER_RIGHT : 'ur' | 'upperright' ;
UPPER_LEFT : 'ul' | 'upperleft' ;
BOTTOM_RIGHT : 'br' | 'bottomright' ;
BOTTOM_LEFT : 'bl' | 'bottomleft' ;
```


# Passing data from CLI


```
f sarg1 sarg2
f sarg1 ("Default")
```
