#!/bin/sh

# On line containing 'SYNTAX' in the README_template.md file, replace with contents from 'syntax.mplot'
# Redirect output to README.md
sed -e '/SYNTAX/ {' -e 'r syntax.mplot' -e 'd' -e '}' README_template.md > README.md
