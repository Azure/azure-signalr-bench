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

build_agent() {
  install_required_libs
  generate_proto
  cd ../agent/
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

package_agent() {
  local outDir=$1
  build_agent
  cd ../agent
  dotnet publish -c Release -f netcoreapp2.1 -o ${outDir} --self-contained -r $PLATFORM
  cd -
}

package() {
  local postfix=$1
  local outDir=$2
  if [ "$1" == "master" ] || [ "$1" == "agent" ]
  then
     local func="package_${postfix}"
     mkdir -p $outDir
     eval $func $outDir
  else
     echo "Illegal inputs. Please input <master|agent> <outDir>"
  fi
}
