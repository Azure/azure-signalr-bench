#!/bin/bash

function parse_service_principal () (
    local content=$1
    local clientId=""
    local clientSecret=""
    local tenantId=""
    local subscription=""

    function trimAllWhiteSpace() {
        echo "$(echo -e "$1" | tr -d '[:space:]')"
    }

    for line in $content; do
        kvp=($(echo $line | tr ":" "\n"))
        case ${kvp[0]} in
            "clientId")
                clientId=$(trimAllWhiteSpace "${kvp[1]}")
                ;;
            "tenantId")
                tenantId=$(trimAllWhiteSpace "${kvp[1]}")
                ;;
            "clientSecret")
                clientSecret=$(trimAllWhiteSpace "${kvp[1]}")
                ;;
            "subscription")
                subscription=$(trimAllWhiteSpace "${kvp[1]}")
                ;;
            *)
                echo $"[Warning]: Not supported service pricipal part ${kvp[0]} ${kvp[1]}"
                ;;
        esac
    done

    echo "${clientId} ${clientSecret} ${tenantId} ${subscription}"
)

function az_login_signalr_dev_sub() {
  set +x
  local servicePrincipal=$sp_INT # hard-code here: generate_clean_resource_script generate the script using this function
  read clientId clientSecret tenantId subscription < <(parse_service_principal "$servicePrincipal")
  
  az login --service-principal \
            -u $clientId \
            --password $clientSecret \
            --tenant $tenantId
  set -x
}

function az_login_ASRS_dogfood() {
  set +x
  local servicePrincipal=$sp_DF
  read clientId clientSecret tenantId subscription < <(parse_service_principal "$servicePrincipal")

  az login --service-principal \
            -u $clientId \
            --password $clientSecret \
            --tenant $tenantId
  set -x
}

function az_signalr_dev_credentials() {
  set +x
  local outputFile=$1
  local servicePrincipal=$sp_INT
  read clientId clientSecret tenantId subscription < <(parse_service_principal "$servicePrincipal")
cat <<EOF > $outputFile
clientId: $clientId
tenantId: $tenantId
clientSecret: $clientSecret
subscription: $subscription
EOF
  set -x
}