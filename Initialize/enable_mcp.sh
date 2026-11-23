#!/bin/bash
set -e
trap "exit" INT

DIR=$(cd $(dirname $0) && pwd)
source $DIR/common.sh

RESOURCE_URL=""

function print_usage() {
    cat <<EOF
Command
    $(basename $0)
    Configures the portal's Entra app registration so MCP clients (e.g. GitHub Copilot in VS Code)
    can call https://<portal>/mcp on behalf of the signed-in user. New deployments get this from init.sh.
Arguments
   --prefix|-p                          [Requied] The prefix used by init.sh
   --subscription|-s                    [Optional] The subscriton of the perf resources
   --cloud|-c                           [Optional] The cloud of the perf resources
   --resource-url|-r                    [Optional] Exact HTTPS resource URI from the portal's MCP metadata
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
    --resource-url | -r)
        RESOURCE_URL="$1"
        shift
        ;;
    --subscription | -s)
        SUBSCTIPTION="$1"
        shift
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

throw_if_empty "prefix" $PREFIX

init_common

appId=$(az keyvault secret show --vault-name $KEYVAULT -n "appid" --query value -o tsv)
throw_if_empty "appid in $KEYVAULT" $appId
enable_mcp_auth $appId "$RESOURCE_URL"
echo "MCP endpoint: https://<portal domain>/mcp, where <portal domain> is the domain you open the portal with."
