#!/bin/bash
. ./env.sh

function gen_summary() {
tmp_sum=/tmp/summary_body.tmpl
sh gen_summary_body_tmpl.sh $nginx_root $tmp_sum
go run gensummary.go -header="tmpl/header.tmpl" -content="tmpl/summary.tmpl" -body="$tmp_sum" -footer="tmpl/footer.tmpl" > ${nginx_root}/summary.html
}

gen_summary
