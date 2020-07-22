#!/bin/bash
set -e 
trap "exit" INT

function print_usage {
    cat <<EOF
Command 
    $(basename $0)
Arguments
   --prefix|-p                          [Requied] Used to distingush your perf resources with others'
   --subscription|-set                  [Requied] The subscriton used to create resources
   --location|-local                    [Requied] The location used to create resouces
   --help|-h                            : Print help
EOF
}

function throw_if_empty {
    local name="$1"
    local value="$2"
    if [[ -z "$value" ]];then
        echo "ERROR: Parameter $name cannot be empty." 1>&2
        exit -1
    fi
}

while [[ "$#" > 0 ]];do
    key="$1"
    shift
    case $key in 
        --prefix|-p)
            PREFIX="$1"
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
            echo "ERROR: Unknow argument '$key'" 1>$2
            print_usage
            exit -1
    esac
done
    
throw_if_empty "subscription" $subscription
throw_if_empty "prefix" $PREFIX
throw_if_empty "location" $LOCATION    

az account set -s $subscription

##
PREFIX="${PREFIX}perf"
RESOURCE_GROUP="${PREFIX}rg"
STORAGE_ACCOUNT="${PREFIX}sa"
KEYVAULT="${PREFIX}kv"
KUBERNETES_SEVICES="${PREFIX}aks"

KV_SA_ACCESS_KEY="sa-accessKey"
KV_KUBE_CONFIG="kube-config"

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
    echo "keyvault $KEYVAULT created."
else 
    echo "keyvault $KEYVAULT already exists. Skip creating.."
fi

if [[ -z $(az storage account show -n $STORAGE_ACCOUNT  -g $RESOURCE_GROUP 2>/dev/null) ]];then
    echo "start to create storage account $STORAGE_ACCOUNT"
    az storage account create -n $STORAGE_ACCOUNT >/dev/null
    access_key=$(az storage account keys list -n biqianperfsa --query [0].value -o tsv)
    az keyvault secret set --vault-name $KEYVAULT -n  $KV_SA_ACCESS_KEY --value "$access_key" 
    echo "storage account $STORAGE_ACCOUNT created."
else 
    echo "storage account $STORAGE_ACCOUNT already exists. Skip creating.."
fi

if  [[ -z $(az aks show --name $KUBERNETES_SEVICES -g $RESOURCE_GROUP 2>/dev/null) ]];then
    echo "start to create kubernetes services $KUBERNETES_SEVICES. May cost several minutes, waiting..."
    az aks create -n $KUBERNETES_SEVICES  --vm-set-type AvailabilitySet --kubernetes-version 1.16.10 --load-balancer-managed-outbound-ip-count 2 \
    --load-balancer-outbound-ports 8000 --enable-managed-identity -s Standard_B4ms --nodepool-name perf --generate-ssh-keys 
    echo "start to create kubernetes services $KUBERNETES_SEVICES created."
    echo "start getting kube/config"
    az aks get-credentials  -n $KUBERNETES_SEVICES 
    echo "upload kube/config to $KEYVAULT"
    az keyvault secret set --vault-name $KEYVAULT -n $KV_KUBE_CONFIG -f ~/.kube/config >/dev/null
else 
    echo "$KUBERNETES_SEVICES already exists. Skip creating.."
fi

echo "init has completed." 
