# mplot Language Reference

This document is intended for LLM/tooling use and mirrors `MplotLib/antlr/Mplot.g4`.

## CLI usage

Current CLI help (from `MplotCli/Program.cs`):

```text
mplot - plotting DSL

USAGE:
mplot [-g] [-s STRING]* FILE
mplot -d FILE
mplot -h | --help
mplot --headers FILE
mplot -V | --version

OPTIONS
 -c        Pipe directly to gnuplot to compile.
 -d        Print dependencies
 -g        Write directly to gnuplot file of same name.
 -s STRING Add a string argument (used as sargXX in code)
```

Additional input behavior implemented in `Main()`:

- If stdin is redirected, input may be read from stdin with no file argument.
- `-` can be used as the file argument to force stdin input.

Example of compiling directly to PNG.

```
mplot -c myfile.mplot # Generates myfile.png
```

## Mplot arrays (core concept)

mplot treats per-series settings as independent arrays. Examples include:

- `series` names
- `xcol` / `ycol`
- colors
- dash types
- markers
- line widths

When generating final series, mplot indexes these arrays by series index.
By default (`Normal` mode), arrays must have matching lengths with other normal-mode arrays.

Two modifiers relax length constraints:

- `cycle`: wraps around (`index % length`)
- `*`: repeats the last element for any out-of-range index

At compile time, normal-mode arrays are checked for equal length.
Arrays in `cycle` or `*` mode are treated as length-compatible with any series count.

Example with a series list, color array, and dash array:

```mplot
series "A" "B" "C" "D"

red
blue cycle

dash dashed
dash solid *
```

How this applies:

- color array is `[red, blue]` in `cycle` mode -> `A:red`, `B:blue`, `C:red`, `D:blue`
- dash array is `[dashed, solid]` in `repeat-last (*)` mode -> `A:dashed`, `B:solid`, `C:solid`, `D:solid`

## Top-level structure

- A file is one or more plots: `mplot : plot (PLOTSEP plot)* EOF`
- Plot separator token: `---` (optionally more dashes)
- A plot is a sequence of `element` commands

Example:

```mplot
ct "Plot A"
...
---
ct "Plot B"
...
```

## Argument and pattern conventions

- `STRING`: double-quoted text, e.g. `"hello"`
- `INT`: integer token, e.g. `12`
- `rational`:
  - integer: `3`
  - fraction: `1/2`
  - mixed fraction: `1-1/2`
  - optional leading negative sign for all forms
- `argString`:
  - normal string: `"abc"`
  - CLI string arg token: `sarg1`, `sarg2`, ...
  - with default: `(sarg1 "default")`
- `count` suffix: `#N` (e.g. `"temp"#2`)
- `*` and `cycle` are supported in many list-like commands
- `range`: `<low> : <high>` or `<low> to <high>`
  - range item can be `rational`, `dateTime`, `*`, or `auto`
- `dateTime`: `YYYY-MM-DD` with optional `HH:MM`

## Data source and series

- `file` / `f`: `file <stringArray>`
- `inlineData`: embedded block token from `start...end` / `DATA...end`
- `cmd` / `command`: `cmd [*] <STRING+>`
- `separator` / `sep`: custom separator string array
- `separator_type`:
  - comma style: `,` / `comma` / `csv`
  - `tab`
  - `ws` / `whitespace`
  - optional trailing `*`
- `series` / `s`: `series <stringArray>`
- `skip`: `skip <intArray>`
- `xcol` / `x`: one or more of int/string refs, with optional `#count`, plus `*`/`cycle`
- `ycol` / `y`: same shape as `xcol`
- `xexp`: `xexp [*] <expression>`
- `yexp`: `yexp [*] <expression>`
- `colordata`: enables coloring by data source

## Labels and titles

- chart title: `ct` / `charttitle` / `title` / `t`
- x label: `xl` / `xlabel` / `xt` / `xtitle`
- x2 label: `xl2` / `x2l` / `xlabel2` / `x2label`
- y label: `yl` / `ylabel` / `yt` / `ytitle`
- y2 label: `yl2` / `y2l` / `ylabel2` / `y2label`
- pair labels: `l` / `labels` `<STRING> <STRING>`

## Figure size/output/font

- `w` / `width` `<rational>`
- `h` / `height` `<rational>`
- `out` / `output` `<argString>` (accepts a literal string, a `sargN` token, or `(sargN "default")`)
- `font` / `fo` `<STRING>`
- `fs` / `fontSize` `<INT>`
- `dpi` / `d` `<INT>`
- `svg` (use the svg gnuplot terminal instead of pngcairo; `-c` writes a `.svg` file)

## Axis/time/range/format

- `time` (time axis keyword)
- `tf` / `timeformat` `<STRING>`
- `isodate`
- `xf` / `xticformat` / `xformat` `<STRING>`
- `yf` / `yticformat` / `yformat` `<STRING>`
- x-range: `xr` / `xrange` / `xlim` / `xlimit` `<range>`
- y-range: `yr` / `yrange` / `ylim` / `ylimit` `<range>`
- x2-range: `xr2` / `xrange2` / `xlim2` / `xlimit2` / `x2r` / `x2range` / `x2lim` / `x2limit` `<range>`
- y2-range: `yr2` / `yrange2` / `ylim2` / `ylimit2` / `y2r` / `y2range` / `y2lim` / `y2limit` `<range>`
- low/high bounds:
  - `xmin` / `xlow`
  - `xmax` / `xhigh`
  - `ymin` / `ylow`
  - `ymax` / `yhigh`
- axis side selection: `y1` or `y2` with optional `count` / `*` / `cycle`
- x step width: `xstep` / `xstepwidth` `<rational>`

## Tics and tic labels

- tics ranges (allow optional `,` or `:` separators):
  - `xtics <min> <step> <max>`
  - `ytics <min> <step> <max>`
  - `x2tics` / `xtics2` `<min> <step> <max>`
  - `y2tics` / `ytics2` `<min> <step> <max>`
- add tic(s):
  - x1: `xticadd` / `xtickadd` / `addxtic` / `addxtick`
  - y1: `yticadd` / `ytickadd` / `addytic` / `addytick`
  - x2: `xticadd2` / `xtickadd2` / `addxtic2` / `addxtick2`
  - y2: `yticadd2` / `ytickadd2` / `addytic2` / `addytick2`
  - forms: `<rational> [STRING]` or `STRING <rational>`
- tic labels:
  - `xticlabels <stringArray>`
  - `yticlabels <stringArray>`
  - `y2ticlabels <stringArray>`
  - `x2ticlabels <stringArray>` (defined in grammar)
- tics outward:
  - `ticsout` / `outtics` / `ticksout` / `outticks`

## Legend/key controls

- force legend: `key` / `legend`
- disable legend: `nokey` / `nolegend`
- location keywords:
  - `top` / `above`
  - `right`
  - `ur` / `upperright`
  - `ul` / `upperleft`
  - `br` / `bottomright`
  - `bl` / `bottomleft`
  - `aboveright`
- columns: `legendcols` / `keycols` / `cols` / `numcols` / `maxcols` `<INT>`
- rows: `legendrows` / `keyrows` / `rows` / `numrows` / `maxrows` `<INT>`

## Style, lines, markers, colors

- series style command: `point|scatter|xy|line|lines|hist|histogram|fstep [count|cycle|*]`
- line width: `lw` / `linewidth` / `lt` / `linethickness` `<intArray>`
- dash:
  - named: `dash` / `dashed` / `nodash` / `solid` with optional `count|*|cycle`
  - custom: `dashtype` / `dt` followed by one or more `argString|*|cycle`
- marker:
  - command token optional: `marker` / `m`
  - supports optional `*`/`cycle` before/after marker type
  - marker types: `o|circle`, `plus`, `square`, `filledcircle|fcircle|fillcircle|filled`, `filledsquare|fsquare|fillsquare`, `nomarker|nomark`
- colors:
  - hex: `#RRGGBB` with optional `*|cycle`
  - rgb: `rgb R G B` with optional `*|cycle`
  - named colors: `red`, `green`, `blue`, `black`, `orange`
  - CCLLC colors: `ccblue|ccllcblue`, `ccgray|ccllcgray`, `ccmaroon|ccllcmaroon`, `ccgreen|ccllcgreen`, `ccyellow|ccllcyellow`
  - named colors also support optional `*|cycle`
- `grid` / `g`
- histogram bin size: `binsize` / `bs` / `binwidth` `[ * ] <rational> [ * ]`

## Margins and annotations

- left margin: `lmargin` / `lm` `<rational>`
- right margin: `rmargin` / `rm` `<rational>`
- text label:
  - `text <argString> <x> <y>`
  - `text <x> <y> <argString>`
- arrow:
  - `arrow <x1> <y1> <x2> <y2>`

## Ignore/filter expressions

- `ignore [*|cycle] <expression>`
- Expression language is RPN/stack style (`expressionItem+`)
- Supported expression items:
  - literals: `INT`, `FLOAT`, `argString`
  - column refs: `$1`, `$2`, ... and `$x`, `$y`
  - operators: `+ - * / < > <= >= = != and or`
  - identifiers: `\\name` (token `EXPRESSIONIDENTIFIER`)
  - `cycle`

Example:

```mplot
ignore "a" 0 < "b" 10 > and
```

## Raw gnuplot passthrough

- `gnuplot <STRING+>` injects literal gnuplot commands

## CLI string argument examples

```mplot
f sarg1 sarg2
f sarg1 (sarg2 "default-file.csv")
ct (sarg3 "Default Title")
out (sarg4 "default-plot.png")
```

## Grammar-defined but not currently in `element`

The grammar includes these rules/tokens even though they are not listed as top-level `element` commands:

- `stdinInput : stdin [*]`
- `firstAsterisk : *`
- `rationalArray`, `colorArray` helper rules

## Element coverage map (exact rule names)

This maps each `element` alternative from the grammar to where it is documented above.

- `xlabel`, `x2label`, `ylabel`, `y2label`, `labels`, `title`: Labels and titles
- `separator`, `separator_type`: Data source and series
- `series`, `skip`, `xcol`, `ycol`, `xexp`, `yexp`, `file`, `inlineData`, `commandSource`, `colorData`: Data source and series
- `width`, `height`, `font`, `fontSize`, `dpi`, `output`: Figure size/output/font
- `timeAxis`, `timeFormat`, `isoDate`, `xformat`, `yformat`, `xrange`, `yrange`, `x2range`, `y2range`, `xmin`, `xmax`, `ymin`, `ymax`, `axisSide`, `xStep`: Axis/time/range/format
- `xtics`, `ytics`, `x2tics`, `y2tics`, `xticAdd`, `yticAdd`, `xticAdd2`, `yticAdd2`, `xticlabels`, `yticlabels`, `y2ticlabels`, `ticsOut`: Tics and tic labels
- `legendOption`, `legendCols`, `legendRows`, `legend`, `noLegend`: Legend/key controls
- `seriesStyle`, `lineWidth`, `dash`, `marker`, `hexColor`, `rgbColor`, `knownColorCycleAsterisk`, `grid`, `binsize`: Style, lines, markers, colors
- `lmargin`, `rmargin`, `textLabel`, `arrow`: Margins and annotations
- `ignore`: Ignore/filter expressions
- `gnuplotLiteral`: Raw gnuplot passthrough
