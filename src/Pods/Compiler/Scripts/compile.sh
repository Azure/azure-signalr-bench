#!/bin/bash
set -e
## only one bash should be run at the same time
DIR=$(cd $(dirname $0) && pwd)
COMPILE_DIR="/tmp"
GIT_REPO="https://github.com/Azure/azure-signalr-bench.git"
PROJECT_NAME="azure-signalr-bench"

while [[ "$#" > 0 ]]; do
  key="$1"
  shift
  case $key in
  --version | -v)
    VERSION="$1"
    shift
    ;;
  --pod | -p)
    POD="$1"
    shift
    ;;
  *)
    echo "ERROR: Unknow argument '$key'" 1>&2
    print_usage
    exit -1
    ;;
  esac
done

function throw_if_empty() {
    local name="$1"
    local value="$2"
    if [[ -z "$value" ]]; then
        echo "ERROR: Parameter $name cannot be empty." 1>&2
        exit -1
    fi
}

function replace() {
  local placeholder=$1
  local content=$2
  content=$(echo "${content////\\/}")
  sed -e "s/$placeholder/$content/g"
}

function publish() {
  cd $COMPILE_DIR/$PROJECT_NAME/src/Pods/$POD
  sudo rm -rf publish_${VERSION} || true
  echo "start to publish $POD"
  dotnet publish  ${POD}_${VERSION}.csproj -r linux-x64 -c release -o publish /p:useapphost=true
  cd publish_${VERSION} && zip -r ${Pod}_${VERSION}.zip *
  echo "create dir:$POD"
}

throw_if_empty "Pod" $POD
throw_if_empty "Version" $Version
LATEST_COMMIT_HASH=$(git ls-remote $GIT_REPO v2 | awk '{print $1 }')
echo $LATEST_COMMIT_HASH
CURRENT_COMMIT_HASH=$(cat $COMPILE_DIR/variable_current_hash) || true
echo $CURRENT_COMMIT_HASH

if [[ $LATEST_COMMIT_HASH == $CURRENT_COMMIT_HASH ]]; then
  echo "Already latest commit"
else
  sudo rm -rf $COMPILE_DIR/$PROJECT_NAME
  git clone $GIT_REPO
  git checkout v2
  echo $LATEST_COMMIT_HASH > $COMPILE_DIR/variable_current_hash
fi
  

cd $COMPILE_DIR/$PROJECT_NAME/src/Pods/$POD

if [[ -f $COMPILE_DIR/$PROJECT_NAME/src/Pods/$POD/public_${VERSION}/${POD}_${VERSION}.zip ]];then
  echo "zip file already exist"
  exit 0
fi 


case $POD in
"Client")
  echo "build Client"
  KEY=$(cat $POD.csproj | grep "Microsoft.AspNet.SignalR.Client" | awk '{print $2 " " $3}')
  echo $KEY
  cat $POD.csproj | replace "$KEY" "Include=\"Microsoft.AspNet.SignalR.Client\" Version=\"${VERSION}\"" | replace "$KEY" "Include=\"Microsoft.AspNetCore.SignalR.Client\" Version=\"${VERSION}\"" > ${POD}_${VERSION}.csproj
  publish
  ;;
esac
