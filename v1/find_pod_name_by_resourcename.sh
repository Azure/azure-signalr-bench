#!/bin/bash

. ./kubectl_utils.sh

if [ $# -ne 1 ]
then
  echo "Usage: resourceName"
  exit 1
fi

function query() {
  local resName=$1
  local config_file=kvsignalrdevseasia.config
  local result=$(k8s_query $config_file $resName)
  if [ "$result" == "" ]
  then
     config_file=srdevacsrpd.config
     result=$(k8s_query $config_file $resName)
  fi
  echo "$result"
}

set +x

query $1

set -x
