@echo off

REM Settings are applied on every boot, in case it gets overrwritten during update
REM reg query HKLM\Software\IoT /v RecoveryBcdEdit >nul 2>&1

REM if %errorlevel% == 1 (
    bcdedit /set bootstatuspolicy IgnoreShutdownFailures
    bcdedit /set recoveryenabled yes
    bcdedit /set recoverysequence {a5935ff2-32ba-4617-bf36-5ac314b3f9bf}
REM    reg add HKLM\Software\IoT /v RecoveryBcdEdit /t REG_DWORD /d 1 /f >nul 2>&1
REM )
