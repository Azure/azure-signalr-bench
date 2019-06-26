#!/bin/bash
. ./analyze_statistic.sh

if [ $# -ne 1 ]
then
  echo "$0: folder"
  exit 1
fi

analyze_1_folder $1
