@echo off

set compiler="G:\Tools\Atlus Script Tools\AtlusScriptCompiler.exe"

echo "Building BF Files"

for %%f in (.//src//*.flow) do (
    %compiler% ./src/%%f -Compile -Out ./bin/%%~nf.BF -OutFormat V3BE -Library P5R -Encoding P5R
)