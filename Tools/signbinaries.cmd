@echo off

goto :START

:Usage
echo Usage: signbinaries [bsp/ppkg/all] [dir]
echo    bsp  .................. Signs all sys/dll files
echo    ppkg .................. Signs all ppkg files
echo    all  .................. Signs all files
echo    dir  .................. Directory where the files are present
echo    [/?]...................... Displays this usage string.
echo    Example:
echo        signbinaries bsp
echo        signbinaries all

exit /b 1

:START

REM Input validation
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage
if [%2] == [] goto Usage
if not exist "%2%" goto Usage

if /i [%1] == [all] (
    set SIGNFILES=dll sys ppkg
) else if /i [%1] == [bsp] (
    set SIGNFILES=sys dll
) else if /i [%1] == [ppkg] (
    set SIGNFILES=ppkg
) else goto Usage
if exist "%BLD_DIR%\signlog.txt" (del "%BLD_DIR%\signlog.txt")

for %%A in (%SIGNFILES%) do (
    echo. Signing %%A files in %2
    for /f "delims=" %%i in ('dir /s /b %2\*.%%A') do (
        echo.   Signing %%i
        call sign.cmd %%i >> %BLD_DIR%\signlog.txt
    )
)

exit /b 0