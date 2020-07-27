#!/bin/bash
set -e
trap "exit" INT

 kubectl apply -f ./application/portal.yaml

