#!/bin/bash

clean_restore() {
  dotnet clean
  dotnet restore --no-cache
}

build() {
  clean_restore
  dotnet build
}

package() {
  local outDir=$1
  clean_restore
  dotnet publish -c Release -f netcoreapp3.0 -o ${outDir} --self-contained -r $PLATFORM
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
