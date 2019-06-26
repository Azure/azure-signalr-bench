#!/bin/sh

. ./func_env.sh

MONITORDIR=/mnt/Data/NginxRoot
LOG_FOLDER=/var/log/nginxroot/monitor.log
RE='^[0-9]+$'
REPORT_DB_TOOL=`pwd`/tools/ReportToDB
DATA_PATH=${REPORT_DB_TOOL}/table.csv

function action() {
  local newFile=$1
  if [ -e $DATA_PATH ]
  then
    rm $DATA_PATH
  fi
  ./categorize_folder.sh ${newFile} | tee $DATA_PATH
  insert_records_to_perf_table $DATA_PATH
  echo "`date +%Y%m%d%H%M%S` ${newFile} created" >> $LOG_FOLDER
}

function monitor() {
  local newFile dirName
  inotifywait -m -r -e create -e moved_to --format '%w%f' "${MONITORDIR}" | while read newFile
  do
   dirName=`basename $newFile`
   if [ "${MONITORDIR}/${dirName}" == $NEWFILE ] && [[ $dirName =~ $RE ]]
   then
     action $newFile
   fi
  done
}

monitor
