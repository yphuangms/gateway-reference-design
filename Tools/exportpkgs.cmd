@echo off

goto START

:Usage
echo Usage: exportpkgs [DestDir] [Product] [BuildType] [OwnerType]
echo    DestDir........... Required, Destination directory to export
echo    ProductName....... Required, Name of the product to be created.
echo    BuildType......... Required, Retail/Test
echo    OwnerType......... Optional, MS/OEM/ALL , default ALL

echo    [/?]...................... Displays this usage string.
echo    Example:
echo        exportpkgs C:\Temp SampleA Test OEM
echo        exportpkgs C:\Temp SampleA Retail ALL
echo Run this command only after a successful ffu creation. (See buildimage.cmd)

exit /b 1

:START
if not defined PKGBLD_DIR (
    echo Environment not defined. Call setenv
    exit /b 1
)
REM Input validation
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage
set WORK_DIR=%1
set OUTPUT=%WORK_DIR%\OCP

if not exist "%BLD_DIR%\%2\%3" ( goto Usage )
if not defined FFUNAME ( set FFUNAME=Flash)
if not exist "%BLD_DIR%\%2\%3\%FFUNAME%.FFU" (
    echo. %CLRRED% %BLD_DIR%\%2\%3\%FFUNAME%.FFU not found. Build the image before exporting.%CLREND%
    exit /b 1
)

if not exist "%BLD_DIR%\%2\%3\%FFUNAME%.UpdateInput.xml" (
    echo. %FFUNAME%.UpdateInput.xml not found. Build the image before exporting.
    exit /b 1
)
if not exist "%OUTPUT%" ( mkdir "%OUTPUT%" )
setlocal

if /I [%4] == [MS] (
    set MSPKG=1
) else if /I [%4] == [OEM] (
    set OEMPKG=1
) else (
    set MSPKG=1
    set OEMPKG=1
)
if exist ("%WORK_DIR%\packagelist.txt") del "%WORK_DIR%\packagelist.txt"

for /f "tokens=1,2,3 delims=<,> skip=5" %%A in (%BLD_DIR%\%2\%3\%FFUNAME%.UpdateInput.xml) do (
    if [%%C] == [] (
        REM echo. Nothing to do.
    ) else (
        REM echo. Found : %%C
        echo.%%C | findstr /C:"MSPackages" >nul && (
            if defined MSPKG (
                echo. [MS package] : %%C
                copy "%%C" "%OUTPUT%" >nul
                echo.%%C >> "%WORK_DIR%\packagelist.txt"
            )
        ) || (
            if defined OEMPKG (
                echo. [OEM package] : %%C
                copy "%%C" "%OUTPUT%" >nul
                echo.%%C >> "%WORK_DIR%\packagelist.txt"
            )
        )
    )
)
copy "%IOTADK_ROOT%\Templates\installupdates.cmd" %WORK_DIR%\installupdates.cmd >nul
echo. Exporting BSP DB
copy "%BLD_DIR%\%2\%3\%FFUNAME%.BSPDB.xml" %OUTPUT% >nul

findstr /C:"OwnerType" %OUTPUT%\%FFUNAME%.BSPDB.xml > %WORK_DIR%\pkgbspdb.txt
if exist %WORK_DIR%\pkgbsplist.txt (del %WORK_DIR%\pkgbsplist.txt)
for /f "tokens=3,8,9,10,11,13 delims=>= " %%A in (%WORK_DIR%\pkgbspdb.txt) do (
    if /I [%%B] == [Version] (
        echo %%~A.cab,%%~C >> %WORK_DIR%\pkgbsplist.txt
    ) else if /I [%%D] == [Version] (
        echo %%~A.cab,%%~E >> %WORK_DIR%\pkgbsplist.txt
    ) else (
          echo %%~A.cab,%%~F >> %WORK_DIR%\pkgbsplist.txt
    )
)
del %WORK_DIR%\pkgbspdb.txt

echo. Making BSP DB cab
call makecab %OUTPUT%\%FFUNAME%.BSPDB.xml %OUTPUT%\%FFUNAME%.BSPDB.cab >nul
echo. Signing BSP DB cab
call sign.cmd %OUTPUT%\%FFUNAME%.BSPDB.cab >nul
del %OUTPUT%\%FFUNAME%.BSPDB.xml
echo. Creating zip file
if exist %WORK_DIR%\OCP.zip (del %WORK_DIR%\OCP.zip )
powershell.exe -command "& { Add-Type -A 'System.IO.Compression.FileSystem'; [IO.Compression.ZipFile]::CreateFromDirectory('%OUTPUT%','%WORK_DIR%\OCP.zip'); }"

endlocal
exit /b

