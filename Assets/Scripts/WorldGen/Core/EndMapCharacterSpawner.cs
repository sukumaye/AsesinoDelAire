using UnityEngine;
using UnityEngine.Rendering;

namespace WorldGen.Core
{
    /// <summary>
    /// Componente encargado de spawnear o posicionar automáticamente al personaje/jefe
    /// (por ejemplo, el Tanque) al final del mapa o al término de la carretera generada.
    /// </summary>
    [ExecuteAlways]
    public class EndMapCharacterSpawner : MonoBehaviour
    {
        [Header("🗺️ Referencia al Generador de Mapa")]
        [Tooltip("Generador de mapa del cual se extraerá el punto final del recorrido. Si está vacío, se busca automáticamente en la escena.")]
        public MapGenerator mapGenerator;

        [Header("👑 Personaje / Jefe Final")]
        [Tooltip("Prefab del personaje (ej. Tanque) a instanciar al final del mapa.")]
        public GameObject characterPrefab;

        [Tooltip("Instancia del personaje ya presente en la escena. Si se asigna o se crea una, se reutilizará y reubicará al regenerar el mapa.")]
        public Transform characterInstance;

        [Header("⚙️ Ajustes de Spawn")]
        [Tooltip("Si está activo, spawnea o reubica automáticamente al personaje cada vez que se genera o regenera el mapa.")]
        public bool autoSpawnOnGenerate = true;

        [Tooltip("Desplazamiento adicional (offset) relativo a la posición final del camino.")]
        public Vector3 spawnOffset = Vector3.zero;

        [Tooltip("Si está activo, rota al personaje para que mire hacia el jugador/sur (opuesto al sentido de avance).")]
        public bool faceTowardsPlayer = true;

        [Tooltip("Rotación Z manual si 'faceTowardsPlayer' está desactivado (o ángulo de encarado). 180° = mirando hacia abajo.")]
        public float rotationZ = 180f;

        [Header("🎨 Orden de Renderizado")]
        [Tooltip("Añade automáticamente un SortingGroup si no lo tiene, para que todas las partes del rig mantengan su orden de capas relativo.")]
        public bool ensureSortingGroup = true;

        [Tooltip("Sorting Layer deseada para el personaje/jefe.")]
        public string sortingLayerName = "Default";

        [Tooltip("Order in Layer del SortingGroup.")]
        public int sortingOrder = 10;

        private void OnEnable()
        {
            if (mapGenerator == null)
            {
                mapGenerator = GetComponent<MapGenerator>();
                if (mapGenerator == null)
                {
                    mapGenerator = FindAnyObjectByType<MapGenerator>();
                }
            }

            if (mapGenerator != null)
            {
                mapGenerator.OnWorldGenerated -= HandleWorldGenerated;
                mapGenerator.OnWorldGenerated += HandleWorldGenerated;
            }
        }

        private void OnDisable()
        {
            if (mapGenerator != null)
            {
                mapGenerator.OnWorldGenerated -= HandleWorldGenerated;
            }
        }

        private void HandleWorldGenerated(Vector3 startPos, Vector3 endPos)
        {
            if (autoSpawnOnGenerate)
            {
                SpawnOrPositionCharacter(endPos);
            }
        }

        /// <summary>
        /// Spawnea o reubica al personaje en la posición final calculada por el generador.
        /// </summary>
        [ContextMenu("🎯 Posicionar al Final del Mapa")]
        public GameObject SpawnOrPositionCharacter()
        {
            if (mapGenerator == null)
            {
                mapGenerator = GetComponent<MapGenerator>();
                if (mapGenerator == null)
                {
                    mapGenerator = FindAnyObjectByType<MapGenerator>();
                }
            }

            Vector3 finalPos = Vector3.zero;
            if (mapGenerator != null)
            {
                finalPos = mapGenerator.GetRoadEndPosition();
            }
            else
            {
                Debug.LogWarning("[EndMapCharacterSpawner] No se encontró MapGenerator en la escena.");
            }

            return SpawnOrPositionCharacter(finalPos);
        }

        /// <summary>
        /// Spawnea o posiciona al personaje en una coordenada específica.
        /// </summary>
        public GameObject SpawnOrPositionCharacter(Vector3 destination)
        {
            Vector3 spawnPos = destination + spawnOffset;
            Quaternion spawnRot = Quaternion.Euler(0f, 0f, faceTowardsPlayer ? 180f : rotationZ);

            GameObject targetGO = null;

            // 1. Reutilizar instancia existente si ya existe
            if (characterInstance != null)
            {
                targetGO = characterInstance.gameObject;
                targetGO.transform.position = spawnPos;
                targetGO.transform.rotation = spawnRot;
                var spider = targetGO.GetComponent<WorldGen.Boss.SpiderTankController>();
                if (spider != null) spider.SetAnchorPosition(spawnPos);
                Debug.Log($"<color=green><b>[EndMapCharacterSpawner]</b> Personaje '{targetGO.name}' reubicado al final del mapa en ({spawnPos.x:F1}, {spawnPos.y:F1}).</color>");
            }
            // 2. O instanciar desde Prefab
            else if (characterPrefab != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    targetGO = UnityEditor.PrefabUtility.InstantiatePrefab(characterPrefab) as GameObject;
                    if (targetGO != null)
                    {
                        UnityEditor.Undo.RegisterCreatedObjectUndo(targetGO, "Spawn End Character");
                    }
                }
                else
#endif
                {
                    targetGO = Instantiate(characterPrefab);
                }

                if (targetGO != null)
                {
                    targetGO.transform.position = spawnPos;
                    targetGO.transform.rotation = spawnRot;
                    characterInstance = targetGO.transform;
                    var spider = targetGO.GetComponent<WorldGen.Boss.SpiderTankController>();
                    if (spider != null) spider.SetAnchorPosition(spawnPos);
                    Debug.Log($"<color=green><b>[EndMapCharacterSpawner]</b> ¡Prefab '{characterPrefab.name}' instanciado al final del mapa en ({spawnPos.x:F1}, {spawnPos.y:F1})!</color>");
                }
            }
            else
            {
                // Buscar si hay SpiderTankController en la escena
                var spiderInScene = FindAnyObjectByType<WorldGen.Boss.SpiderTankController>();
                if (spiderInScene != null)
                {
                    characterInstance = spiderInScene.transform;
                    targetGO = spiderInScene.gameObject;
                    targetGO.transform.position = spawnPos;
                    targetGO.transform.rotation = spawnRot;
                    spiderInScene.SetAnchorPosition(spawnPos);
                    Debug.Log($"<color=green><b>[EndMapCharacterSpawner]</b> Tanque Araña '{targetGO.name}' encontrado y posicionado al final en ({spawnPos.x:F1}, {spawnPos.y:F1}).</color>");
                }
                else
                {
                    Debug.LogWarning("<color=yellow><b>[EndMapCharacterSpawner]</b> No hay 'characterPrefab' ni 'characterInstance' asignados para posicionar.</color>");
                    return null;
                }
            }


            // 3. Asegurar SortingGroup para que las capas del personaje mantengan su jerarquía 2D
            if (targetGO != null && ensureSortingGroup)
            {
                var sg = targetGO.GetComponent<SortingGroup>();
                if (sg == null)
                {
                    sg = targetGO.AddComponent<SortingGroup>();
                }

                if (!string.IsNullOrEmpty(sortingLayerName))
                {
                    sg.sortingLayerName = sortingLayerName;
                }
                sg.sortingOrder = sortingOrder;
            }

            return targetGO;
        }

        /// <summary>
        /// Destruye la instancia actual si fue creada procedimentalmente.
        /// </summary>
        [ContextMenu("🧹 Limpiar Personaje")]
        public void ClearCharacter()
        {
            if (characterInstance != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(characterInstance.gameObject);
                }
                else
                {
                    DestroyImmediate(characterInstance.gameObject);
                }
                characterInstance = null;
            }
        }
    }
}
