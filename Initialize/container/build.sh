#/bin/bash

#docker build - < ./base_image.docker -t signalrbenchmark/perf:1.4.4
#docker build - < ./base_image.docker -t public/signalrbenchmark/perf:1.0.0
docker build - < ./base_image.docker -t signalrservice.azurecr.io/public/signalrbenchmark/base:1.0.0
