@echo off
chcp 65001 >nul
title Comprobador de Móvil Android - AsesinoDelAire
cls
echo ===================================================================
echo     COMPROBADOR DE DISPOSITIVOS ANDROID CONECTADOS (ADB)
echo ===================================================================
echo.

set ADB_PATH="C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"

if not exist %ADB_PATH% (
    set ADB_PATH="C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
)

if not exist %ADB_PATH% (
    set ADB_PATH=adb
)

echo Verificando dispositivos USB conectados con ADB...
echo.
%ADB_PATH% devices -l
echo.
echo ===================================================================
echo EXPLICACIÓN DE RESULTADOS:
echo.
echo 1. Si aparece tu movil con "device":
echo    ^--> ¡TODO LISTO! Tu movil esta perfectamente conectado y reconocido.
echo.
echo 2. Si aparece con "unauthorized":
echo    ^--> Mira la pantalla del movil y pulsa "Permitir siempre la depuración USB".
echo.
echo 3. Si la lista esta vacia:
echo    ^--> Conecta el movil por USB.
echo    ^--> En el movil: Ajustes ^> Opciones de desarrollador ^> Activa "Depuracion USB".
echo    ^--> En Xiaomi / Redmi: Activa tambien "Instalar via USB" y
echo         "Depuracion USB (Ajustes de seguridad)".
echo    ^--> Selecciona "Transferencia de archivos" en la notificacion USB del movil.
echo ===================================================================
echo.
pause
