#!/bin/bash
. ./analyze_statistic.sh

if [ $# -lt 1 ]
then
  echo "$0: folder <longrun>"
  exit 1
fi

if [ $# -eq 2 ]
then
  analyze_1_folder $1 $2
else
  analyze_1_folder $1
fi
