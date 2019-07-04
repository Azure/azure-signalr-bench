#!/bin/bash
#httpBase="http://hz2benchdns0.westus2.cloudapp.azure.com:8000"
httpBase=$env_g_http_base # global environment
scenarioList="$env_g_scenario_list"

. ./analyze_statistic.sh
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

analyze_longrun_date_in_window $start_date $end_date
