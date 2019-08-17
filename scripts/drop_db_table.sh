#!/bin/bash
. ./func_env.sh

if [ $# -ne 1 ]
then
  echo "Specify: <table_name>"
  echo " table_name: AzureSignalRLongrun or AzureSignalRLongrunProduct or AzureSignalRAzWebAppLongrun"
  exit 1
fi

drop_sql_perf_table $1
