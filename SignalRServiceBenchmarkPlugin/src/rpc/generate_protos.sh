#!/bin/bash

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

PLATFORM=${OS}_${MACHINE}
PROTOC=$HOME/.nuget/packages/google.protobuf.tools/3.6.1/tools/${PLATFORM}/protoc
PLUGIN=$HOME/.nuget/packages/grpc.tools/1.15.0/tools/${PLATFORM}/grpc_csharp_plugin

mkdir -p build
$PROTOC -Iprotos --csharp_out build  protos/rpc.proto --grpc_out build --plugin=protoc-gen-grpc=$PLUGIN
