using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using WorldGen.Data;
using WorldGen.Generators;

namespace WorldGen.Core
{
    public class MapGenerator : MonoBehaviour
    {
        [Header("Dimensiones del Mapa")]
        [Tooltip("Tamaño del mundo en tiles. Se recomienda 80x80 para archipiélagos con espacio desahogado para pueblos, mesetas y bosques sin apiñamiento.")]
        public int width = 80;
        public int height = 80;

        [Header("Geografía del Mundo")]
        [Tooltip("Tipo de mundo a generar: Archipiélago (islas, bahías, cabos y estrechos), Continente (golfos y penínsulas), Costa e Islas, o Lagos y Ríos.")]
        public TerrainStyle terrainStyle = TerrainStyle.Archipelago;

        [Range(0.2f, 0.7f)]
        [Tooltip("Nivel del mar / cantidad de agua. Controla la proporción de océanos, estrechos o lagos.")]
        public float waterLevel = 0.35f;

        [Header("Configuración de Archipiélago")]
        [Tooltip("Genera islas de radios grandes y consolidadas donde quepan pueblos, mesetas y bosques sin micro-islotes.")]
        public bool largeIslands = true;
        [Range(2, 5)]
        [Tooltip("Número de islas principales a generar en el archipiélago.")]
        public int archipelagoIslandCount = 3;

        [Header("Mesetas Procedurales Opcionales")]
        [Tooltip("Genera automáticamente una meseta procedural adaptable en una de las islas con su interior poblado.")]
        public bool generateDynamicPlateaus = false;
        [Range(1, 3)]
        [Tooltip("Cantidad de mesetas dinámicas a generar en el mapa.")]
        public int dynamicPlateauCount = 1;

        [Header("🎲 Generación Aleatoria Automática")]
        [Tooltip("Si está activo, cada vez que generes o entres al nivel se randomizarán TODOS los parámetros del terreno (estilo, agua, islas, mesetas, bosques, etc.).")]
        public bool randomizeParametersOnGenerate = true;

        [Tooltip("Si está activo, usa una semilla aleatoria diferente cada vez.")]
        public bool useRandomSeed = true;
        [Tooltip("Semilla fija para repetir un mapa exacto si 'useRandomSeed' está desactivado.")]
        public int seed = 12345;

        [Header("🔒 Parámetros Fijos (Bloquear para no randomizar)")]
        [Tooltip("Fija el estilo de mundo actual (no lo cambia al azar si está marcado).")]
        public bool lockTerrainStyle = false;

        [Tooltip("Fija el nivel de agua actual (no lo cambia al azar si está marcado).")]
        public bool lockWaterLevel = false;

        [Tooltip("Fija las dimensiones del mapa width/height (recomendado activo).")]
        public bool lockDimensions = true;

        [Tooltip("Fija la cantidad de islas / continentes.")]
        public bool lockIslandSettings = false;

        [Tooltip("Fija la configuración de mesetas y bosques.")]
        public bool lockFeatures = false;

        [Tooltip("Fija los ajustes del Chunk Map.")]
        public bool lockChunkSettings = false;

        [Header("🚀 Modo Shoot 'em Up (Avión y Cámara)")]
        [Tooltip("Avión del jugador a posicionar al inicio del camino.")]
        public Transform shmupPlayer;

        [Tooltip("Cámara principal que seguirá el camino hacia arriba.")]
        public Camera shmupCamera;

        [Tooltip("Si está activo, al generar o randomizar el mapa se posicionan automáticamente el avión y la cámara al inicio de la carretera.")]
        public bool autoPositionShmupOnGenerate = true;

        [HideInInspector]
        public Vector3 roadStartPosition;
        [HideInInspector]
        public Vector3 roadEndPosition;
        [HideInInspector]
        public List<Vector3> roadPathPoints = new List<Vector3>();
        [HideInInspector]
        public List<Vector3> optimalFlightPath = new List<Vector3>();
        [HideInInspector]
        public List<Vector2Int> roadWaypoints = new List<Vector2Int>();

        [Header("Caminos y Puentes")]
        [Tooltip("Permite que las carreteras construyan puentes automáticos sobre el agua para conectar las distintas islas y costas.")]
        public bool allowBridges = true;
        [Tooltip("Penalización de coste para cruzar agua. Un valor mayor hace que busque el estrecho más angosto entre islas.")]
        public float bridgeWaterCost = 5.0f;

        [Header("Tilemaps en la Escena")]
        public Tilemap groundMap;
        public Tilemap pathMap;
        public Tilemap structureMap;

        [Header("Base de Datos Visual WFC (Todo-en-Uno)")]
        [Tooltip("Arrastra aquí tu base de datos WFC. Gestiona caminos, terrenos y variantes automáticamente.")]
        public WfcVisualDatabase wfcDatabase;

        [Header("Categorías de WFC")]
        public string roadCategory = "Camino_Piedra";
        public string dirtRoadCategory = "Camino";
        public string waterCategory = "Agua_Clara";
        public string bridgeCategory = "Puente";

        [Header("Red Vial Híbrida (Piedra y Tierra)")]
        [Tooltip("Si está activo, usa adoquines de piedra cerca de las casas/ciudades y transiciona a senderos de tierra en campo abierto.")]
        public bool useHybridRoads = true;
        [Tooltip("Radio en casillas alrededor de las puertas donde el camino es de piedra antes de pasar a tierra.")]
        [Range(1, 20)]
        public int stoneRoadRadius = 5;

        [Header("Sobrescritura Manual de Tiles (Opcional)")]
        public TileBase customGrassTile;
        public TileBase customWaterTile;
        public TileBase customPathTile;
        public TileBase customBridgeTile;

        [Header("Vegetación y Variantes")]
        [Range(0f, 1f)]
        [Tooltip("Probabilidad de que una casilla de pasto contenga flores/detalles de la categoría Flores")]
        public float flowerDensity = 0.15f;

        [Header("Estructuras Urbanas (Con Puerta y Camino Troncal)")]
        [Tooltip("Edificios como Casa, Fuerte, Castillo, Torre. El camino troncal conectará con sus puertas.")]
        public List<StructureData> structuresToPlace = new List<StructureData>();

        [Header("Elementos Naturales (Bosques, Mesetas - Sin Puerta)")]
        [Tooltip("Elementos naturales del terreno (Bosques, Mesetas, Arboledas, etc.). Los caminos los esquivarán automáticamente sin conectarse a ellos.")]
        public List<StructureData> naturalFeatures = new List<StructureData>();
        [Range(0, 30)]
        [Tooltip("Cantidad de elementos naturales aleatorios a colocar sobre el pasto libre.")]
        public int naturalFeaturesCount = 5;

        [Header("Sistema de Chunk Map (4 Continentes / Secciones)")]
        [Tooltip("Activa la partición del mundo en Chunks. Permite que los chunks aparezcan y desaparezcan según la posición del mapa o continente activo.")]
        public bool useChunkMap = true;

        [Tooltip("Tamaño de cada chunk en casillas (tiles). Ej: 20x20, 16x16, o 40x40 para abarcar un continente entero por chunk.")]
        public Vector2Int chunkSize = new Vector2Int(20, 20);

        [Tooltip("Modo de visibilidad: Streaming por radio (mundo abierto), Sección de continente (4 cuadrantes), Pantalla única (retro) o Todo visible.")]
        public ChunkStreamingMode chunkStreamingMode = ChunkStreamingMode.ContinentSection;

        [Range(0, 3)]
        [Tooltip("Radio de chunks visibles alrededor del jugador/cámara en modo StreamingRadius (0 = solo chunk actual, 1 = 3x3 chunks).")]
        public int chunkViewDistance = 1;

        [Tooltip("Transform objetivo para streaming dinámico (Jugador o Main Camera).")]
        public Transform streamingTarget;

        [Range(0, 3)]
        [Tooltip("Sección continental activa en modo ContinentSection (0: Noroeste, 1: Noreste, 2: Suroeste, 3: Sureste).")]
        public int activeContinentSection = 0;

        [Tooltip("Coordenada del chunk actualmente seleccionado en el visor.")]
        public Vector2Int activeChunkCoord = Vector2Int.zero;

        [Header("Ejecución")]
        public bool generateOnStart = true;

        private CellType[,] mapGrid;
        private List<Vector2Int> doors = new List<Vector2Int>();
        private Dictionary<Vector2Int, WorldChunk> chunks = new Dictionary<Vector2Int, WorldChunk>();
        private Vector2Int lastStreamedChunkCoord = new Vector2Int(int.MinValue, int.MinValue);

        public int TotalChunks => chunks != null ? chunks.Count : 0;
        public int LoadedChunksCount
        {
            get
            {
                if (chunks == null) return 0;
                int count = 0;
                foreach (var kvp in chunks) if (kvp.Value.isLoaded) count++;
                return count;
            }
        }
        public Dictionary<Vector2Int, WorldChunk> Chunks => chunks;

        private void Start()
        {
            if (generateOnStart)
            {
                GenerateWorld();
            }
        }

        private void Update()
        {
            if (!useChunkMap || chunks == null || chunks.Count == 0) return;

            // En modo streaming por radio o pantalla única, detectar si el jugador cruzó a otro chunk
            if (chunkStreamingMode == ChunkStreamingMode.StreamingRadius || chunkStreamingMode == ChunkStreamingMode.SingleScreenChunk)
            {
                Vector2Int targetTile = GetTargetTileCoord();
                Vector2Int currentChunk = GetChunkCoordFromTile(targetTile);
                if (currentChunk != lastStreamedChunkCoord)
                {
                    lastStreamedChunkCoord = currentChunk;
                    activeChunkCoord = currentChunk;
                    ApplyChunkVisibility();
                }
            }
        }

        [ContextMenu("Generar con Nueva Semilla")]
        public void GenerateWithNewSeed()
        {
            useRandomSeed = true;
            seed = Random.Range(1, 1000000);
            GenerateWorld();
        }

        [ContextMenu("Randomizar y Generar")]
        public void RandomizeAndGenerate()
        {
            RandomizeTerrainParameters();
            GenerateWorld();
        }

        [ContextMenu("Randomizar Todos los Parámetros")]
        public void RandomizeTerrainParameters()
        {
            // 1. Semilla
            seed = Random.Range(1, 1000000);
            useRandomSeed = true;

            // 2. Estilo de Geografía (si no está bloqueado)
            if (!lockTerrainStyle)
            {
                float roll = Random.value;
                if (roll < 0.35f) terrainStyle = TerrainStyle.FourContinents;
                else if (roll < 0.65f) terrainStyle = TerrainStyle.Archipelago;
                else if (roll < 0.80f) terrainStyle = TerrainStyle.Continent;
                else if (roll < 0.90f) terrainStyle = TerrainStyle.CoastalIslands;
                else terrainStyle = TerrainStyle.LakesAndRivers;
            }

            // 3. Nivel de Agua (si no está bloqueado)
            if (!lockWaterLevel)
            {
                waterLevel = Mathf.Round(Random.Range(0.28f, 0.44f) * 100f) / 100f;
            }

            // 4. Islas y Accidentes
            if (!lockIslandSettings)
            {
                largeIslands = Random.value > 0.20f;
                archipelagoIslandCount = Random.Range(2, 5);
            }

            // 5. Mesetas y Elementos Naturales
            if (!lockFeatures)
            {
                generateDynamicPlateaus = Random.value > 0.35f;
                dynamicPlateauCount = Random.Range(1, 3);
                naturalFeaturesCount = Random.Range(3, 10);
                flowerDensity = Mathf.Round(Random.Range(0.10f, 0.35f) * 100f) / 100f;
            }

            // 6. Caminos y Puentes
            allowBridges = Random.value > 0.15f;
            bridgeWaterCost = Mathf.Round(Random.Range(3.5f, 6.5f) * 10f) / 10f;
            useHybridRoads = Random.value > 0.20f;
            stoneRoadRadius = Random.Range(3, 8);

            // 7. Chunk Map y Continente inicial
            if (!lockChunkSettings && useChunkMap)
            {
                if (terrainStyle == TerrainStyle.FourContinents)
                {
                    activeContinentSection = Random.Range(0, 4);
                }
            }

            // 8. Reordenar aleatoriamente las estructuras asignadas para variar distribución
            if (structuresToPlace != null && structuresToPlace.Count > 1)
            {
                for (int i = 0; i < structuresToPlace.Count; i++)
                {
                    int rnd = Random.Range(i, structuresToPlace.Count);
                    var temp = structuresToPlace[i];
                    structuresToPlace[i] = structuresToPlace[rnd];
                    structuresToPlace[rnd] = temp;
                }
            }

            Debug.Log($"<color=yellow><b>[MapGenerator Randomizer]</b> ¡Parámetros randomizados para nuevo nivel! Estilo: {terrainStyle} | Agua: {waterLevel:P0} | Islas: {archipelagoIslandCount} | Bosques: {naturalFeaturesCount} | Semilla: {seed}</color>");
        }

        public void SetDimensions(int newWidth, int newHeight)
        {
            width = Mathf.Clamp(newWidth, 30, 200);
            height = Mathf.Clamp(newHeight, 30, 200);
        }

        [ContextMenu("Generar Mundo")]
        public void GenerateWorld()
        {
            if (randomizeParametersOnGenerate)
            {
                RandomizeTerrainParameters();
            }

            Debug.Log("<color=cyan><b>[MapGenerator]</b> === INICIANDO GENERACIÓN DE MUNDO ===</color>");

            // 0. Validar Referencias Críticas
            bool hasErrors = false;
            if (groundMap == null)
            {
                Debug.LogError("<color=red><b>[MapGenerator] ERROR:</b> 'Ground Map' no está asignado en el Inspector.</color>");
                hasErrors = true;
            }
            if (pathMap == null)
            {
                Debug.LogWarning("<color=yellow><b>[MapGenerator] AVISO:</b> 'Path Map' no está asignado en el Inspector.</color>");
            }
            if (structureMap == null)
            {
                Debug.LogWarning("<color=yellow><b>[MapGenerator] AVISO:</b> 'Structure Map' no está asignado en el Inspector (las estructuras no se podrán dibujar).</color>");
            }

            if (wfcDatabase == null && customGrassTile == null && customWaterTile == null)
            {
                Debug.LogError("<color=red><b>[MapGenerator] ERROR:</b> 'Wfc Database' no está asignada y no hay tiles manuales de pasto/agua configurados.</color>");
                hasErrors = true;
            }

            if (hasErrors)
            {
                Debug.LogError("<color=red><b>[MapGenerator] Generación cancelada por referencias faltantes.</b> Revisa el Inspector.</color>");
                return;
            }

            // Diagnóstico de Base de Datos
            if (wfcDatabase != null)
            {
                var pradoCount = wfcDatabase.GetTilesByCategory("Prado").Count;
                var floresCount = wfcDatabase.GetTilesByCategory("Flores").Count;
                var aguaCount = wfcDatabase.GetTilesByCategory(waterCategory).Count;
                var caminoCount = wfcDatabase.GetTilesByCategory(roadCategory).Count;
                Debug.Log($"<b>[MapGenerator DB]</b> Base de datos '{wfcDatabase.name}': Prado={pradoCount}, Flores={floresCount}, Agua({waterCategory})={aguaCount}, Caminos({roadCategory})={caminoCount}");

                if (pradoCount == 0 && customGrassTile == null)
                {
                    Debug.LogWarning("<color=yellow><b>[MapGenerator AVISO]</b> No hay tiles en la categoría 'Prado' ni 'customGrassTile'. El pasto no se verá.</color>");
                }
            }

            int currentSeed = useRandomSeed ? Random.Range(1, 1000000) : seed;
            Random.InitState(currentSeed);

            // Asegurar capas de renderizado: Ground=0, Path=5, Structure=10
            if (groundMap != null)
            {
                var r = groundMap.GetComponent<TilemapRenderer>();
                if (r != null) r.sortingOrder = 0;
            }
            if (pathMap != null)
            {
                var r = pathMap.GetComponent<TilemapRenderer>();
                if (r != null) r.sortingOrder = 5;
            }
            if (structureMap != null)
            {
                var r = structureMap.GetComponent<TilemapRenderer>();
                if (r != null) r.sortingOrder = 10;
            }

            ClearTilemaps();
            InitializeChunkGrid();

            // 1. Generar Terreno Base (Accidentes geográficos orgánicos e islas grandes)
            mapGrid = TerrainGenerator.Generate(width, height, waterLevel, currentSeed, terrainStyle, largeIslands, archipelagoIslandCount);
            int waterCells = 0, grassCells = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (mapGrid[x, y] == CellType.Water) waterCells++;
                    else grassCells++;
                }
            }
            Debug.Log($"<b>[Paso 1 - Terreno Base ({terrainStyle})]</b> Dimensiones: {width}x{height}. Pasto: {grassCells}, Agua: {waterCells}");

            // 1.1 Detección Inteligente de Islas
            List<IslandData> detectedIslands = TerrainGenerator.DetectIslands(mapGrid, width, height, 16);
            Debug.Log($"<b>[Paso 1.1 - Análisis de Islas]</b> Se detectaron {detectedIslands.Count} masas de tierra habitables.");
            for (int i = 0; i < detectedIslands.Count; i++)
            {
                var isl = detectedIslands[i];
                Debug.Log($"   * Masa #{i + 1} (ID:{isl.id}): Área={isl.Area} tiles, Centro=({Mathf.RoundToInt(isl.center.x)}, {Mathf.RoundToInt(isl.center.y)})");
            }

            // 2. Colocar Estructuras y Asignar Roles por Isla
            doors.Clear();
            int structSuccessCount = 0;

            if (terrainStyle == TerrainStyle.FourContinents)
            {
                // DISTRIBUCIÓN POR LOS 4 CONTINENTES / 4 SECCIONES (Noroeste, Noreste, Suroeste, Sureste)
                if (structuresToPlace != null && structuresToPlace.Count > 0)
                {
                    for (int sIdx = 0; sIdx < structuresToPlace.Count; sIdx++)
                    {
                        var structData = structuresToPlace[sIdx];
                        if (structData == null) continue;

                        int targetQuadrant = sIdx % 4; // 0: NW, 1: NE, 2: SW, 3: SE
                        if (StructurePlacer.TryPlaceStructureInQuadrant(structData, mapGrid, width, height, targetQuadrant, out Vector2Int origin, 300) ||
                            StructurePlacer.TryPlaceStructure(structData, mapGrid, width, height, out origin, 300))
                        {
                            DrawStructure(structData, origin);
                            if (structData.hasDoor)
                            {
                                Vector2Int doorPos = origin + structData.doorOffset;
                                doors.Add(doorPos);
                            }
                            structSuccessCount++;
                            Debug.Log($"<color=green><b>[Estructura en Continente #{targetQuadrant + 1}]</b> '{structData.structureName}' en ({origin.x}, {origin.y}).</color>");
                        }
                    }
                }

                // Elementos naturales distribuidos en los 4 continentes
                if (naturalFeatures != null && naturalFeatures.Count > 0 && naturalFeaturesCount > 0)
                {
                    for (int n = 0; n < naturalFeaturesCount; n++)
                    {
                        var natData = naturalFeatures[n % naturalFeatures.Count];
                        if (natData == null) continue;

                        int targetQuadrant = (n + 1) % 4;
                        if (StructurePlacer.TryPlaceStructureInQuadrant(natData, mapGrid, width, height, targetQuadrant, out Vector2Int origin, 200) ||
                            StructurePlacer.TryPlaceStructure(natData, mapGrid, width, height, out origin, 200))
                        {
                            DrawStructure(natData, origin);
                            if (natData.hasDoor)
                            {
                                Vector2Int doorPos = origin + natData.doorOffset;
                                if (!doors.Contains(doorPos)) doors.Add(doorPos);
                            }
                        }
                    }
                }

                // Mesetas dinámicas en los continentes
                if (generateDynamicPlateaus)
                {
                    for (int p = 0; p < dynamicPlateauCount; p++)
                    {
                        if (PlateauGenerator.TryGenerateDynamicPlateau(mapGrid, width, height, null, wfcDatabase, structureMap, out Vector2Int rampDoor))
                        {
                            if (rampDoor != Vector2Int.zero && !doors.Contains(rampDoor))
                            {
                                doors.Add(rampDoor);
                            }
                        }
                    }
                }
            }
            else if (terrainStyle == TerrainStyle.Archipelago && detectedIslands.Count >= 2)
            {
                // DISTRIBUCIÓN COHERENTE POR ISLAS:
                // Repartir las estructuras y automesetas que tú asignaste entre las islas disponibles
                if (structuresToPlace != null && structuresToPlace.Count > 0)
                {
                    for (int sIdx = 0; sIdx < structuresToPlace.Count; sIdx++)
                    {
                        var structData = structuresToPlace[sIdx];
                        if (structData == null) continue;

                        int targetIslandIdx = sIdx % detectedIslands.Count;
                        var targetIsl = detectedIslands[targetIslandIdx];

                        if (StructurePlacer.TryPlaceStructureInIsland(structData, mapGrid, width, height, targetIsl, out Vector2Int origin, 300) ||
                            StructurePlacer.TryPlaceStructure(structData, mapGrid, width, height, out origin, 300))
                        {
                            DrawStructure(structData, origin);
                            if (structData.hasDoor)
                            {
                                Vector2Int doorPos = origin + structData.doorOffset;
                                doors.Add(doorPos);
                            }
                            structSuccessCount++;
                            Debug.Log($"<color=green><b>[Estructura en Isla #{targetIsl.id}]</b> '{structData.structureName}' en ({origin.x}, {origin.y}).</color>");
                        }
                    }
                }

                // Elementos naturales (bosques o automesetas naturales) distribuidos en las islas
                if (naturalFeatures != null && naturalFeatures.Count > 0 && naturalFeaturesCount > 0)
                {
                    for (int n = 0; n < naturalFeaturesCount; n++)
                    {
                        var natData = naturalFeatures[n % naturalFeatures.Count];
                        if (natData == null) continue;

                        int targetIslandIdx = (n + 1) % detectedIslands.Count;
                        var targetIsl = detectedIslands[targetIslandIdx];

                        if (StructurePlacer.TryPlaceStructureInIsland(natData, mapGrid, width, height, targetIsl, out Vector2Int origin, 200) ||
                            StructurePlacer.TryPlaceStructure(natData, mapGrid, width, height, out origin, 200))
                        {
                            DrawStructure(natData, origin);
                            if (natData.hasDoor)
                            {
                                Vector2Int doorPos = origin + natData.doorOffset;
                                if (!doors.Contains(doorPos)) doors.Add(doorPos);
                            }
                            Debug.Log($"<color=cyan><b>[Elemento Natural en Isla #{targetIsl.id}]</b> '{natData.structureName}' en ({origin.x}, {origin.y}).</color>");
                        }
                    }
                }

                // Solo si activas explícitamente la meseta procedural adicional:
                if (generateDynamicPlateaus && detectedIslands.Count >= 2)
                {
                    var plateauIsland = detectedIslands[1];
                    if (PlateauGenerator.TryGenerateDynamicPlateau(mapGrid, width, height, plateauIsland, wfcDatabase, structureMap, out Vector2Int rampDoor))
                    {
                        if (rampDoor != Vector2Int.zero && !doors.Contains(rampDoor))
                        {
                            doors.Add(rampDoor);
                        }
                    }
                }
            }
            else
            {
                // MODO CONTINENTE O ISLA ÚNICA
                if (structuresToPlace != null && structuresToPlace.Count > 0)
                {
                    int totalStructures = structuresToPlace.Count;
                    int marginY = 4;
                    int usableHeight = Mathf.Max(20, height - marginY * 2);
                    int sliceHeight = usableHeight / totalStructures;

                    for (int sIdx = 0; sIdx < totalStructures; sIdx++)
                    {
                        var structData = structuresToPlace[sIdx];
                        if (structData == null) continue;

                        int minY = marginY + sIdx * sliceHeight;
                        int maxY = Mathf.Min(height - structData.size.y - marginY, minY + sliceHeight);

                        if (StructurePlacer.TryPlaceStructureInZone(structData, mapGrid, width, height, minY, maxY, out Vector2Int origin, 300))
                        {
                            DrawStructure(structData, origin);
                            if (structData.hasDoor)
                            {
                                Vector2Int doorPos = origin + structData.doorOffset;
                                doors.Add(doorPos);
                            }
                            structSuccessCount++;
                        }
                    }
                }

                // Generar Meseta Dinámica en espacio abierto del continente
                if (generateDynamicPlateaus)
                {
                    for (int p = 0; p < dynamicPlateauCount; p++)
                    {
                        if (PlateauGenerator.TryGenerateDynamicPlateau(mapGrid, width, height, null, wfcDatabase, structureMap, out Vector2Int rampDoor))
                        {
                            if (rampDoor != Vector2Int.zero && !doors.Contains(rampDoor))
                            {
                                doors.Add(rampDoor);
                            }
                        }
                    }
                }

                // Elementos Naturales
                if (naturalFeatures != null && naturalFeatures.Count > 0 && naturalFeaturesCount > 0)
                {
                    for (int n = 0; n < naturalFeaturesCount; n++)
                    {
                        var natData = naturalFeatures[Random.Range(0, naturalFeatures.Count)];
                        if (natData == null) continue;

                        if (StructurePlacer.TryPlaceStructure(natData, mapGrid, width, height, out Vector2Int origin, 300))
                        {
                            DrawStructure(natData, origin);
                        }
                    }
                }
            }

            // 3. Trazar Red Troncal Continua (A*) de Sur a Norte (Base -> Estructuras -> Tope)
            int pathTilesCount = 0;
            int bridgeTilesCount = 0;

            // Encontrar punto de partida en la base sur del mapa (y mínimo)
            Vector2Int startBase = FindBaseEntrance();

            // Encontrar punto de llegada en el tope norte del mapa (y máximo)
            Vector2Int endTop = FindTopDestination();

            // Ordenar las puertas de abajo hacia arriba (menor Y a mayor Y)
            doors.Sort((a, b) => a.y.CompareTo(b.y));

            // Lista secuencial de hitos / waypoints a recorrer por la carretera
            List<Vector2Int> waypoints = new List<Vector2Int>();
            if (startBase != Vector2Int.zero) waypoints.Add(startBase);

            foreach (var d in doors)
            {
                if (!waypoints.Contains(d)) waypoints.Add(d);
            }

            if (endTop != Vector2Int.zero && !waypoints.Contains(endTop))
            {
                waypoints.Add(endTop);
            }

            // Ordenar todos los waypoints de Sur a Norte (Y ascendente) para garantizar una ruta troncal limpia
            waypoints.Sort((a, b) =>
            {
                int cmp = a.y.CompareTo(b.y);
                return (cmp != 0) ? cmp : a.x.CompareTo(b.x);
            });

            // Fallback si el mapa no encontró suficientes puntos
            if (waypoints.Count < 2)
            {
                Vector2Int f1 = FindFirstGrass();
                Vector2Int f2 = FindScenicPathTarget(f1);
                waypoints.Clear();
                if (f1 != Vector2Int.zero) waypoints.Add(f1);
                if (f2 != Vector2Int.zero && f2 != f1) waypoints.Add(f2);
            }

            roadPathPoints.Clear();
            roadWaypoints.Clear();
            foreach (var wp in waypoints) roadWaypoints.Add(wp);

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                Vector2Int pA = waypoints[i];
                Vector2Int pB = waypoints[i + 1];

                List<Vector2Int> segment = GridAStar.FindPath(pA, pB, mapGrid, width, height, allowBridges, bridgeWaterCost);
                if (segment != null && segment.Count > 0)
                {
                    foreach (var pt in segment)
                    {
                        Vector3 worldPt = new Vector3(pt.x + 0.5f, pt.y + 0.5f, 0f);
                        if (roadPathPoints.Count == 0 || roadPathPoints[roadPathPoints.Count - 1] != worldPt)
                        {
                            roadPathPoints.Add(worldPt);
                        }

                        if (mapGrid[pt.x, pt.y] == CellType.Water)
                        {
                            mapGrid[pt.x, pt.y] = CellType.Bridge;
                            bridgeTilesCount++;
                        }
                        else if (mapGrid[pt.x, pt.y] == CellType.Grass)
                        {
                            mapGrid[pt.x, pt.y] = CellType.Path;
                            pathTilesCount++;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"<b>[Paso 3 - Red Troncal]</b> No se pudo conectar tramo A* ({pA.x}, {pA.y}) -> ({pB.x}, {pB.y}).");
                }
            }

            // 3.5 Extraer y suavizar la columna vertebral óptima (intersección más óptima de sur a norte)
            Vector2Int spineStart = (startBase != Vector2Int.zero) ? startBase : FindBaseEntrance();
            Vector2Int spineEnd = (endTop != Vector2Int.zero) ? endTop : FindTopDestination();

            List<Vector2Int> rawSpine = GridAStar.FindOptimalSpinePath(spineStart, spineEnd, mapGrid, width, height);
            List<Vector3> rawSpineWorld = new List<Vector3>();

            if (rawSpine != null && rawSpine.Count > 1)
            {
                foreach (var p in rawSpine)
                {
                    rawSpineWorld.Add(new Vector3(p.x + 0.5f, p.y + 0.5f, 0f));
                }
            }
            else
            {
                rawSpineWorld = new List<Vector3>(roadPathPoints);
            }

            // Suavizar la trayectoria con filtro Laplaciano para eliminar quiebros bruscos de 90°
            optimalFlightPath = SmoothPathLaplacian(rawSpineWorld, 8);

            // Calcular inicio y fin del camino para el modo Shmup
            if (optimalFlightPath != null && optimalFlightPath.Count > 0)
            {
                roadStartPosition = optimalFlightPath[0];
                roadEndPosition = optimalFlightPath[optimalFlightPath.Count - 1];
            }
            else if (roadPathPoints.Count > 0)
            {
                roadStartPosition = roadPathPoints[0];
                roadEndPosition = roadPathPoints[roadPathPoints.Count - 1];
            }
            else if (startBase != Vector2Int.zero)
            {
                roadStartPosition = new Vector3(startBase.x + 0.5f, startBase.y + 0.5f, 0f);
                roadEndPosition = (endTop != Vector2Int.zero) ? new Vector3(endTop.x + 0.5f, endTop.y + 0.5f, 0f) : roadStartPosition;
            }
            else
            {
                roadStartPosition = new Vector3(width * 0.5f, 5f, 0f);
                roadEndPosition = new Vector3(width * 0.5f, height - 12f, 0f);
            }

            Debug.Log($"<b>[Paso 3 - Red Troncal]</b> Carreteras trazadas. Columna vertebral de vuelo óptima: {optimalFlightPath.Count} puntos suavizados (resuelve intersecciones hacia el norte). Inicio Shmup en ({roadStartPosition.x:F1}, {roadStartPosition.y:F1}).");

            // 4. Resolver y Pintar Caminos y Puentes en pathMap
            ResolveAndDrawPaths();

            // 5. Pintar Terreno (Pasto orgánico, Flores y Agua continua)
            var flowerTiles = (wfcDatabase != null) ? wfcDatabase.GetTilesByCategory("Flores") : null;
            bool hasFlowers = flowerTiles != null && flowerTiles.Count > 0;
            int paintedTiles = 0;

            if (groundMap != null)
            {
                bool IsWaterOrBridge(int nx, int ny)
                {
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    {
                        // Fuera del mapa es mar abierto continuo (mapa de un mundo rodeado de océano exterior)
                        return true;
                    }
                    return mapGrid[nx, ny] == CellType.Water || mapGrid[nx, ny] == CellType.Bridge;
                }

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Vector3Int pos = new Vector3Int(x, y, 0);

                        // Si es agua o puente: en groundMap pintamos el agua continua
                        if (mapGrid[x, y] == CellType.Water || mapGrid[x, y] == CellType.Bridge)
                        {
                            TileBase water = null;
                            if (customWaterTile != null)
                            {
                                water = customWaterTile;
                            }
                            else if (wfcDatabase != null)
                            {
                                bool N  = IsWaterOrBridge(x, y + 1);
                                bool S  = IsWaterOrBridge(x, y - 1);
                                bool W  = IsWaterOrBridge(x - 1, y);
                                bool E  = IsWaterOrBridge(x + 1, y);

                                bool NW = IsWaterOrBridge(x - 1, y + 1);
                                bool NE = IsWaterOrBridge(x + 1, y + 1);
                                bool SW = IsWaterOrBridge(x - 1, y - 1);
                                bool SE = IsWaterOrBridge(x + 1, y - 1);

                                water = wfcDatabase.GetWaterAutotile(N, S, W, E, NW, NE, SW, SE, waterCategory);
                            }

                            if (water != null)
                            {
                                groundMap.SetTile(pos, water);
                                WorldChunk chunk = GetChunkAt(x, y);
                                if (chunk != null) chunk.RecordGround(pos, water);
                                paintedTiles++;
                            }
                        }
                        else
                        {
                            TileBase grass = null;
                            if (hasFlowers && Random.value < flowerDensity)
                            {
                                grass = wfcDatabase.GetRandomTileByCategory("Flores");
                            }

                            if (grass == null)
                            {
                                grass = (customGrassTile != null)
                                    ? customGrassTile
                                    : (wfcDatabase != null ? wfcDatabase.GetRandomTileByCategory("Prado") : null);
                            }

                            if (grass != null)
                            {
                                groundMap.SetTile(pos, grass);
                                WorldChunk chunk = GetChunkAt(x, y);
                                if (chunk != null) chunk.RecordGround(pos, grass);
                                paintedTiles++;
                            }
                        }
                    }
                }
            }

            // 5.1 Preparar la caché nativa de todos los chunks generados
            foreach (var c in chunks.Values)
            {
                c.PrepareCache();
            }

            // 6. Aplicar Chunk Map y Visibilidad Dinámica si está activado
            if (useChunkMap)
            {
                ClearTilemaps();
                ApplyChunkVisibility();
            }
            else
            {
                RefreshTilemaps();
            }

            // Pintar franja exterior de mar abierto continuo (Ocean Skirt) para que la cámara Shmup jamás vea el vacío
            PaintOceanSkirt();

            Debug.Log($"<color=green><b>[MapGenerator ÉXITO]</b> ¡Mundo generado completamente! Semilla: {currentSeed} | Suelo pintado: {paintedTiles} | Estructuras: {structSuccessCount} | Puentes: {bridgeTilesCount} | Caminos: {pathTilesCount} | Chunks: {chunks.Count}</color>");

            // 7. Modo Shoot 'em Up: Posicionar automáticamente avión y cámara en el inicio del camino
            if (autoPositionShmupOnGenerate)
            {
                PositionShmupAtRoadStart();
            }
        }

        [ContextMenu("Posicionar Avión y Cámara al Inicio del Camino")]
        public void PositionShmupAtRoadStart()
        {
            // 1. Determinar el punto más al sur (abajo) donde empieza el camino
            Vector3 startPos = roadStartPosition;
            if (optimalFlightPath != null && optimalFlightPath.Count > 0)
            {
                startPos = optimalFlightPath[0];
            }
            else if (roadPathPoints != null && roadPathPoints.Count > 0)
            {
                startPos = roadPathPoints[0];
            }
            else
            {
                Vector2Int baseEntrance = FindBaseEntrance();
                startPos = new Vector3(baseEntrance.x + 0.5f, baseEntrance.y + 0.5f, 0f);
            }

            // 2. Localizar o referenciar el avión del jugador
            if (shmupPlayer == null)
            {
                var plane = FindAnyObjectByType<Shmup.ShmupPlaneController>();
                if (plane != null) shmupPlayer = plane.transform;
                else
                {
                    var playerObj = GameObject.FindWithTag("Player");
                    if (playerObj != null) shmupPlayer = playerObj.transform;
                }
            }

            var pathToUse = (optimalFlightPath != null && optimalFlightPath.Count > 0) ? optimalFlightPath : roadPathPoints;

            // 2. Sincronizar objetivo guía de Cinemachine si existe en la escena
            var cmTarget = FindAnyObjectByType<Shmup.CinemachineShmupTarget>();
            if (cmTarget != null)
            {
                cmTarget.SetupAtPathStart(startPos, pathToUse);
                Debug.Log($"<color=cyan><b>[Cinemachine Setup]</b> ¡Objetivo guía 'Cinemachine_PathTarget' sincronizado al inicio del camino!</color>");
            }

            // 3. Localizar y sincronizar las cámaras
            if (shmupCamera == null)
            {
                shmupCamera = Camera.main;
            }

            if (shmupCamera != null)
            {
                var camController = shmupCamera.GetComponent<Shmup.ShmupCameraController>();
                if (camController != null)
                {
                    if (shmupPlayer != null && camController.targetPlane == null)
                    {
                        camController.targetPlane = shmupPlayer;
                    }
                    camController.SetupAtPathStart(startPos, pathToUse);
                }
                else
                {
                    shmupCamera.transform.position = new Vector3(startPos.x, startPos.y, -10f);
                }
                Debug.Log($"<color=cyan><b>[Shmup Setup]</b> ¡Cámara posicionada y sincronizada al inicio del camino!</color>");
            }

            var cmCam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsInactive.Include);
            if (cmCam != null)
            {
                cmCam.transform.position = new Vector3(startPos.x, startPos.y, -10f);
                cmCam.PreviousStateIsValid = false;
            }

            // 4. Localizar o referenciar el avión del jugador
            if (shmupPlayer == null)
            {
                var plane = FindAnyObjectByType<Shmup.ShmupPlaneController>();
                if (plane != null) shmupPlayer = plane.transform;
                else
                {
                    var playerObj = GameObject.FindWithTag("Player");
                    if (playerObj != null) shmupPlayer = playerObj.transform;
                }
            }

            if (shmupPlayer != null)
            {
                var plane = shmupPlayer.GetComponent<Shmup.ShmupPlaneController>();
                if (plane != null)
                {
                    plane.targetCamera = shmupCamera;
                    plane.SetFlightPath(pathToUse, startPos);
                }
                else
                {
                    shmupPlayer.position = new Vector3(startPos.x, startPos.y - 4f, shmupPlayer.position.z);
                }
                Debug.Log($"<color=green><b>[Shmup Setup]</b> ¡Avión posicionado en inicio del camino: ({startPos.x:F1}, {startPos.y:F1}) y configurado con ruta de {pathToUse.Count} waypoints (horizontal y vertical)!</color>");
            }
        }

        /// <summary>
        /// Suaviza una lista de puntos mediante un filtro Laplaciano-Gaussiano iterativo (1-2-1).
        /// Elimina quiebros de 90° de la cuadrícula y genera una curva orgánica y aerodinámica.
        /// </summary>
        public static List<Vector3> SmoothPathLaplacian(List<Vector3> points, int iterations = 6)
        {
            if (points == null || points.Count < 3) return points;

            List<Vector3> current = new List<Vector3>(points);

            for (int iter = 0; iter < iterations; iter++)
            {
                List<Vector3> next = new List<Vector3>(current.Count);
                next.Add(current[0]); // Mantener extremo inicial exacto

                for (int i = 1; i < current.Count - 1; i++)
                {
                    Vector3 prev = current[i - 1];
                    Vector3 curr = current[i];
                    Vector3 fwd = current[i + 1];

                    // Filtro Laplaciano 1-2-1
                    Vector3 smoothed = (prev * 0.25f) + (curr * 0.50f) + (fwd * 0.25f);
                    next.Add(smoothed);
                }

                next.Add(current[current.Count - 1]); // Mantener extremo final exacto
                current = next;
            }

            return current;
        }

        private Vector2Int FindFirstGrass()
        {
            for (int x = 2; x < width - 2; x++)
            {
                for (int y = 2; y < height - 2; y++)
                {
                    if (mapGrid[x, y] == CellType.Grass) return new Vector2Int(x, y);
                }
            }
            return Vector2Int.zero;
        }

        private Vector2Int FindBaseEntrance()
        {
            Vector2Int best = Vector2Int.zero;
            float bestScore = float.MaxValue;
            int centerX = width / 2;

            // Buscar en el tercio sur del mapa la casilla de pasto más baja y cercana al centro
            for (int y = 1; y < Mathf.Min(height / 2, 30); y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    if (mapGrid[x, y] == CellType.Grass)
                    {
                        float score = y * 4f + Mathf.Abs(x - centerX);
                        if (score < bestScore)
                        {
                            bestScore = score;
                            best = new Vector2Int(x, y);
                        }
                    }
                }
                if (best != Vector2Int.zero && y >= 6) break;
            }

            return best != Vector2Int.zero ? best : FindFirstGrass();
        }

        private Vector2Int FindTopDestination()
        {
            Vector2Int best = Vector2Int.zero;
            float bestScore = float.MaxValue;
            int centerX = width / 2;

            // Buscar en el tercio norte del mapa la casilla de pasto más alta y cercana al centro
            // Respetando la franja norte de mar abierto continuo (al menos 12 casillas de margen arriba)
            int topLimit = height - 12;
            int bottomLimit = Mathf.Max(height / 2, height - 40);

            for (int y = topLimit; y >= bottomLimit; y--)
            {
                for (int x = 4; x < width - 4; x++)
                {
                    if (mapGrid[x, y] == CellType.Grass)
                    {
                        float score = (topLimit - y) * 3f + Mathf.Abs(x - centerX);
                        if (score < bestScore)
                        {
                            bestScore = score;
                            best = new Vector2Int(x, y);
                        }
                    }
                }
                if (best != Vector2Int.zero && y <= topLimit - 6) break;
            }

            // Fallback: buscar la casilla de pasto con mayor Y disponible en todo el mapa pero bajo topLimit
            if (best == Vector2Int.zero)
            {
                for (int y = topLimit; y >= 4; y--)
                {
                    for (int x = 4; x < width - 4; x++)
                    {
                        if (mapGrid[x, y] == CellType.Grass)
                        {
                            return new Vector2Int(x, y);
                        }
                    }
                }
            }

            return best;
        }

        private Vector2Int FindScenicPathTarget(Vector2Int start)
        {
            // 1. Inundación (Flood Fill) para encontrar la casilla de pasto más lejana EN LA MISMA ISLA
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            List<Vector2Int> reachableGrass = new List<Vector2Int>();

            Vector2Int[] dirs = { Vector2Int.down, Vector2Int.up, Vector2Int.left, Vector2Int.right };
            Vector2Int exitTile = start;

            // Si start es una puerta o estructura, encontrar la casilla de pasto inmediatamente adyacente
            if (mapGrid[start.x, start.y] != CellType.Grass)
            {
                foreach (var d in dirs)
                {
                    Vector2Int n = start + d;
                    if (n.x >= 0 && n.x < width && n.y >= 0 && n.y < height && mapGrid[n.x, n.y] == CellType.Grass)
                    {
                        exitTile = n;
                        break;
                    }
                }
            }

            queue.Enqueue(exitTile);
            visited.Add(exitTile);

            while (queue.Count > 0)
            {
                Vector2Int curr = queue.Dequeue();
                reachableGrass.Add(curr);

                foreach (var d in dirs)
                {
                    Vector2Int neighbor = curr + d;
                    if (neighbor.x >= 0 && neighbor.x < width && neighbor.y >= 0 && neighbor.y < height)
                    {
                        if (!visited.Contains(neighbor) && mapGrid[neighbor.x, neighbor.y] == CellType.Grass)
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            // Seleccionar una casilla de pasto lejana dentro de la misma masa terrestre
            if (reachableGrass.Count > 1)
            {
                Vector2Int best = start;
                float bestDist = 0f;
                foreach (var pt in reachableGrass)
                {
                    float dist = Vector2Int.Distance(start, pt);
                    if (dist > bestDist)
                    {
                        bestDist = dist;
                        best = pt;
                    }
                }
                if (best != start) return best;
            }

            // 2. Fallback: buscar la casilla de pasto más distante en general si no hay en la misma isla
            Vector2Int fallback = start;
            float maxDist = -1f;
            for (int x = 2; x < width - 2; x++)
            {
                for (int y = 2; y < height - 2; y++)
                {
                    if (mapGrid[x, y] == CellType.Grass)
                    {
                        float dist = Vector2Int.Distance(start, new Vector2Int(x, y));
                        if (dist > maxDist)
                        {
                            maxDist = dist;
                            fallback = new Vector2Int(x, y);
                        }
                    }
                }
            }

            return fallback;
        }

        private void ResolveAndDrawPaths()
        {
            if (pathMap == null) return;
            if (wfcDatabase == null && customPathTile == null && customBridgeTile == null)
            {
                Debug.LogWarning("<b>[MapGenerator Caminos]</b> WfcVisualDatabase no está asignada y no hay tiles manuales de camino configurados.");
                return;
            }

            int placedPaths = 0;
            int placedBridges = 0;

            // Determina si una celda es carretera (o puente o puerta de estructura)
            bool IsAnyRoadOrDoor(int nx, int ny)
            {
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) return false;
                if (mapGrid[nx, ny] == CellType.Path || mapGrid[nx, ny] == CellType.Bridge) return true;
                if (doors != null && doors.Contains(new Vector2Int(nx, ny))) return true;
                return false;
            }

            // Determina si una celda de camino debe ser de Piedra (zona urbana alrededor de estructuras) o Tierra (campo abierto)
            bool IsStoneCell(int cx, int cy)
            {
                if (!useHybridRoads) return true; // Si está desactivado, todo el camino usa roadCategory
                if (doors == null || doors.Count == 0) return false;

                foreach (var d in doors)
                {
                    int dist = Mathf.Abs(cx - d.x) + Mathf.Abs(cy - d.y);
                    if (dist <= stoneRoadRadius) return true;
                }
                return false;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool isBridge = (mapGrid[x, y] == CellType.Bridge);
                    bool isPath = (mapGrid[x, y] == CellType.Path);

                    if (isBridge || isPath)
                    {
                        Vector3Int pos = new Vector3Int(x, y, 0);

                        if (isBridge && customBridgeTile != null)
                        {
                            pathMap.SetTile(pos, customBridgeTile);
                            WorldChunk chunk = GetChunkAt(x, y);
                            if (chunk != null) chunk.RecordPath(pos, customBridgeTile);
                            placedBridges++;
                        }
                        else if (isPath && customPathTile != null)
                        {
                            pathMap.SetTile(pos, customPathTile);
                            WorldChunk chunk = GetChunkAt(x, y);
                            if (chunk != null) chunk.RecordPath(pos, customPathTile);
                            placedPaths++;
                        }
                        else if (wfcDatabase != null)
                        {
                            if (isBridge)
                            {
                                bool up    = IsAnyRoadOrDoor(x, y + 1);
                                bool down  = IsAnyRoadOrDoor(x, y - 1);
                                bool left  = IsAnyRoadOrDoor(x - 1, y);
                                bool right = IsAnyRoadOrDoor(x + 1, y);

                                string catToUse = !string.IsNullOrEmpty(bridgeCategory) ? bridgeCategory : roadCategory;
                                TileBase tile = wfcDatabase.GetPathTile(up, down, left, right, catToUse);
                                if (tile == null)
                                {
                                    tile = wfcDatabase.GetPathTile(up, down, left, right, roadCategory);
                                }

                                if (tile != null)
                                {
                                    pathMap.SetTile(pos, tile);
                                    WorldChunk chunk = GetChunkAt(x, y);
                                    if (chunk != null) chunk.RecordPath(pos, tile);
                                    placedBridges++;
                                }
                            }
                            else // isPath
                            {
                                bool isStone = IsStoneCell(x, y);

                                if (isStone)
                                {
                                    // Para piedra: solo conecta con otras celdas de piedra, puentes o puertas.
                                    // Cuando el vecino es camino de tierra, NO conecta como piedra continua,
                                    // sino que dispara el tile de transición hacia la tierra (Road_Trans_*)!
                                    bool ConnectsStone(int nx, int ny)
                                    {
                                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) return false;
                                        if (doors != null && doors.Contains(new Vector2Int(nx, ny))) return true;
                                        if (mapGrid[nx, ny] == CellType.Bridge) return true;
                                        if (mapGrid[nx, ny] == CellType.Path && IsStoneCell(nx, ny)) return true;
                                        return false;
                                    }

                                    bool up    = ConnectsStone(x, y + 1);
                                    bool down  = ConnectsStone(x, y - 1);
                                    bool left  = ConnectsStone(x - 1, y);
                                    bool right = ConnectsStone(x + 1, y);

                                    TileBase tile = wfcDatabase.GetPathTile(up, down, left, right, roadCategory);
                                    if (tile != null)
                                    {
                                        pathMap.SetTile(pos, tile);
                                        WorldChunk chunk = GetChunkAt(x, y);
                                        if (chunk != null) chunk.RecordPath(pos, tile);
                                        placedPaths++;
                                    }
                                }
                                else
                                {
                                    // Para tierra: conecta a cualquier camino (tierra o piedra), puente o puerta.
                                    // Si solo tiene 1 vecino (ej. termina en pasto), usa Road_Cap_* (terminación en hierbajo).
                                    bool up    = IsAnyRoadOrDoor(x, y + 1);
                                    bool down  = IsAnyRoadOrDoor(x, y - 1);
                                    bool left  = IsAnyRoadOrDoor(x - 1, y);
                                    bool right = IsAnyRoadOrDoor(x + 1, y);

                                    string catToUse = !string.IsNullOrEmpty(dirtRoadCategory) ? dirtRoadCategory : roadCategory;
                                    TileBase tile = wfcDatabase.GetPathTile(up, down, left, right, catToUse);
                                    if (tile == null)
                                    {
                                        tile = wfcDatabase.GetPathTile(up, down, left, right, roadCategory);
                                    }

                                    if (tile != null)
                                    {
                                        pathMap.SetTile(pos, tile);
                                        WorldChunk chunk = GetChunkAt(x, y);
                                        if (chunk != null) chunk.RecordPath(pos, tile);
                                        placedPaths++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Debug.Log($"<b>[Paso 4 - Caminos y Puentes]</b> {placedPaths} caminos y {placedBridges} puentes estampados en 'Path Map'.");
        }

        private void DrawStructure(StructureData data, Vector2Int origin)
        {
            if (structureMap == null)
            {
                Debug.LogWarning("<b>[Paso 2 - Estructuras]</b> No se puede dibujar la estructura porque 'structureMap' es null.");
                return;
            }

            int tilesDrawn = 0;
            for (int x = 0; x < data.size.x; x++)
            {
                for (int y = 0; y < data.size.y; y++)
                {
                    TileBase tile = data.GetTile(x, y);
                    if (tile != null)
                    {
                        Vector3Int pos = new Vector3Int(origin.x + x, origin.y + y, 0);
                        structureMap.SetTile(pos, tile);
                        WorldChunk chunk = GetChunkAt(origin.x + x, origin.y + y);
                        if (chunk != null) chunk.RecordStructure(pos, tile);
                        tilesDrawn++;
                    }
                }
            }
            Debug.Log($"<b>[Paso 2 - DrawStructure]</b> '{data.structureName}' dibujó {tilesDrawn} tiles en 'Structure Map'.");
        }

        [ContextMenu("Limpiar")]
        public void ClearTilemaps()
        {
            if (groundMap != null) groundMap.ClearAllTiles();
            if (pathMap != null) pathMap.ClearAllTiles();
            if (structureMap != null) structureMap.ClearAllTiles();

            if (chunks != null)
            {
                foreach (var c in chunks.Values)
                {
                    c.isLoaded = false;
                }
            }
            Debug.Log("<b>[MapGenerator]</b> Tilemaps limpiados.");
        }

        /// <summary>
        /// Pinta una orla/franja exterior de mar abierto continuo alrededor de los límites del mapa
        /// para garantizar que la cámara Shmup jamás vea el vacío hacia el exterior (efecto mundo rodeado de océano).
        /// </summary>
        public void PaintOceanSkirt(int skirtSize = 25)
        {
            if (groundMap == null) return;

            TileBase waterTile = null;
            if (customWaterTile != null)
            {
                waterTile = customWaterTile;
            }
            else if (wfcDatabase != null)
            {
                waterTile = wfcDatabase.GetWaterAutotile(true, true, true, true, true, true, true, true, waterCategory);
            }

            if (waterTile == null) return;

            int minX = -skirtSize;
            int maxX = width + skirtSize;
            int minY = -skirtSize;
            int maxY = height + skirtSize;

            List<Vector3Int> positions = new List<Vector3Int>();
            List<TileBase> tiles = new List<TileBase>();

            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    // Solo casillas estrictamente fuera de la cuadrícula jugable [0, width) x [0, height)
                    if (x < 0 || x >= width || y < 0 || y >= height)
                    {
                        positions.Add(new Vector3Int(x, y, 0));
                        tiles.Add(waterTile);
                    }
                }
            }

            if (positions.Count > 0)
            {
                groundMap.SetTiles(positions.ToArray(), tiles.ToArray());
            }
        }

        #region Sistema de Chunk Map y Visibilidad Dinámica
        public void InitializeChunkGrid()
        {
            chunks.Clear();
            int cSizeX = Mathf.Max(4, chunkSize.x);
            int cSizeY = Mathf.Max(4, chunkSize.y);

            int chunksX = Mathf.CeilToInt((float)width / cSizeX);
            int chunksY = Mathf.CeilToInt((float)height / cSizeY);

            int midX = width / 2;
            int midY = height / 2;

            for (int cx = 0; cx < chunksX; cx++)
            {
                for (int cy = 0; cy < chunksY; cy++)
                {
                    int startX = cx * cSizeX;
                    int startY = cy * cSizeY;
                    int endX = Mathf.Min(width, startX + cSizeX);
                    int endY = Mathf.Min(height, startY + cSizeY);

                    RectInt bounds = new RectInt(startX, startY, endX - startX, endY - startY);
                    Vector2 center = new Vector2(startX + bounds.width * 0.5f, startY + bounds.height * 0.5f);

                    int contId = 0;
                    string contName = "Continente 1: Noroeste";
                    if (center.x < midX && center.y >= midY)
                    {
                        contId = 0; contName = "Continente 1: Noroeste";
                    }
                    else if (center.x >= midX && center.y >= midY)
                    {
                        contId = 1; contName = "Continente 2: Noreste";
                    }
                    else if (center.x < midX && center.y < midY)
                    {
                        contId = 2; contName = "Continente 3: Suroeste";
                    }
                    else
                    {
                        contId = 3; contName = "Continente 4: Sureste";
                    }

                    Vector2Int coord = new Vector2Int(cx, cy);
                    chunks[coord] = new WorldChunk(coord, bounds, contId, contName);
                }
            }
            Debug.Log($"<b>[Chunk Map]</b> Cuadrícula inicializada: {chunksX}x{chunksY} = {chunks.Count} chunks de {cSizeX}x{cSizeY} tiles.");
        }

        public WorldChunk GetChunkAt(int x, int y)
        {
            if (chunks == null || chunks.Count == 0) return null;
            int cSizeX = Mathf.Max(1, chunkSize.x);
            int cSizeY = Mathf.Max(1, chunkSize.y);
            int cx = x / cSizeX;
            int cy = y / cSizeY;
            Vector2Int coord = new Vector2Int(cx, cy);
            if (chunks.TryGetValue(coord, out WorldChunk chunk))
            {
                return chunk;
            }
            foreach (var kvp in chunks)
            {
                if (kvp.Value.ContainsTile(x, y)) return kvp.Value;
            }
            return null;
        }

        public WorldChunk GetChunk(int cx, int cy)
        {
            if (chunks == null) return null;
            chunks.TryGetValue(new Vector2Int(cx, cy), out WorldChunk chunk);
            return chunk;
        }

        public List<WorldChunk> GetChunksInContinent(int continentIndex)
        {
            List<WorldChunk> list = new List<WorldChunk>();
            if (chunks == null) return list;
            foreach (var c in chunks.Values)
            {
                if (c.continentId == continentIndex) list.Add(c);
            }
            return list;
        }

        public void SetChunkDimensions(int cx, int cy)
        {
            chunkSize = new Vector2Int(Mathf.Clamp(cx, 4, width), Mathf.Clamp(cy, 4, height));
            if (chunks != null && chunks.Count > 0)
            {
                GenerateWorld();
            }
        }

        public void FitChunksToFourContinents()
        {
            chunkSize = new Vector2Int(Mathf.CeilToInt(width / 2f), Mathf.CeilToInt(height / 2f));
            if (chunks != null && chunks.Count > 0)
            {
                GenerateWorld();
            }
        }

        public void ApplyChunkVisibility()
        {
            if (chunks == null || chunks.Count == 0) return;

            if (!useChunkMap)
            {
                foreach (var c in chunks.Values)
                {
                    c.Load(groundMap, pathMap, structureMap);
                }
                RefreshTilemaps();
                return;
            }

            switch (chunkStreamingMode)
            {
                case ChunkStreamingMode.AllVisible:
                    foreach (var c in chunks.Values)
                    {
                        c.Load(groundMap, pathMap, structureMap);
                    }
                    break;

                case ChunkStreamingMode.ContinentSection:
                    // Solo el continente seleccionado aparece; los otros 3 continentes desaparecen
                    foreach (var c in chunks.Values)
                    {
                        if (c.continentId == activeContinentSection)
                        {
                            c.Load(groundMap, pathMap, structureMap);
                        }
                        else
                        {
                            c.Unload(groundMap, pathMap, structureMap);
                        }
                    }
                    break;

                case ChunkStreamingMode.StreamingRadius:
                    Vector2Int targetTile = GetTargetTileCoord();
                    Vector2Int targetChunk = GetChunkCoordFromTile(targetTile);

                    foreach (var kvp in chunks)
                    {
                        Vector2Int cCoord = kvp.Key;
                        WorldChunk chunk = kvp.Value;
                        int dist = Mathf.Max(Mathf.Abs(cCoord.x - targetChunk.x), Mathf.Abs(cCoord.y - targetChunk.y));
                        if (dist <= chunkViewDistance)
                        {
                            chunk.Load(groundMap, pathMap, structureMap);
                        }
                        else
                        {
                            chunk.Unload(groundMap, pathMap, structureMap);
                        }
                    }
                    break;

                case ChunkStreamingMode.SingleScreenChunk:
                    targetTile = GetTargetTileCoord();
                    targetChunk = GetChunkCoordFromTile(targetTile);

                    foreach (var kvp in chunks)
                    {
                        if (kvp.Key == targetChunk)
                        {
                            kvp.Value.Load(groundMap, pathMap, structureMap);
                        }
                        else
                        {
                            kvp.Value.Unload(groundMap, pathMap, structureMap);
                        }
                    }
                    break;
            }

            PaintOceanSkirt();
            RefreshTilemaps();
        }

        public void ShowContinent(int continentIndex)
        {
            activeContinentSection = Mathf.Clamp(continentIndex, 0, 3);
            chunkStreamingMode = ChunkStreamingMode.ContinentSection;
            ApplyChunkVisibility();

            Vector2 center = GetContinentCenter(activeContinentSection);
            if (streamingTarget != null)
            {
                streamingTarget.position = new Vector3(center.x, center.y, streamingTarget.position.z);
            }
            else if (Camera.main != null && Application.isPlaying)
            {
                Camera.main.transform.position = new Vector3(center.x, center.y, Camera.main.transform.position.z);
            }

            Debug.Log($"<b>[Chunk Map]</b> Mostrando <b>Continente #{activeContinentSection + 1}</b> ({GetContinentName(activeContinentSection)}). Chunks cargados: {LoadedChunksCount}/{TotalChunks}.");
        }

        public void ShowAllChunks()
        {
            chunkStreamingMode = ChunkStreamingMode.AllVisible;
            ApplyChunkVisibility();
            Debug.Log($"<b>[Chunk Map]</b> Mostrando todos los {TotalChunks} chunks simultáneamente.");
        }

        public void HideAllChunks()
        {
            if (chunks == null) return;
            foreach (var c in chunks.Values)
            {
                c.Unload(groundMap, pathMap, structureMap);
            }
            RefreshTilemaps();
            Debug.Log("<b>[Chunk Map]</b> Todos los chunks ocultados.");
        }

        public Vector2Int GetTargetTileCoord()
        {
            if (streamingTarget != null)
            {
                Vector3 p = streamingTarget.position;
                return new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(p.x), 0, width - 1), Mathf.Clamp(Mathf.FloorToInt(p.y), 0, height - 1));
            }
            if (Camera.main != null)
            {
                Vector3 p = Camera.main.transform.position;
                return new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(p.x), 0, width - 1), Mathf.Clamp(Mathf.FloorToInt(p.y), 0, height - 1));
            }
            Vector2 center = GetContinentCenter(activeContinentSection);
            return new Vector2Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y));
        }

        public Vector2Int GetChunkCoordFromTile(Vector2Int tileCoord)
        {
            int cSizeX = Mathf.Max(1, chunkSize.x);
            int cSizeY = Mathf.Max(1, chunkSize.y);
            return new Vector2Int(tileCoord.x / cSizeX, tileCoord.y / cSizeY);
        }

        public Vector2 GetContinentCenter(int continentIndex)
        {
            float midX = width * 0.5f;
            float midY = height * 0.5f;
            switch (continentIndex)
            {
                case 0: return new Vector2(midX * 0.5f, midY + midY * 0.5f); // Noroeste
                case 1: return new Vector2(midX + midX * 0.5f, midY + midY * 0.5f); // Noreste
                case 2: return new Vector2(midX * 0.5f, midY * 0.5f); // Suroeste
                case 3: return new Vector2(midX + midX * 0.5f, midY * 0.5f); // Sureste
                default: return new Vector2(midX, midY);
            }
        }

        public string GetContinentName(int continentIndex)
        {
            switch (continentIndex)
            {
                case 0: return "Continente 1: Noroeste";
                case 1: return "Continente 2: Noreste";
                case 2: return "Continente 3: Suroeste";
                case 3: return "Continente 4: Sureste";
                default: return $"Continente #{continentIndex + 1}";
            }
        }

        public void RefreshTilemaps()
        {
            if (groundMap != null) groundMap.RefreshAllTiles();
            if (pathMap != null) pathMap.RefreshAllTiles();
            if (structureMap != null) structureMap.RefreshAllTiles();

#if UNITY_EDITOR
            if (groundMap != null) UnityEditor.EditorUtility.SetDirty(groundMap);
            if (pathMap != null) UnityEditor.EditorUtility.SetDirty(pathMap);
            if (structureMap != null) UnityEditor.EditorUtility.SetDirty(structureMap);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
        }

        private void OnDrawGizmos()
        {
            if (!useChunkMap || chunks == null || chunks.Count == 0) return;

            // 1. Dibujar bordes de los Chunks
            foreach (var kvp in chunks)
            {
                WorldChunk chunk = kvp.Value;
                Gizmos.color = chunk.isLoaded ? new Color(0.2f, 1f, 0.4f, 0.7f) : new Color(0.2f, 0.3f, 0.6f, 0.25f);
                Vector3 center = new Vector3(chunk.worldBounds.x + chunk.worldBounds.width * 0.5f, chunk.worldBounds.y + chunk.worldBounds.height * 0.5f, 0);
                Vector3 size = new Vector3(chunk.worldBounds.width, chunk.worldBounds.height, 0.1f);
                Gizmos.DrawWireCube(center, size);
            }

            // 2. Líneas divisorias de los 4 Continentes / 4 Secciones
            if (terrainStyle == TerrainStyle.FourContinents)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.85f);
                Gizmos.DrawLine(new Vector3(width * 0.5f, 0, 0), new Vector3(width * 0.5f, height, 0));
                Gizmos.DrawLine(new Vector3(0, height * 0.5f, 0), new Vector3(width, height * 0.5f, 0));
            }
        }
        #endregion
    }
}

