@echo off
:: Environment configurations

if [%1] == [] (
    if exist %PKGSRC_DIR%\versioninfo.txt (
        set /P BSP_VERSION=< %PKGSRC_DIR%\versioninfo.txt
    ) else (
        call :SET_VERSION 10.0.0.0
        echo. %PKGSRC_DIR%\versioninfo.txt not found. Defaulting to 10.0.0.0
    )
) else (
    REM TODO Insert version format validation
    call :SET_VERSION %1
)

if defined SIGNTOOL_OEM_SIGN (
    set PROMPT=IoTCoreShellv%IOT_ADDON_VERSION% %BSP_ARCH% %BSP_VERSION% Retail$_$P$G
    TITLE IoTCoreShellv%IOT_ADDON_VERSION% %BSP_ARCH% %BSP_VERSION% Retail
) else (
    set PROMPT=IoTCoreShellv%IOT_ADDON_VERSION% %BSP_ARCH% %BSP_VERSION%$_$P$G
    TITLE IoTCoreShellv%IOT_ADDON_VERSION% %BSP_ARCH% %BSP_VERSION%
)
exit /b 0

:SET_VERSION
set BSP_VERSION=%1
echo %BSP_VERSION%> %PKGSRC_DIR%\versioninfo.txt
exit /b 0