@echo off
chcp 65001 > nul
setlocal EnableDelayedExpansion

REM ============================================================
REM  DataForge - 생성 파일 복사 배치파일
REM
REM  [복사 대상 매핑] (FileSourceCodeSaver 기준 폴더명)
REM
REM  [1] Dlls\*.dll, *.pdb
REM        -> Assets\Plugins\RuntimeData\
REM
REM  [2] EditorData\Blob*EditorData.cs  (SourceCategory.EditorData)
REM  [2] EditorData\*Baker.cs           (SourceCategory.EditorData)
REM        -> Assets\Scripts\Editor\Data\
REM
REM  [3] UnityScripts\*.cs              (SourceCategory.UnityScripts)
REM        -> Assets\Scripts\Game\Data\Generated\
REM
REM  [4] EditorData\*.bytes             (ExcelToMessagePackData 출력)
REM        -> Assets\Editor\EditorAssets\EditorData\
REM
REM  [복사 제외]
REM    Parser\        -> DLL 내부로 빌드됨, MainFramework 직접 사용 안 함
REM    GameData\      -> DLL 내부로 빌드됨, MainFramework 직접 사용 안 함
REM    Enums\         -> DLL 내부로 빌드됨, MainFramework 직접 사용 안 함
REM    Resolvers\     -> DLL 내부로 빌드됨, MainFramework 직접 사용 안 함
REM    sheets.json    -> DataForge 내부 레지스트리, Unity 불필요
REM
REM  [처리 방식] 최신 파일만 덮어쓰기 (/Y /D)
REM ============================================================

REM ──────────────────────────────────────────────
REM  ★ 경로 설정 (여기만 수정하세요)
REM ──────────────────────────────────────────────

REM DataForge OutputPath (Settings에서 지정한 경로)
set "DATAFORGE_OUTPUT=D:\Programming\DataForge\Resources"

REM Unity 프로젝트 루트
set "UNITY_ROOT=D:\Programming\SkillTrial01_Unity"

REM ──────────────────────────────────────────────
REM  인수로 경로를 받을 수도 있습니다
REM  사용법: CopyGeneratedFiles.bat [DataForge출력경로] [Unity루트경로]
REM ──────────────────────────────────────────────
if not "%~1"=="" set "DATAFORGE_OUTPUT=%~1"
if not "%~2"=="" set "UNITY_ROOT=%~2"

REM ──────────────────────────────────────────────
REM  소스 경로 정의 (DataForge OutputPath 기준)
REM ──────────────────────────────────────────────
set "SRC_DLLS=%DATAFORGE_OUTPUT%\Dlls"
set "SRC_EDITOR_DATA=%DATAFORGE_OUTPUT%\EditorData"
set "SRC_UNITY_SCRIPTS=%DATAFORGE_OUTPUT%\UnityScripts"

REM ──────────────────────────────────────────────
REM  대상 경로 정의 (Unity 프로젝트 기준)
REM ──────────────────────────────────────────────
set "DST_DLLS=%UNITY_ROOT%\Assets\Plugins\RuntimeData"
set "DST_EDITOR_CS=%UNITY_ROOT%\Assets\Scripts\Editor\Data"
set "DST_UNITY_SCRIPTS=%UNITY_ROOT%\Assets\Scripts\Game\Data\Generated"
set "DST_EDITOR_BYTES=%UNITY_ROOT%\Assets\Editor\EditorAssets\EditorData"

REM ──────────────────────────────────────────────
REM  헤더 출력
REM ──────────────────────────────────────────────
echo.
echo ============================================================
echo  DataForge 생성 파일 복사 시작
echo ============================================================
echo  [DataForge] %DATAFORGE_OUTPUT%
echo  [Unity]     %UNITY_ROOT%
echo ============================================================
echo.

REM ──────────────────────────────────────────────
REM  경로 유효성 검사
REM ──────────────────────────────────────────────
if not exist "%DATAFORGE_OUTPUT%" (
    echo [오류] DataForge 출력 경로를 찾을 수 없습니다:
    echo        %DATAFORGE_OUTPUT%
    goto :ERROR
)
if not exist "%UNITY_ROOT%" (
    echo [오류] Unity 프로젝트 경로를 찾을 수 없습니다:
    echo        %UNITY_ROOT%
    goto :ERROR
)

set COPY_COUNT=0
set SKIP_COUNT=0
set ERROR_COUNT=0

REM ============================================================
REM  [1] Dlls\*.dll, *.pdb  ->  Assets\Plugins\RuntimeData\
REM ============================================================
echo [1/4] DLL 복사
echo       %SRC_DLLS%
echo       -> %DST_DLLS%
echo.

if exist "%SRC_DLLS%" (
    if not exist "%DST_DLLS%" mkdir "%DST_DLLS%"
    set FILE_FOUND=0

    for %%E in (dll pdb) do (
        for %%F in ("%SRC_DLLS%\*.%%E") do (
            if exist "%%F" (
                xcopy "%%F" "%DST_DLLS%\" /Y /D /Q
                if errorlevel 1 (
                    echo   [경고] %%~nxF
                    set /a ERROR_COUNT+=1
                ) else (
                    echo   [완료] %%~nxF
                    set /a COPY_COUNT+=1
                    set FILE_FOUND=1
                )
            )
        )
    )
    if "!FILE_FOUND!"=="0" (
        echo   [건너뜀] .dll / .pdb 파일 없음
        set /a SKIP_COUNT+=1
    )
) else (
    echo   [건너뜀] Dlls 폴더 없음
    set /a SKIP_COUNT+=1
)
echo.

REM ============================================================
REM  [2] EditorData\Blob*EditorData.cs + *Baker.cs
REM      ->  Assets\Scripts\Editor\Data\
REM      (SourceCategory.EditorData 에 해당하는 .cs만 선별)
REM ============================================================
echo [2/4] EditorData 스크립트 복사 ^(Blob*EditorData.cs / *Baker.cs^)
echo       %SRC_EDITOR_DATA%
echo       -> %DST_EDITOR_CS%
echo.

if exist "%SRC_EDITOR_DATA%" (
    if not exist "%DST_EDITOR_CS%" mkdir "%DST_EDITOR_CS%"
    set FILE_FOUND=0

    for %%F in ("%SRC_EDITOR_DATA%\Blob*EditorData.cs") do (
        if exist "%%F" (
            xcopy "%%F" "%DST_EDITOR_CS%\" /Y /D /Q
            if errorlevel 1 (
                echo   [경고] %%~nxF
                set /a ERROR_COUNT+=1
            ) else (
                echo   [완료] %%~nxF
                set /a COPY_COUNT+=1
                set FILE_FOUND=1
            )
        )
    )

    for %%F in ("%SRC_EDITOR_DATA%\*Baker.cs") do (
        if exist "%%F" (
            xcopy "%%F" "%DST_EDITOR_CS%\" /Y /D /Q
            if errorlevel 1 (
                echo   [경고] %%~nxF
                set /a ERROR_COUNT+=1
            ) else (
                echo   [완료] %%~nxF
                set /a COPY_COUNT+=1
                set FILE_FOUND=1
            )
        )
    )

    if "!FILE_FOUND!"=="0" (
        echo   [건너뜀] 해당 파일 없음
        set /a SKIP_COUNT+=1
    )
) else (
    echo   [건너뜀] EditorData 폴더 없음
    set /a SKIP_COUNT+=1
)
echo.

REM ============================================================
REM  [3] UnityScripts\*.cs  ->  Assets\Scripts\Game\Data\Generated\
REM      (GeneratedBlobLoader.cs 등 SourceCategory.UnityScripts)
REM ============================================================
echo [3/4] UnityScripts 복사 ^(GeneratedBlobLoader.cs 등^)
echo       %SRC_UNITY_SCRIPTS%
echo       -> %DST_UNITY_SCRIPTS%
echo.

if exist "%SRC_UNITY_SCRIPTS%" (
    if not exist "%DST_UNITY_SCRIPTS%" mkdir "%DST_UNITY_SCRIPTS%"
    set FILE_FOUND=0

    for %%F in ("%SRC_UNITY_SCRIPTS%\*.cs") do (
        if exist "%%F" (
            xcopy "%%F" "%DST_UNITY_SCRIPTS%\" /Y /D /Q
            if errorlevel 1 (
                echo   [경고] %%~nxF
                set /a ERROR_COUNT+=1
            ) else (
                echo   [완료] %%~nxF
                set /a COPY_COUNT+=1
                set FILE_FOUND=1
            )
        )
    )
    if "!FILE_FOUND!"=="0" (
        echo   [건너뜀] .cs 파일 없음
        set /a SKIP_COUNT+=1
    )
) else (
    echo   [건너뜀] UnityScripts 폴더 없음
    set /a SKIP_COUNT+=1
)
echo.

REM ============================================================
REM  [4] EditorData\*.bytes  ->  Assets\Editor\EditorAssets\EditorData\
REM      (ExcelToMessagePackData 가 출력한 바이너리 데이터)
REM ============================================================
echo [4/4] EditorData 바이너리 복사 ^(*.bytes^)
echo       %SRC_EDITOR_DATA%
echo       -> %DST_EDITOR_BYTES%
echo.

if exist "%SRC_EDITOR_DATA%" (
    if not exist "%DST_EDITOR_BYTES%" mkdir "%DST_EDITOR_BYTES%"
    set FILE_FOUND=0

    for %%F in ("%SRC_EDITOR_DATA%\*.bytes") do (
        if exist "%%F" (
            xcopy "%%F" "%DST_EDITOR_BYTES%\" /Y /D /Q
            if errorlevel 1 (
                echo   [경고] %%~nxF
                set /a ERROR_COUNT+=1
            ) else (
                echo   [완료] %%~nxF
                set /a COPY_COUNT+=1
                set FILE_FOUND=1
            )
        )
    )
    if "!FILE_FOUND!"=="0" (
        echo   [건너뜀] .bytes 파일 없음
        set /a SKIP_COUNT+=1
    )
) else (
    echo   [건너뜀] EditorData 폴더 없음
    set /a SKIP_COUNT+=1
)
echo.

REM ──────────────────────────────────────────────
REM  결과 요약
REM ──────────────────────────────────────────────
echo ============================================================
echo  복사 완료 요약
echo ============================================================
echo  성공: !COPY_COUNT! 파일
echo  건너뜀: !SKIP_COUNT! 항목 ^(소스 없음^)
echo  오류: !ERROR_COUNT! 항목
echo ============================================================

if !ERROR_COUNT! gtr 0 (
    echo.
    echo [주의] 일부 항목에서 오류가 발생했습니다. 위 로그를 확인하세요.
    goto :END
)

echo.
echo 모든 파일이 성공적으로 복사되었습니다.
goto :END

:ERROR
echo.
echo [실패] 배치 실행이 중단되었습니다.
echo.
pause
exit /b 1

:END
echo.
pause
exit /b 0
