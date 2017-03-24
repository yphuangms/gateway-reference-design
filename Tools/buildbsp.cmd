@echo off

goto START

:Usage
echo Usage: buildbsp [bspname]/[all] [version]
echo    bspname................... build bsp directory
echo    all....................... build all bsp directories
echo        One of the above should be specified
echo    [version]................. Optional, Package version. If not specified, it uses BSP_VERSION
echo    [/?]...................... Displays this usage string.
echo    Example:
echo        buildbsp rpi2
echo        buildbsp rpi2 10.0.1.0
echo        buildbsp all
echo        buildbsp all 10.0.2.0

exit /b 1

:START
pushd
setlocal ENABLEDELAYEDEXPANSION

if not defined PKGBLD_DIR (
    echo Environment not defined. Call setenv
    exit /b 1
)
if not exist %PKGLOG_DIR% ( mkdir %PKGLOG_DIR% )

REM Input validation
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage

if /I [%1] == [all] (
    echo Sign all bsp binaries...
    call signbinaries.cmd bsp %BSPSRC_DIR%
    echo Building all bsp packages
    call buildpkg %BSPSRC_DIR%
    dir /b /AD %BSPSRC_DIR% > %PKGLOG_DIR%\bsplist.txt
    for /f "delims=" %%i in (%PKGLOG_DIR%\bsplist.txt) do (
       echo.  Running FeatureMerger for %%i
       call buildfm.cmd bsp %%i  > %PKGLOG_DIR%\buildfm_bsp_%%i.log
    )
) else if exist "%BSPSRC_DIR%\%1" (
    echo Sign %1 bsp binaries...
    call signbinaries.cmd bsp %BSPSRC_DIR%\%1
    echo Building %1 bsp packages
    call buildpkg %BSPSRC_DIR%\%1
    echo Running feature merger
    call buildfm bsp %1 > %PKGLOG_DIR%\buildfm_bsp_%1.log
) else (
    echo. %1 not found.
    echo. Available BSPs are
    dir /b %BSPSRC_DIR%
)

endlocal
popd
exit /b
