#!/bin/bash


function init_common() {
    echo "init naming conventions and configs"
    PREFIX_PERF="${PREFIX}perfv2"
    RESOURCE_GROUP="${PREFIX_PERF}rg"
    STORAGE_ACCOUNT="${PREFIX_PERF}sa"
    COSMOSDB_ACCOUNT="${PREFIX_PERF}ca"
    KEYVAULT="${PREFIX_PERF}kv"
    KUBERNETES_SEVICES="${PREFIX_PERF}aks"
    ACR_NAME="${PREFIX_PERF}acr"
    IMAGE_PREFIX="${ACR_NAME}.azurecr.io/pods/"
    PORTAL_IP_NAME="${PREFIX_PERF}ip"
    KV_SA_ACCESS_KEY="sa-accessKey"
    KV_CA_ACCESS_KEY="cdb-accessKey"
    KV_KUBE_CONFIG="kube-config"
    SA_SHARE="perf"
    PORTAL_DNS="${PREFIX_PERF}"
    WORK_SPACE="${PREFIX_PERF}la"
    SERVICE_PRINCIPAL="${PREFIX}"
    PPE_SERVICE_PRINCIPAL="${PREFIX_PERF}ppesp"
    KVURL="https://${KEYVAULT}.vault.azure.net/"

    if [[ ! -z $CLOUD ]]; then
        az cloud set -n $CLOUD
    fi

    if [[ ! -z $SUBSCTIPTION ]]; then
        az account set -s $SUBSCTIPTION
    else
        SUBSCTIPTION=$(az account show --query "id" -o tsv)
    fi
}

function init_aks_group() {
    echo "init aks configs"
    AKS_RESOURCE_GROUP=$(az aks show -g $RESOURCE_GROUP -n $KUBERNETES_SEVICES --query nodeResourceGroup -o tsv)
    AGENTPOOL_MSI_CLIENT_ID=$(az aks show -n $KUBERNETES_SEVICES -g $RESOURCE_GROUP --query identityProfile.kubeletidentity.clientId -o tsv)
    AGENTPOOL_MSI_OBJECT_ID=$(az aks show -n $KUBERNETES_SEVICES -g $RESOURCE_GROUP --query identityProfile.kubeletidentity.objectId -o tsv)
    signalr_role_scope="/subscriptions/$SUBSCTIPTION"
    # --assignee-object-id and --fill-principal-name false keep this ARM-only: no Microsoft Graph call (az >= 2.72).
    signalr_role_assignment=$(az role assignment list --assignee-object-id "$AGENTPOOL_MSI_OBJECT_ID" --fill-principal-name false --role "SignalR Service Owner" --scope "$signalr_role_scope" --query "[0].id" -o tsv)
    if [[ -z "$signalr_role_assignment" ]]; then
        echo "grant aks-agent-pool-msi SignalR Service Owner"
        az role assignment create --role "SignalR Service Owner" --assignee-object-id "$AGENTPOOL_MSI_OBJECT_ID" --assignee-principal-type ServicePrincipal --scope "$signalr_role_scope"
    fi
    az aks get-credentials -g $RESOURCE_GROUP -n $KUBERNETES_SEVICES -a  --overwrite-existing
}

function throw_if_empty() {
    local name="$1"
    local value="$2"
    if [[ -z "$value" ]]; then
        echo "ERROR: Parameter $name cannot be empty." 1>&2
        exit -1
    fi
}

# Lets MCP clients call the portal's /mcp endpoint on behalf of the signed-in user:
# exposes the delegated scope api://<appId>/user_impersonation on the portal's own app registration
# and lists VS Code's public client ID there as an authorized client ("Expose an API" > "Authorized
# client applications"), so users get no consent prompt. Nothing is changed on VS Code's app. Idempotent.
function enable_mcp_auth() {
    local app_id=$1
    local mcp_resource_url=${2:-}
    if [[ -n "$mcp_resource_url" && ! "$mcp_resource_url" =~ ^https://[^/?#[:space:]@]+(/[^?#[:space:]]*)?$ ]]; then
        echo "ERROR: MCP resource URL must be an absolute HTTPS URL without a query or fragment." >&2
        return 1
    fi
    local scope="user_impersonation"
    local vscode_client_id="aebc6443-996d-45c2-90f0-388ff96faa56"
    local graph="$(az cloud show --query endpoints.microsoftGraphResourceId -o tsv | sed 's:/*$::')/v1.0"
    local object_id=$(az ad app show --id $app_id --query id -o tsv)
    throw_if_empty "application object id of $app_id" $object_id
    local app=$(az rest --method GET --uri "$graph/applications/$object_id?\$select=identifierUris,api")
    throw_if_empty "application $app_id" "$app"
    if [[ -n "$mcp_resource_url" ]]; then
        echo "register MCP resource identifier $mcp_resource_url with v2 access tokens"
        app=$(echo "$app" | jq --arg uri "$mcp_resource_url" \
            '.identifierUris = ((.identifierUris // []) + [$uri] | unique) | .api.requestedAccessTokenVersion = 2')
        local resource_body
        resource_body=$(echo "$app" | jq '{identifierUris, api: {requestedAccessTokenVersion: 2}}')
        az rest --method PATCH --uri "$graph/applications/$object_id" --headers Content-Type=application/json --body "$resource_body"
    else
        echo "MCP resource URL not supplied. For Entra MCP sign-in, rerun enable_mcp.sh with --resource-url set to the exact resource in the portal's MCP metadata."
    fi
    local scope_id=$(echo "$app" | jq -r --arg s $scope '.api.oauth2PermissionScopes[] | select(.value == $s) | .id')

    if [[ -z $scope_id ]]; then
        echo "expose scope api://$app_id/$scope"
        scope_id=$(cat /proc/sys/kernel/random/uuid)
        local body=$(echo "$app" | jq --arg id $scope_id --arg s $scope --arg uri "api://$app_id" '{
            identifierUris: ((.identifierUris - [$uri]) + [$uri]),
            api: { oauth2PermissionScopes: (.api.oauth2PermissionScopes + [{
                id: $id, value: $s, type: "User", isEnabled: true,
                adminConsentDisplayName: "Access the perf portal",
                adminConsentDescription: "Allows the app to use the perf portal on behalf of the signed-in user.",
                userConsentDisplayName: "Access the perf portal",
                userConsentDescription: "Allows the app to use the perf portal on your behalf."
            }]) }
        }')
        az rest --method PATCH --uri "$graph/applications/$object_id" --headers Content-Type=application/json --body "$body"
    fi

    # The scope must exist before an application can be pre-authorized for it.
    echo "pre-authorize VS Code for api://$app_id/$scope"
    local body=$(echo "$app" | jq --arg id $scope_id --arg c $vscode_client_id '{
        api: { preAuthorizedApplications: ([.api.preAuthorizedApplications[] | select(.appId != $c)] + [{
            appId: $c, delegatedPermissionIds: [$id]
        }]) }
    }')
    az rest --method PATCH --uri "$graph/applications/$object_id" --headers Content-Type=application/json --body "$body"
}

function replace() {
    local placeholder=$1
    local content=$2
    content=$(echo "${content////\\/}")
    sed -e "s/$placeholder/$content/g"
}
