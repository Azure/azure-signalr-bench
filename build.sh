#!/bin/bash

function build()
{
  cd src/rpc
  ./generate_protos.sh
  cd ../..
}

function arcade_build()
{
  sh eng/common/cibuild.sh "$@"
}

export _PackTool=true
build
arcade_build "$@"
