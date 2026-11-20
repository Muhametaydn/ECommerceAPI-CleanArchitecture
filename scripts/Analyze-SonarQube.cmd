@echo off
setlocal

if "%~1"=="" (
    echo Usage: scripts\Analyze-SonarQube.cmd SONAR_TOKEN
    exit /b 2
)

pushd "%~dp0.."
if errorlevel 1 exit /b 1

set "SONAR_SCAN_TOKEN=%~1"
set "SONAR_RUN_ID=%RANDOM%-%RANDOM%-%RANDOM%"
set "SONAR_RESULTS=test-results\sonar\%SONAR_RUN_ID%"
set "SONAR_REPORTS=test-results/sonar/%SONAR_RUN_ID%"

echo [1/4] Starting SonarQube analysis...
dotnet sonarscanner begin /k:"ECommerceAPI" /d:sonar.host.url="http://localhost:9000" /d:sonar.token="%SONAR_SCAN_TOKEN%" /d:sonar.cs.opencover.reportsPaths="%SONAR_REPORTS%/**/coverage.opencover.xml" /d:sonar.cs.vstest.reportsPaths="%SONAR_REPORTS%/**/*.trx"
if errorlevel 1 goto failed

echo [2/4] Building the solution...
dotnet build ECommerceAPI.sln --no-incremental
if errorlevel 1 goto failed

echo [3/4] Running all test projects and collecting coverage...
dotnet test ECommerceAPI.sln --no-build --settings coverage.runsettings --collect:"XPlat Code Coverage" --logger trx --results-directory "%SONAR_RESULTS%"
if errorlevel 1 goto failed

dir /b /s "%SONAR_RESULTS%\coverage.opencover.xml" >nul 2>&1
if errorlevel 1 (
    echo ERROR: No OpenCover report was generated. Analysis was not uploaded.
    goto failed
)

echo [4/4] Uploading analysis and coverage...
dotnet sonarscanner end /d:sonar.token="%SONAR_SCAN_TOKEN%"
if errorlevel 1 goto failed

echo Done. Reports: %SONAR_RESULTS%
echo Dashboard: http://localhost:9000/summary/overall?id=ECommerceAPI
echo Coverage and Quality Gate results will appear after server processing.
popd
exit /b 0

:failed
echo Analysis stopped. Fix the error above and run this script again.
popd
exit /b 1
