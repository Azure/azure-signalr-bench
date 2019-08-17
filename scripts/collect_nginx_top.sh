#!/bin/bash
if [ $# -ne 4 ]
then
  echo "usage: resourceName namespace outputDir duration"
  exit 1
fi

res=$1
outdir=$3
ns=$2
duration=$4
. ./kubectl_utils.sh

track_nginx_top $res $ns $outdir $duration
