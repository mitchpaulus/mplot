This repository contains source code for a CLI utility to generate scientific and engineering figures.
It is a C# project, and uses ANTLR for the grammar, lexing, and parsing.

To build, you must first compile the grammar for ANTLR.
This is done with:
`MplotLib/antlr/compile_grammer`

Do not worry about running unit tests at this point.

The main function that writes the Gnuplot file compilation is `WriteGnuplot` in `MplotListener.cs`.
