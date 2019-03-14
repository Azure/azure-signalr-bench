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

generate_counterlist() {
  local start_date end_date output
  local filter=$1 rawCounterOutput
  local timestamp=`date +%Y%m%d%H%M%S`
  rawCounterOutput=/tmp/rawCounterList${timestamp}
  if [ $# -eq 3 ]
  then
    start_date=$2
    end_date=$3
    output=/tmp/rawReportInfo${timestamp}
    filter_date_window $start_date $end_date $output
    local output_size=`wc -l $output|awk '{print $1}'`
    if [ $output_size -eq 0 ]
    then
       cat $output
       echo "Error"
       return
    fi
    sort -k 2 $output|grep "$filter" >$rawCounterOutput
    rm $output
  else
    python find_counters.py | sort -k 2 |grep "$filter" > $rawCounterOutput
  fi
  while read -r i
  do
    local d=`echo "$i"|awk '{print $1}'`
    local f=`echo "$i"|awk '{print $2}'`
    counterPath=`echo "$i"|awk '{print $3}'`
    sh normalize.sh $counterPath /tmp/normal.txt
    read maxConnection maxSend < <(python parse_counter.py -i /tmp/normal.txt)
    if [ $maxSend -ne 0 ]
    then
      html=${httpBase}/${d}/${f}/index.html
      echo "$d,$f,$maxConnection,$maxSend,$html"
    fi
  done < $rawCounterOutput
  rm $rawCounterOutput
}

category_scenario() {
  local filter=$1
  local i d f counterPath maxConnection maxSend html
  local timestamp=`date +%Y%m%d%H%M%S`
  python find_counters.py | sort -k 2 |grep "$filter" > /tmp/rawCounterList${timestamp}
  while read -r i
  do
    d=`echo "$i"|awk '{print $1}'`
    f=`echo "$i"|awk '{print $2}'`
    counterPath=`echo "$i"|awk '{print $3}'`
    sh normalize.sh $counterPath /tmp/normal.txt
    read maxConnection maxSend < <(python parse_counter.py -i /tmp/normal.txt)
    if [ $maxSend -ne 0 ]
    then
      html=${httpBase}/${d}/${f}/index.html
      echo "$d,$f,$maxConnection,$maxSend,$html"
    fi
  done < /tmp/rawCounterList${timestamp}
}

function analyze_all() {
  local i
  local scenarios=`python find_counters.py|sort -k 2 |awk '{print $2}'|uniq`
  for i in $scenarios
  do
    generate_counterlist $i
  done
}

analyze_date_in_window() {
  local start_date=$1
  local end_date=$2
  local i
  local scenarios=`python find_counters.py|sort -k 2 |awk '{print $2}'|uniq`
  for i in $scenarios
  do
    generate_counterlist $i $start_date $end_date
  done
}

#analyze_all
if [ $# -lt 1 ]
then
  echo "$0: <start_date> (<end_date>), i.g. 20181010 20181104. If you do not specify end_date, the default value is today."
  exit 1
fi

start_date=$1
end_date=`date +%Y%m%d`

if [ $# -eq 2 ]
then
  end_date=$2
fi

analyze_date_in_window $start_date $end_date
