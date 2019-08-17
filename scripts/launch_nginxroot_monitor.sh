#!/bin/bash
  ps aux | grep inotifywait | grep -q -v grep
  if [[ $? -ne 0 ]]; then
     nohup sh monitor_nginxroot_dir.sh &
  fi
