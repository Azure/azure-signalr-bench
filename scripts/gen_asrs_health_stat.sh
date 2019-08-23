#!/bin/bash

. ./analyze_health_stat.sh

#RAW_FILTER_RESULT=/tmp/asrs_health_log_list.txt

if [ $# -ne 2 ]
then
  echo "Specify the <NginxRoot> <RootDir>. i.g. /mnt/Data/NginxRoot/ 20181114064732"
  exit 1
fi

NginxRoot=$1
RootDir=$2

if [ -e $RAW_FILTER_RESULT ]
then
   rm $RAW_FILTER_RESULT
fi

health_stat_result=${NginxRoot%/}/$RootDir/health

filter_asrs_log_a_single_run ${NginxRoot%/}/$RootDir $RAW_FILTER_RESULT

parse_all_logs $health_stat_result

gen_js_files $health_stat_result

gen_list_html $health_stat_result > $health_stat_result/index.html
