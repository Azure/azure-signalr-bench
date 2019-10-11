#!/bin/bash
. ./az_signalr_service.sh
. ./ASRS.sh

function print_usage()
{
  echo ""
cat <<EOF
Usage:
    General:
    $(basename $0) <create|createFree|delete|createServerless>

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
