#!/bin/bash

. ./util.sh

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

if [ $# -eq 2 ]
then
  package $1 $2
else
  build_master
  build_slave
fi
