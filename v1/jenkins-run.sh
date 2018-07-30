#!/bin/bash
. ./func_env.sh

create_root_folder

. ./jenkins-run-websocket.sh

gen_final_report
