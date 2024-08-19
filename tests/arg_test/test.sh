#!/bin/bash
set -e
diff <(mplot -s 'X Label 1' string_args.mplot) string_args.gnuplot
