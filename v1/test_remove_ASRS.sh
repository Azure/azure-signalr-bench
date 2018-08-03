. ./az_signalr_service.sh
. ./ASRS_env.sh

az_login_ASRS_dogfood

#delete_signalr_service $asrs_name $target_grp

delete_group $target_grp

unregister_signalr_service_dogfood
