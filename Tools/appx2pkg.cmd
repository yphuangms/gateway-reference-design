:: Run setenv before running this script
:: This script creates the folder structure and copies the template files for a new package
@echo off

goto START

:Usage
echo Usage: appx2pkg input.appx [CompName.SubCompName]
echo    input.appx.............. Required, input .appx file
echo    CompName.SubCompName.... Optional, default is Appx.input
echo    [/?].................... Displays this usage string.
echo    Example:
echo        appx2pkg C:\test\sample_1.0.0.0_arm.appx
exit /b 1

:START

setlocal ENABLEDELAYEDEXPANSION

if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage
if not [%~x1] == [.appx] goto Usage
set LONG_NAME=%~n1
set "FILE_PATH=%~dp1"

for /f "tokens=1,2,3 delims=_" %%i in ("%LONG_NAME%") do (
    set FILE_NAME=%%i
    set FILE_Version=%%j
    set FILE_ARCH=%%k
)

if [%2] == [] (
    set COMP_NAME=Appx
    set SUB_NAME=%FILE_NAME%
) else (
    for /f "tokens=1,2 delims=." %%i in ("%2") do (
        set COMP_NAME=%%i
        set SUB_NAME=%%j
    )
)

REM Start processing command
REM Get Appx dependencies
if exist "%FILE_PATH%\Dependencies\%ARCH%" (
    set DEP_PATH=Dependencies\%ARCH%
    dir /b "%FILE_PATH%\Dependencies\%ARCH%\*.appx" > "%FILE_PATH%\appx_deplist.txt"
) else (
    set DEP_PATH=Dependencies
    dir /b "%FILE_PATH%\Dependencies\*.appx" > "%FILE_PATH%\appx_deplist.txt"
)

dir /b "%FILE_PATH%\*.cer" > "%FILE_PATH%\appx_cerlist.txt"
echo. Authoring %COMP_NAME%.%SUB_NAME%.pkg.xml
if exist "%FILE_PATH%\%COMP_NAME%.%SUB_NAME%.pkg.xml" (del "%FILE_PATH%\%COMP_NAME%.%SUB_NAME%.pkg.xml" )
call :CREATE_PKGFILE

echo. Authoring customizations.xml
if exist "%FILE_PATH%\customizations.xml" (del "%FILE_PATH%\customizations.xml" )
REM Get a new GUID for the Provisioning config file
powershell -Command "[System.Guid]::NewGuid().toString() | Out-File %PRODSRC_DIR%\uuid.txt -Encoding ascii"
set /p NEWGUID=<%PRODSRC_DIR%\uuid.txt
del %PRODSRC_DIR%\uuid.txt
call :CREATE_CUSTFILE

del "%FILE_PATH%\appx_cerlist.txt"
del "%FILE_PATH%\appx_deplist.txt"

endlocal
exit /b 0

:CREATE_PKGFILE
REM Printing the headers
call :PRINT_TEXT "<?xml version="1.0" encoding="utf-8" ?>"
call :PRINT_TEXT "<Package xmlns="urn:Microsoft.WindowsPhone/PackageSchema.v8.00""
echo          Owner="$(OEMNAME)" OwnerType="OEM" ReleaseType="Production" >> "%FILE_PATH%\%COMP_NAME%.%SUB_NAME%.pkg.xml"
call :PRINT_TEXT "         Platform="%BSP_ARCH%" Component="%COMP_NAME%" SubComponent="%SUB_NAME%">"
call :PRINT_TEXT "   <Components>"
call :PRINT_TEXT "      <OSComponent>"
call :PRINT_TEXT "         <Files>"
REM Printing script files inclusion
call :PRINT_TEXT "            <File Source="%COMP_NAME%.%SUB_NAME%.ppkg" "
echo                   DestinationDir="$(runtime.windows)\Provisioning\Packages" >> "%FILE_PATH%\%COMP_NAME%.%SUB_NAME%.pkg.xml"
call :PRINT_TEXT "                  Name="%COMP_NAME%.%SUB_NAME%.ppkg" />"

call :PRINT_TEXT "         </Files>"
call :PRINT_TEXT "      </OSComponent>"
call :PRINT_TEXT "   </Components>"
call :PRINT_TEXT "</Package>"
)

:CREATE_CUSTFILE
if not exist "%FILE_PATH%\appx_deplist.txt" (
    echo. error, file not found :%FILE_PATH%\appx_deplist.txt
    exit /b 1
)

REM Printing the headers
call :PRINT_TO_CUSTFILE "<?xml version="1.0" encoding="utf-8" ?>"
call :PRINT_TO_CUSTFILE "<WindowsCustomizations>"
call :PRINT_TO_CUSTFILE "  <PackageConfig xmlns="urn:schemas-Microsoft-com:Windows-ICD-Package-Config.v1.0">"
call :PRINT_TO_CUSTFILE "    <ID>{%NEWGUID%}</ID>"
call :PRINT_TO_CUSTFILE "    <Name>%SUB_NAME%Prov</Name>"
call :PRINT_TO_CUSTFILE "    <Version>1.0</Version>"
call :PRINT_TO_CUSTFILE "    <OwnerType>OEM</OwnerType>"
call :PRINT_TO_CUSTFILE "    <Rank>0</Rank>"
call :PRINT_TO_CUSTFILE "  </PackageConfig>"
call :PRINT_TO_CUSTFILE "  <Settings xmlns="urn:schemas-microsoft-com:windows-provisioning">"
call :PRINT_TO_CUSTFILE "    <Customizations>"
call :PRINT_TO_CUSTFILE "      <Common>"
REM Printing Certificates
call :PRINT_TO_CUSTFILE "        <Certificates>"
call :PRINT_TO_CUSTFILE "          <RootCertificates>"
for /f "useback delims=" %%A in ("%FILE_PATH%\appx_cerlist.txt") do (
    call :PRINT_TO_CUSTFILE "            <RootCertificate CertificateName="%%~nA" Name="%%~nA">"
    call :PRINT_TO_CUSTFILE "              <CertificatePath>%%A</CertificatePath>"
    call :PRINT_TO_CUSTFILE "            </RootCertificate>"
)
call :PRINT_TO_CUSTFILE "          </RootCertificates>"
call :PRINT_TO_CUSTFILE "        </Certificates>"

REM Printing APP Install
call :PRINT_TO_CUSTFILE "        <UniversalAppInstall>"
call :PRINT_TO_CUSTFILE "          <UserContextApp>"
call :PRINT_TO_CUSTFILE "            <Application PackageFamilyName="%SUB_NAME%_SIGNATURE" Name="%SUB_NAME%_SIGNATURE">"
call :PRINT_TO_CUSTFILE "              <ApplicationFile>%LONG_NAME%.appx</ApplicationFile>"
REM Printing Dependencies
call :PRINT_TO_CUSTFILE "              <DependencyAppxFiles>"
for /f "useback delims=" %%A in ("%FILE_PATH%\appx_deplist.txt") do (
    call :PRINT_TO_CUSTFILE "                <Dependency Name="%%A">%DEP_PATH%\%%A</Dependency>"
)
call :PRINT_TO_CUSTFILE "              </DependencyAppxFiles>"
call :PRINT_TO_CUSTFILE "              <DeploymentOptions>Force target application shutdown</DeploymentOptions>"
call :PRINT_TO_CUSTFILE "            </Application>"
call :PRINT_TO_CUSTFILE "          </UserContextApp>"
call :PRINT_TO_CUSTFILE "        </UniversalAppInstall>"

call :PRINT_TO_CUSTFILE "      </Common>"
call :PRINT_TO_CUSTFILE "    </Customizations>"
call :PRINT_TO_CUSTFILE "  </Settings>"
call :PRINT_TO_CUSTFILE "</WindowsCustomizations>"
)
exit /b 0

:PRINT_TEXT
for /f "useback tokens=*" %%a in ('%1') do set TEXT=%%~a
echo !TEXT! >> "%FILE_PATH%\%COMP_NAME%.%SUB_NAME%.pkg.xml"
exit /b

:PRINT_TO_CUSTFILE
for /f "useback tokens=*" %%a in ('%1') do set TEXT=%%~a
echo !TEXT! >> "%FILE_PATH%\customizations.xml"
exit /b