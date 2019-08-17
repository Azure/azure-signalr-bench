#!/bin/bash

RAW_FILETER_RESULT=/tmp/nginx_log_list.txt
COUNT_POSTFIX=_nginx_error_count.csv
DETAILS_POSTFIX=_nginx_error_detail.csv
COUNT_JS_POSTFIX=_error_count
ASRS_WARN_HTML_POSTFIX=_error.html
SUMMARY_TMPL=tmpl/nginx.html
ASRS_WARN_SUMMARY_TABLE=nginx_error.csv

filter_nginx_log_a_single_run() {
  local tgt_dir=$1
  local output_file=$2
  if [ -e $output_file ]
  then
    rm $output_file
  fi
  find $tgt_dir -iname "nginx-*.log.tgz" |while read line
  do
    local d=`echo "$line"|awk -F / '{print $5}'`
    local unit=`echo "$line"|awk -F / '{print $6}'`
    echo "$d $unit $line" >>$output_file
  done
}

filter_nginx_logs() {
  local startDate endDate
  if [ $# -eq 2 ]
  then
    startDate=$1
    endDate=$2
    python filter_specific_file.py -s $startDate -e $endDate -w "nginx*.log.tgz"|sort -k 1 -n -r >$RAW_FILETER_RESULT
  else if [ $# -eq 1 ]
       then
          startDate=$1
          python filter_specific_file.py -s $startDate -w "nginx*.log.tgz"|sort -k 1 -n -r >$RAW_FILETER_RESULT
       else
          python filter_specific_file.py -w "nginx*.log.tgz"|sort -k 1 -n -r >$RAW_FILETER_RESULT
       fi
  fi
}

parse_single_log() {
  local tgz_log_path=$1
  local output_dir=$2
  local log_file=`echo "$tgz_log_path"|awk -F / '{print $NF}'|awk -F . '{print $1}'`
  local counter_output=${log_file}${COUNT_POSTFIX}
  local detail_output=${log_file}${DETAILS_POSTFIX}
  local workdir=/tmp/nginx_pwd
  mkdir -p $workdir
  cd $workdir
  if [[ $tgz_log_path == *.tgz ]]
  then
    tar zxvf $tgz_log_path
  else
    cp $tgz_log_path .
  fi
  cd -
  local errorCount=`grep "error" $workdir/${log_file}.log|wc -l`
  local reloadCount=`grep "reloaded" $workdir/${log_file}.log|wc -l`
  echo $errorCount "|" $reloadCount > $output_dir/$counter_output
  egrep "error|reloaded" $workdir/${log_file}.log > $output_dir/$detail_output
  rm $workdir/${log_file}.log
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
    local log_file=`echo "$path"|awk -F / '{print $NF}'|awk -F . '{print $1}'`
    mkdir -p $scenario_outdir
    parse_single_log $path $scenario_outdir
    local error_count=`cat $scenario_outdir/${log_file}${COUNT_POSTFIX}`
    echo "${datetime}|${unit}|${error_count}|$datetime/$unit/${log_file}${DETAILS_POSTFIX}" >>$outdir/$ASRS_WARN_SUMMARY_TABLE
  done < $RAW_FILETER_RESULT
  python gen_nginx_error_html.py -s '|' -i $outdir/$ASRS_WARN_SUMMARY_TABLE > $outdir/latency_table_1s_category.js 
  cp $SUMMARY_TMPL $outdir/index.html
}

