@echo off
chcp 65001 >nul
title Instalador de Soporte Android - Unity 6000.6.0f1
cls
echo ===================================================================
echo     INSTALADOR DE SOPORTE ANDROID PARA UNITY 6000.6.0f1
echo ===================================================================
echo.

set INSTALLER="C:\Users\REIKO\AppData\Roaming\UnityHub\downloads\UnitySetup-Android-Support-for-Editor-6000.6.0f1.exe"
set DESTINATION="C:\Program Files\Unity\Hub\Editor\6000.6.0f1"

if not exist %INSTALLER% (
    echo [ERROR] No se encontro el instalador descargado en:
    echo %INSTALLER%
    echo Puedes instalarlo desde Unity Hub ^> Installs ^> Unity 6000.6.0f1 ^> Add modules ^> Android Build Support.
    pause
    exit /b
)

echo El archivo instalador oficial ya esta descargado (1.3 GB).
echo Se va a solicitar permisos de Administrador para instalarlo en:
echo %DESTINATION%
echo.
echo NOTA: Si tienes Unity Editor abierto, cierralo primero para que no haya archivos bloqueados.
echo.
pause

powershell -Command "Start-Process '%INSTALLER%' -ArgumentList '/S', '/D=%DESTINATION%' -Verb RunAs -Wait"

echo.
echo ===================================================================
echo ¡Proceso de instalacion finalizado!
echo Abre Unity Editor y verifica en File ^> Build Settings que Android ya esta disponible.
echo ===================================================================
echo.
pause
