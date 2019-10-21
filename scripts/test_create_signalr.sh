#!/bin/bash
. ./az_signalr_service.sh
. ./ASRS.sh

function print_usage()
{
  echo ""
cat <<EOF
Usage:
    General:
    $(basename $0) <create|createFree|delete|createServerless|createOnProdWestUS2>

EOF
    exit 1
}

function login()
{
if [ "$env" == "dogfood" ]
then
  register_signalr_service_dogfood
  az_login_ASRS_dogfood
else
  az_login_signalr_dev_sub
fi
}

function create()
{
   login
   create_group_if_not_exist $group $location
   create_signalr_service $group $name "Basic_DS2" $unit
}

function createFree()
{
   local acs=`cat westus2_acs_rowkey.txt`
   login
   create_group_if_not_exist $group $location
   create_free_asrs_with_acs $group $name "$acs"
}

function createServerless()
{
   login
   create_group_if_not_exist $group $location
   create_serverless_signalr_service $group $name $location $unit
}

function createOnProdWestUS2()
{
   login
   create_group_if_not_exist $group $location
   env="prod"
   location="westus2"
   local separatedRedis
   local separatedRouteRedis
   local separatedAcs
   local separatedIngressVMSS
   if [ -e westus2_redis_rowkey.txt ]
   then
     separatedRedis=`cat westus2_redis_rowkey.txt`
   fi
   if [ -e westus2_route_redis_rowkey.txt ]
   then
     separatedRouteRedis=`cat westus2_route_redis_rowkey.txt`
   fi
   if [ -e westus2_acs_rowkey.txt ]
   then
     separatedAcs=`cat westus2_acs_rowkey.txt`
   fi
   if [ -e westus2_vm_set.txt ]
   then
     separatedIngressVMSS=`cat westus2_vm_set.txt`
   fi

   if [ "$separatedRedis" == "" ]; then
     echo "Missing tags file westus2_redis_rowkey.txt"
     return
   fi

   if [ "$separatedRouteRedis" == "" ]; then
     echo "Missing tags file westus2_route_redis_rowkey.txt"
     return
   fi

   if [ "$separatedAcs" == "" ]; then
     echo "Missing tags file westus2_acs_rowkey.txt"
     return
   fi

   if [ "$separatedIngressVMSS" == "" ]; then
     echo "Missing tags file westus2_vm_set.txt"
     return
   fi
   create_group_if_not_exist $group $location
   create_asrs_with_acs_redises $group $name "Basic_DS2" $unit $separatedRedis $separatedRouteRedis $separatedAcs $separatedIngressVMSS
}

function delete()
{
   login
   delete_signalr_service $name $group
}

function evalTest()
{
   echo evalTest
}

ACTION=
while [[ $# > 0 ]]; do
    key="$1"
    shift
    case "$key" in
        create)
          ACTION=$key
          shift
            ;;
        createFree)
          ACTION=$key
          shift
            ;;
        createOnProdWestUS2)
          ACTION=$key
          shift
            ;;
        delete)
          ACTION=$key
          shift
            ;;
        createServerless)
          ACTION=$key
          shift
            ;;
        evalTest)
          ACTION=$key
          shift
            ;;
    *)
            echo "Unknown parameter: $key" >&2
            print_usage
            ;;
    esac
done

if [[ -z $ACTION ]];then
    echo "Please specify at least one action"
    print_usage
fi

eval $ACTION
