#!/bin/bash
set -e
trap "exit" INT

DIR=$(cd $(dirname $0) && pwd)
source $DIR/common.sh

function print_usage() {
    cat <<EOF
Command 
    $(basename $0)
Arguments
   --prefix|-p                          [Requied] Used to distingush your perf resources with others'
   --location|-l                        [Requied] The location used to create resouces
   --subscription|-s                    [Optional] The subscriton used to create resources
   --cloud|-c                           [Optional] The cloud used to create resources
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
    --location | -l)
        LOCATION="$1"
        shift
        ;;
    --help | h)
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
throw_if_empty "location" $LOCATION

init_common

if [[ $(az group exists -n $RESOURCE_GROUP) == "false" ]]; then
    echo "start to create resouce group $RESOURCE_GROUP"
    az group create -n $RESOURCE_GROUP -l $LOCATION
    echo "resouce group $RESOURCE_GROUP created."
else
    echo "resouce group $RESOURCE_GROUP already exists. Skip creating.."
fi

az configure --defaults group=$RESOURCE_GROUP

if [[ -z $(az keyvault show -n $KEYVAULT 2>/dev/null) ]]; then
    echo "start to create keyvault $KEYVAULT"
    az keyvault create -n $KEYVAULT -g $RESOURCE_GROUP
    echo "keyvault $KEYVAULT created. Grant current user permission"
    #no longer necessary. Permission Will be added automatically for current user
    #az keyvault set-policy --name $KEYVAULT --upn $(az account show --query user.name -o tsv) --secret-permissions delete get list set >/dev/null
else
    echo "keyvault $KEYVAULT already exists. Skip creating.."
fi

if [[ -z $(az network public-ip show -n $PORTAL_IP_NAME -g $RESOURCE_GROUP 2>/dev/null) ]]; then
    echo "start to init portal ip $PORTAL_IP_NAME"
    az network public-ip create -n $PORTAL_IP_NAME -g $RESOURCE_GROUP --dns-name $PORTAL_DNS --sku Standard --allocation-method  Static
    domain=$(az network public-ip show -n $PORTAL_IP_NAME -g $RESOURCE_GROUP --query dnsSettings.fqdn -o tsv)
    echo "portal domain:$domain init"
else
    echo "IP $PORTAL_IP_NAME already exists. Skip creating.."
fi

if [[ -z $(az monitor log-analytics workspace show -n $WORK_SPACE -g $RESOURCE_GROUP 2>/dev/null) ]]; then
    echo "start to init workspace $WORK_SPACE"
    az monitor log-analytics workspace create -n $WORK_SPACE -g $RESOURCE_GROUP
    echo "work space:$WORK_SPACE init"
else
    echo "work space:$WORK_SPACE already exists. Skip creating.."
fi

if [[ -z $(az storage account show -n $STORAGE_ACCOUNT -g $RESOURCE_GROUP 2>/dev/null) ]]; then
    echo "start to create storage account $STORAGE_ACCOUNT"
    az storage account create -n $STORAGE_ACCOUNT >/dev/null
    access_key=$(az storage account show-connection-string -n $STORAGE_ACCOUNT --query connectionString  -o tsv)
    az keyvault secret set --vault-name $KEYVAULT -n $KV_SA_ACCESS_KEY --value "$access_key"
    echo "storage account $STORAGE_ACCOUNT created."
    az storage share create --account-name $STORAGE_ACCOUNT --quota 20 -n $SA_SHARE
    echo "create dir:manifest"
    az storage directory create -n "manifest" --account-name $STORAGE_ACCOUNT -s $SA_SHARE
else
    echo "storage account $STORAGE_ACCOUNT already exists. Skip creating.."
fi

if [  $(az cosmosdb  check-name-exists -n $COSMOSDB_ACCOUNT) == "false" ]; then
   echo "start to create cosmosdb account $COSMOSDB_ACCOUNT"
   az cosmosdb create -n $COSMOSDB_ACCOUNT --tags default_Experience="Azure Table" --capabilities EnableTable
   access_key=$(az cosmosdb keys list -n $COSMOSDB_ACCOUNT --type connection-strings |  jq .connectionStrings[4].connectionString -r)
   az keyvault secret set --vault-name $KEYVAULT -n $KV_CA_ACCESS_KEY --value "$access_key"
   echo "cosmosdb $COSMOSDB_ACCOUNT created"
else
   echo "cosmosdb $COSMOSDB_ACCOUNT already exists. SKip creating.."
fi

if [[ -z $(az aks show --name $KUBERNETES_SEVICES -g $RESOURCE_GROUP 2>/dev/null) ]]; then
    echo "start to create kubernetes services $KUBERNETES_SEVICES. May cost several minutes, waiting..."
    work_space_resource_id=$(az monitor log-analytics workspace show -g $RESOURCE_GROUP -n $WORK_SPACE --query id -o tsv)
    az aks create -n $KUBERNETES_SEVICES --vm-set-type VirtualMachineScaleSets --enable-managed-identity -s Standard_D4as_v4 --nodepool-name captain --generate-ssh-keys \
      --load-balancer-managed-outbound-ip-count 8 --load-balancer-outbound-ports 10000 --workspace-resource-id "$work_space_resource_id" --enable-addons monitoring --network-plugin azure
    echo "kubernetes services $KUBERNETES_SEVICES created."
    echo "start getting kube/config"
    rm ~/.kube/perf || true
    az aks get-credentials -a -n $KUBERNETES_SEVICES --overwrite-existing -f ~/.kube/perf
    kubectl apply -f https://raw.githubusercontent.com/kubernetes/dashboard/v2.3.1/aio/deploy/recommended.yaml --kubeconfig ~/.kube/perf
    echo "upload kube/config to $KEYVAULT"
    az keyvault secret set --vault-name $KEYVAULT -n $KV_KUBE_CONFIG -f ~/.kube/perf >/dev/null
    agentpool_msi_object_id=$(az aks show -n $KUBERNETES_SEVICES --query identityProfile.kubeletidentity.objectId -o tsv)
    echo "grant aks-agent-pool-msi keyvault permission"
    az keyvault set-policy --name $KEYVAULT --object-id $agentpool_msi_object_id --secret-permissions delete get list set >/dev/null
    STORAGE_KEY=$(az storage account keys list --resource-group $RESOURCE_GROUP --account-name $STORAGE_ACCOUNT --query "[0].value" -o tsv)
    kubectl create secret generic azure-secret --from-literal=azurestorageaccountname=$STORAGE_ACCOUNT --from-literal=azurestorageaccountkey=$STORAGE_KEY --kubeconfig ~/.kube/perf
    aks_principal_id=$(az aks show -n $KUBERNETES_SEVICES --query identity.principalId -o tsv)
    echo "grant aks_principal_id=$aks_principal_id permission to  $RESOURCE_GROUP to auth service IP binding"
    az role assignment create --role owner -g $RESOURCE_GROUP --assignee-object-id $aks_principal_id --assignee-principal-type ServicePrincipal
else
    echo "$KUBERNETES_SEVICES already exists. Skip creating.."
fi

if [[ -z $(az ad sp show --id http://$SERVICE_PRINCIPAL 2>/dev/null) ]]; then
    echo "skip ppe"
#    echo "start to create service principal $SERVICE_PRINCIPAL"
#    sp=$(az ad sp create-for-rbac -n $SERVICE_PRINCIPAL --role contributor --scopes /subscriptions/$SUBSCTIPTION)
#    echo "add $SERVICE_PRINCIPAL to keyvault"
#    az keyvault secret set --vault-name $KEYVAULT -n "service-principal" --value "$sp"
else
    echo "$SERVICE_PRINCIPAL already exists. Skip creating.."
fi

echo "set keyvault constants"
az keyvault secret set --vault-name $KEYVAULT -n "prefix" --value $PREFIX
az keyvault secret set --vault-name $KEYVAULT -n "subscription" --value $SUBSCTIPTION
cloud_name=$(az cloud show --query name -o tsv)
az keyvault secret set --vault-name $KEYVAULT -n "cloud" --value $cloud_name
az keyvault secret set --vault-name $KEYVAULT -n "location" --value $LOCATION
echo "init has completed."

domain=$(az network public-ip show -n $PORTAL_IP_NAME -g $RESOURCE_GROUP --query dnsSettings.fqdn -o tsv)
echo "redirectUrl: https://$domain/signin-oidc "
