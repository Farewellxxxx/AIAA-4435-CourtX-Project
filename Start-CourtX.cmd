@echo off
setlocal
set "playerDir=%~dp0Player\win-x64"
if not exist "%playerDir%\CourtX.exe" (
    echo CourtX.exe is missing. Pull the full main branch, including Player\win-x64.
    pause
    exit /b 1
)
start "" /D "%playerDir%" "%playerDir%\CourtX.exe" %*
