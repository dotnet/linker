@call run.cmd restore "'-Project=linker\Mono.Linker.new.csproj'" %*
@call run.cmd restore "'-Project=cecil\Mono.Cecil.csproj'" "'-Configuration=netstandard_Debug'" %*
@exit /b %ERRORLEVEL%
