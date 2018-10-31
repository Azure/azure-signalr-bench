#!/bin/bash
httpBase="http://honzhan1eusbenchdns0.eastus.cloudapp.azure.com:8000"
#nginx_server_dns
function category_scenario() {
  local filter=$1
  local i d f counterPath maxSend html
  local timestamp=`date +%Y%m%d%H%M%S`
  #local filter="$unit"_"$transport"_"$protocol"_"$scenario"
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

function analyze_all() {
  local i
  for i in `python find_counters.py|sort -k 2 |awk '{print $2}'|uniq` 
  do
    category_scenario $i
  done
}
#category_scenario unit2_Websockets_json_broadcast
analyze_all
