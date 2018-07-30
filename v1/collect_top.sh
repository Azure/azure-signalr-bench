#!/bin/bash

if [ $# -ne 4 ]
then
  echo "Specify <remote_host> <remote_port> <remote_user> <output_file>"
  exit
fi
remote_host=$1
remote_port=$2
remote_user=$3
output_file=$4

while [ 1 ]
do
  echo =========`date --iso-8601='seconds'`======== >> $output_file
  ssh -o StrictHostKeyChecking=no -p${remote_port} ${remote_user}@${remote_host} "top -b|head -n 10" >> $output_file
  sleep 1
done
