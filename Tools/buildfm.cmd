@echo off

goto START

:Usage
echo Usage: buildfm [oem/bsp] [bspname]
echo    [oem/bsp] ....... Specify oem to build oemfm/oemcommonfm files, bsp for bsp
echo    [bspname] ....... Mandatory for bsp; not required for oem
echo    [/?]      ....... Displays this usage string.
echo    Example:
echo        buildfm oem
echo        buildfm bsp Rpi2

echo Existing BSPs are
dir /b /AD %BSPSRC_DIR%
exit /b 1

:START
if [%1] == [/?] goto Usage
if [%1] == [] goto Usage

if not exist %BLD_DIR%\InputFMs ( mkdir %BLD_DIR%\InputFMs )

if /I [%1] ==[oem] (
    echo Exporting OEMFM files
    powershell -Command "(gc %PKGSRC_DIR%\OEMFM.xml) -replace '%%PKGBLD_DIR%%', '%PKGBLD_DIR%' -replace '%%OEM_NAME%%', '%OEM_NAME%' | Out-File %BLD_DIR%\InputFMs\OEMFM.xml -Encoding utf8"
    powershell -Command "(gc %COMMON_DIR%\Packages\OEMCommonFM.xml) -replace '%%PKGBLD_DIR%%', '%PKGBLD_DIR%' -replace '%%OEM_NAME%%', '%OEM_NAME%' | Out-File %BLD_DIR%\InputFMs\OEMCommonFM.xml -Encoding utf8"
    powershell -Command "(gc %IOTADK_ROOT%\Templates\OEMFMFileList.xml) -replace 'OEM_NAME', '%OEM_NAME%'  -replace 'ARCH', '%ARCH_CAP%' | Out-File %BLD_DIR%\InputFMs\OEMFMFileList.xml -Encoding utf8"

    echo Running Feature merger for OEMFMFileList.xml
    FeatureMerger %BLD_DIR%\InputFMs\OEMFMFileList.xml %PKGBLD_DIR% %BSP_VERSION% %BLD_DIR%\MergedFMs /InputFMDir:%BLD_DIR%\InputFMs /Languages:en-us /Resolutions:1024x768 /ConvertToCBS /variables:_cputype=%BSP_ARCH%;buildtype=fre;releasetype=production > %BLD_DIR%\buildfm_oem.log

) else if /I [%1] == [bsp] (
    if [%2] == [] goto Usage
    if /i not exist %BSPSRC_DIR%\%2 (
        echo Error : %2 does not exist
        goto Usage
    )
    echo. Exporting %2 FM files
    powershell -Command "(gc %BSPSRC_DIR%\%2\Packages\%2FM.xml) -replace '%%PKGBLD_DIR%%', '%PKGBLD_DIR%' -replace '%%OEM_NAME%%', '%OEM_NAME%' -replace '%%BSPPKG_DIR%%', '%BSPPKG_DIR%' -replace '%%MSPKG_DIR%%', '%MSPKG_DIR%' | Out-File %BLD_DIR%\InputFMs\%2FM.xml -Encoding utf8"
    powershell -Command "(gc %IOTADK_ROOT%\Templates\BSPFMFileList.xml) -replace 'OEM_NAME', '%OEM_NAME%' -replace 'BSP', '%2' -replace 'ARCH', '%ARCH_CAP%' | Out-File %BLD_DIR%\InputFMs\%2FMFileList.xml -Encoding utf8"

    echo. Running Feature merger %2FMFileList.xml
    FeatureMerger %BLD_DIR%\InputFMs\%2FMFileList.xml %PKGBLD_DIR% %BSP_VERSION% %BLD_DIR%\MergedFMs /InputFMDir:%BLD_DIR%\InputFMs /Languages:en-us /Resolutions:1024x768 /ConvertToCBS /variables:_cputype=%BSP_ARCH%;buildtype=fre;releasetype=production > %BLD_DIR%\buildfm_bsp_%2.log

) else ( goto Usage )

del %PKGBLD_DIR%\*.spkg >nul 2>nul
del %PKGBLD_DIR%\*.merged.txt >nul 2>nul

exit /b
