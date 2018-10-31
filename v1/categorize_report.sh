#!/bin/bash
httpBase="http://hz2benchdns0.westus2.cloudapp.azure.com:8000"
#nginx_server_dns
function category_scenario() {
  local unit=$1
  local transport=$2
  local scenario=$3
  local i d f counterPath maxSend html
  local protocol="json"
  local timestamp=`date +%Y%m%d%H%M%S`
  if [ $# -eq 4 ]
  then
     protocol=$4
  fi
  local filter="$unit"_"$transport"_"$protocol"_"$scenario"
  python find_counters.py | sort -k 2 |grep "$filter" > /tmp/rawCounterList${timestamp}
  while read -r i
  do
    d=`echo "$i"|awk '{print $1}'`
    f=`echo "$i"|awk '{print $2}'`
    counterPath=`echo "$i"|awk '{print $3}'`
    sh normalize.sh $counterPath /tmp/normal.txt
    maxSend=`python parse_counter.py -i /tmp/normal.txt`
    if [ $maxSend -ne 0 ]
    then
      html=${httpBase}/${d}/${f}/index.html
      echo "$d,$f,$maxSend,$html"
    fi
  done < /tmp/rawCounterList${timestamp}
}

category_scenario "unit2" "Websockets" broadcast
