@ECHO OFF
SETLOCAL
SET LOCAL_VER_FILE="%CD%\.dart-version"
SET GLOBAL_VER_FILE="%~dp0.dart-version"
IF EXIST %LOCAL_VER_FILE% (
    FOR /F "USEBACKQ" %%A IN (%LOCAL_VER_FILE%) DO (
        CALL "%~dp0\versions\%%A\dart-sdk\bin\dart.exe" %*
    )
) ELSE (
    FOR /F "USEBACKQ" %%A IN (%GLOBAL_VER_FILE%) DO (
        CALL "%~dp0\versions\%%A\dart-sdk\bin\dart.exe" %*
    )
)
ENDLOCAL
