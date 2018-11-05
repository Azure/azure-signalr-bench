#!/bin/bash
. ./kubectl_utils.sh

if [ $# -ne 1 ]
then
  echo "Specify <resource_name>"
  exit 1
fi

function delete_all_pods() {
  local resName=$1
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $resName k8s_query
  local config_file=$g_config
  local result=$g_result
  for i in $result
  do
    echo "kubectl delete pods ${i} --kubeconfig=${config_file}"
    kubectl delete pods ${i} --kubeconfig=${config_file}
  done
}

delete_all_pods $1
