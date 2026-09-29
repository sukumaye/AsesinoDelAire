using UnityEngine;

namespace WorldGen.Core
{
    /// <summary>
    /// Componente de prueba y control para explorar el Chunk Map en tiempo de ejecución.
    /// Permite moverse con WASD / Flechas y observar cómo los chunks aparecen y desaparecen dinámicamente.
    /// También incluye atajos de teclado [1, 2, 3, 4] para saltar directamente entre los 4 continentes.
    /// </summary>
    public class ChunkPlayerTester : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Generador de mapa con el sistema de Chunk Map activo.")]
        public MapGenerator mapGenerator;

        [Header("Movimiento")]
        [Tooltip("Velocidad de movimiento en casillas/tiles por segundo.")]
        public float moveSpeed = 10f;

        [Header("Cámara")]
        [Tooltip("Si está activo, hace que la cámara principal siga al jugador suavemente.")]
        public bool followWithMainCamera = true;

        [Header("HUD en Pantalla")]
        public bool showDebugHUD = true;

        private void Start()
        {
            if (mapGenerator == null)
            {
                mapGenerator = FindAnyObjectByType<MapGenerator>();
            }

            if (mapGenerator != null && mapGenerator.streamingTarget == null)
            {
                mapGenerator.streamingTarget = transform;
            }
        }

        private void Update()
        {
            // 1. Movimiento con WASD o Flechas
            float h = 0f;
            float v = 0f;

#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb[UnityEngine.InputSystem.Key.W].isPressed || kb[UnityEngine.InputSystem.Key.UpArrow].isPressed) v += 1f;
                if (kb[UnityEngine.InputSystem.Key.S].isPressed || kb[UnityEngine.InputSystem.Key.DownArrow].isPressed) v -= 1f;
                if (kb[UnityEngine.InputSystem.Key.A].isPressed || kb[UnityEngine.InputSystem.Key.LeftArrow].isPressed) h -= 1f;
                if (kb[UnityEngine.InputSystem.Key.D].isPressed || kb[UnityEngine.InputSystem.Key.RightArrow].isPressed) h += 1f;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
#endif

            if (h != 0 || v != 0)
            {
                Vector3 moveDir = new Vector3(h, v, 0).normalized;
                transform.position += moveDir * (moveSpeed * Time.deltaTime);

                if (mapGenerator != null)
                {
                    // Limitar dentro de los bordes del mapa
                    float clampedX = Mathf.Clamp(transform.position.x, 0, mapGenerator.width - 1);
                    float clampedY = Mathf.Clamp(transform.position.y, 0, mapGenerator.height - 1);
                    transform.position = new Vector3(clampedX, clampedY, transform.position.z);
                }
            }

            // 2. Seguimiento de Cámara
            if (followWithMainCamera && Camera.main != null)
            {
                Vector3 camPos = transform.position;
                camPos.z = Camera.main.transform.position.z;
                Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, camPos, Time.deltaTime * 8f);
            }

            // 3. Atajos de Teclado Rápidos
            if (mapGenerator != null)
            {
#if ENABLE_INPUT_SYSTEM
                if (kb != null)
                {
                    if (kb[UnityEngine.InputSystem.Key.Digit1].wasPressedThisFrame || kb[UnityEngine.InputSystem.Key.Numpad1].wasPressedThisFrame)
                        mapGenerator.ShowContinent(0); // Noroeste
                    else if (kb[UnityEngine.InputSystem.Key.Digit2].wasPressedThisFrame || kb[UnityEngine.InputSystem.Key.Numpad2].wasPressedThisFrame)
                        mapGenerator.ShowContinent(1); // Noreste
                    else if (kb[UnityEngine.InputSystem.Key.Digit3].wasPressedThisFrame || kb[UnityEngine.InputSystem.Key.Numpad3].wasPressedThisFrame)
                        mapGenerator.ShowContinent(2); // Suroeste
                    else if (kb[UnityEngine.InputSystem.Key.Digit4].wasPressedThisFrame || kb[UnityEngine.InputSystem.Key.Numpad4].wasPressedThisFrame)
                        mapGenerator.ShowContinent(3); // Sureste
                    else if (kb[UnityEngine.InputSystem.Key.M].wasPressedThisFrame)
                        mapGenerator.ShowAllChunks();
                    else if (kb[UnityEngine.InputSystem.Key.R].wasPressedThisFrame)
                    {
                        mapGenerator.RandomizeAndGenerate();
                        transform.position = new Vector3(mapGenerator.width * 0.25f, mapGenerator.height * 0.75f, 0);
                    }
                }
#elif ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                    mapGenerator.ShowContinent(0); // Noroeste
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                    mapGenerator.ShowContinent(1); // Noreste
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                    mapGenerator.ShowContinent(2); // Suroeste
                else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
                    mapGenerator.ShowContinent(3); // Sureste
                else if (Input.GetKeyDown(KeyCode.M))
                    mapGenerator.ShowAllChunks();
                else if (Input.GetKeyDown(KeyCode.R))
                {
                    mapGenerator.RandomizeAndGenerate();
                    transform.position = new Vector3(mapGenerator.width * 0.25f, mapGenerator.height * 0.75f, 0);
                }
#endif
            }
        }

        private void OnGUI()
        {
            if (!showDebugHUD || mapGenerator == null) return;

            GUI.color = Color.white;
            int px = Mathf.FloorToInt(transform.position.x);
            int py = Mathf.FloorToInt(transform.position.y);

            WorldChunk currentChunk = mapGenerator.GetChunkAt(px, py);
            string chunkName = (currentChunk != null) ? $"[{currentChunk.chunkCoord.x}, {currentChunk.chunkCoord.y}] ({currentChunk.continentName})" : "Fuera de límites";

            GUILayout.BeginArea(new Rect(15, 15, 360, 175), "🧭 Chunk Map Explorer", GUI.skin.window);
            GUILayout.Label($"📍 <b>Posición Jugador:</b> ({px}, {py})");
            GUILayout.Label($"📦 <b>Chunk Actual:</b> {chunkName}");
            GUILayout.Label($"👁️ <b>Chunks Visibles:</b> {mapGenerator.LoadedChunksCount} / {mapGenerator.TotalChunks}");
            GUILayout.Label($"⚙️ <b>Modo:</b> {mapGenerator.chunkStreamingMode} | <b>Estilo:</b> {mapGenerator.terrainStyle}");

            GUILayout.Space(4);
            GUILayout.Label("⌨️ <i>[1-4]: Continentes | [M]: Todo | [R]: Nivel Random</i>", GUI.skin.box);
            GUILayout.EndArea();
        }
    }
}
