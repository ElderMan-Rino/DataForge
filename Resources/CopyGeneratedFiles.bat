@echo off
setlocal EnableDelayedExpansion

REM ============================================================
REM  DataForge - Copy Generated Files
REM
REM  [1] Dlls\Elder.Data.Runtime.dll + .pdb
REM        -> Assets\Plugins\RuntimeData\
REM
REM  [2] EditorScripts\*Baker.cs
REM        -> Assets\Scripts\Editor\Data\
REM
REM  [3] BlobLoader\GeneratedBlobLoader.cs
REM        -> Assets\Scripts\Game\Data\Generated\
REM
REM  [4] Data\*.bytes
REM        -> Assets\Editor\EditorAssets\EditorData\
REM
REM  [SKIP] GameData, SharedDTO, Enums, Resolvers �� DLL�� ����
REM  [SKIP] UnityScripts �� ���
REM ============================================================

set "DATAFORGE_OUTPUT=%~dp0"
if "%DATAFORGE_OUTPUT:~-1%"=="\" set "DATAFORGE_OUTPUT=%DATAFORGE_OUTPUT:~0,-1%"

set "UNITY_ROOT=D:\Programming\SkillTrial01_Unity"
if not "%~1"=="" set "UNITY_ROOT=%~1"

REM -----------------------------------------------
REM  Source paths
REM -----------------------------------------------
set "SRC_DLLS=%DATAFORGE_OUTPUT%\Dlls"
set "SRC_EDITOR_SCRIPTS=%DATAFORGE_OUTPUT%\EditorScripts"
set "SRC_DATA=%DATAFORGE_OUTPUT%\Data"
set "SRC_BLOB_LOADER=%DATAFORGE_OUTPUT%\BlobLoader"

REM -----------------------------------------------
REM  Destination paths
REM -----------------------------------------------
set "DST_DLLS=%UNITY_ROOT%\Assets\Plugins\RuntimeData"
set "DST_EDITOR_CS=%UNITY_ROOT%\Assets\Scripts\Editor\Data"
set "DST_UNITY_SCRIPTS=%UNITY_ROOT%\Assets\Scripts\Game\Data\Generated"
set "DST_EDITOR_BYTES=%UNITY_ROOT%\Assets\Editor\EditorAssets\EditorData"

set "GENERATED_DLL=Elder.Data.Runtime.dll"
set "GENERATED_PDB=Elder.Data.Runtime.pdb"

echo.
echo ============================================================
echo  DataForge Copy Start
echo ============================================================
echo  [Source] %DATAFORGE_OUTPUT%
echo  [Unity]  %UNITY_ROOT%
echo ============================================================
echo.

if not exist "%UNITY_ROOT%" (
    echo [ERROR] Unity project path not found: %UNITY_ROOT%
    goto :ERROR
)

set COPY_COUNT=0
set SKIP_COUNT=0
set ERROR_COUNT=0

REM ============================================================
REM  [1] Dlls\Elder.Data.Runtime.dll + .pdb
REM        -> Assets\Plugins\RuntimeData\
REM ============================================================
echo [1/4] DLL copy
echo       %SRC_DLLS% -^> %DST_DLLS%
echo.

if exist "%SRC_DLLS%" (
    if not exist "%DST_DLLS%" mkdir "%DST_DLLS%"

    if exist "%SRC_DLLS%\%GENERATED_DLL%" (
        xcopy "%SRC_DLLS%\%GENERATED_DLL%" "%DST_DLLS%\" /Y /Q
        if errorlevel 1 (
            echo   [WARN] %GENERATED_DLL%
            set /a ERROR_COUNT+=1
        ) else (
            echo   [OK]   %GENERATED_DLL%
            set /a COPY_COUNT+=1
        )
    ) else (
        echo   [SKIP] %GENERATED_DLL% not found
        set /a SKIP_COUNT+=1
    )

    if exist "%SRC_DLLS%\%GENERATED_PDB%" (
        xcopy "%SRC_DLLS%\%GENERATED_PDB%" "%DST_DLLS%\" /Y /Q
        if errorlevel 1 (
            echo   [WARN] %GENERATED_PDB%
            set /a ERROR_COUNT+=1
        ) else (
            echo   [OK]   %GENERATED_PDB%
            set /a COPY_COUNT+=1
        )
    ) else (
        echo   [SKIP] %GENERATED_PDB% not found
        set /a SKIP_COUNT+=1
    )
) else (
    echo   [SKIP] Dlls folder not found
    set /a SKIP_COUNT+=1
)
echo.

REM ============================================================
REM  [2] EditorData\*Baker.cs -> Assets\Scripts\Editor\Data\
REM ============================================================
echo [2/4] EditorScripts Baker scripts
echo       %SRC_EDITOR_SCRIPTS% -^> %DST_EDITOR_CS%
echo.

if exist "%SRC_EDITOR_SCRIPTS%" (
    if not exist "%DST_EDITOR_CS%" mkdir "%DST_EDITOR_CS%"
    del /Q "%DST_EDITOR_CS%\*Baker.cs" 2>nul

    set FILE_FOUND=0
    for %%F in ("%SRC_EDITOR_SCRIPTS%\*Baker.cs") do (
        if exist "%%F" (
            xcopy "%%F" "%DST_EDITOR_CS%\" /Y /Q
            if errorlevel 1 (
                echo   [WARN] %%~nxF
                set /a ERROR_COUNT+=1
            ) else (
                echo   [OK]   %%~nxF
                set /a COPY_COUNT+=1
                set FILE_FOUND=1
            )
        )
    )
    if "!FILE_FOUND!"=="0" (
        echo   [SKIP] no *Baker.cs files found
        set /a SKIP_COUNT+=1
    )
) else (
    echo   [SKIP] EditorScripts folder not found
    set /a SKIP_COUNT+=1
)
echo.

REM ============================================================
REM  [3] BlobLoader\GeneratedBlobLoader.cs
REM        -> Assets\Scripts\Game\Data\Generated\
REM ============================================================
echo [3/4] BlobLoader copy  (GeneratedBlobLoader.cs)
echo       %SRC_BLOB_LOADER% -^> %DST_UNITY_SCRIPTS%
echo.

if exist "%SRC_BLOB_LOADER%" (
    if not exist "%DST_UNITY_SCRIPTS%" mkdir "%DST_UNITY_SCRIPTS%"
    del /Q "%DST_UNITY_SCRIPTS%\GeneratedBlobLoader.cs" 2>nul

    set FILE_FOUND=0
    for %%F in ("%SRC_BLOB_LOADER%\*.cs") do (
        if exist "%%F" (
            xcopy "%%F" "%DST_UNITY_SCRIPTS%\" /Y /Q
            if errorlevel 1 (
                echo   [WARN] %%~nxF
                set /a ERROR_COUNT+=1
            ) else (
                echo   [OK]   %%~nxF
                set /a COPY_COUNT+=1
                set FILE_FOUND=1
            )
        )
    )
    if "!FILE_FOUND!"=="0" (
        echo   [SKIP] no .cs files found in BlobLoader
        set /a SKIP_COUNT+=1
    )
) else (
    echo   [SKIP] BlobLoader folder not found
    set /a SKIP_COUNT+=1
)
echo.

REM ============================================================
REM  [4] EditorData\*.bytes -> Assets\Editor\EditorAssets\EditorData\
REM ============================================================
echo [4/4] Data binary  (*.bytes)
echo       %SRC_DATA% -^> %DST_EDITOR_BYTES%
echo.

if exist "%SRC_DATA%" (
    if not exist "%DST_EDITOR_BYTES%" mkdir "%DST_EDITOR_BYTES%"
    set FILE_FOUND=0
    for %%F in ("%SRC_DATA%\*.bytes") do (
        if exist "%%F" (
            xcopy "%%F" "%DST_EDITOR_BYTES%\" /Y /Q
            if errorlevel 1 (
                echo   [WARN] %%~nxF
                set /a ERROR_COUNT+=1
            ) else (
                echo   [OK]   %%~nxF
                set /a COPY_COUNT+=1
                set FILE_FOUND=1
            )
        )
    )
    if "!FILE_FOUND!"=="0" (
        echo   [SKIP] no .bytes files found
        set /a SKIP_COUNT+=1
    )
) else (
    echo   [SKIP] Data folder not found
    set /a SKIP_COUNT+=1
)
echo.

REM -----------------------------------------------
REM  Summary
REM -----------------------------------------------
echo ============================================================
echo  Result
echo ============================================================
echo  Success : !COPY_COUNT! files
echo  Skipped : !SKIP_COUNT! items
echo  Error   : !ERROR_COUNT! items
echo ============================================================

if !ERROR_COUNT! gtr 0 (
    echo.
    echo [WARN] Some errors occurred. Check the log above.
    goto :END
)

echo.
echo All files copied successfully.
goto :END

:ERROR
echo.
echo [FAILED] Batch aborted.
echo.
pause
exit /b 1

:END
echo.
pause
exit /b 0