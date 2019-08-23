#!/bin/bash
. ./kubectl_utils.sh

if [ $# -ne 2 ]
then
  echo "Specify <res> <namespace>"
  exit 1
fi

delete_all_nginx_pods $1 $2
