@echo off
setlocal
rem ---------------------------------------------------------------------------
rem  Aviassembly - build the WebGL version (first run only) and play it on
rem  http://localhost:8000
rem
rem  Needs Unity 6000.0.69f1 with "WebGL Build Support", installed through
rem  Unity Hub, and you signed in to Unity Hub (Unity requires an activated
rem  licence to build). To use a different Unity location, set UNITY_PATH.
rem ---------------------------------------------------------------------------
cd /d "%~dp0"
set "PROJECT=%~dp0"
set "BUILD=%~dp0Builds\WebGL"
set "PORT=8000"

if exist "%BUILD%\index.html" if not "%1"=="rebuild" goto serve

if not defined UNITY_PATH set "UNITY_PATH=C:\Program Files\Unity\Hub\Editor\6000.0.69f1\Editor\Unity.exe"
if not exist "%UNITY_PATH%" (
  echo Could not find Unity at:
  echo   %UNITY_PATH%
  echo Install Unity 6000.0.69f1 with WebGL Build Support from Unity Hub,
  echo or set UNITY_PATH to your Unity.exe and run this again.
  pause
  exit /b 1
)

echo Building the WebGL version. The first build imports the whole project and
echo can take 15-40 minutes. Progress is written to build.log.
"%UNITY_PATH%" -batchmode -quit -buildTarget WebGL -projectPath "%PROJECT%." -executeMethod AviassemblyWebGLBuild.BuildFromCommandLine -buildPath "Builds/WebGL" -logFile "%~dp0build.log"
if not exist "%BUILD%\index.html" (
  echo.
  echo The build failed. Open build.log and look for lines containing "error".
  pause
  exit /b 1
)

:serve
echo.
echo Starting the game at http://localhost:%PORT%/  (close this window to stop)
start "" "http://localhost:%PORT%/"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\serve.ps1" -Root "%BUILD%" -Port %PORT%
pause
