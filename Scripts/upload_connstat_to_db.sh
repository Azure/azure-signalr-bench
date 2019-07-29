#!/bin/bash
. ./func_env.sh

if [ $# -ne 1 ] && [ $# -ne 2 ]
then
  echo "Specify: table.csv <table_name>"
  echo " table_name: AzureSignalRLongrun or AzureSignalRLongrunProduct"
  exit 1
fi

if [ $# -eq 1 ]
then
  insert_longrun_records_to_perf_table $1
else
  if [ $# -eq 2 ]
  then
    insert_longrun_records_to_perf_table $1 $2
  fi
fi
