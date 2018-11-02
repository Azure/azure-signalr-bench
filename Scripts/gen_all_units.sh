#!/bin/bash
. ./env.sh

function gen_all_units_report() {
  local js_refer_tmpl_file="/tmp/js_refer_tmpl.txt"
  local charts_1s_latency_div="/tmp/charts_1s_latency_div"
  local i

  echo "{{define \"allunitsjs\"}}" > $js_refer_tmpl_file
  echo "{{define \"allunits1scharts\"}}" > $charts_1s_latency_div

  for i in `ls $result_dir`
  do
   if [ -e $result_dir/$i/latency_table_1s_category.js ]
   then
      sed "s/1s_percent_table_div/${i}_1s_percent_table_div/g" $result_dir/$i/latency_table_1s_category.js > $result_dir/${i}_latency_table_1s_category.js
      echo "   <script type='text/javascript' src='${i}_latency_table_1s_category.js'></script>" >> $js_refer_tmpl_file
      echo "                                <li><a href='${i}/index.html'>${i} 1s latency</a><div id='${i}_1s_percent_table_div'></div></li>" >> $charts_1s_latency_div
   fi
   if [ -e $result_dir/$i/latency_table_500ms_category.js ]
   then
      sed "s/500ms_percent_table_div/${i}_500ms_percent_table_div/g" $result_dir/$i/latency_table_500ms_category.js > $result_dir/${i}_latency_table_500ms_category.js
      echo "   <script type='text/javascript' src='${i}_latency_table_500ms_category.js'></script>" >> $js_refer_tmpl_file
   fi
  done
  echo "{{end}}" >> $js_refer_tmpl_file
  echo "{{end}}" >> $charts_1s_latency_div
  go run gensummary.go -header="tmpl/header.tmpl" -content="tmpl/allunits.tmpl" -body="$js_refer_tmpl_file" -footer="$charts_1s_latency_div" >${html_dir}/allunits.html
}

gen_all_units_report
