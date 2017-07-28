@echo off

goto START

:Usage
echo Usage: buildrecovery [Product] [BuildType]
echo    ProductName....... Required, Name of the product to be created.
echo    BuildType......... Required, Retail/Test
echo    [/?]...................... Displays this usage string.
echo    Example:
echo        buildrecovery SampleA Test
echo        buildrecovery SampleA Retail

exit /b 1

:START
setlocal
REM Input validation
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage
if [%2] == [] goto Usage
if /I not [%2] == [Retail] ( if /I not [%2] == [Test] goto Usage )

if not defined PKGBLD_DIR (
    echo Environment not defined. Call setenv
    exit /b 1
)

if not defined FFUNAME ( set FFUNAME=Flash)
set OUTPUTDIR=%BLD_DIR%\%1\%2
set IMG_FILE=%OUTPUTDIR%\%FFUNAME%.ffu

if not exist "%IMG_FILE%" (
    echo Building the base FFU
    call buildimage %1 %2
)

if not exist "%IMG_FILE%" (
....REM File not found even after invoking buildimage. 
    echo.%CLRRED%Error: Building the base FFU failed.%CLREND%
)
set IMG_RECOVERY_FILE=%OUTPUTDIR%\%FFUNAME%_Recovery.ffu
echo Mounting %IMG_FILE% (this can take some time)..
call wpimage mount "%IMG_FILE%" > %OUTPUTDIR%\mountlog.txt

REM This will break if there is space in the user account (eg.C:\users\test acct\)
for /f "tokens=3,4,* skip=9 delims= " %%i in (%OUTPUTDIR%\mountlog.txt) do (
    if [%%i] == [Path:] (
        set MOUNT_PATH=%%j
    ) else if [%%i] == [Name:] (
        set DISK_DRIVE=%%j
    )
)

echo Mounted at %MOUNT_PATH% as %DISK_DRIVE%..
echo Extracting data wim
dism /Capture-Image /ImageFile:%MOUNT_PATH%\mmos\data.wim /CaptureDir:%MOUNT_PATH%Data\ /Name:"DATA" /Compress:max
echo Extracting MainOS wim, this can take a while too..
dism /Capture-Image /ImageFile:%MOUNT_PATH%\mmos\mainos.wim /CaptureDir:%MOUNT_PATH% /Name:"MainOS" /Compress:max

if exist %BSPSRC_DIR%\%BSP%\Packages\Recovery.WinPE\winpe.wim (
    echo Copying winpe.wim..
    copy %BSPSRC_DIR%\%BSP%\Packages\Recovery.WinPE\winpe.wim %MOUNT_PATH%\mmos
) else (
    echo.%CLRRED%Error:WinPE file not found. Recovery functionality will not work.%CLREND% 
)

echo %BSP_VERSION% > %MOUNT_PATH%\mmos\RecoveryImageVersion.txt

echo Unmounting %DISK_DRIVE%
wpimage dismount -physicaldrive %DISK_DRIVE% -imagepath %IMG_RECOVERY_FILE%
REM del %OUTPUTDIR%\mountlog.txt

endlocal
exit /b

:Error
endlocal
exit /b 1
