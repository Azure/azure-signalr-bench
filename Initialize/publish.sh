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
  dotnet restore
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
  kubectl delete deployment portal || true
  cat portal.yaml | replace KVURL_PLACE_HOLDER $KVURL | replace MSI_PLACE_HOLDER $AGENTPOOL_MSI_CLIENT_ID | kubectl apply -f -
  kubectl apply -f portal-service.yaml
  domain=$(az network public-ip show -n $PORTAL_IP_NAME -g $RESOURCE_GROUP --query dnsSettings.fqdn -o tsv)
  echo " portal domain: $domain "
fi

if [[ $ALL || $COORDINATOR ]]; then
  publish Coordinator
  cd $DIR/yaml/coordinator
  kubectl delete deployment coordinator || true
  access_key=$(az storage account show-connection-string -n $STORAGE_ACCOUNT -g $RESOURCE_GROUP --query connectionString -o tsv)
  domain=$(az network public-ip show -n $PORTAL_IP_NAME -g $RESOURCE_GROUP --query dnsSettings.fqdn -o tsv)
  cat coordinator.yaml | replace KVURL_PLACE_HOLDER $KVURL | replace MSI_PLACE_HOLDER $AGENTPOOL_MSI_CLIENT_ID | replace STORAGE_PLACE_HOLDER $access_key | replace DOMAIN_PLACE_HOLDER $domain | kubectl apply -f -
fi

if [[ $ALL || $COMPILER ]]; then
  #no implemention
  return
  #publish Compiler
  # cd $DIR/yaml/compiler
  # kubectl apply -f compiler.yaml
fi

if [[ $ALL || $APPSERVER ]]; then
  publish AppServer
fi

if [[ $ALL || $ASPNET ]]; then
  echo "skip this part for simplicity. Or Uncomment below to add aspnet code"
  return
  #Pod="AspNetAppServer"
  #cd $DIR/../src/Pods/$Pod
  #echo "Need to run :   msbuild.exe /p:OutDir=publish /p:Configuration=Release in windows first"
  #cd publish && zip -r ${Pod}.zip *
  #echo "create dir:$Pod"
  #az storage directory create -n "manifest/$Pod" --account-name $STORAGE_ACCOUNT -s $SA_SHARE
  #az storage file upload --account-name $STORAGE_ACCOUNT -s $SA_SHARE --source $Pod.zip -p manifest/$Pod
  #echo "upload $Pod succeeded"
fi

if [[ $ALL || $CLIENT ]]; then
  publish Client
fi

if [[ $ALL || $UPSTREAM ]]; then
  publish SignalRUpstream
fi

if [[ $ALL || $WPSUPSTREAM ]]; then
  publish WpsUpstream
fi


if [[ $ALL || $REDIS ]]; then
  ##This redis has only one instance. Change this to cluster mode later
  cd $DIR/yaml/redis
  PORTAL_IP=$(az network public-ip show -n $PORTAL_IP_NAME -g $RESOURCE_GROUP --query "ipAddress" -o tsv)
  kubectl apply -f redis-master-deployment.yaml
  kubectl apply -f redis-master-service.yaml
  # cat redis-master-test.yaml | replace RESOURCE_GROUP_PLACE_HOLDER $RESOURCE_GROUP | kubectl apply -f -
  echo "redis dns inside cluster: redis-master "
fi

if [[ $ALL || $UPDATEPOOL ]]; then
  ## The pool size would impact the unit size that could be tested
  az aks nodepool add \
    --resource-group $RESOURCE_GROUP \
    --cluster-name $KUBERNETES_SEVICES \
    -n linux0 \
    -s Standard_D4s_v3 \
    -e \
    --min-count 0 \
    --max-count 40 \
    --os-type Linux \
    -c 0 || true
  az aks nodepool add \
    --resource-group $RESOURCE_GROUP \
    --cluster-name $KUBERNETES_SEVICES \
    -n win0 \
    -s Standard_D4s_v3 \
    -e \
    --min-count 0 \
    --max-count 15 \
    --os-type Windows \
    -c 0 || true
fi

if [[ $ALL || $AUTOSCALE ]]; then
  az aks update \
    --resource-group $RESOURCE_GROUP \
    -n $KUBERNETES_SEVICES \
    --cluster-autoscaler-profile scale-down-delay-after-add=60m scale-down-unneeded-time=60m scale-down-utilization-threshold=0.5 skip-nodes-with-system-pods=false new-pod-scale-up-delay=1s ok-total-unready-count=0 
fi

if [[ $ALL || $PPE ]]; then
  return
  # internal test only
#  az cloud register -n ppe \
#                  --endpoint-active-directory "https://login.windows-ppe.net" \
#                  --endpoint-active-directory-graph-resource-id "https://graph.ppe.windows.net/" \
#                  --endpoint-active-directory-resource-id "https://management.core.windows.net/" \
#                  --endpoint-gallery "https://gallery.azure.com/" \
#                  --endpoint-management "https://umapi-preview.core.windows-int.net/" \
#                  --endpoint-resource-manager "https://api-dogfood.resources.windows-int.net/" \
#                  --profile "latest" || true
#  az cloud set -n ppe
#  sp=$(az ad sp create-for-rbac -n $PPE_SERVICE_PRINCIPAL --role contributor  --scopes /subscriptions/c0e88e48-6490-40d0-a4c2-6c963c5a4d1e)
#  az cloud set -n "AzureCloud"
#  az keyvault secret set  --vault-name $KEYVAULT -n "ppe-service-principal" --value  "$sp"
#  az keyvault secret set  --vault-name $KEYVAULT -n "ppe-subscription" --value  "c0e88e48-6490-40d0-a4c2-6c963c5a4d1e"
#  az keyvault secret set  --vault-name $KEYVAULT -n "ppe-location" --value  $LOCATION
fi

if [[ $ALL || $INGRESS ]]; then
  cd $DIR/yaml/ingress
  kubectl apply -f service-account.yaml
  kubectl apply -f cluster-role.yaml
  kubectl apply -f role-binding-sa.yaml
  domain=$(az network public-ip show -n $PORTAL_IP_NAME -g $RESOURCE_GROUP --query dnsSettings.fqdn -o tsv)
  echo $domain
  ip=$(az network public-ip show -n $PORTAL_IP_NAME -g $RESOURCE_GROUP --query ipAddress -o tsv)

  kubectl create namespace ingress-basic || true
  sudo apt-get install helm

  # Add the ingress-nginx repository
  helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx

  # Use Helm to deploy an NGINX ingress controller
  helm install nginx-ingress ingress-nginx/ingress-nginx \
    --namespace ingress-basic \
    --set controller.replicaCount=2 \
    --set controller.nodeSelector."beta\.kubernetes\.io/os"=linux \
    --set defaultBackend.nodeSelector."beta\.kubernetes\.io/os"=linux \
    --set controller.service.loadBalancerIP="$ip" \
    --set controller.deployment.spec.template.annotations."nginx\.ingress\.kubernetes\.io/proxy-buffer-size"=10m \
    --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-dns-label-name"="$PORTAL_DNS" \
    --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-resource-group"="$RESOURCE_GROUP" || true
  # Label the cert-manager namespace to disable resource validation
  kubectl label namespace ingress-basic cert-manager.io/disable-validation=true || true
  # Add the Jetstack Helm repository
  helm repo add jetstack https://charts.jetstack.io
  # Update your local Helm chart repository cache
  helm repo update
  # Install the cert-manager Helm chart
  helm install \
    cert-manager \
    --namespace ingress-basic \
    --version v0.16.1 \
    --set installCRDs=true \
    --set nodeSelector."beta\.kubernetes\.io/os"=linux \
    jetstack/cert-manager || true
  sleep 10
  echo "NSG may break the outbound traffic and fail this step"
  kubectl apply -f cluster-issuer.yaml
  kubectl apply -f dashboard-ext.yaml
  cat portal-ingress.yaml | replace PORTAL_DOMAIN_PLACE_HOLDER $domain | kubectl apply -f -
fi

domain=$(az network public-ip show -n $PORTAL_IP_NAME -g $RESOURCE_GROUP --query dnsSettings.fqdn -o tsv)
echo "portal url: https://$domain "