#!/bin/bash

build()
{
  cd src/rpc
  ./generate_protos.sh
  cd ../..
}

arcade_build()
{
  set -euo pipefail
  DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Call "sync" between "chmod" and execution to prevent "text file busy" error in Docker (aufs)
  chmod +x "$DIR/eng/build.sh"; sync
  "$DIR/eng/build.sh" "$@"
}

export _PackTool=true
build
arcade_build "$@"
