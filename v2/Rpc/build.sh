#!/bin/bash

dotnet restore

./generate_protos.sh

dotnet build
