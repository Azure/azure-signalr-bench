#!/bin/bash
set -e

DIR=$(cd $(dirname $0) && pwd)
COMPILE_DIR="/tmp"
GIT_REPO="https://github.com/Azure/azure-signalr-bench.git"
PROJECT_NAME="azure-signalr-bench"

function publish() {
  Pod=$1
  cd $COMPILE_DIR/$PROJECT_NAME/src/Pods/$Pod
  sudo rm -rf publish || true
  echo "start to publish $Pod"
  dotnet publish -r linux-x64 -c release -o publish /p:useapphost=true
  dotnet restore
  cd publish && zip -r ${Pod}.zip *
  echo "create dir:$Pod"
}

# cd compile dir
cd $COMPILE_DIR
LATEST_COMMIT_HASH=$(git ls-remote $GIT_REPO v2 | awk '{print $1 }')
echo $LATEST_COMMIT_HASH
CURRENT_COMMIT_HASH=$(cat variable_current_hash )  || true
echo $CURRENT_COMMIT_HASH
if [[ $LATEST_COMMIT_HASH == $CURRENT_COMMIT_HASH ]];then
  echo "Already latest commit"
else
  echo "Remove project"
#  sudo rm -rf $PROJECT_NAME || true
  echo "cloning project"
 # git clone $GIT_REPO
  cd $COMPILE_DIR/$PROJECT_NAME
  git checkout v2
  publish Client
  
fi



