@ECHO OFF

dotnet clean
dotnet restore --no-cache
dotnet build
