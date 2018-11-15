#!/bin/bash
if [ $# -ne 2 ]
then
  echo "usage: resourceName outputDir"
  exit 1
fi

. ./kubectl_utils.sh

start_connection_tracking $1 $2
