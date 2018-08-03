#!/bin/bash

. ./az_vm_manage.sh

az_login_jenkins_sub

resource_group=honzhansea
vm_tmpl=hzbenchtmpl

deallocate_vm $resource_group $vm_tmpl

generalize_vm $resource_group $vm_tmpl

create_image $resource_group $vm_tmpl hzbenchimg
