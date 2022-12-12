```
f "file.tsv"
font "Arial"
w 6
h 3-1/2 {- Rational numbers are used -}
fs 8
dpi 300
out "plot.png"

sep "," {- or -}
tab
,

ct "Chart Title"

xl "X Axis Label"
yl "Y Axis Label"

x 1 {- x column -}
y 2 {- y column -}
{- Can also do it by name -}

g {- grid -}

xr 0:100 {- x range -}
xr :100
xr 0:
yr 0 to 10

{- marker types -}
o + -

s "Series 1" "Series 2"

lw 2 {- line width -}

{- time axis settings -}
time
tf "%Y-%m-%d"
isodate {- for "%Y-%m-%d" }
```
