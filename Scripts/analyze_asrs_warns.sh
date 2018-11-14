#!/bin/bash

RAW_FILETER_RESULT=/tmp/asrs_log_list.txt
COUNT_POSTFIX=_error_count.csv
DETAILS_POSTFIX=_error_detail.csv
COUNT_JS_POSTFIX=_error_count
DETAILS_JS_POSTFIX=_error_detail
ASRS_WARN_HTML_POSTFIX=_error.html
ASRS_TMPL=tmpl/asrswarns.html
SUMMARY_TMPL=tmpl/analysis.html
ASRS_WARN_SUMMARY_TABLE=table.csv
SEPARATOR='|'

filter_asrs_logs() {
  local startDate endDate
  if [ $# -eq 2 ]
  then
    startDate=$1
    endDate=$2
    python filter_signalr_log.py -s $startDate -e $endDate |sort -k 1 -n -r >$RAW_FILETER_RESULT
  else if [ $# -eq 1 ]
       then
          startDate=$1
          python filter_signalr_log.py -s $startDate |sort -k 1 -n -r >$RAW_FILETER_RESULT
       else
          python filter_signalr_log.py |sort -k 1 -n -r >$RAW_FILETER_RESULT
       fi
  fi
}

parse_single_log() {
  local tgz_log_path=$1
  local output_dir=$2
  local log_file=`echo "$tgz_log_path"|awk -F / '{print $NF}'|awk -F . '{print $1}'`
  local log_ext=`echo "$tgz_log_path"|awk -F / '{print $NF}'|awk -F . '{print $2}'`
  local counter_output=${log_file}${COUNT_POSTFIX}
  local detail_output=${log_file}${DETAILS_POSTFIX}
  local workdir=/tmp/asrs_warn_pwd
  mkdir -p $workdir
  cd $workdir
  if [ "$log_ext" == "tgz" ]
  then
    tar zxvf $tgz_log_path
  else
    cp $tgz_log_path .
  fi
  cd -
  python parse_asrs_log.py -i $workdir/${log_file}.txt -q counter |sort -t $SEPARATOR -k 2 -n -r > $output_dir/$counter_output
  python parse_asrs_log.py -i $workdir/${log_file}.txt -q details > $output_dir/$detail_output
  rm $workdir/${log_file}.txt
}

gen_html_files() {
  local tgz_log_path=$1
  local output_dir=$2
  local log_file=`echo "$tgz_log_path"|awk -F / '{print $NF}'|awk -F . '{print $1}'`
  local log_ext=`echo "$tgz_log_path"|awk -F / '{print $NF}'|awk -F . '{print $2}'`
  local counter_output=${log_file}${COUNT_POSTFIX}
  local detail_output=${log_file}${DETAILS_POSTFIX}
  python gen_warn_count_html.py -s $SEPARATOR -i $output_dir/$counter_output -g counter >$output_dir/${log_file}${COUNT_JS_POSTFIX}.js
  python gen_warn_count_html.py -s $SEPARATOR -i $output_dir/$detail_output -g details >$output_dir/${log_file}${DETAILS_JS_POSTFIX}.js
  sed -e "s/WARN_COUNT_TABLE/${log_file}${COUNT_JS_POSTFIX}/g" -e "s/WARN_DETAILS_TABLE/${log_file}${DETAILS_JS_POSTFIX}/g" $ASRS_TMPL > $output_dir/${log_file}${ASRS_WARN_HTML_POSTFIX}
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
    parse_single_log $path $scenario_outdir
    gen_html_files $path $scenario_outdir
    local log_file=`echo "$path"|awk -F / '{print $NF}'|awk -F . '{print $1}'`
    local error_count=`wc -l $scenario_outdir/${log_file}${COUNT_POSTFIX}|awk '{print $1}'`
    local top_error=`head -n 1 $scenario_outdir/${log_file}${COUNT_POSTFIX}|awk -F \| '{print $1}'`
    echo "${datetime}|${unit}|${error_count}|${top_error}|$datetime/$unit/${log_file}${ASRS_WARN_HTML_POSTFIX}" >>$outdir/$ASRS_WARN_SUMMARY_TABLE
  done < $RAW_FILETER_RESULT
  python gen_warn_count_html.py -s '|' -i $outdir/$ASRS_WARN_SUMMARY_TABLE -g "summary"> $outdir/latency_table_1s_category.js 
  cp $SUMMARY_TMPL $outdir/index.html
}

