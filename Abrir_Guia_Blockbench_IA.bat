@echo off
start "" "C:\Program Files\Google\Chrome\Application\chrome.exe" "%~dp0GUIA_FLUJO_IA_A_BLOCKBENCH.html"
if %errorlevel% neq 0 (
    start "" "%~dp0GUIA_FLUJO_IA_A_BLOCKBENCH.html"
)
