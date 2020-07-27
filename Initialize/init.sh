#!/bin/bash
set -e 
trap "exit" INT

DIR=$(cd `dirname $0` && pwd)
source $DIR/common.sh

function print_usage {
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

while [[ "$#" > 0 ]];do
    key="$1"
    shift
    case $key in 
        --prefix|-p)
            PREFIX="$1"
            shift
        ;;
        --cloud|-c)
            CLOUD="$1"
            shift
        ;;
        --subscription|-s)
            SUBSCTIPTION="$1"
            shift
        ;;
        --location|-l)
            LOCATION="$1"
            shift
        ;;
        --help|h)
            print_usage
            exit
        ;;
        *)
            echo "ERROR: Unknow argument '$key'" 1>&2
            print_usage
            exit -1
    esac
done

throw_if_empty "prefix" $PREFIX
throw_if_empty "location" $LOCATION    

init_common

if [[ $(az group exists -n $RESOURCE_GROUP) == "false" ]]; then
   echo "start to create resouce group $RESOURCE_GROUP"
   az group create -n $RESOURCE_GROUP -l  $LOCATION  
   echo "resouce group $RESOURCE_GROUP created."
else
   echo "resouce group $RESOURCE_GROUP already exists. Skip creating.."
fi

az configure --defaults group=$RESOURCE_GROUP

if [[ -z $(az keyvault  show -n $KEYVAULT 2>/dev/null) ]];then
    echo "start to create keyvault $KEYVAULT"
    az keyvault create -n $KEYVAULT 
    echo "keyvault $KEYVAULT created. Grant current user permission"
    az keyvault set-policy --name $KEYVAULT --upn $(az account show --query user.name -o tsv )  --secret-permissions  delete get list  set >/dev/null
else 
    echo "keyvault $KEYVAULT already exists. Skip creating.."
fi

if [[ -z $(az storage account show -n $STORAGE_ACCOUNT  -g $RESOURCE_GROUP 2>/dev/null) ]];then
    echo "start to create storage account $STORAGE_ACCOUNT"
    az storage account create -n $STORAGE_ACCOUNT >/dev/null
    access_key=$(az storage account keys list -n $STORAGE_ACCOUNT --query [0].value -o tsv)
    az keyvault secret set --vault-name $KEYVAULT -n  $KV_SA_ACCESS_KEY --value "$access_key" 
    echo "storage account $STORAGE_ACCOUNT created."
    az storage share create --account-name $STORAGE_ACCOUNT --quoto 20 -n $SA_SHARE
else 
    echo "storage account $STORAGE_ACCOUNT already exists. Skip creating.."
fi

if  [[ -z $(az aks show --name $KUBERNETES_SEVICES -g $RESOURCE_GROUP 2>/dev/null) ]];then
    echo "start to create kubernetes services $KUBERNETES_SEVICES. May cost several minutes, waiting..."
    az aks create -n $KUBERNETES_SEVICES  --vm-set-type VirtualMachineScaleSets  --kubernetes-version 1.16.10  --enable-managed-identity -s Standard_D4s_v3 --nodepool-name captain --generate-ssh-keys \
    --load-balancer-managed-outbound-ip-count  3 
    echo "start to create kubernetes services $KUBERNETES_SEVICES created."
    echo "start getting kube/config"
    az aks get-credentials -a -n $KUBERNETES_SEVICES -f ~/.kube/perf
    echo "upload kube/config to $KEYVAULT"
    az keyvault secret set --vault-name $KEYVAULT -n $KV_KUBE_CONFIG -f ~/.kube/perf >/dev/null
    agentpool_msi_object_id=$(az aks show -n $KUBERNETES_SEVICES  --query identityProfile.kubeletidentity.objectId -o tsv)
    echo "grant aks-agent-pool-msi keyvault permission"
    az keyvault set-policy --name $KEYVAULT --object-id $agentpool_msi_object_id  --secret-permissions  delete get list  set >/dev/null
    echo "grant aks-agent-pool-msi contibutor role of $SUBSCTIPTION"
    az role assignment create --role "Contributor" --assignee-object-id $agentpool_msi_object_id --scope "/subscriptions/$SUBSCTIPTION"
else 
    echo "$KUBERNETES_SEVICES already exists. Skip creating.."
fi

echo "init has completed." 
