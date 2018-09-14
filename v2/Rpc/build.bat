@echo off

REM download the grpc tools
dotnet restore

call generate_protos.bat

dotnet build
