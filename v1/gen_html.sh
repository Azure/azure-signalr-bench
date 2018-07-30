#!/bin/bash
. ./func_env.sh

connectionString=""

function gen_single_html
{
	local bench_type=$1
	local bench_codec=$2
	local bench_name=$3
	local serviceName="" metricName=""
	local i j=1 max=4
	local resultdir=${bench_type}_${bench_codec}_${bench_name}
	local html_dir=$result_dir/$resultdir
	local norm_file=$result_dir/$resultdir/$sigbench_norm_file
	sh normalize.sh $result_dir/$resultdir/counters.txt $norm_file
	if [ ! -e $html_dir ]
	then
	  mkdir $html_dir
	fi
	go run parseresult.go -input $norm_file -sizerate > $html_dir/latency_rate_size.js
	go run parseresult.go -input $norm_file -rate > $html_dir/latency_rate.js
	go run parseresult.go -input $norm_file -areachart > $html_dir/latency_area.js
	go run parseresult.go -input $norm_file -lastlatency > $html_dir/latency_donut.js
	go run parseresult.go -input $norm_file -lastlatabPercent > $html_dir/latency_table.js
	go run parseresult.go -input $norm_file -category500ms > $html_dir/latency_table_500ms_category.js
	go run parseresult.go -input $norm_file -category1s > $html_dir/latency_table_1s_category.js

        if [ "$connectionString" != "" ]
	then
		serviceName=$(extract_servicename_from_connectionstring $connectionString)
		if [ "$serviceName" != "" ]
		then
			local timeWindows=`go run parseresult.go -input $norm_file -timeWindow`
			for i in `sh find_pod_name_by_resourcename.sh $serviceName`
			do
				metricName=CPU_metrics${j}
				sh query_cpu_memory_usage.sh CPU $i $timeWindows > $html_dir/${metricName}.json
				go run parsemdm.go -input $html_dir/${metricName}.json -index ${metricName} > $html_dir/${metricName}.js

				metricName=Memory_metrics${j}
				sh query_cpu_memory_usage.sh Memory $i $timeWindows > $html_dir/${metricName}.json
                                go run parsemdm.go -input $html_dir/${metricName}.json -index ${metricName} > $html_dir/${metricName}.js
				j=`expr $j + 1`
			done
		fi
	fi

	local cmd_prefix=$cmd_config_prefix
	. $sigbench_config_dir/${cmd_prefix}_${bench_codec}_${bench_name}_${bench_type}
	export OnlineConnections=$connection
	export ActiveConnections=$send
	export ConcurrentConnection=$connection_concurrent
	export Duration=$sigbench_run_duration
	export Endpoint=$bench_config_endpoint
	export Hub=$bench_config_hub
	local benchmark=${bench_name}:${bench_type}:${bench_codec}
	export Benchmark=$benchmark
	go run genhtml.go -header="tmpl/header.tmpl" -content="tmpl/content.tmpl" -footer="tmpl/footer.tmpl" > $html_dir/index.html
}

function gen_html() {
	iterate_all_scenarios gen_single_html
}

if [ $# -eq 1 ]
then
  # Get the service name through connection string. It is used to query MDMetrics for CPU and Memory
  connectionString=$1
fi
gen_html
