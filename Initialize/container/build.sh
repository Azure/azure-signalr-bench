#/bin/bash
#REPO="signalrbenchmark/perf"
#repo="mcr.microsoft.com/signalrbenchmark/base"
REPO="signalrservice.azurecr.io/public/signalrbenchmark/base"
TAG="1.2.0"
docker build - < ./base_image.docker --platform linux/amd64 -t $REPO:$TAG
