@ECHO OFF

set _PackTool=true

cd src\rpc
call generate_protos.bat
cd ..\..

call korebuild.cmd %*
