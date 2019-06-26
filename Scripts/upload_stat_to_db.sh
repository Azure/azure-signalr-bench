#!/bin/bash
. ./func_env.sh

if [ $# -ne 1 ]
then
  echo "Specify table.csv"
  exit 1
fi

insert_records_to_perf_table $1
