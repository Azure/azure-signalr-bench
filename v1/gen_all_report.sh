#!/bin/bash
. ./env.sh

function gen_all_report() {
for i in `ls $result_dir`
do
   if [ -e $result_dir/$i/latency_table_1s_category.js ]
   then
      sed "s/1s_percent_table_div/${i}_1s_percent_table_div/g" $result_dir/$i/latency_table_1s_category.js > $result_dir/${i}_latency_table_1s_category.js
   fi
   if [ -e $result_dir/$i/latency_table_500ms_category.js ]
   then
      sed "s/500ms_percent_table_div/${i}_500ms_percent_table_div/g" $result_dir/$i/latency_table_500ms_category.js > $result_dir/${i}_latency_table_500ms_category.js
   fi
done

. ./servers_env.sh
export BenchEndpoint=${bench_server}:${bench_server_port}
export SignalRServiceExtSSHEndpoint=${bench_service_pub_server}:${bench_service_pub_port}
export SignalRServiceIntEndpoint=${bench_service_server}:${bench_service_port}
export SignalRDemoAppExtSSHEndpoint=${bench_app_pub_server}:${bench_app_pub_port}
export SignalRDemoAppIntEndpoint=${bench_app_server}:${bench_app_port}

python render_tmpl.py -t tmpl/all.html >$result_dir/all.html
}

gen_all_report
