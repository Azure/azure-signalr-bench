@ECHO OFF

set _PackTool=true

cd src\rpc
call build.bat
cd ..\..

cd src\appserver
call build.bat
cd ..\..

call korebuild.cmd %*
