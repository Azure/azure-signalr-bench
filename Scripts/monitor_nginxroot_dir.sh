#!/bin/sh

. ./func_env.sh

MONITORDIR=/mnt/Data/NginxRoot
LOG_FOLDER=/var/log/nginxroot/monitor.log
RE='^[0-9]+$'
REPORT_DB_TOOL=`pwd`/tools/ReportToDB
DATA_PATH=${REPORT_DB_TOOL}/table.csv

function genTable() {
  local newFile=$1
  if [ "$g_env_kind" == "longrun" ]
  then
    echo "`date +%Y%m%d%H%M%S` ./categorize_folder.sh ${newFile} $g_env_kind > $DATA_PATH" >> $LOG_FOLDER
    ./categorize_folder.sh ${newFile} $g_env_kind > $DATA_PATH
  else
    echo "`date +%Y%m%d%H%M%S` ./categorize_folder.sh ${newFile} > $DATA_PATH" >> $LOG_FOLDER
    ./categorize_folder.sh ${newFile} > $DATA_PATH
  fi
}

function action() {
  local newFile=$1
  local try=0
  if [ -e $DATA_PATH ]
  then
    rm $DATA_PATH
  fi
  genTable ${newFile}
  while [ ! -s $DATA_PATH ] && [ $try -lt 3 ]
  do
     echo "`date +%Y%m%d%H%M%S` $DATA_PATH is empty, and wait ... " >> $LOG_FOLDER
     sleep 120
     genTable ${newFile}
     try=$(($try+1))
  done
  genTable ${newFile}
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
  local timeout=300
  echo "launch monitor"
  inotifywait -m --exclude '/\.' -r -e create -e moved_to --format '%w%f' "${MONITORDIR}" | while read newFile
  do
   dirName=`basename $newFile`
   if [ "${MONITORDIR}/${dirName}" == "$newFile" ] && [[ $dirName =~ $RE ]]
   then
     echo "`date +%Y%m%d%H%M%S` Detect creation on $newFile" >> $LOG_FOLDER
     inotifywait -t $timeout --exclude '/\.' -r -e close --format '%w%f' "${MONITORDIR}" # this wait returns once detected a close event
     local ret=$?
     echo "`date +%Y%m%d%H%M%S` close event for $newFile return $ret" >> $LOG_FOLDER
     sleep 120 # wait another 2min to make sure all copies finish
     echo "`date +%Y%m%d%H%M%S` starting to take action"  >> $LOG_FOLDER
     action $newFile
     echo "`date +%Y%m%d%H%M%S` end action"  >> $LOG_FOLDER
   fi
  done
  echo "monitor closed"
}

monitor
