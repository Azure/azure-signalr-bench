#!/bin/bash

RAW_FILETER_RESULT=/tmp/top_list.txt
COUNT_POSTFIX=_top_count.csv
DETAILS_POSTFIX=_top_detail.csv
HTML_JS_POSTFIX=_top.js
ASRS_WARN_HTML_POSTFIX=_top.html
SUMMARY_TMPL=tmpl/appserver.html
ASRS_WARN_SUMMARY_TABLE=appserver_exception.csv

filter_top_a_single_run() {
  local tgt_dir=$1
  local output_file=$2
  local regex_pat=$3
  if [ -e $output_file ]
  then
    rm $output_file
  fi
  find $tgt_dir -iname "$regex_pat" |while read line
  do
    local d=`echo "$line"|awk -F / '{print $5}'`
    local unit=`echo "$line"|awk -F / '{print $6}'`
    echo "$d $unit $line" >>$output_file
  done
}

filter_tops() {
  local startDate endDate
  if [ $# -eq 2 ]
  then
    startDate=$1
    endDate=$2
    python filter_top.py -s $startDate -e $endDate |sort -k 1 -n -r >$RAW_FILETER_RESULT
  else if [ $# -eq 1 ]
       then
          startDate=$1
          python filter_top.py -s $startDate |sort -k 1 -n -r >$RAW_FILETER_RESULT
       else
          python filter_top.py |sort -k 1 -n -r >$RAW_FILETER_RESULT
       fi
  fi
}

get_cpu_usage() {
  local topFile=$1
  local outputFile=$2
  local fname=${topFile##*/}
  echo $fname > $outputFile
  grep dotnet $topFile |awk '{ print $9}' >> $outputFile
}

get_memory_usage() {
  local topFile=$1
  local outputFile=$2
  local fname=${topFile##*/}
  echo $fname > $outputFile
  grep dotnet $topFile |awk '{ print $10}' >> $outputFile
}

pad_zero_lines() {
  local targetFile=$1
  local padZeroLine=$2
  local msg=""
  local i=0
  local column=`awk -F , '{if (NR==1) print NF}' $targetFile`
  while [ $i -lt $column ]
  do
    if [ "$msg" != "" ]
    then
      msg=${msg}",0"
    else
      msg="0"
    fi
    i=$(($i+1))
  done
  i=0
  while [ $i -lt $padZeroLine ]
  do
    echo "$msg" >> $targetFile
    i=$(($i+1))
  done
}

gen_CPU_for_same_unit() {
  local filter_result=$1
  local outdir=$2
  local outputFile=$3
  local datetime unit path
  local line
  local i=0 f
  while read line
  do
    datetime=`echo "$line"|awk '{print $1}'`
    unit=`echo "$line"|awk '{print $2}'`
    path=`echo "$line"|awk '{print $3}'`

    local fname=${path##*/}
    get_cpu_usage $path $outdir/top_$fname
  done < $filter_result

  i=0
  local left
  local res=$outdir/$outputFile
  #ls -alFS $outdir/top_*.txt|awk '{if ($5 != 0) print $9}'
  for f in `ls -alFS $outdir/top_*|awk '{if ($5 != 0) print $9}'`
  do
    if [ ! -s $f ]
    then
      echo "empty: $f"
      continue
    fi
    if [ $i -eq 0 ]
    then
      left=$f
    else
      local leftLine=`wc -l $left|awk '{print $1}'`
      local rightLine=`wc -l $f|awk '{print $1}'`
      #echo "$leftLine $rightLine"
      if [ $leftLine -lt $rightLine ]
      then
        pad_zero_lines $left $(($rightLine-$leftLine))
      else
        pad_zero_lines $f $(($leftLine-$rightLine))
      fi
      local leftCopy=${left}_cp
      cp $left $leftCopy
      paste -d , $leftCopy $f > $res
      left=$res
    fi
    i=$(($i+1))
  done
  rm $outdir/*_cp
}

gen_html() {
  local resultFolder=$1
  local rawResult=$2
  local column=`awk -F , '{if (NR==1) print NF}' $resultFolder/$rawResult`
  local fname=`echo "$rawResult" | cut -d '.' -f1`
  python gen_top_html.py -i $resultFolder/$rawResult -c $column > $resultFolder/${fname}_$HTML_JS_POSTFIX
}

gen_CPU_html_for_same_unit() {
  local filter_result=$1
  local outdir=$2
  local outputFile=$3
  gen_CPU_for_same_unit $filter_result $outdir $outputFile
  gen_html $outdir $outputFile
}

gen_CPU_for_all() {
  local outdir=$1
  local datetime unit path
  if [ ! -e $outdir ]
  then
    mkdir $outdir
  fi
  if [ ! -e $RAW_FILETER_RESULT ]
  then
    return
  fi

  local scenario_outdir
  local previousUnit=""
  local previousOut=""
  local line
  while read line
  do
    datetime=`echo "$line"|awk '{print $1}'`
    unit=`echo "$line"|awk '{print $2}'`
    path=`echo "$line"|awk '{print $3}'`

    scenario_outdir=$outdir/$datetime/$unit
    mkdir -p $scenario_outdir
    if [ "$previousUnit" != "" ] && [ "$previousUnit" != "$unit" ]
    then
      gen_CPU_html_for_same_unit ${previousOut}/${previousUnit} $previousOut ${previousUnit}.csv
    fi
    previousUnit=$unit
    previousOut=$scenario_outdir
    echo "$line" >> $scenario_outdir/${unit}
  done < $RAW_FILETER_RESULT
  gen_CPU_html_for_same_unit ${previousOut}/${previousUnit} $previousOut ${previousUnit}.csv
}

