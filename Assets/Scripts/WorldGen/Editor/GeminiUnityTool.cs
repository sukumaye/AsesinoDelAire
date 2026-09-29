using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Text;
using System.Collections;

public class GeminiUnityTool : EditorWindow
{
    private string apiKey = "";
    private MonoScript targetScript;
    private string userInstructions = "Optimiza este script para rendimiento en Unity: elimina GC Alloc, optimiza Update() y mejora el consumo de CPU.";
    private string responseText = "";
    private Vector2 scrollPos;
    private bool isLoading = false;

    [MenuItem("Window/Gemini AI Assistant")]
    public static void ShowWindow()
    {
        GetWindow<GeminiUnityTool>("Gemini AI");
    }

    private void OnGUI()
    {
        GUILayout.Label("Conexión con Google AI Studio (Gemini)", EditorStyles.boldLabel);
        
        apiKey = EditorGUILayout.PasswordField("Gemini API Key:", apiKey);
        targetScript = (MonoScript)EditorGUILayout.ObjectField("Script a analizar:", targetScript, typeof(MonoScript), false);
        
        GUILayout.Label("Instrucciones:");
        userInstructions = EditorGUILayout.TextArea(userInstructions, GUILayout.Height(50));

        GUI.enabled = !isLoading && !string.IsNullOrEmpty(apiKey) && targetScript != null;
        if (GUILayout.Button(isLoading ? "Consultando a Gemini..." : "Analizar y Optimizar con Gemini"))
        {
            SendToGemini();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();
        GUILayout.Label("Respuesta de Gemini:", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.TextArea(responseText, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void SendToGemini()
    {
        isLoading = true;
        responseText = "Analizando código...";

        string codeContent = targetScript.text;
        string prompt = $"{userInstructions}\n\nCódigo fuente del script ({targetScript.name}.cs):\n```csharp\n{codeContent}\n```";

        // JSON payload para la API de Gemini
        string jsonPayload = "{\"contents\":[{\"parts\":[{\"text\":\"" + EscapeJson(prompt) + "\"}]}]}";
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        var asyncOp = request.SendWebRequest();
        asyncOp.completed += (op) =>
        {
            isLoading = false;
            if (request.result == UnityWebRequest.Result.Success)
            {
                responseText = ExtractTextFromGeminiResponse(request.downloadHandler.text);
            }
            else
            {
                responseText = "Error al conectar con Gemini: " + request.error + "\n" + request.downloadHandler.text;
            }
            Repaint();
        };
    }

    private string EscapeJson(string text)
    {
        return text.Replace("\\", "\\\\")
                   .Replace("\"", "\\\"")
                   .Replace("\n", "\\n")
                   .Replace("\r", "\\r")
                   .Replace("\t", "\\t");
    }

    private string ExtractTextFromGeminiResponse(string rawJson)
    {
        // Extracción sencilla de texto de la respuesta JSON
        int textIndex = rawJson.IndexOf("\"text\": \"");
        if (textIndex == -1) return rawJson;
        int startIndex = textIndex + 9;
        int endIndex = rawJson.IndexOf("\"\\n", startIndex);
        if (endIndex == -1) endIndex = rawJson.IndexOf("\"\n", startIndex);
        if (endIndex == -1) endIndex = rawJson.LastIndexOf("\"");

        return rawJson.Substring(startIndex, endIndex - startIndex)
                      .Replace("\\n", "\n")
                      .Replace("\\\"", "\"");
    }
}