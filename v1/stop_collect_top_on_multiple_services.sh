#!/bin/bash
. ./func_env.sh
. ./multiple_vm_env.sh

if [ $# -ne 1 ]
then
  echo "Specify the <top_folder>"
  exit 1
fi

top_folder=$1
stop_collect_top_on_single_vm "$top_folder"
