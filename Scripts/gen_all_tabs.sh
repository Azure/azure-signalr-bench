#!/bin/bash
. ./env.sh

gen_all_tabs_report() {
  local in_dir=$1
  local out_dir=$2
  local postfix=`date +%Y%m%d%H%M%S`
  local tmp_tabs=/tmp/tabs_${postfix}
  local js_refer_tmpl_file=/tmp/tabs_js_refer_${postfix}
  local i j

  echo "{{define \"allunitsjs\"}}" > $js_refer_tmpl_file

  for i in `ls $in_dir`
  do
    if [ -e $in_dir/$i/latency_table_1s_category.js ]
    then
      sed "s/1s_percent_table_div/${i}_1s_percent_table_div/g" $in_dir/$i/latency_table_1s_category.js > $in_dir/${i}_latency_table_1s_category.js
      echo "   <script type='text/javascript' src='${i}_latency_table_1s_category.js'></script>" >> $js_refer_tmpl_file
      echo $i|awk -F _ '{print $1}' >>$tmp_tabs
    fi
  done
  echo "{{end}}" >> $js_refer_tmpl_file

  local tmp_tabs_tmpl=/tmp/tabs_tmpl_${postfix}
  local tmp_tabs_tmpl_single=/tmp/tabs_tmpl_single_${postfix}
  local tabs_list_gen=/tmp/tabs_list_tmpl_${postfix}
  local tabs_tmpl_gen=/tmp/tabs_content_tmpl_${postfix}
  
  echo "{{define \"tablist\"}}" > $tabs_list_gen
  echo "{{define \"tabcontents\"}}" > $tabs_tmpl_gen
  for i in `sort $tmp_tabs|uniq`
  do
    echo "                <li><a href='#${i}'>${i}</a></li>" >>$tabs_list_gen

    echo "{{define \"tabcontentlist\"}}" > $tmp_tabs_tmpl_single
    for j in $in_dir/${i}_*
    do
      if [ -e $j/latency_table_1s_category.js ]
      then
        local item=`echo $j|awk -F / '{print $2}'`
        echo "                                <li><a href='$item/index.html'>$item 1s latency</a><div id='${item}_1s_percent_table_div'></div></li>" >> $tmp_tabs_tmpl_single
      fi
    done
    echo "{{end}}" >> $tmp_tabs_tmpl_single
    export TabID="$i"
    export TabHeadline="$i 1s latency"
    go run gentabcontent.go -content=tmpl/tabitem.tmpl -tabcontentlist=$tmp_tabs_tmpl_single > $tmp_tabs_tmpl
    cat $tmp_tabs_tmpl >> $tabs_tmpl_gen
  done
  echo "{{end}}" >> $tabs_tmpl_gen
  echo "{{end}}" >> $tabs_list_gen

  go run gen5tmpl.go -content=tmpl/alltabs.tmpl -t1=tmpl/header.tmpl -t2=${js_refer_tmpl_file} -t3=$tabs_list_gen -t4=$tabs_tmpl_gen > $out_dir/allunits.html
  rm /tmp/tabs*${postfix}
}

gen_all_tabs_report $result_dir ${html_dir}
