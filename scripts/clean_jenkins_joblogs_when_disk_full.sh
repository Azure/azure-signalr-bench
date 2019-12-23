#!/bin/bash

function clean_any_job()
{
        local i j
        cd $HOME/.jenkins/jobs
        pushd `pwd`
        for i in `du -s *|sort -k 1 -n -r|head -n 1|awk '{print $2}'`
        do
                cd $i/builds
                pushd `pwd`
                for j in `du -s *|sort -k 1 -n -r|head -n 10|awk '{print $2}'`
                do
                        truncate -s 1024 $j/log
                done
                popd
        done
        popd
}

clean_any_job
