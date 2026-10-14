`mplot` is my personal domain specific language for figure code generation.
Very much a work in progress.

```
f "file.tsv"
font "Arial"
w 6
h 3-1/2 {- Rational numbers are used -}
fs 8 {- fontsize -}
dpi 300
out "plot.png"

{- data formats -}
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

START
1 2
3 4
END

xr 0:100 {- x range -}
xr :100
xr 0:
yr 0 to 10

{- marker types -}
o + -

s "Series 1" "Series 2"

lw 2 {- line width -}
dt ".-" "--" {- dash types -}
text "My Label" 10 20 {- place a text annotation (aliases: note annotation) -}
text 10 20 "My Label" {- string may also come last -}
text "My Label" 10 20 sw {- optional anchor: c n s e w ne nw se sw l r, in any position -}
text 2024-01-01T12:00 20 "My Label" {- date/time coordinates for time axes -}
arrow 10 20 30 35 {- draw an arrow annotation -}
xhighlight 10 20 {- shade a background band between x=10 and x=20, light gray by default -}
xhighlight 2024-01-01 2024-01-02T12:00 #ffe0e0 {- datetime bounds, optional color (aliases: highlightx xrect rectx) -}
yhighlight 0 5 rgb 200 230 255 {- horizontal band (aliases: highlighty yrect recty) -}

{- time axis settings -}
time
tf "%Y-%m-%d"
tf "yyyy-mm-ddThh:nn:ss" {- y m d h(24) c(12) n(minute) s ampm, case-insensitive -}
xf "mmm d c ampm" {- Jan 5 9 pm, doubled letters zero pad -}
isodate {- for "%Y-%m-%d" -}

{- legend locations -}
top right ul upperleft ur upperright

{- colors -}
#000000 {- hex -}
rgb 123 124 125 {- rgb -}
red green blue black orange ccblue ccgray ccmaroon ccgreen ccyellow {- named -}
```

`dashtype` (or the shorthand `dt`) accepts one or more strings containing only `.`, `-`, `_`, or space to describe the dash sequence used for subsequent series. Repeat (`*`) and cycle keywords apply the same way they do for other arrays.

Use the `text` command to drop arbitrary labels: `text "Peak" 42 7.5` becomes `set label n at 42,7.5` in gnuplot (one-based `n` increments per label). Prefer the string first or last—`text 42 7.5 "Peak"` is also valid. `note` and `annotation` are aliases for `text`. An optional compass anchor (`c n s e w ne nw se sw`, plus `l`/`r` for `w`/`e`) may appear anywhere in the command, e.g. `text "Peak" 42 7.5 sw`, and names the point of the text placed at the coordinate. The `arrow` command emits `set arrow n from x1,y1 to x2,y2 head filled lw 2`, for example `arrow 10 20 30 40`.
