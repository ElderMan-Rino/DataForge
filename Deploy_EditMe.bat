@echo off
setlocal
title DataForge DLL Deployer

:: ==========================================
:: 1. 경로 설정 (여기를 실제 경로로 수정하세요)
:: ==========================================

:: DLL이 생성된 폴더 경로 (예: .\bin\Debug)
set SOURCE_PATH="C:\Users\Admin\Documents\DataForge\bin\Debug"

:: 유니티 프로젝트 내부 경로
set TARGET_PATH="C:\Users\Admin\Documents\MyUnityProject\Assets\Plugins\DataForge"

:: ==========================================
:: 2. 파일 복사 실행
:: ==========================================

echo [INFO] 배포를 시작합니다...
echo [FROM] %SOURCE_PATH%
echo [TO]   %TARGET_PATH%

:: 대상 폴더가 없으면 생성
if not exist %TARGET_PATH% (
mkdir %TARGET_PATH%
echo [INFO] 대상 폴더가 없어 새로 생성했습니다.
)

:: DLL 및 PDB(디버깅 정보) 파일 복사
:: /Y: 덮어쓰기 확인 생략
:: /D: 변경된 파일만 복사 (속도 향상)
xcopy %SOURCE_PATH%\*.dll %TARGET_PATH% /Y /D
xcopy %SOURCE_PATH%\*.pdb %TARGET_PATH% /Y /D

:: ==========================================
:: 3. 결과 확인
:: ==========================================

if %ERRORLEVEL% EQU 0 (
echo ------------------------------------------
echo [SUCCESS] 모든 DLL이 성공적으로 복사되었습니다!
echo ------------------------------------------
) else (
echo ------------------------------------------
echo [ERROR] 복사 중 문제가 발생했습니다. (에러 코드: %ERRORLEVEL%)
echo ------------------------------------------
)

pause