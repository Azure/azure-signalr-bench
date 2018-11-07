#!/bin/sh
. ./env.sh

send_mail()
{
   local html_summary_path=$1
   echo "$html_summary_path"
   local subject=${subject_prefix}${result_dir}
   local server_ip
   if [ "$nginx_server_dns" != "" ]
   then
      server_ip=$nginx_server_dns
   else
      server_ip=`ifconfig eth0 | grep "inet "|awk '{print $2}'|awk -F : '{print $2}'`
   fi
   local href=""
   local tmp=""
   local curr_dir=`echo ${html_summary_path}|awk -F "NginxRoot/" '{print $2}'`
   echo $curr_dir
   html_href="http://${server_ip}:8000/$curr_dir"
   echo "Check the result from: $html_href"
   cat << EOF > /tmp/send_mail.txt
Performance result: $html_href
EOF
   if [ "$BUILD_URL" != "" ]
   then
   cat << EOF >> /tmp/send_mail.txt
More details: $BUILD_URL/console
EOF
   fi
   cat << EOF >> /tmp/send_mail.txt

Auto generated mail. Never reply it.
EOF
   if [ "$Email" != "" ]
   then
      echo "Email configuration does not support yet!"
      sendmail $Email < /tmp/send_mail.txt
   fi
}

if [ $# -ne 1 ]
then
   echo "Specify folder"
   exit 1
fi
send_mail $*
