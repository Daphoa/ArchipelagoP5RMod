# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/ArchipelagoP5RMod/*" -Force -Recurse
dotnet publish "./ArchipelagoP5RMod.csproj" -c Release -o "$env:RELOADEDIIMODS/ArchipelagoP5RMod" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location