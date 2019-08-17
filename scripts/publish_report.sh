#!/bin/bash
. ./env.sh

cp -r tmpl/css $nginx_root/
mv $result_dir $nginx_root/
