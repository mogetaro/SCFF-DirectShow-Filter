@echo off
set ROOT_DIR=%~dp0..\
pushd "%ROOT_DIR%"

if "%1"=="-sys" (
  rem System
  if "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    "%systemroot%\syswow64\regsvr32.exe" "bin\Debug_Win32\scff_dsf_Win32.ax"
    "%systemroot%\system32\regsvr32.exe" "bin\Debug_x64\scff_dsf_x64.ax"
  ) else (
    "%systemroot%\system32\regsvr32.exe" "bin\Debug_Win32\scff_dsf_Win32.ax"
  )
) else (
  rem Normal
  "tools\bin\regsvrex32.exe" "bin\Debug_Win32\scff_dsf_Win32.ax"
  if "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    "tools\bin\regsvrex64.exe" "bin\Debug_x64\scff_dsf_x64.ax"
  )
)

popd