#!/bin/bash
set -e
trap "exit" INT

DIR=$(cd $(dirname $0) && pwd)
source $DIR/common.sh

PREFIX=""

function print_usage() {
    cat <<EOF
Command 
    $(basename $0)
Arguments
   --prefix|-p                          [Requied] Used to distingush your perf resources with others'
   --subscription|-s                    [Optional] The subscriton used to create resources
   --cloud|-c                           [Optional] The cloud used to create resources
   --all|-a                             [Optional] publish portal,coordinator and compiler
   --portal                             [Optional] publish portal
   --coordinator                        [Optional] publish coordinator
   --compiler                           [Optional] publish compiler
   --server                             [Optional] publish compiler
   --client                             [Optional] publish compiler
   --help|-h                            Print help
EOF
}

while [[ "$#" > 0 ]]; do
    key="$1"
    shift
    case $key in
    --prefix | -p)
        PREFIX="$1"
        shift
        ;;
    --cloud | -c)
        CLOUD="$1"
        shift
        ;;
    --subscription | -s)
        SUBSCTIPTION="$1"
        shift
        ;;
    --portal)
        PORTAL=true
        ;;
    --coordinator)
        COORDINATOR=true
        ;;
    --compiler)
        COMPILER=true
        ;;
    --server)
        APPSERVER=true
        ;;
    --client)
        CLIENT=true
        ;;
    --redis)
        REDIS=true
        ;;
    --all | -a)
        ALL=true
        ;;
    --help | -h)
        print_usage
        exit
        ;;
    *)
        echo "ERROR: Unknow argument '$key'" 1>&2
        print_usage
        exit -1
        ;;
    esac
done

function publish() {
    Pod=$1
    cd $DIR/../src/Pods/$Pod
    rm -rf publish
    echo "start to publish $Pod"
    dotnet publish -r linux-x64 -c release -o publish /p:useapphost=true
    cd publish && zip -r ${Pod}.zip *
    echo "create dir:$Pod"
    az storage directory create -n "manifest/$Pod" --account-name $STORAGE_ACCOUNT -s $SA_SHARE
    az storage file upload --account-name $STORAGE_ACCOUNT -s $SA_SHARE --source $Pod.zip -p manifest/$Pod
    echo "upload $Pod succeeded"
}

throw_if_empty "prefix" $PREFIX

init_common
init_aks_group

if [[ $ALL || $PORTAL ]]; then
    publish Portal
    cd $DIR/yaml/portal
    cat portal.yaml | replace KVURL_PLACE_HOLDER $KVURL | replace MSI_PLACE_HOLDER $AGENTPOOL_MSI_CLIENT_ID | kubectl apply -f -
    kubectl apply -f portal-service.yaml 
    domain=$(az network public-ip show -n $PORTAL_IP_NAME -g $RESOURCE_GROUP --query dnsSettings.fqdn -o tsv)
    echo " portal domain: $domain "
fi

if [[ $ALL || $COORDINATOR ]]; then
  #  publish Coordinator
    cd $DIR/yaml/coordinator
    cat coordinator.yaml | replace KVURL_PLACE_HOLDER $KVURL | replace MSI_PLACE_HOLDER $AGENTPOOL_MSI_CLIENT_ID | kubectl  apply -f -
fi

if [[ $ALL || $COMPILER ]]; then
    publish Compiler
    cd $DIR/yaml/compiler
    kubectl apply -f compiler.yaml
fi

if [[ $ALL || $APPSERVER ]]; then
    publish AppServer
fi

if [[ $ALL || $CLIENT ]]; then
    publish Client
fi

if [[ $ALL || $REDIS ]]; then
    ##This redis has only one instance. Change this to cluster mode later
    cd $DIR/yaml/redis
    PORTAL_IP=$(az network public-ip show -n $PORTAL_IP_NAME -g $RESOURCE_GROUP --query "ipAddress" -o tsv)
    cat redis-master-service2.yaml | replace RESOURCE_GROUP_PLACE_HOLDER $RESOURCE_GROUP  | kubectl apply -f -
    kubectl apply -f redis-master-deployment.yaml
    echo "redis dns inside cluster: redis-master "
fi
