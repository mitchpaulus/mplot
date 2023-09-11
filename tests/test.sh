#!/bin/bash

exit=0
for f in *.mplot; do
    # Process file $f and then compare output to same file name with .txt extension
    diff_output="$(diff <(mplot "$f") "${f%.mplot}.gnuplot")"
    if test "$?" -eq 0; then
        printf "%s passed\n" "$f"
    else
        printf "%s FAILED\n" "$f"
        printf "==================\n"
        printf "%s\n" "$diff_output"
        exit=1
    fi
done
exit "$exit"
