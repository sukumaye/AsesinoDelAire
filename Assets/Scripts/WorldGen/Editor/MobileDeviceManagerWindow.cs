using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace WorldGen.Editor
{
    /// <summary>
    /// Ventana de herramientas en Unity para conectar, diagnosticar y gestionar móviles Android (Redmi / Xiaomi / etc.)
    /// Permite probar por Unity Remote, compilar APKs, y preparar conexión multijugador Bluetooth.
    /// </summary>
    public class MobileDeviceManagerWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private string adbOutput = "Presiona 'Escanear Móviles' para buscar dispositivos conectados.";
        private bool isScanning = false;
        private string lastStatusMessage = "";
        private MessageType lastStatusType = MessageType.Info;

        private const string InstallerPath = @"C:\Users\REIKO\AppData\Roaming\UnityHub\downloads\UnitySetup-Android-Support-for-Editor-6000.6.0f1.exe";
        private const string Unity6AndroidPlayerPath = @"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\PlaybackEngines\AndroidPlayer";

        [MenuItem("Tools/📱 Conexión Móvil y Android/Panel de Control Móvil", false, 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<MobileDeviceManagerWindow>("Conexión Móvil");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnEnable()
        {
            ScanAdbDevices();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawAdbStatusSection();
            EditorGUILayout.Space(15);

            DrawUnityRemoteSection();
            EditorGUILayout.Space(15);

            DrawAndroidModuleSection();
            EditorGUILayout.Space(15);

            DrawXiaomiChecklistSection();
            EditorGUILayout.Space(15);

            DrawWifiAdbSection();
            EditorGUILayout.Space(20);

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("✈️ Gestor de Conexión Móvil - AsesinoDelAire", titleStyle);
            EditorGUILayout.LabelField("Conexión USB, Unity Remote, Drivers y Preparación Bluetooth", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawAdbStatusSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("1. 🔌 Diagnóstico de Móvil Conectado (ADB)", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(lastStatusMessage))
            {
                EditorGUILayout.HelpBox(lastStatusMessage, lastStatusType);
            }

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !isScanning;
            if (GUILayout.Button("🔄 Escanear Móviles Conectados", GUILayout.Height(30)))
            {
                ScanAdbDevices();
            }
            if (GUILayout.Button("⚡ Reiniciar Servidor ADB", GUILayout.Height(30)))
            {
                RestartAdbServer();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Salida del terminal ADB:", EditorStyles.miniLabel);
            EditorGUILayout.TextArea(adbOutput, GUILayout.Height(70));

            EditorGUILayout.EndVertical();
        }

        private void DrawUnityRemoteSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("2. 🎮 Unity Remote 5 (Prueba Rápida sin Compilar)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Te permite jugar en el móvil al instante pulsando Play en Unity. La pantalla se transmite por USB y los toques en el móvil controlan el avión.", MessageType.None);

            string currentDevice = EditorSettings.unityRemoteDevice;
            bool isConfigured = !string.IsNullOrEmpty(currentDevice) && currentDevice != "None";

            EditorGUILayout.LabelField($"Dispositivo Unity Remote configurado: {(isConfigured ? currentDevice : "Ninguno (Desactivado)")}");

            if (GUILayout.Button("✅ Activar Unity Remote para Cualquier Android", GUILayout.Height(28)))
            {
                EditorSettings.unityRemoteDevice = "Any Android Device";
                EditorSettings.unityRemoteCompression = "JPEG";
                EditorSettings.unityRemoteResolution = "Normal";
                AssetDatabase.SaveAssets();
                lastStatusMessage = "¡Unity Remote configurado con éxito para 'Any Android Device'!";
                lastStatusType = MessageType.Info;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawAndroidModuleSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("3. 📦 Soporte de Compilación Android (Build Support)", EditorStyles.boldLabel);

            bool isInstalled = Directory.Exists(Unity6AndroidPlayerPath);
            if (isInstalled)
            {
                EditorGUILayout.HelpBox("✅ El módulo de compilación para Android de Unity 6 está detectado.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("⚠️ El instalador oficial de Android Build Support ya está descargado en tu disco. Pulsa el botón de abajo para instalarlo si deseas compilar APKs nativos.", MessageType.Warning);

                if (File.Exists(InstallerPath))
                {
                    if (GUILayout.Button("📥 Ejecutar Instalador Oficial de Soporte Android", GUILayout.Height(32)))
                    {
                        RunAndroidInstallerElevated();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("El instalador se puede gestionar directamente desde Unity Hub > Installs > Add modules.");
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawXiaomiChecklistSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("4. 📋 Checklist para Móviles Xiaomi / Redmi", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Si el móvil está conectado por USB pero no aparece en la lista de arriba:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• 1. Ajustes > Acerca del teléfono > Pulsa 7 veces en 'Versión de MIUI / OS'.");
            EditorGUILayout.LabelField("• 2. Ajustes > Ajustes adicionales > Opciones de desarrollador.");
            EditorGUILayout.LabelField("• 3. Activa 'Depuración USB'.");
            EditorGUILayout.LabelField("• 4. Activa 'Instalar vía USB' y 'Depuración USB (Ajustes de seguridad)'.");
            EditorGUILayout.LabelField("• 5. Conecta el cable USB y en la pantalla del móvil selecciona 'Transferencia de archivos'.");
            EditorGUILayout.LabelField("• 6. Acepta el mensaje emergente en el móvil: '¿Permitir depuración USB?'.");
            EditorGUILayout.EndVertical();
        }

        private void DrawWifiAdbSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("5. 📶 Depuración Inalámbrica por Wi-Fi (Sin Cables)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Una vez conectado por USB al menos una vez, puedes activar el modo Wi-Fi para jugar o depurar sin necesidad del cable.", MessageType.None);

            if (GUILayout.Button("🌐 Activar Modo ADB por Wi-Fi (Puerto 5555)", GUILayout.Height(28)))
            {
                EnableWifiAdb();
            }
            EditorGUILayout.EndVertical();
        }

        private void ScanAdbDevices()
        {
            isScanning = true;
            string adbPath = FindAdbPath();
            if (string.IsNullOrEmpty(adbPath))
            {
                adbOutput = "No se encontró el ejecutable adb.exe.";
                lastStatusMessage = "No se localizó adb.exe en los SDKs de Unity.";
                lastStatusType = MessageType.Error;
                isScanning = false;
                return;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(adbPath, "devices -l")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        proc.WaitForExit(3000);
                        string output = proc.StandardOutput.ReadToEnd();
                        adbOutput = output;

                        if (output.Contains("device ") || output.Contains("device\r") || output.Contains("device\n"))
                        {
                            lastStatusMessage = "¡Móvil Android detectado y autorizado con éxito!";
                            lastStatusType = MessageType.Info;
                        }
                        else if (output.Contains("unauthorized"))
                        {
                            lastStatusMessage = "Móvil detectado pero NO AUTORIZADO. Mira la pantalla del móvil y pulsa 'Permitir depuración USB'.";
                            lastStatusType = MessageType.Warning;
                        }
                        else
                        {
                            lastStatusMessage = "No se detecta ningún móvil por USB actualmente. Revisa el checklist de Xiaomi/Redmi abajo.";
                            lastStatusType = MessageType.Warning;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                adbOutput = "Error al ejecutar adb: " + ex.Message;
            }
            finally
            {
                isScanning = false;
            }
        }

        private void RestartAdbServer()
        {
            string adb = FindAdbPath();
            if (string.IsNullOrEmpty(adb)) return;

            try
            {
                Process.Start(new ProcessStartInfo(adb, "kill-server") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit(2000);
                Process.Start(new ProcessStartInfo(adb, "start-server") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit(3000);
                ScanAdbDevices();
            }
            catch (Exception ex)
            {
                Debug.LogError("[MobileDeviceManager] Error al reiniciar ADB: " + ex.Message);
            }
        }

        private void EnableWifiAdb()
        {
            string adb = FindAdbPath();
            if (string.IsNullOrEmpty(adb)) return;

            try
            {
                Process proc = Process.Start(new ProcessStartInfo(adb, "tcpip 5555")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                proc?.WaitForExit(2000);
                string res = proc?.StandardOutput.ReadToEnd() ?? "";
                lastStatusMessage = "Modo Wi-Fi activado en el móvil (puerto 5555). Consulta la IP de tu móvil en Ajustes > Wi-Fi y usa 'adb connect <IP>:5555' para conectar sin cables.";
                lastStatusType = MessageType.Info;
            }
            catch (Exception ex)
            {
                Debug.LogError("[MobileDeviceManager] Error activando TCP/IP: " + ex.Message);
            }
        }

        private void RunAndroidInstallerElevated()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(InstallerPath, "/S /D=\"C:\\Program Files\\Unity\\Hub\\Editor\\6000.6.0f1\"")
                {
                    Verb = "runas",
                    UseShellExecute = true
                };
                Process.Start(psi);
                lastStatusMessage = "Se ha lanzado el instalador oficial. Si Windows te solicita permisos de Administrador (UAC), acéptalos para completar la instalación.";
                lastStatusType = MessageType.Info;
            }
            catch (Exception ex)
            {
                lastStatusMessage = "No se pudo iniciar el instalador: " + ex.Message;
                lastStatusType = MessageType.Error;
            }
        }

        [InitializeOnLoadMethod]
        private static void AutoConfigureUnityRemote()
        {
            if (EditorSettings.unityRemoteDevice != "Any Android Device")
            {
                EditorSettings.unityRemoteDevice = "Any Android Device";
                EditorSettings.unityRemoteCompression = "JPEG";
                EditorSettings.unityRemoteResolution = "Normal";
                AssetDatabase.SaveAssets();
                Debug.Log("<b>[Unity Remote]</b> Configurado automáticamente para 'Any Android Device'.");
            }
        }

        private static string FindAdbPath()
        {
            string[] possiblePaths = new string[]
            {
                @"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe",
                @"C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Android\Sdk\platform-tools\adb.exe")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path)) return path;
            }
            return "adb";
        }
    }
}
