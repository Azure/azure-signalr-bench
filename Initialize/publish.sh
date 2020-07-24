#!/bin/bash
set -e
trap "exit" INT

## Used to compile and publish the project
DIR=$(cd `dirname $0` && cd ../src/Pods && pwd)

cd $DIR/Portal
rm -rf  publish
echo "start to publish the Portal"
dotnet publish --self-contained true -r linux-x64 -c release -o publish /p:useapphost=true

