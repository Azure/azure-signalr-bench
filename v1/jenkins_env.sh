__jenkins_env__="1"
sigbench_run_duration=$Duration
connection_string=$ConnectionString
connection_number=$ClientConnectionNumber
connection_concurrent=$ConcurrentConnectionNumber
send_number=$SendNumber
# verify jenkins input

# check UseWss
if [[ $connection_string = *"https://"* ]]
then
   use_https=1
else
   use_https=0
fi

