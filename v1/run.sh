#!/bin/bash

export result_root=`date +%Y%m%d%H%M%S`

sh run_websocket.sh
sh gen_html.sh # gen_html
sh gen_all_report.sh # gen_all_report
sh publish_report.sh 
sh gen_summary.sh # refresh summary.html in NginxRoot gen_summary
