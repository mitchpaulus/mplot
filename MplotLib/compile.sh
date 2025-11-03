#!/bin/sh
set -e
cd antlr
./compile_grammer
cd ..
dotnet build
