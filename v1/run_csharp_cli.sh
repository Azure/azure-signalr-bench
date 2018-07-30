#!/bin/bash
. ./csharpcli.sh

prepare

entry_gen_all_cli_scripts

entry_copy_cli_scripts_to_master

entry_copy_start_cli_bench

entry_launch_master_cli_script

entry_copy_stop_cli_bench
