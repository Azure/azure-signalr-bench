#!/bin/bash
. ./env.sh

if [ $# != 2 ]
then
	echo "Specify the <html_src_root_dir> <output_file_name>"
	exit 1
fi

function gen_summary_body
{
  local html_root=$1
  local output=$2
  local i
  echo "{{define \"body\"}}" > $output
  for i in `ls -t $html_root`
  do
    is_valid_src=`echo $i|awk '{if ($1 ~ /^[+-]?[0-9]+$/) {print 1;} else {print 0;}}'`
    if [ $is_valid_src == 1 ]
    then
	if [ -e $html_root/$i/all.html ]
	then
		echo "    <div><a href=\"${i}/all.html\">${i} all scenarios</a></div>" >> $output
	else
		for j in `ls $html_root/$i`
		do
			if [ -e $html_root/$i/$j/index.html ]
			then
				echo "    <div><a href=\"${i}/${j}/index.html\">$i</a></div>" >> $output
			fi
		done
	fi
    fi
  done
  echo "{{end}}" >> $output
}

gen_summary_body $1 $2
