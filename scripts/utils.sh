#!/bin/bash

function az_login_signalr_dev_sub() {
  set +x
  az login --service-principal \
            -u $AZURE_CLIENT_ID_INT \
            --password $AZURE_CLIENT_SECRET_INT \
            --tenant $AZURE_TENANT_ID_INT
  set -x
}

function az_login_ASRS_dogfood() {
  set +x
  az login --service-principal \
            -u $AZURE_CLIENT_ID_DF \
            --password $AZURE_CLIENT_SECRET_DF \
            --tenant $AZURE_TENANT_ID_DF
  set -x
}

function az_signalr_dev_credentials() {
  set +x
  local outputFile=$1
cat <<EOF > $outputFile
clientId: $AZURE_CLIENT_ID_INT
tenantId: $AZURE_TENANT_ID_INT
clientSecret: $AZURE_CLIENT_SECRET_INT
subscription: $AZURE_SUBSCRIPTION_ID_INT
EOF
  set -x
}