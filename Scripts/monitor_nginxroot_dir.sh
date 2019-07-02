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
  echo "./categorize_folder.sh ${newFile} > $DATA_PATH" >> $LOG_FOLDER
  ./categorize_folder.sh ${newFile} > $DATA_PATH
  if [ "$g_db_table" != "" ]
  then
    insert_records_to_perf_table $DATA_PATH $g_db_table >> $LOG_FOLDER
  else
    insert_records_to_perf_table $DATA_PATH >> $LOG_FOLDER
  fi
  echo "`date +%Y%m%d%H%M%S` ${newFile} created" >> $LOG_FOLDER
}

function monitor() {
  local newFile dirName
  local timeout=1800
  echo "launch monitor"
  inotifywait -m --exclude '/\.' -r -e create -e moved_to --format '%w%f' "${MONITORDIR}" | while read newFile
  do
   dirName=`basename $newFile`
   if [ "${MONITORDIR}/${dirName}" == "$newFile" ] && [[ $dirName =~ $RE ]]
   then
     # wait until all copy operations finishes, stop it if timedout
     inotifywait -s -t $timeout --exclude '/\.' -r -e close --format '%w%f' "${MONITORDIR}"
     if [ $? -eq 0 ]
     then
       action $newFile
     fi
   fi
  done
  echo "monitor closed"
}

monitor
