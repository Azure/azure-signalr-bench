#!/bin/bash

build() {
  dotnet build
}

package() {
  local outDir=$1
  dotnet publish -c Release -f netcoreapp2.1 -o ${outDir} --self-contained -r $PLATFORM
}

OS=""
if [ "$(uname)" == "Darwin" ]; then
   OS="macosx"
elif [ "$(uname)" == "Linux" ]; then
   OS="linux"
else
   OS="windows"
fi

MACHINE=""
if [ "$(uname -m)" == "x86_64" ]; then
   MACHINE="x64"
else
   MACHINE="x86"
fi

PLATFORM=${OS}-${MACHINE}

if [ $# -eq 1 ]
then
  package $1
else
  build
fi
