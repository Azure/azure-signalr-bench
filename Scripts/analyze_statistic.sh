#!/bin/bash
#httpBase="http://hz2benchdns0.westus2.cloudapp.azure.com:8000"
httpBase=$env_g_http_base # global environment
scenarioList="$env_g_scenario_list"

#nginx_server_dns
filter_date_window() {
  # append 00 for start hour:min:sec
  local start_date=${1}000000
  # append max value for end hour:min:sec
  local end_date=${2}235959
  local output_file=$3
  local timestamp
  local isExpect
  local i
  local curScenario
  isExpect=`echo ${1} ${2}|awk '{printf("%d\n", $1 <= $2 ? 1 : 0)}'`
  if [ $isExpect -eq 0 ]
  then
     echo "Invalid input: Start date ($1) should be less or equal than end date ($2)"
     return
  fi
  python find_counters.py |sort -k 1|while read line; do
    timestamp=`echo "$line"|awk '{print $1}'`
    curScenario=`echo "$line"|awk '{print $2}'`
    isExpect=`echo $start_date $end_date $timestamp|awk '{printf("%d\n", ($3 >= $1 && $3 <= $2) ? 1 : 0)}'`
    if [ $isExpect -eq 1 ]
    then
       if [ "$scenarioList" != "" ]
       then
          for i in $scenarioList
          do
             if [ "$curScenario" == "$i" ]
             then
                echo "$line" >> $output_file
             fi
          done
       else
          echo "$line" >> $output_file
       fi
    fi
  done
}

generate_stat() {
  local rawCounterOutput="$1"
  local i maxConnection maxSend sendTPuts recvTPuts drop reconnCost lifeSpan offline
  local longrun=0 html normFile=/tmp/normal.txt
  if [ $# -eq 2 ] && [ "$2" == "longrun" ]
  then
    longrun=1
  fi
  while read -r i
  do
    local d=`echo "$i"|awk '{print $1}'`
    local f=`echo "$i"|awk '{print $2}'`
    counterPath=`echo "$i"|awk '{print $3}'`
    sh normalize.sh $counterPath $normFile
    if [ $longrun -eq 0 ]
    then
      read maxConnection maxSend sendTPuts recvTPuts< <(python parse_counter.py -i $normFile)
      if [ $maxSend -ne 0 ]
      then
        html=${httpBase}/${d}/${f}/index.html
        echo "$d,$f,$maxConnection,$maxSend,$sendTPuts,$recvTPuts,$html"
      fi
    else
      read maxConnection maxSend sendTPuts recvTPuts drop reconnCost lifeSpan offline< <(python parse_counter.py -i $normFile -q longrun)
      if [ $maxSend -ne 0 ]
      then
        html=${httpBase}/${d}/${f}/index.html
        echo "$d,$f,$maxConnection,$maxSend,$sendTPuts,$recvTPuts,$html,$drop,$reconnCost,$lifeSpan,$offline"
      fi
    fi
  done < $rawCounterOutput
}

generate_1_counterlist() {
  local folder="$1"
  local timestamp=`date +%Y%m%d%H%M%S`
  local rawCounterOutput=/tmp/rawCounterList${timestamp}
  local result
  python find_counters.py -q "$folder"| sort -k 2 > $rawCounterOutput
  if [ $# -eq 2 ] && [ "$2" == "longrun" ]
  then
    result=$(generate_stat $rawCounterOutput "$2")
  else
    result=$(generate_stat $rawCounterOutput)
  fi
  echo "$result"
  rm $rawCounterOutput
}

generate_counterlist_time_window() {
  local start_date end_date output
  start_date=$1
  end_date=$2
  local timestamp=`date +%Y%m%d%H%M%S`
  local rawCounterOutput=/tmp/rawCounterList${timestamp}
  filter_date_window $start_date $end_date $rawCounterOutput
  local output_size=`wc -l $rawCounterOutput|awk '{print $1}'`
  if [ $output_size -eq 0 ]
  then
     cat $rawCounterOutput
     echo "Error"
     return
  fi
  local result
  if [ $# -eq 3 ] && [ "$3" == "longrun" ]
  then
     result=$(generate_stat $rawCounterOutput "$3")
  else
     result=$(generate_stat $rawCounterOutput)
  fi
  echo "$result"
  rm $rawCounterOutput
}

function analyze_date_in_window() {
  local start_date=$1
  local end_date=$2
  local result=$(generate_counterlist_time_window $start_date $end_date)
  echo "$result"
}

function analyze_1_folder() {
  local folder="$1" result
  if [ $# -eq 2 ] && [ "$2" == "longrun" ]
  then
    result=$(generate_1_counterlist $folder "$2")
  else
    result=$(generate_1_counterlist $folder)
  fi
  echo "$result"
}

function analyze_longrun_date_in_window() {
  local start_date=$1
  local end_date=$2
  local result=$(generate_counterlist_time_window $start_date $end_date "longrun")
  echo "$result"
}
