@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "Set-Location %~dp0; & """%~dp0eng\dotnet.ps1""" ""tool restore"""
powershell -ExecutionPolicy ByPass -NoProfile -command "Set-Location %~dp0; & """%~dp0eng\dotnet.ps1""" ""tool run dotnet-format illink.sln --verbosity diagnostic --fix-whitespace --fix-style warn --exclude src/analyzer src/tuner external %*"""
