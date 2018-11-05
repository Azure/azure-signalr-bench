#!/bin/bash

service_url="http://signalr1.eastus.cloudapp.azure.com:5353/mdm/query"

if [ $# -ne 4 ]
then
  echo "Usage: <CPU|Memory> <PodName> <StartDateUtc> <EndDateUtc>"
  echo "eg: CPU signalr-dc3f3d5f-6e33-424c-9773-0be8ce2a4f90-55cfbb9784-rhmc2 2018-05-24T06:10:00 2018-05-24T06:25:00"
  exit 1
fi

sysType=$1
podName=$2
startDateUtc=$3
endDateUtc=$4

function dogfood_memory() {
curl -X GET --data-urlencode "platform=Dogfood" \
            --data-urlencode "systemLoad=Memory" \
            --data-urlencode "podName=${podName}" \
            --data-urlencode "dateStart=${startDateUtc}" \
            --data-urlencode "dateEnd=${endDateUtc}" \
            $service_url
}

function dogfood_cpu() {
curl -X GET --data-urlencode "platform=Dogfood" \
            --data-urlencode "systemLoad=CPU" \
            --data-urlencode "podName=${podName}" \
            --data-urlencode "dateStart=${startDateUtc}" \
            --data-urlencode "dateEnd=${endDateUtc}" \
            $service_url
}

function dogfood_curl() {
 local startDateUtc=`echo $startDateUtc| python -c 'import sys,urllib;print urllib.quote(sys.stdin.read().strip())'`
 local endDateUtc=`echo $endDateUtc| python -c 'import sys,urllib;print urllib.quote(sys.stdin.read().strip())'`
 curl "http://signalr1.eastus.cloudapp.azure.com:5353/mdm/query?platform=Dogfood&systemLoad=CPU&podName=${podName}&dateStart=${startDateUtc}&dateEnd=${endDateUtc}"
}

if [ "$sysType" == "CPU" ]
then
  dogfood_cpu
else
  dogfood_memory
fi
