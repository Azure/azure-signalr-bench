#!/bin/bash
if [ $# -lt 2 ] || [ $# -gt 3 ]
then
  echo "usage: resourceName outputDir <namespace>"
  exit 1
fi

. ./kubectl_utils.sh

if [ $# -eq 2 ]
then
  start_top_tracking $1 $2
else
  start_top_tracking $1 $2 $3
fi
