#!/bin/bash
. ./func_env.sh

prepare

gen_websocket_bench

deploy_all_scripts_config_bench

start_sigbench

launch_all_websocket_scripts

stop_sigbench
