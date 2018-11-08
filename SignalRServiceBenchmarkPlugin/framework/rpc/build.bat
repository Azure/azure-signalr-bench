@echo off

dotnet restore
CALL generate_protos.bat

FOR /F "tokens=* USEBACKQ" %%F IN (`echo %cd%`) do (SET pwd=%%F)

cd ../master
dotnet build

cd %pwd%

cd ../slave
dotnet build
cd %pwd%
