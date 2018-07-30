#!/bin/bash
. ./az_vm_instances_manage.sh

echo "---------------------------"
date
change_all_vm_sshd_port
echo "---------------------------"
date
