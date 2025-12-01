@echo off
REM TwinCAT 코드 품질 검증 Pre-commit Hook
REM 자동 생성됨: 2025-11-20
REM
REM 이 Hook은 커밋 전에 자동으로 실행되어 TwinCAT 코드 품질을 검증합니다.
REM Critical 등급의 위반이 발견되면 커밋을 차단합니다.

echo ========================================
echo TwinCAT 코드 품질 검증 중...
echo ========================================

REM 변경된 파일만 검증 (증분 검증 모드)
dotnet twincat-qa validate --mode Incremental --fail-on-critical

REM 검증 결과 확인
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ========================================
    echo ❌ 품질 검증 실패
    echo ========================================
    echo.
    echo Critical 등급의 품질 위반이 발견되었습니다.
    echo 커밋을 차단합니다.
    echo.
    echo 다음 조치를 취하세요:
    echo   1. 위에 표시된 위반 사항을 확인하세요
    echo   2. 코드를 수정하여 위반을 해결하세요
    echo   3. 수정 후 다시 커밋을 시도하세요
    echo.
    echo 또는 다음 옵션을 고려하세요:
    echo   - Hook 우회 ^(권장하지 않음^): git commit --no-verify
    echo   - 전체 검증 보고서: dotnet twincat-qa validate --mode Full --output report.html
    echo.
    exit /b 1
)

echo.
echo ========================================
echo ✅ 품질 검증 통과
echo ========================================
echo.
echo 모든 품질 규칙을 준수합니다.
echo 커밋을 진행합니다...
echo.

exit /b 0
