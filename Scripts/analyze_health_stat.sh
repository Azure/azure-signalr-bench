#!/bin/bash
HEALTH_STAT_LIST="clientConnectionCount serverConnectionCount localRedisPubCount localClientMessageCount localServerMessageCount"
RAW_FILTER_RESULT="/tmp/asrs_health_list.txt"
COUNT_STAT=health_stat.csv
COUNT_JS_POSTFIX=_health_stat
ASRS_HEALTH_STAT_JS_POSTFIX=_health_stat.js
LIST_TMPL=tmpl/health_stat.tmpl

filter_asrs_log_a_single_run() {
  local tgt_dir=$1
  local output_file=$2
  if [ -e $output_file ]
  then
    rm $output_file
  fi
  find $tgt_dir -iname "_connections.txt" |while read line
  do
    local d=`echo "$line"|awk -F / '{print $5}'`
    local unit=`echo "$line"|awk -F / '{print $6}'`
    echo "$d $unit $line" >>$output_file
  done
}

filter_asrs_logs() {
  local startDate endDate
  if [ $# -eq 2 ]
  then
    startDate=$1
    endDate=$2
    python filter_specific_file.py -p "_connections.txt" -s $startDate -e $endDate |sort -k 1 -n -r >$RAW_FILTER_RESULT
  else if [ $# -eq 1 ]
       then
          startDate=$1
          python filter_signalr_log.py -p "_connections.txt" -s $startDate |sort -k 1 -n -r >$RAW_FILTER_RESULT
       else
          python filter_signalr_log.py -p "_connections.txt" |sort -k 1 -n -r >$RAW_FILTER_RESULT
       fi
  fi
}

parse_single_log() {
  local tgz_log_path=$1
  local output_dir=$2
  local scenario=$3
  local log_file=`echo "$tgz_log_path"|awk -F / '{print $NF}'|awk -F . '{print $1}'`
  local log_ext=`echo "$tgz_log_path"|awk -F / '{print $NF}'|awk -F . '{print $2}'`
  local counter_output=${scenario}_${COUNT_STAT}
  local workdir=/tmp/asrs_health_pwd
  mkdir -p $workdir
  cd $workdir
  if [ "$log_ext" == "tgz" ]
  then
    tar zxvf $tgz_log_path
  else
    cp $tgz_log_path .
  fi
  cd -
  local i key value
  local record="$log_file"
  for i in $HEALTH_STAT_LIST
  do
    key=$i
    value=`python parse_health_stat.py -i $workdir/${log_file}.txt -q $i`
    record="$record ${key}:${value}"
  done
  echo "$record" >> $output_dir/$counter_output
  rm $workdir/${log_file}.txt
}

gen_js_files() {
  local outdir=$1
  local datetime unit path
  while read line
  do
    datetime=`echo "$line"|awk '{print $1}'`
    unit=`echo "$line"|awk '{print $2}'`
    path=`echo "$line"|awk '{print $3}'`
    local scenario_outdir=$outdir/$datetime/$unit
    python gen_items_html.py -i $scenario_outdir/*${COUNT_STAT} > $scenario_outdir/${unit}${ASRS_HEALTH_STAT_JS_POSTFIX}
  done < $RAW_FILTER_RESULT
}

gen_list_html() {
  local outdir=$1
  python gen_list_html.py -i $outdir -t $LIST_TMPL
}

parse_all_logs() {
  local outdir=$1
  local datetime unit path
  if [ ! -e $outdir ]
  then
    mkdir $outdir
  fi
  while read line
  do
    datetime=`echo "$line"|awk '{print $1}'`
    unit=`echo "$line"|awk '{print $2}'`
    path=`echo "$line"|awk '{print $3}'`
    local scenario_outdir=$outdir/$datetime/$unit
    mkdir -p $scenario_outdir
    parse_single_log $path $scenario_outdir $unit
  done < $RAW_FILTER_RESULT
}

