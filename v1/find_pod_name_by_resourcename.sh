#!/bin/bash

. ./kubectl_utils.sh

if [ $# -ne 1 ]
then
  echo "Usage: resourceName"
  exit 1
fi

function query() {
  local resName=$1
  local resName=$1
  local output_dir=$2
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $resName k8s_query
  local config_file=$g_config
  local result=$g_result
  echo "$result"
}

set +x

query $1

set -x
