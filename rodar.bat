@echo off
cd /d "%~dp0"
start "" http://localhost:5080
dotnet run --project src\OrganizaJob --urls http://0.0.0.0:5080
