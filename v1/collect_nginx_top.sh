#!/bin/bash
if [ $# -ne 3 ]
then
  echo "usage: resourceName namespace outputDir"
  exit 1
fi

res=$1
outdir=$3
ns=$2

. ./kubectl_utils.sh

track_nginx_top $res $ns $outdir
