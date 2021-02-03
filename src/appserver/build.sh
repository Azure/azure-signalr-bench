#!/bin/bash

clean_restore() {
  dotnet clean
  dotnet add package Microsoft.Azure.SignalR --version 1.6.2-preview1-10691 --source https://www.myget.org/F/azure-signalr-dev/api/v3/index.json
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
