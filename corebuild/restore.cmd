@if not defined _echo @echo off

REM restore.sh will bootstrap the cli and ultimately call "dotnet
REM restore". Dependencies of the linker will get restored as well.

@call %~dp0dotnet.cmd restore %~dp0linker.sln %*
@exit /b %ERRORLEVEL%
