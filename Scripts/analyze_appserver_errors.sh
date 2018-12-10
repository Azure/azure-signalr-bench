#!/bin/bash

RAW_FILETER_RESULT=/tmp/appserver_log_list.txt
COUNT_POSTFIX=_error_count.csv
DETAILS_POSTFIX=_error_detail.csv
COUNT_JS_POSTFIX=_error_count
ASRS_WARN_HTML_POSTFIX=_error.html
SUMMARY_TMPL=tmpl/appserver.html
ASRS_WARN_SUMMARY_TABLE=appserver_exception.csv

filter_app_log_a_single_run() {
  local tgt_dir=$1
  local output_file=$2
  if [ -e $output_file ]
  then
    rm $output_file
  fi
  find $tgt_dir -iname "log_appserver*" |while read line
  do
    local d=`echo "$line"|awk -F / '{print $5}'`
    local unit=`echo "$line"|awk -F / '{print $6}'`
    echo "$d $unit $line" >>$output_file
  done
}

filter_app_logs() {
  local startDate endDate
  if [ $# -eq 2 ]
  then
    startDate=$1
    endDate=$2
    python filter_applog_file.py -s $startDate -e $endDate |sort -k 1 -n -r >$RAW_FILETER_RESULT
  else if [ $# -eq 1 ]
       then
          startDate=$1
          python filter_applog_file.py -s $startDate |sort -k 1 -n -r >$RAW_FILETER_RESULT
       else
          python filter_applog_file.py |sort -k 1 -n -r >$RAW_FILETER_RESULT
       fi
  fi
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
    cp $path $scenario_outdir/
    local log_file=`echo "$path"|awk -F / '{print $NF}'`
    ## exception count
    local exception_count=`grep Exception ${path}|wc -l`
    echo "${datetime},${unit},${exception_count},$datetime/$unit/${log_file}" >>$outdir/$ASRS_WARN_SUMMARY_TABLE
  done < $RAW_FILETER_RESULT
  python gen_appserver_exception_html.py -s ',' -i $outdir/$ASRS_WARN_SUMMARY_TABLE > $outdir/latency_table_1s_category.js 
  cp $SUMMARY_TMPL $outdir/index.html
}

