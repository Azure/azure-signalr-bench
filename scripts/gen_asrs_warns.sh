#!/bin/bash

. ./analyze_asrs_warns.sh

RAW_FILETER_RESULT=/tmp/asrs_tmp_log_list.txt

if [ $# -ne 2 ]
then
  echo "Specify the <NginxRoot> <RootDir>. i.g. /mnt/Data/NginxRoot/ 20181114064732"
  exit 1
fi

NginxRoot=$1
RootDir=$2

if [ -e $RAW_FILETER_RESULT ]
then
   rm $RAW_FILETER_RESULT
fi
filter_asrs_log_a_single_run ${NginxRoot%/}/$RootDir $RAW_FILETER_RESULT

parse_all_logs $NginxRoot/$RootDir/error
