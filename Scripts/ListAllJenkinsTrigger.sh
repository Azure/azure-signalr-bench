#!/bin/bash

FindAllConfig() {
  local job trigger normalTrigger
  for i in `find ~/.jenkins/jobs -iname "config.xml"`
  do
    job=`echo $i|awk -F / '{print $6}'`
    trigger=`python FindTrigger.py -i $i`
    if [ "$trigger" != "" ]
    then
      normalTrigger=`echo "$trigger"|tr '\n' ' '`
      echo "$job|$normalTrigger"
    fi
  done
}

FindAllConfig
