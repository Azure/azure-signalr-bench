#/bin/bash

helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

#helm install prometheus prometheus-community/kube-prometheus-stack -f values.yml --version 61.3.2
#helm upgrade prometheus prometheus-community/kube-prometheus-stack -f values.yml --version 61.3.2

helm uninstall prometheus

