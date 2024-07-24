#/bin/bash

#helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
#helm repo update

#helm show values prometheus-community/kube-prometheus-stack

#helm install prometheus prometheus-community/kube-prometheus-stack -f values.yml
helm upgrade prometheus prometheus-community/kube-prometheus-stack -f values.yml

#helm uninstall prometheus

