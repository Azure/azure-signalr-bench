#!/bin/bash
OS=""
if [ "$(uname)" == "Darwin" ]; then
   OS="macosx"
elif [ "$(uname)" == "Linux" ]
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

PLATFORM=${OS}_${MACHINE}
PROTOC=$HOME/.nuget/packages/google.protobuf.tools/3.6.0/tools/${PLATFORM}/protoc
PLUGIN=$HOME/.nuget/packages/grpc.tools/1.13.1/${PLATFORM}/grpc_csharp_plugin

$PROTOC -I./Bench.Common --csharp_out Bench.Common  ./Bench.Common/Bench.proto --grpc_out Bench.Common --plugin=protoc-gen-grpc=${PLUGIN}
