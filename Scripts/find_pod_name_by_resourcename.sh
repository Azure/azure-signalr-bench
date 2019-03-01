#!/bin/bash

. ./kubectl_utils.sh

if [ $# -lt 1 ]
then
  echo "Usage: resourceName"
  exit 1
fi

function query() {
  local resName=$1
  local ns
  if [ $# -eq 2 ]
  then
    ns=$2
  fi
  g_config=""
  g_result=""
  if [ "$ns" == "" ]
  then
    find_target_by_iterate_all_k8slist $resName k8s_query
  else
    find_target_by_iterate_all_k8slist $resName get_nginx_pod_internal $ns
  fi
  local config_file=$g_config
  local result=$g_result
  echo "$result"
}

#set +x

if [ $# -eq 2 ]
then
  res=$1
  ns=$2
else
  res=$1
fi

query $res $ns

#set -x
