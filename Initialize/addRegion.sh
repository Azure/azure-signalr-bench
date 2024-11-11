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
KUBERNETES_SEVICES="${KUBERNETES_SEVICES}-${LOCATION}"

az configure --defaults group=$RESOURCE_GROUP

echo "start to create kubernetes services $KUBERNETES_SEVICES. May cost several minutes, waiting..."
# choose the correct os image
az aks create -n $KUBERNETES_SEVICES --vm-set-type VirtualMachineScaleSets --enable-managed-identity -s Standard_D4s_v3 --nodepool-name captain --generate-ssh-keys \
  --load-balancer-managed-outbound-ip-count 8 --load-balancer-outbound-ports 20000  --enable-addons monitoring --network-plugin azure --location $LOCATION
echo "kubernetes services $KUBERNETES_SEVICES created."
echo "start getting kube/config"
rm ~/.kube/perf || true
az aks get-credentials -a -n $KUBERNETES_SEVICES --overwrite-existing -f ~/.kube/perf
agentpool_msi_object_id=$(az aks show -n $KUBERNETES_SEVICES --query identityProfile.kubeletidentity.objectId -o tsv)

echo "grant aks-agent-pool-msi storage account blob data contributor"
az role assignment create --role "Storage Blob Data Contributor" --assignee $agentpool_msi_object_id --scope "/subscriptions/$SUBSCTIPTION/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT"
echo "grant aks-agent-pool-msi storage account queue data contributor"
az role assignment create --role "Storage Queue Data Contributor" --assignee $agentpool_msi_object_id --scope "/subscriptions/$SUBSCTIPTION/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT"

cosmosdbid=="/SUBSCRIPTIONS/$SUBSCTIPTION/RESOURCEGROUPS/$RESOURCE_GROUP/PROVIDERS/MICROSOFT.DOCUMENTDB/DATABASEACCOUNTS/$COSMOSDB_ACCOUNT"
echo "grant aks-agent-pool-msi cosmosdb table data contributor"
roleassignid=$(echo -n "$cosmosdbid" | md5sum | cut -d ' ' -f 1 | sed 's/^\(........\)\(....\)\(....\)\(....\)\(............\)$/\1-\2-\3-\4-\5/')
echo "roleassignid is $roleassignid"
az rest \
    --method "PUT" \
    --url "$cosmosdbid/tableRoleAssignments/$roleassignid?api-version=2023-04-15" \
    --body "{\"properties\": {\"roleDefinitionId\": \"$cosmosdbid/tableRoleDefinitions/00000000-0000-0000-0000-000000000002\", \"scope\": \"$cosmosdbid\", \"principalId\": \"$agentpool_msi_object_id\"}}"

echo "grant aks-agent-pool-msi keyvault permission"
az keyvault set-policy --name $KEYVAULT --object-id $agentpool_msi_object_id --secret-permissions delete get list set >/dev/null
STORAGE_KEY=$(az storage account keys list --resource-group $RESOURCE_GROUP --account-name $STORAGE_ACCOUNT --query "[0].value" -o tsv)
kubectl create secret generic azure-secret --from-literal=azurestorageaccountname=$STORAGE_ACCOUNT --from-literal=azurestorageaccountkey=$STORAGE_KEY --kubeconfig ~/.kube/perf
aks_principal_id=$(az aks show -n $KUBERNETES_SEVICES --query identity.principalId -o tsv)
echo "grant aks_principal_id=$aks_principal_id permission to  $RESOURCE_GROUP to auth service IP binding"
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
az role assignment create --role owner --assignee-object-id $aks_principal_id --assignee-principal-type ServicePrincipal --scope  /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP
echo "attach acr $ACR_NAME to $KUBERNETES_SEVICES"
az aks update --name $KUBERNETES_SEVICES --resource-group $RESOURCE_GROUP --attach-acr $ACR_NAME


echo "manually add entity in cosmosdb $COSMOSDB_ACCOUNT with PartitionKey=aks RowKey=$LOCATION"
echo "need to deploy redis in $LOCATION"

#az keyvault secret set --vault-name $KEYVAULT -n "default-host-location" --value $LOCATION
#az aks update -n $KUBERNETES_SEVICES --load-balancer-managed-outbound-ip-count 40 --load-balancer-outbound-ports 20000 







