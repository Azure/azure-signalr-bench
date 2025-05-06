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
   --all|-a                             [Optional] publish all components
   --portal                             [Optional] publish portal
   --coordinator                        [Optional] publish coordinator
   --compiler                           [Optional] publish compiler
   --server                             [Optional] publish server
   --client                             [Optional] publish client
   --sioserver                          [Optional] publish socket.io server
   --wpspyserver                        [Optional] publish wps python server
   --aksregion|-ar                      [Optional] use aks in different region
   --grafana|-g                         [Optional] publish grafana
   --skipInitAks|-ska                   [Optional] skip init aks
   --adhoc                              [Optional] run adhoc
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
  --aspnet)
    ASPNET=true
    ;;
  --client)
    CLIENT=true
    ;;
  --redis)
    REDIS=true
    ;;
  --ingress)
    INGRESS=true
    ;;
  --upstream)
    UPSTREAM=true
    ;;
  --wpsupstream)
    WPSUPSTREAM=true
    ;;
  --sioserver)
    SIOSERVER=true
    ;;
  --autoscale)
    AUTOSCALE=true
    ;;
  --updatepool)
    UPDATEPOOL=true
    ;;
   --ppe)
    PPE=true
    LOCATION="$1"
    shift
    ;;
  --wpspyserver)
    WPSPYSERVER=true
    ;;
  --grafana | -g)
    GRAFANA=true
    ;;
  --kubedashboard | -kd)
    KUBEDASHBOARD=true
    ;;
  --skipInitAks | -ska)
    SKIP_INIT_AKS=true
    ;;
  --aksregion | -ar)
    AKSLCOATION="$1"
    shift    
    ;;
  --adhoc)
    ADHOC=true
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

throw_if_empty "prefix" $PREFIX

init_common

## If akslocation is not empty, change KUBERNETES_SEVICES to KUBERNETES_SEVICES-$AKSLCOATION
if [[ ! -z $AKSLCOATION ]]; then
  KUBERNETES_SEVICES="${KUBERNETES_SEVICES}-${AKSLCOATION}"
fi

if [[ -z $SKIP_INIT_AKS ]]; then
  init_aks_group
fi

base_image=$( az keyvault secret show --vault-name $KEYVAULT -n "image" | jq ".value" -r )
internal=$( az keyvault secret show --vault-name $KEYVAULT -n "internal" | jq ".value"  )
az acr login -n $ACR_NAME

function publish() {
  local Pod=$1
  local DockerFile=$2
  local pod_lower
  pod_lower=$(echo "$Pod" | tr '[:upper:]' '[:lower:]')

  local timestamp
  timestamp=$(date +%Y%m%d)
  timestamp=$(date +%Y%m%d%H%M%S)

  image=${IMAGE_PREFIX}${pod_lower}:${timestamp}

  docker build -f docker/${DockerFile}.docker -t "${image}" \
    --build-arg IMAGE="${base_image}" \
    --build-arg POD="${Pod}" \
    ../src

  echo "Building $Pod image: ${image}"
  docker push "${image}"
  az keyvault secret set \
    --vault-name "${KEYVAULT}" \
    -n "Image-${Pod}" \
    --value "${image}"
}

if [[ $ALL || $PORTAL ]]; then
  echo "replace the clientId and tenantId in src/Pods/Portal/appsettings.json"
  appId=$( az keyvault secret show --vault-name $KEYVAULT -n "appid" | jq ".value" -r )
  tenant=$( az keyvault secret show --vault-name $KEYVAULT -n "tenant" | jq ".value" -r )
  echo "tenant is $tenant"
  cd $DIR/../src/Pods/Portal
  cat appsettings.template.json | replace CLIENTID_PLACE_HOLDER $appId | replace TENANTID_PLACE_HOLDER $tenant > appsettings.json
  cd $DIR
  publish Portal portal
  cd $DIR/yaml/portal
  kubectl delete deployment portal  > /dev/null 2>&1 || true
  cat portal_docker.yaml | replace KVURL_PLACE_HOLDER $KVURL | replace MSI_PLACE_HOLDER $AGENTPOOL_MSI_CLIENT_ID | replace IMAGE_PLACE_HOLDER $image | kubectl apply -f -
  kubectl apply -f portal-service.yaml
  domain=$(az network public-ip show -n $PORTAL_IP_NAME -g $RESOURCE_GROUP --query dnsSettings.fqdn -o tsv)
  echo " portal domain: $domain " # This domain is no longer used internally
  cd $DIR/../src/Pods/Portal
  rm appsettings.json
fi

if [[ $ALL || $COORDINATOR ]]; then
  publish Coordinator default
  cd $DIR/yaml/coordinator
  kubectl delete deployment coordinator  > /dev/null 2>&1 || true
  domain=$(az network public-ip show -n $PORTAL_IP_NAME -g $RESOURCE_GROUP --query dnsSettings.fqdn -o tsv)
  echo " coordinator domain: $domain "
  cat coordinator_docker.yaml | replace KVURL_PLACE_HOLDER $KVURL | replace MSI_PLACE_HOLDER $AGENTPOOL_MSI_CLIENT_ID | replace STORAGE_PLACE_HOLDER $access_key | replace DOMAIN_PLACE_HOLDER $domain | replace IMAGE_PLACE_HOLDER $image | replace INTERNAL_PLACE_HOLDER $internal | replace LOCATION_PLACE_HOLDER $AKSLCOATION | kubectl apply -f -
fi

if [[ $ALL || $APPSERVER ]]; then
  publish AppServer default
fi

if [[ $ALL || $CLIENT ]]; then
  publish Client default
fi

if [[ $ALL || $UPSTREAM ]]; then
  publish SignalRUpstream default
fi

if [[ $ALL || $WPSUPSTREAM ]]; then
  publish WpsUpstream default
fi

if [[ $ALL || $SIOSERVER ]]; then
  publish SioServer sio
fi

if [[ $ALL || $WPSPYSERVER ]]; then
  publish WpsPyServer wpspyserver
fi

