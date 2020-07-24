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

cd $DIR/Portal
rm -rf  publish
echo "start to publish the Portal"
dotnet publish --self-contained true -r linux-x64 -c release -o publish /p:useapphost=true

