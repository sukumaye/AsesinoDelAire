@echo off
start "" "C:\Program Files\Google\Chrome\Application\chrome.exe" "%~dp0GUIA_CONEXION_MOVIL_Y_DRIVERS.html"
if %errorlevel% neq 0 (
    start "" "%~dp0GUIA_CONEXION_MOVIL_Y_DRIVERS.html"
)
