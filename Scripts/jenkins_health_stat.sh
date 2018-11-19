#!/bin/bash
. ./analyze_health_stat.sh

prepare() {
  mkdir $env_g_root
}

function publish_html() {
  if [ ! -e $env_g_nginx_root_dir ]
  then
     mkdir -p $env_g_nginx_root_dir
  fi
  mv $env_g_root $env_g_nginx_root_dir/
}

if [ -e jenkins_health_stat_env.sh ]
then
  . ./jenkins_health_stat_env.sh
  export env_g_http_base=$env_g_http_base
  export env_g_nginx_root_dir=$env_g_nginx_root_dir
  export nginx_server_dns=$env_g_dns
else
  echo "Specify the jenkins_health_stat_env.sh which contains required parameters"
  exit 1
fi

prepare

filter_asrs_logs $env_g_start_date $env_g_end_date

parse_all_logs $env_g_root

gen_js_files $env_g_root

gen_list_html $env_g_root > $env_g_root/index.html 

publish_html

sh send_mail.sh $env_g_nginx_root_dir/$env_g_root/index.html
