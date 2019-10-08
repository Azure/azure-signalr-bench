@ECHO OFF

set _PackTool=true

cd src\rpc
call generate_protos.bat
cd ..\..

@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0eng\common\Build.ps1""" -restore -build %*"
exit /b %ErrorLevel%
