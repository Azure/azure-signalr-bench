#!/bin/bash
if [ $# -lt 3 ] || [ $# -gt 4 ]
then
  echo "usage: resourceName outputDir duration <namespace>"
  exit 1
fi

. ./kubectl_utils.sh

if [ $# -eq 3 ]
then
  start_top_tracking $1 $2 $3
else
  start_top_tracking $1 $2 $3 $4
fi
