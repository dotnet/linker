@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "Set-Location %~dp0; & """%~dp0eng\dotnet.ps1""" ""tool restore"""
powershell -ExecutionPolicy ByPass -NoProfile -command "Set-Location %~dp0; & """%~dp0eng\dotnet.ps1""" ""tool run dotnet-format --verbosity diagnostic -f . --exclude src/analyzer,src/tuner,external %*"""
