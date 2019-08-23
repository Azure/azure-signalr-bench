#!/bin/bash
if [ $# -ne 3 ]
then
  echo "usage: resourceName outputDir duration"
  exit 1
fi

. ./kubectl_utils.sh

start_connection_tracking $1 $2 $3
