@echo off

goto :START

:Usage
echo Usage: signbinaries [bsp/all/ext] [dir]
echo    bsp  .................. Signs all sys/dll files
echo    all  .................. Signs all dll/sys/ppkg files
echo    ext  .................. Signs all .ext files (say cab / dll / sys / ppkg )
echo    dir  .................. Directory where the files are present
echo    [/?] .................. Displays this usage string.
echo    Example:
echo        signbinaries bsp
echo        signbinaries all
echo        signbinaries cab

exit /b 1

:START

REM Input validation
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage
if [%2] == [] goto Usage
if not exist "%2" goto Usage

if /i [%1] == [all] (
    set SIGNFILES=dll sys ppkg
) else if /i [%1] == [bsp] (
    set SIGNFILES=sys dll
) else (
    set SIGNFILES=%1
)
if exist "%PKGLOG_DIR%\signbinaries.log" (del "%PKGLOG_DIR%\signbinaries.log")

for %%A in (%SIGNFILES%) do (
    echo. Signing %%A files in %2
    for /f "delims=" %%i in ('dir /s /b %2\*.%%A') do (
        echo.   Signing %%i
        call sign.cmd %%i >> %PKGLOG_DIR%\signbinaries.log
    )
)

exit /b 0