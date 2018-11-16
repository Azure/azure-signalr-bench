#!/bin/bash

install_required_libs() {
  dotnet restore
}

generate_proto() {
  # depends on install_required_libs
  ./generate_protos.sh
}

build_rpc() {
  install_required_libs
  generate_proto
  dotnet build
}

build_master() {
  install_required_libs
  generate_proto
  cd ../master/
  dotnet build
  cd -
}

build_slave() {
  install_required_libs
  generate_proto
  cd ../slave/
  dotnet build
  cd -
}

build_app_server() {
  install_required_libs
  cd ../../utils/AppServer/
  dotnet build
  cd -
}


package_master() {
  local outDir=$1
  build_master
  cd ../master
  dotnet publish -c Release -f netcoreapp2.1 -o ${outDir} --self-contained -r $PLATFORM
  cd -
}

package_slave() {
  local outDir=$1
  build_slave
  cd ../slave
  dotnet publish -c Release -f netcoreapp2.1 -o ${outDir} --self-contained -r $PLATFORM
  cd -
}

package() {
  local postfix=$1
  local outDir=$2
  if [ "$1" == "master" ] || [ "$1" == "slave" ]
  then
     local func="package_${postfix}"
     mkdir -p $outDir
     eval $func $outDir
  else
     echo "Illegal inputs. Please input <master|slave> <outDir>"
  fi
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

if [ $# -eq 2 ]
then
  package $1 $2
else
  build_master
  build_slave
fi
