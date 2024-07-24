# Port forward a local port to the service port
kubectl port-forward svc/prometheus-operated 9001:9090 -n default

kubectl port-forward svc/prometheus-operator-grafana  9002:80 -n default


# Access the service using the forwarded port
curl http://localhost:<local-port>
