@call run.cmd restore "'-Project=linker\Mono.Linker.csproj'" "'-Configuration=netcore_Debug'" %*
@call run.cmd restore "'-Project=cecil\Mono.Cecil.csproj'" "'-Configuration=netstandard_Debug'" %*
@exit /b %ERRORLEVEL%
