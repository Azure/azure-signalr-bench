#!/bin/bash
set -e
trap "exit" INT

DIR=$(cd `dirname $0` && pwd)
source $DIR/common.sh

PREFIX=""

function print_usage {
    cat <<EOF
Command 
    $(basename $0)
Arguments
   --prefix|-p                          [Requied] Used to distingush your perf resources with others'
   --subscription|-s                    [Optional] The subscriton used to create resources
   --cloud|-c                           [Optional] The cloud used to create resources
   --help|-h                            Print help
EOF
}

while [[ "$#" > 0 ]];do
    key="$1"
    shift
    case $key in 
        --prefix|-p)
            PREFIX="$1"
            shift
        ;;
        --cloud|-c)
            CLOUD="$1"
            shift
        ;;
        --subscription|-s)
            SUBSCTIPTION="$1"
            shift
        ;;
        --help|h)
            print_usage
            exit
        ;;
        *)
            echo "ERROR: Unknow argument '$key'" 1>&2
            print_usage
            exit -1
    esac
done
throw_if_empty "prefix" $PREFIX

init_common

echo "create dir:mainifest"
az storage directory create -n "mainifest" --account-name $STORAGE_ACCOUNT -s $SA_SHARE

DIR=$DIR/../src/Pods
cd $DIR/Portal
rm -rf  publish
echo "start to publish the Portal"
dotnet publish --self-contained true -r linux-x64 -c release -o publish /p:useapphost=true /p:PublishSingleFile=true
zip -r portal.zip publish
echo "create dir:portal"
az storage directory create -n "mainifest/portal" --account-name $STORAGE_ACCOUNT -s $SA_SHARE
az storage file upload --account-name $STORAGE_ACCOUNT -s $SA_SHARE --source portal.zip -p mainifest/portal
echo "upload portal succeeded"


