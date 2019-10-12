#!/bin/bash

build()
{
  cd src/rpc
  ./generate_protos.sh
  cd ../..
}

arcade_build()
{
  eng/common/cibuild.sh "$@"
}

export _PackTool=true
build
arcade_build "$@"
