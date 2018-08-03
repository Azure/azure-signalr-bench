. ./az_signalr_service.sh
. ./kubectl_utils.sh
. ./ASRS_env.sh

function test_create_signalr_service() {
  local rsg=$1
  local name=$2
  local sku=$3
  local unit=$4
  signalr_service=$(create_signalr_service $rsg $name $sku $unit)
  if [ "$signalr_service" == "" ]
  then
    echo "Fail to create SignalR Service"
    return
  else
    echo "Create SignalR Service ${signalr_service}"
  fi
  check_signalr_service_dns $rsg $name
  ConnectionString=$(query_connection_string $name $rsg)
  echo "Connection string: '$ConnectionString'"
  patch $name 5 4 4 4000 20000 
}
echo --------------------------------
date
echo --------------------------------
register_signalr_service_dogfood
az_login_ASRS_dogfood
#az_login_signalr_dev_sub
grps=`az group list -o json|jq .[].name|grep $target_grp`

if [ "$grps" != "" ]
then
  delete_group $target_grp
fi
az group create --name $target_grp --location $location

test_create_signalr_service $target_grp $asrs_name "Basic_DS2" $unit
echo --------------------------------
date
echo --------------------------------
