#!/bin/bash

result_root=`date +%Y%m%d%H%M%S`

sh run_websocket.sh
sh gen_html.sh
sh gen_all_report.sh
sh publish_report.sh
sh gen_summary.sh # refresh summary.html in NginxRoot
