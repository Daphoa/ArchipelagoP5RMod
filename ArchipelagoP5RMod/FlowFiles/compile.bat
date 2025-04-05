@echo off

set compiler="G:\Tools\Atlus Script Tools\AtlusScriptCompiler.exe"

echo "Building BF Files"

for %%f in (.//src//*.flow) do (
    %compiler% ./src/%%f -Compile -Out ./bin/%%~nf.bf -OutFormat V3BE -Library P5R -Encoding P5R
    
    set destFile=./src/%%~nf.dst
    call echo %%destFile%%
    setlocal enableDelayedExpansion
    if exist !destFile! (
        for /f "delims=" %%x in (!destFile!) do set localDest=%%x
        set "localDest=!localDest:*/=/!"
        set dest="../P5REssentials!localDest!"
        call echo !dest!
        set dest=!dest!
        mv ./bin/%%~nf.bf !dest!
    )
    endlocal
)