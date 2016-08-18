@echo off
REM OEM Customization Script file

REM Enable Administrator User
net user Administrator p@ssw0rd /active:yes

if exist C:\OEMInstall\Provisioning\Enroll.ppkg (
    REM Applying Provisioning packages in order
    provtool C:\OEMInstall\Provisioning\Enroll.ppkg
    provtool C:\OEMInstall\Provisioning\ProSKU.ppkg
    REM Cleaning up Provisioning folder
    rmdir /S /Q C:\OEMInstall
)

if exist C:\OEMTools\InstallAppx.cmd (
    REM Run the Appx Installer. This will install the appx present in C:\OEMApps\
    call C:\OEMTools\InstallAppx.cmd
)


