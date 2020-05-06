@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0eng\common\Build.ps1""" -projects """%~dp0illink.sln""" -restore -build %*"
powershell -ExecutionPolicy ByPass -NoProfile -command "Set-Location %~dp0; & """%~dp0eng\dotnet.ps1""" ""tool run dotnet-format --check --verbosity diagnostic -f . --exclude src/analyzer,src/tuner,external"""
