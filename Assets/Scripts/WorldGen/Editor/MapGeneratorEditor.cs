using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;
using WorldGen.Core;
using WorldGen.Data;

namespace WorldGen.EditorTools
{
    [CustomEditor(typeof(MapGenerator))]
    public class MapGeneratorEditor : UnityEditor.Editor
    {
        private bool showTilemaps = true;

        public override void OnInspectorGUI()
        {
            MapGenerator gen = (MapGenerator)target;
            serializedObject.Update();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🗺️ Panel de Control de Generación de Mundos", EditorStyles.boldLabel);

            // 1. Botones Principales de Ejecución
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.75f, 0.2f); // Golden Orange
            if (GUILayout.Button("🎲 RANDOMIZAR Y GENERAR", GUILayout.Height(38)))
            {
                Undo.RecordObject(gen, "Randomizar y Generar Terreno");
                gen.RandomizeAndGenerate();
                EditorUtility.SetDirty(gen);
                serializedObject.Update();
            }

            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.35f); // Green
            if (GUILayout.Button("🚀 GENERAR MUNDO", GUILayout.Height(38)))
            {
                Undo.RecordObject(gen, "Generar Mundo");
                gen.GenerateWorld();
                EditorUtility.SetDirty(gen);
                serializedObject.Update();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.3f, 0.75f, 1f);
            if (GUILayout.Button("🎲 NUEVA SEMILLA", GUILayout.Height(26)))
            {
                Undo.RecordObject(gen, "Nueva Semilla");
                gen.GenerateWithNewSeed();
                EditorUtility.SetDirty(gen);
                serializedObject.Update();
            }

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("🧹 LIMPIAR", GUILayout.Height(26), GUILayout.Width(100)))
            {
                Undo.RecordObject(gen, "Limpiar Tilemaps");
                gen.ClearTilemaps();
                EditorUtility.SetDirty(gen);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // 1.5 Modo Nivel Procedural (Randomización Automática y Bloqueos)
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎲 Modo Nivel Procedural (Randomización Automática)", EditorStyles.boldLabel);

            SerializedProperty autoRndProp = serializedObject.FindProperty("randomizeParametersOnGenerate");
            EditorGUILayout.PropertyField(autoRndProp, new GUIContent("Randomizar al Generar", "Si está activo, cada vez que generes o entres a un nivel se recalcularán todos los parámetros desbloqueados para crear un mundo totalmente nuevo."));

            if (autoRndProp.boolValue)
            {
                EditorGUILayout.HelpBox("✅ MODO NIVEL NUEVO ACTIVO: Cada generación recalcula estilo, mar, islas, bosques, mesetas y puentes para que el jugador siempre entre a un nivel nuevo y fresco.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("⏸️ Randomización automática desactivada. Se usarán los parámetros manuales configurados abajo.", MessageType.None);
            }

            SerializedProperty useRndSeedProp = serializedObject.FindProperty("useRandomSeed");
            SerializedProperty seedProp = serializedObject.FindProperty("seed");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(useRndSeedProp, new GUIContent("Semilla Aleatoria"), GUILayout.Width(150));
            if (!useRndSeedProp.boolValue)
            {
                EditorGUILayout.PropertyField(seedProp, new GUIContent("Semilla"));
            }
            else
            {
                EditorGUILayout.LabelField($"Semilla actual: {seedProp.intValue}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("🔒 Parámetros Fijos (Bloquear para futuras peticiones):", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔓 Desbloquear Todo", EditorStyles.miniButtonLeft))
            {
                Undo.RecordObject(gen, "Desbloquear Todo");
                gen.lockTerrainStyle = false;
                gen.lockWaterLevel = false;
                gen.lockDimensions = false;
                gen.lockIslandSettings = false;
                gen.lockFeatures = false;
                gen.lockChunkSettings = false;
                EditorUtility.SetDirty(gen);
            }
            if (GUILayout.Button("🔒 Bloquear Todo", EditorStyles.miniButtonMid))
            {
                Undo.RecordObject(gen, "Bloquear Todo");
                gen.lockTerrainStyle = true;
                gen.lockWaterLevel = true;
                gen.lockDimensions = true;
                gen.lockIslandSettings = true;
                gen.lockFeatures = true;
                gen.lockChunkSettings = true;
                EditorUtility.SetDirty(gen);
            }
            if (GUILayout.Button("🗺️ Fijar 4 Continentes", EditorStyles.miniButtonRight))
            {
                Undo.RecordObject(gen, "Fijar 4 Continentes");
                gen.terrainStyle = TerrainStyle.FourContinents;
                gen.lockTerrainStyle = true;
                gen.lockDimensions = true;
                gen.SetDimensions(80, 80);
                gen.FitChunksToFourContinents();
                EditorUtility.SetDirty(gen);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            SerializedProperty lockStyleProp = serializedObject.FindProperty("lockTerrainStyle");
            SerializedProperty lockWaterProp = serializedObject.FindProperty("lockWaterLevel");
            EditorGUILayout.PropertyField(lockStyleProp, new GUIContent("🔒 Estilo"));
            EditorGUILayout.PropertyField(lockWaterProp, new GUIContent("🔒 Nivel Agua"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            SerializedProperty lockDimProp = serializedObject.FindProperty("lockDimensions");
            SerializedProperty lockIslandsProp = serializedObject.FindProperty("lockIslandSettings");
            EditorGUILayout.PropertyField(lockDimProp, new GUIContent("🔒 Dimensiones"));
            EditorGUILayout.PropertyField(lockIslandsProp, new GUIContent("🔒 Islas"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            SerializedProperty lockFeatProp = serializedObject.FindProperty("lockFeatures");
            SerializedProperty lockChunksProp = serializedObject.FindProperty("lockChunkSettings");
            EditorGUILayout.PropertyField(lockFeatProp, new GUIContent("🔒 Mesetas/Bosque"));
            EditorGUILayout.PropertyField(lockChunksProp, new GUIContent("🔒 Chunks"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 2. Dimensiones y Escala (Preajustes Rápidos)
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📐 Dimensiones y Escala del Mundo", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Preajustes Rápidos:");
            if (GUILayout.Button("60x60", EditorStyles.miniButton))
            {
                Undo.RecordObject(gen, "Preset 60x60");
                gen.SetDimensions(60, 60);
                EditorUtility.SetDirty(gen);
            }
            if (GUILayout.Button("100x300 (Shmup Vertical)", EditorStyles.miniButton))
            {
                Undo.RecordObject(gen, "Preset 100x300 Shmup");
                gen.SetDimensions(100, 300);
                gen.SetChunkDimensions(100, 100);
                EditorUtility.SetDirty(gen);
            }
            if (GUILayout.Button("80x80", EditorStyles.miniButton))
            {
                Undo.RecordObject(gen, "Preset 80x80 Continentes");
                gen.SetDimensions(80, 80);
                gen.terrainStyle = TerrainStyle.FourContinents;
                gen.FitChunksToFourContinents();
                EditorUtility.SetDirty(gen);
            }
            if (GUILayout.Button("60x60", EditorStyles.miniButton))
            {
                Undo.RecordObject(gen, "Preset 60x60");
                gen.SetDimensions(60, 60);
                EditorUtility.SetDirty(gen);
            }
            EditorGUILayout.EndHorizontal();

            SerializedProperty widthProp = serializedObject.FindProperty("width");
            SerializedProperty heightProp = serializedObject.FindProperty("height");
            EditorGUILayout.IntSlider(widthProp, 30, 200, new GUIContent("Ancho (Width)"));
            EditorGUILayout.IntSlider(heightProp, 30, 400, new GUIContent("Alto (Height)"));
            EditorGUILayout.LabelField($"• Área total: {widthProp.intValue * heightProp.intValue} casillas. (100x300 recomendado para Shoot 'em Up vertical)", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 2.5 Sistema de Chunk Map y Visibilidad Dinámica (4 Continentes)
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📦 Sistema de Chunk Map (Streaming y 4 Continentes)", EditorStyles.boldLabel);
            SerializedProperty useChunkMapProp = serializedObject.FindProperty("useChunkMap");
            EditorGUILayout.PropertyField(useChunkMapProp, new GUIContent("Activar Chunk Map", "Divide el mundo en chunks modulares y gestiona qué partes aparecen y desaparecen según el mapa."));

            if (useChunkMapProp.boolValue)
            {
                EditorGUILayout.HelpBox("Los chunks aparecen (se cargan) y desaparecen (se descargan) en los Tilemaps según la posición o el continente activo.", MessageType.Info);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Preajustes Chunks:");
                if (GUILayout.Button("100x100 (3 Secciones)", EditorStyles.miniButton))
                {
                    Undo.RecordObject(gen, "Preset Chunks 100x100");
                    gen.SetChunkDimensions(100, 100);
                    EditorUtility.SetDirty(gen);
                }
                if (GUILayout.Button("4 Continentes (40x40)", EditorStyles.miniButton))
                {
                    Undo.RecordObject(gen, "Ajustar Chunks a 4 Continentes");
                    gen.FitChunksToFourContinents();
                    EditorUtility.SetDirty(gen);
                }
                if (GUILayout.Button("20x20", EditorStyles.miniButton))
                {
                    Undo.RecordObject(gen, "Preset Chunks 20x20");
                    gen.SetChunkDimensions(20, 20);
                    EditorUtility.SetDirty(gen);
                }
                EditorGUILayout.EndHorizontal();

                SerializedProperty chunkSizeProp = serializedObject.FindProperty("chunkSize");
                EditorGUILayout.PropertyField(chunkSizeProp, new GUIContent("Tamaño de Chunk (Tiles)"));

                SerializedProperty streamingModeProp = serializedObject.FindProperty("chunkStreamingMode");
                EditorGUILayout.PropertyField(streamingModeProp, new GUIContent("Modo de Visibilidad"));

                if ((ChunkStreamingMode)streamingModeProp.enumValueIndex == ChunkStreamingMode.StreamingRadius)
                {
                    SerializedProperty viewDistProp = serializedObject.FindProperty("chunkViewDistance");
                    EditorGUILayout.IntSlider(viewDistProp, 0, 3, new GUIContent("Radio de Visión"));
                }

                SerializedProperty targetProp = serializedObject.FindProperty("streamingTarget");
                EditorGUILayout.PropertyField(targetProp, new GUIContent("Objetivo Streaming (Jugador/Cámara)"));

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("🧭 Visor de Continentes (Aparecen / Desaparecen Chunks):", EditorStyles.miniBoldLabel);

                EditorGUILayout.BeginHorizontal();
                int activeSec = gen.activeContinentSection;

                GUI.backgroundColor = (activeSec == 0 && gen.chunkStreamingMode == ChunkStreamingMode.ContinentSection) ? Color.green : Color.white;
                if (GUILayout.Button("🌍 1: Noroeste", EditorStyles.miniButtonLeft, GUILayout.Height(26)))
                {
                    Undo.RecordObject(gen, "Ver Continente 1");
                    gen.ShowContinent(0);
                }

                GUI.backgroundColor = (activeSec == 1 && gen.chunkStreamingMode == ChunkStreamingMode.ContinentSection) ? Color.green : Color.white;
                if (GUILayout.Button("🌍 2: Noreste", EditorStyles.miniButtonRight, GUILayout.Height(26)))
                {
                    Undo.RecordObject(gen, "Ver Continente 2");
                    gen.ShowContinent(1);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = (activeSec == 2 && gen.chunkStreamingMode == ChunkStreamingMode.ContinentSection) ? Color.green : Color.white;
                if (GUILayout.Button("🌍 3: Suroeste", EditorStyles.miniButtonLeft, GUILayout.Height(26)))
                {
                    Undo.RecordObject(gen, "Ver Continente 3");
                    gen.ShowContinent(2);
                }

                GUI.backgroundColor = (activeSec == 3 && gen.chunkStreamingMode == ChunkStreamingMode.ContinentSection) ? Color.green : Color.white;
                if (GUILayout.Button("🌍 4: Sureste", EditorStyles.miniButtonRight, GUILayout.Height(26)))
                {
                    Undo.RecordObject(gen, "Ver Continente 4");
                    gen.ShowContinent(3);
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("👁️ Cargar Todos los Chunks", EditorStyles.miniButtonLeft))
                {
                    gen.ShowAllChunks();
                }
                if (GUILayout.Button("🙈 Ocultar Todos", EditorStyles.miniButtonRight))
                {
                    gen.HideAllChunks();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField($"• Chunks Totales: {gen.TotalChunks} | Cargados en Pantalla: {gen.LoadedChunksCount}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"• Continente Activo: {gen.GetContinentName(gen.activeContinentSection)}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 3. Geografía y Archipiélago
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🏝️ Geografía y Océano", EditorStyles.boldLabel);
            SerializedProperty terrainStyleProp = serializedObject.FindProperty("terrainStyle");
            EditorGUILayout.PropertyField(terrainStyleProp, new GUIContent("Estilo de Mundo"));

            SerializedProperty waterLevelProp = serializedObject.FindProperty("waterLevel");
            EditorGUILayout.Slider(waterLevelProp, 0.20f, 0.60f, new GUIContent($"Nivel de Agua ({waterLevelProp.floatValue:P0})"));

            SerializedProperty largeIslandsProp = serializedObject.FindProperty("largeIslands");
            EditorGUILayout.PropertyField(largeIslandsProp, new GUIContent("Islas Grandes (Habitables)", "Crea islas de amplio radio con mar abierto perimetral."));

            SerializedProperty islandCountProp = serializedObject.FindProperty("archipelagoIslandCount");
            EditorGUILayout.IntSlider(islandCountProp, 2, 5, new GUIContent("Cantidad de Islas"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 4. Mesetas Procedurales Adaptables
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("⛰️ Mesetas Dinámicas en Islas", EditorStyles.boldLabel);
            SerializedProperty genPlateausProp = serializedObject.FindProperty("generateDynamicPlateaus");
            EditorGUILayout.PropertyField(genPlateausProp, new GUIContent("Generar Meseta Dinámica", "Crea una meseta adaptada a la escala de la isla con interior poblado de cabañas o cultivos."));

            if (genPlateausProp.boolValue)
            {
                SerializedProperty plateauCountProp = serializedObject.FindProperty("dynamicPlateauCount");
                EditorGUILayout.IntSlider(plateauCountProp, 1, 3, new GUIContent("Cantidad Mesetas"));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 5. Estructuras Urbanas y Elementos Naturales
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🏘️ Estructuras Urbanas (Conectadas con Carreteras)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Edificios como Casas, Fuertes o Torres. Se distribuyen entre las islas y se unen con caminos.", MessageType.None);

            SerializedProperty structsProp = serializedObject.FindProperty("structuresToPlace");
            EditorGUILayout.PropertyField(structsProp, new GUIContent("Estructuras Urbanas"), true);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Añadir Rápido:");
            if (GUILayout.Button("+ Casa", EditorStyles.miniButton)) AddStructureAsset(gen, "Casa");
            if (GUILayout.Button("+ Torre", EditorStyles.miniButton)) AddStructureAsset(gen, "Torre");
            if (GUILayout.Button("+ Fuerte", EditorStyles.miniButton)) AddStructureAsset(gen, "Fuerte");
            if (GUILayout.Button("+ Meseta", EditorStyles.miniButton)) AddStructureAsset(gen, "Meseta");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("🌲 Elementos Naturales (Bosques / Arboledas)", EditorStyles.boldLabel);
            SerializedProperty natProp = serializedObject.FindProperty("naturalFeatures");
            EditorGUILayout.PropertyField(natProp, new GUIContent("Elementos Naturales"), true);

            SerializedProperty natCountProp = serializedObject.FindProperty("naturalFeaturesCount");
            EditorGUILayout.IntSlider(natCountProp, 0, 20, new GUIContent("Cantidad a Colocar"));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Añadir Natural:");
            if (GUILayout.Button("+ Bosque Masivo", EditorStyles.miniButton)) AddNaturalAsset(gen, "Bosque_Masivo");
            if (GUILayout.Button("+ Bosque 3x3", EditorStyles.miniButton)) AddNaturalAsset(gen, "Bosque_3x3");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 6. Caminos y Puentes
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🛣️ Red Vial y Puentes Marítimos", EditorStyles.boldLabel);
            SerializedProperty allowBridgesProp = serializedObject.FindProperty("allowBridges");
            EditorGUILayout.PropertyField(allowBridgesProp, new GUIContent("Puentes sobre Agua", "Traza puentes automáticos sobre el agua entre las distintas islas."));

            SerializedProperty useHybridRoadsProp = serializedObject.FindProperty("useHybridRoads");
            EditorGUILayout.PropertyField(useHybridRoadsProp, new GUIContent("Red Vial Híbrida", "Adoquines de piedra cerca de edificios y senderos de tierra en campo abierto."));

            if (useHybridRoadsProp.boolValue)
            {
                SerializedProperty stoneRadiusProp = serializedObject.FindProperty("stoneRoadRadius");
                EditorGUILayout.IntSlider(stoneRadiusProp, 2, 10, new GUIContent("Radio Zona Piedra"));
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 6.5 CÁMARA CINEMACHINE (MODO SHOOT 'EM UP POR RAÍL)
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎬 CÁMARA CINEMACHINE (Modo Shoot 'em Up por Raíl)", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "🎮 CÓMO FUNCIONA EL MODO CINEMACHINE:\n" +
                "1. OBJETIVO GUÍA ('Cinemachine_PathTarget'): Avanza solo de sur a norte siguiendo las curvas de la carretera.\n" +
                "2. CINEMACHINE CAMERA: Sigue suavemente al objetivo guía con amortiguación PositionComposer.\n" +
                "3. EL AVIÓN: Está confinado en la pantalla. Puedes esquivar con WASD, pero nunca te sales del visor de Cinemachine.",
                MessageType.Info);

            EditorGUILayout.Space(2);

            // Botón Maestro Cinemachine 1-Clic
            GUI.backgroundColor = new Color(0.1f, 0.85f, 0.45f);
            if (GUILayout.Button("🎬 CONFIGURAR CINEMACHINE + OBJETIVO DE RAÍL + AVIÓN (1-CLIC)", GUILayout.Height(38)))
            {
                SetupCinemachineShmupScene(gen);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("⚙️ AJUSTES EN VIVO (CINEMACHINE / RAÍL):", EditorStyles.miniBoldLabel);

            SerializedProperty autoShmupProp = serializedObject.FindProperty("autoPositionShmupOnGenerate");
            EditorGUILayout.PropertyField(autoShmupProp, new GUIContent("Reposicionar al Generar", "Al generar o randomizar el mapa, sitúa la cámara y el avión en el inicio del camino al sur."));

            SerializedProperty shmupPlayerProp = serializedObject.FindProperty("shmupPlayer");
            EditorGUILayout.PropertyField(shmupPlayerProp, new GUIContent("Avión (Encerrado)", "Transform del avión del jugador que estará confinado en la pantalla."));

            SerializedProperty shmupCameraProp = serializedObject.FindProperty("shmupCamera");
            EditorGUILayout.PropertyField(shmupCameraProp, new GUIContent("Cámara Principal", "Main Camera con CinemachineBrain."));

            // Ajustes directos de Cinemachine (Punto Guía y Zoom Panorámico)
            var cmTarget = Object.FindAnyObjectByType<WorldGen.Shmup.CinemachineShmupTarget>();
            var cmCam = Object.FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Include);
            if (cmTarget != null || cmCam != null)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("🎥 Cámara y Recorrido Cinemachine:", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                if (cmTarget != null)
                {
                    cmTarget.scrollSpeed = EditorGUILayout.Slider(new GUIContent("⏩ Velocidad Recorrido", "Velocidad de avance pausado de la cámara a lo largo de la carretera."), cmTarget.scrollSpeed, 0.5f, 6.0f);
                    cmTarget.smoothTime = EditorGUILayout.Slider(new GUIContent("🕊️ Suavizado Curvas", "Amortiguación suave para que la cámara no dé tirones."), cmTarget.smoothTime, 0.1f, 1.5f);
                }
                if (cmCam != null)
                {
                    cmCam.Lens.OrthographicSize = EditorGUILayout.Slider(new GUIContent("🔍 Zoom Panorámico (Size)", "Tamaño ortográfico: 14 para ver ciudades e islas completas; menos para ver más de cerca."), cmCam.Lens.OrthographicSize, 6f, 25f);
                }
                EditorGUI.indentLevel--;
            }

            // Ajustes directos del Avión (Vuelo Libre & Combate)
            var planeCtrl = (gen.shmupPlayer != null) ? gen.shmupPlayer.GetComponent<WorldGen.Shmup.ShmupPlaneController>() : Object.FindAnyObjectByType<WorldGen.Shmup.ShmupPlaneController>();
            if (planeCtrl != null)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("✈️ Avión del Jugador (Vuelo Libre & Combate):", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                planeCtrl.followFlightPath = EditorGUILayout.Toggle(new GUIContent("🛤️ Autopiloto Carretera", "Si está desactivado (Recomendado), el avión vuela libremente con WASD encajado en la pantalla."), planeCtrl.followFlightPath);
                if (!planeCtrl.followFlightPath)
                {
                    planeCtrl.freeMoveSpeed = EditorGUILayout.Slider(new GUIContent("⚡ Velocidad WASD", "Velocidad de maniobra del avión para esquivar y limpiar zonas."), planeCtrl.freeMoveSpeed, 4.0f, 25.0f);
                    planeCtrl.rotationMode = (WorldGen.Shmup.PlaneRotationMode)EditorGUILayout.EnumPopup(new GUIContent("🎯 Modo Rotación", "FixedUp: Clásico arcade mirando arriba. Omnidirectional: 360° según WASD. AlignWithPath: Sigue orientación de la ruta."), planeCtrl.rotationMode);
                }
                else
                {
                    planeCtrl.flightSpeed = EditorGUILayout.Slider(new GUIContent("⚡ Velocidad de Vuelo", "Velocidad de crucero del avión a lo largo de la carretera."), planeCtrl.flightSpeed, 1.0f, 12.0f);
                    planeCtrl.lateralDodgeSpeed = EditorGUILayout.Slider(new GUIContent("↔️ Velocidad Esquive", "Velocidad al esquivar a izquierda y derecha con A / D."), planeCtrl.lateralDodgeSpeed, 2.0f, 12.0f);
                }
                planeCtrl.canShoot = EditorGUILayout.Toggle(new GUIContent("💥 Disparo Activado", "Permite disparar proyectiles con Espacio o Clic Izquierdo."), planeCtrl.canShoot);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.8f, 1f);
            if (GUILayout.Button("✈️ Reposicionar al Inicio del Camino", GUILayout.Height(30)))
            {
                if (!Application.isPlaying) Undo.RecordObject(gen, "Posicionar Shmup al Camino");
                gen.PositionShmupAtRoadStart();
                if (!Application.isPlaying) EditorUtility.SetDirty(gen);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Diagnóstico en vivo
            DrawCameraPathDiagnostics(gen);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 7. Jefe Final / Personaje al Final del Mapa
            EditorGUILayout.LabelField("👑 Jefe Final / Personaje al Final del Mapa (Tanque)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            SerializedProperty autoEndCharProp = serializedObject.FindProperty("autoPositionEndCharacterOnGenerate");
            EditorGUILayout.PropertyField(autoEndCharProp, new GUIContent("Spawn / Reposicionar al Generar", "Al generar o randomizar el mapa, sitúa automáticamente al personaje en el extremo final del camino."));

            SerializedProperty endPrefabProp = serializedObject.FindProperty("endLevelCharacterPrefab");
            EditorGUILayout.PropertyField(endPrefabProp, new GUIContent("Prefab Personaje (Tanque)", "Prefab a instanciar al final del mapa."));

            SerializedProperty endInstanceProp = serializedObject.FindProperty("endLevelCharacterInstance");
            EditorGUILayout.PropertyField(endInstanceProp, new GUIContent("Instancia en Escena", "Si ya existe en la jerarquía, se reubicará al final en vez de instanciar uno nuevo."));

            SerializedProperty endOffsetProp = serializedObject.FindProperty("endLevelSpawnOffset");
            EditorGUILayout.PropertyField(endOffsetProp, new GUIContent("Offset de Posición", "Desplazamiento adicional respecto al final del camino."));

            SerializedProperty endRotProp = serializedObject.FindProperty("endLevelCharacterRotationZ");
            EditorGUILayout.PropertyField(endRotProp, new GUIContent("Rotación Z (Encarado)", "0° = alineado en el eje sin rotar."));

            SerializedProperty ensureSgProp = serializedObject.FindProperty("ensureSortingGroupOnEndCharacter");
            EditorGUILayout.PropertyField(ensureSgProp, new GUIContent("Asegurar SortingGroup", "Garantiza que las piezas 2D mantengan su orden de capas relativo."));

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.45f, 0.3f);
            if (GUILayout.Button("🎯 Posicionar Jefe al Final del Camino", GUILayout.Height(30)))
            {
                if (!Application.isPlaying) Undo.RecordObject(gen, "Posicionar Jefe al Final");
                gen.PositionOrSpawnEndCharacter();
                if (!Application.isPlaying) EditorUtility.SetDirty(gen);
            }

            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.35f);
            if (GUILayout.Button("🕷️ Configurar / Seleccionar Tanque Araña", GUILayout.Height(30)))
            {
                SpiderTankSetupUtility.SetupSpiderTankInScene();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 8. Base de Datos y Tilemaps
            showTilemaps = EditorGUILayout.Foldout(showTilemaps, "🎨 Tilemaps y Base de Datos Visual WFC");
            if (showTilemaps)
            {
                EditorGUILayout.BeginVertical("box");
                SerializedProperty groundProp = serializedObject.FindProperty("groundMap");
                SerializedProperty pathProp = serializedObject.FindProperty("pathMap");
                SerializedProperty structMapProp = serializedObject.FindProperty("structureMap");
                SerializedProperty wfcProp = serializedObject.FindProperty("wfcDatabase");

                EditorGUILayout.PropertyField(groundProp, new GUIContent("Ground Map (Suelo)"));
                EditorGUILayout.PropertyField(pathProp, new GUIContent("Path Map (Caminos)"));
                EditorGUILayout.PropertyField(structMapProp, new GUIContent("Structure Map (Edificios)"));
                EditorGUILayout.PropertyField(wfcProp, new GUIContent("WFC Database (Atlas)"));
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(4);

            // 8. Diagnóstico en Vivo
            DrawDiagnostics(gen);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCameraPathDiagnostics(MapGenerator gen)
        {
            var mainCam = (gen.shmupCamera != null) ? gen.shmupCamera : Camera.main;
            if (mainCam == null)
            {
                var camObj = GameObject.Find("Main Camera");
                if (camObj != null) mainCam = camObj.GetComponent<Camera>();
            }

            var brain = (mainCam != null) ? mainCam.GetComponent<CinemachineBrain>() : null;
            var cmTarget = Object.FindAnyObjectByType<WorldGen.Shmup.CinemachineShmupTarget>();
            var cmCam = Object.FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Include);

            if (brain != null && brain.enabled && cmTarget != null && cmCam != null && cmCam.gameObject.activeInHierarchy)
            {
                EditorGUILayout.LabelField($"• Cinemachine 2D: Activo y calibrado (Objetivo: '{cmTarget.name}', Vel: {cmTarget.scrollSpeed:F1})", EditorStyles.miniLabel);
            }
            else
            {
                if (mainCam == null)
                {
                    EditorGUILayout.HelpBox("⚠️ No se encontró 'Main Camera' en la escena.", MessageType.Warning);
                }
                else
                {
                    var camCtrl = mainCam.GetComponent<WorldGen.Shmup.ShmupCameraController>();
                    if (camCtrl != null)
                    {
                        EditorGUILayout.LabelField($"• ShmupCameraController: Activo en '{mainCam.name}' (Velocidad: {camCtrl.scrollSpeed:F1})", EditorStyles.miniLabel);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("ℹ️ Pulsa el botón verde para configurar Cinemachine 2D automáticamente.", MessageType.Info);
                    }
                }
            }

            // Comprobar avión
            var planeCtrl = Object.FindAnyObjectByType<WorldGen.Shmup.ShmupPlaneController>();
            if (planeCtrl == null)
            {
                EditorGUILayout.HelpBox("⚠️ No hay 'PlayerPlane_Shmup' en la escena. Pulsa el botón verde para crearlo.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"• Avión '{planeCtrl.name}': Enlazado y confinado a la pantalla", EditorStyles.miniLabel);
            }

            // Puntos de camino
            int pathPointsCount = (gen.optimalFlightPath != null && gen.optimalFlightPath.Count > 0) ? gen.optimalFlightPath.Count : ((gen.roadPathPoints != null) ? gen.roadPathPoints.Count : 0);
            if (pathPointsCount > 0)
            {
                EditorGUILayout.LabelField($"• Camino procedimental: {pathPointsCount} puntos listos para recorrer.", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("ℹ️ El mapa aún no tiene camino generado. Pulsa 'Generar Mapa' o 'Randomizar' para trazar la ruta.", MessageType.None);
            }
        }

        [MenuItem("Tools/Shmup/🚀 Configurar Cámara del Camino + Avión")]
        public static void SetupCameraPathShmupMenu()
        {
            var gen = Object.FindAnyObjectByType<MapGenerator>();
            SetupCameraPathShmupScene(gen);
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Object.Destroy(obj);
            }
            else
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }

        public static void SetupCameraPathShmupScene(MapGenerator gen)
        {
            // 1. REPARAR Y LIMPIAR EL AVIÓN (PlayerPlane_Shmup)
            GameObject planeObj = GameObject.Find("PlayerPlane_Shmup");
            if (planeObj == null)
            {
                var existingPlane = Object.FindAnyObjectByType<WorldGen.Shmup.ShmupPlaneController>();
                if (existingPlane != null) planeObj = existingPlane.gameObject;
            }
            if (planeObj == null)
            {
                var allRenderers = Object.FindObjectsByType<SpriteRenderer>();
                foreach (var r in allRenderers)
                {
                    if (r.sprite != null && r.sprite.name.Contains("Ki21"))
                    {
                        planeObj = r.gameObject;
                        break;
                    }
                }
            }
            if (planeObj == null)
            {
                planeObj = new GameObject("PlayerPlane_Shmup");
                if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(planeObj, "Crear Avión Shmup");
            }

            planeObj.name = "PlayerPlane_Shmup";

            // Eliminar componentes accidentales que NO pertenecen al avión
            var strayCams = planeObj.GetComponents<Camera>();
            foreach (var c in strayCams) SafeDestroy(c);

            var strayURPData = planeObj.GetComponents<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            foreach (var u in strayURPData) SafeDestroy(u);

            var strayListeners = planeObj.GetComponents<AudioListener>();
            foreach (var a in strayListeners) SafeDestroy(a);

            var strayBrains = planeObj.GetComponents<CinemachineBrain>();
            foreach (var b in strayBrains) SafeDestroy(b);

            var strayCamCtrls = planeObj.GetComponents<WorldGen.Shmup.ShmupCameraController>();
            foreach (var s in strayCamCtrls) SafeDestroy(s);

            var strayBullets = planeObj.GetComponents<WorldGen.Shmup.ShmupBullet>();
            foreach (var sb in strayBullets) SafeDestroy(sb);

            // Configurar SpriteRenderer
            var sr = planeObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = planeObj.AddComponent<SpriteRenderer>();

            if (sr.sprite == null)
            {
                string[] guids = AssetDatabase.FindAssets("JAP_Ki21 t:Texture2D");
                if (guids != null && guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var a in allAssets)
                    {
                        if (a is Sprite s)
                        {
                            sr.sprite = s;
                            break;
                        }
                    }
                }
            }
            sr.sortingOrder = 50;
            planeObj.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

            // Configurar ShmupPlaneController
            var planeCtrl = planeObj.GetComponent<WorldGen.Shmup.ShmupPlaneController>();
            if (planeCtrl == null) planeCtrl = planeObj.AddComponent<WorldGen.Shmup.ShmupPlaneController>();
            planeCtrl.followFlightPath = true; // Sigue la carretera generada en horizontal y vertical
            planeCtrl.flightSpeed = 4.5f;
            planeCtrl.lateralDodgeSpeed = 6.5f;
            planeCtrl.maxLateralDodge = 3.5f;
            planeCtrl.maxBankAngle = 20f;
            planeCtrl.canShoot = true;

            // 2. REPARAR CÁMARA PRINCIPAL (Main Camera)
            GameObject mainCamObj = GameObject.Find("Main Camera");
            if (mainCamObj == null)
            {
                var camWithTag = GameObject.FindWithTag("MainCamera");
                if (camWithTag != null) mainCamObj = camWithTag;
            }
            if (mainCamObj == null)
            {
                mainCamObj = new GameObject("Main Camera");
                mainCamObj.tag = "MainCamera";
                if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(mainCamObj, "Crear Main Camera");
            }

            // Asegurar Camera de Unity en Main Camera
            Camera unityCam = mainCamObj.GetComponent<Camera>();
            if (unityCam == null) unityCam = mainCamObj.AddComponent<Camera>();
            unityCam.orthographic = true;
            unityCam.orthographicSize = 10f;
            unityCam.nearClipPlane = 0.3f;
            unityCam.farClipPlane = 1000f;
            unityCam.clearFlags = CameraClearFlags.SolidColor;
            unityCam.backgroundColor = new Color(0.192f, 0.302f, 0.475f, 1f);

            // URP Camera Data
            var urpData = mainCamObj.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (urpData == null) urpData = mainCamObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

            // AudioListener
            var listener = mainCamObj.GetComponent<AudioListener>();
            if (listener == null) listener = mainCamObj.AddComponent<AudioListener>();

            // DESACTIVAR CinemachineBrain para que no interfiera con el camino
            var brain = mainCamObj.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                brain.enabled = false;
            }

            // Desactivar cualquier CinemachineCamera en escena
            var cmCams = Object.FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include);
            foreach (var cm in cmCams)
            {
                cm.gameObject.SetActive(false);
            }

            // Configurar ShmupCameraController en la cámara
            var camCtrl = mainCamObj.GetComponent<WorldGen.Shmup.ShmupCameraController>();
            if (camCtrl == null) camCtrl = mainCamObj.AddComponent<WorldGen.Shmup.ShmupCameraController>();

            camCtrl.targetPlane = planeObj.transform;
            camCtrl.isScrolling = true;
            camCtrl.scrollSpeed = 2.2f;
            camCtrl.smoothTimeX = 0.5f;
            camCtrl.cameraOffset = new Vector3(0f, 3.5f, -10f);

            planeCtrl.targetCamera = unityCam;

            // 3. POSICIONAR AL INICIO DEL CAMINO
            if (gen != null)
            {
                gen.shmupPlayer = planeObj.transform;
                gen.shmupCamera = unityCam;

                // Si ya hay camino generado, inicializar la ruta en la cámara
                var pathList = (gen.optimalFlightPath != null && gen.optimalFlightPath.Count > 0) ? gen.optimalFlightPath : gen.roadPathPoints;
                if (pathList != null && pathList.Count > 0)
                {
                    camCtrl.SetupAtPathStart(gen.roadStartPosition, pathList);
                }

                gen.PositionShmupAtRoadStart();
            }
            else
            {
                planeObj.transform.position = new Vector3(0f, 0f, 0f);
                mainCamObj.transform.position = new Vector3(0f, 3.5f, -10f);
            }

            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(planeObj);
                EditorUtility.SetDirty(mainCamObj);
                if (gen != null) EditorUtility.SetDirty(gen);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            Selection.activeGameObject = mainCamObj;

            Debug.Log("<color=green><b>[Shmup Rail Camera]</b> ¡Configuración 100% Exitosa!</color>\n" +
                      "• La CÁMARA ahora conduce y avanza por el camino procedimental a velocidad constante.\n" +
                      "• El AVIÓN está encerrado en la pantalla de la cámara (se mueve con WASD pero no puede salirse).\n" +
                      "• Cinemachine desactivado para evitar conflictos.\n" +
                      "• Ambos posicionados en la entrada sur del camino.");
        }

        [MenuItem("Tools/Shmup/🎬 Configurar Modo Cinemachine 2D")]
        public static void SetupCinemachineShmupMenu()
        {
            var gen = Object.FindAnyObjectByType<MapGenerator>();
            SetupCinemachineShmupScene(gen);
        }

        public static void SetupCinemachineShmupScene(MapGenerator gen)
        {
            // 1. OBJETIVO GUÍA INVISIBLE PARA CINEMACHINE (Cinemachine_PathTarget)
            GameObject targetObj = GameObject.Find("Cinemachine_PathTarget");
            if (targetObj == null)
            {
                var existingTarget = Object.FindAnyObjectByType<WorldGen.Shmup.CinemachineShmupTarget>();
                if (existingTarget != null) targetObj = existingTarget.gameObject;
            }
            if (targetObj == null)
            {
                targetObj = new GameObject("Cinemachine_PathTarget");
                if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(targetObj, "Crear Objetivo Cinemachine");
            }
            targetObj.name = "Cinemachine_PathTarget";
            targetObj.SetActive(true);

            var cmTarget = targetObj.GetComponent<WorldGen.Shmup.CinemachineShmupTarget>();
            if (cmTarget == null) cmTarget = targetObj.AddComponent<WorldGen.Shmup.CinemachineShmupTarget>();
            cmTarget.isScrolling = true;
            cmTarget.scrollSpeed = 1.6f;
            cmTarget.smoothTime = 0.55f;

            // 2. REPARAR Y LIMPIAR EL AVIÓN (PlayerPlane_Shmup)
            GameObject planeObj = GameObject.Find("PlayerPlane_Shmup");
            if (planeObj == null)
            {
                var existingPlane = Object.FindAnyObjectByType<WorldGen.Shmup.ShmupPlaneController>();
                if (existingPlane != null) planeObj = existingPlane.gameObject;
            }
            if (planeObj == null)
            {
                var allRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
                foreach (var r in allRenderers)
                {
                    if (r.sprite != null && r.sprite.name.Contains("Ki21"))
                    {
                        planeObj = r.gameObject;
                        break;
                    }
                }
            }
            if (planeObj == null)
            {
                planeObj = new GameObject("PlayerPlane_Shmup");
                if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(planeObj, "Crear Avión Shmup");
            }

            planeObj.name = "PlayerPlane_Shmup";
            planeObj.SetActive(true);

            // Eliminar componentes erróneos
            var strayCams = planeObj.GetComponents<Camera>();
            foreach (var c in strayCams) SafeDestroy(c);
            var strayBrains = planeObj.GetComponents<CinemachineBrain>();
            foreach (var b in strayBrains) SafeDestroy(b);
            var strayCamCtrls = planeObj.GetComponents<WorldGen.Shmup.ShmupCameraController>();
            foreach (var s in strayCamCtrls) SafeDestroy(s);
            var strayBullets = planeObj.GetComponents<WorldGen.Shmup.ShmupBullet>();
            foreach (var sb in strayBullets) SafeDestroy(sb);

            var sr = planeObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = planeObj.AddComponent<SpriteRenderer>();
            if (sr.sprite == null)
            {
                string[] guids = AssetDatabase.FindAssets("JAP_Ki21 t:Texture2D");
                if (guids != null && guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                    foreach (var a in allAssets)
                    {
                        if (a is Sprite s) { sr.sprite = s; break; }
                    }
                }
            }
            sr.sortingOrder = 50;
            planeObj.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

            var planeCtrl = planeObj.GetComponent<WorldGen.Shmup.ShmupPlaneController>();
            if (planeCtrl == null) planeCtrl = planeObj.AddComponent<WorldGen.Shmup.ShmupPlaneController>();
            planeCtrl.followFlightPath = false; // Vuelo libre con WASD dentro de la pantalla
            planeCtrl.freeMoveSpeed = 11.5f;
            planeCtrl.lateralDodgeSpeed = 6.5f;
            planeCtrl.maxLateralDodge = 3.5f;
            planeCtrl.maxBankAngle = 20f;
            planeCtrl.canShoot = true;
            planeCtrl.rotationMode = WorldGen.Shmup.PlaneRotationMode.FixedUp;

            // 3. CÁMARA PRINCIPAL (Main Camera)
            GameObject mainCamObj = GameObject.Find("Main Camera");
            if (mainCamObj == null)
            {
                var camWithTag = GameObject.FindWithTag("MainCamera");
                if (camWithTag != null) mainCamObj = camWithTag;
            }
            if (mainCamObj == null)
            {
                mainCamObj = new GameObject("Main Camera");
                mainCamObj.tag = "MainCamera";
                if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(mainCamObj, "Crear Main Camera");
            }
            mainCamObj.SetActive(true);

            Camera unityCam = mainCamObj.GetComponent<Camera>();
            if (unityCam == null) unityCam = mainCamObj.AddComponent<Camera>();
            unityCam.orthographic = true;
            unityCam.orthographicSize = 14f; // Cámara alejada para contemplar ciudades e islas
            unityCam.nearClipPlane = 0.3f;
            unityCam.farClipPlane = 1000f;
            unityCam.clearFlags = CameraClearFlags.SolidColor;
            unityCam.backgroundColor = new Color(0.192f, 0.302f, 0.475f, 1f);

            var urpData = mainCamObj.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (urpData == null) urpData = mainCamObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

            var listener = mainCamObj.GetComponent<AudioListener>();
            if (listener == null) listener = mainCamObj.AddComponent<AudioListener>();

            // Quitar ShmupCameraController si lo tenía para que Cinemachine mande al 100%
            var oldCamCtrl = mainCamObj.GetComponent<WorldGen.Shmup.ShmupCameraController>();
            if (oldCamCtrl != null) SafeDestroy(oldCamCtrl);

            // Asegurar CinemachineBrain activo
            var brain = mainCamObj.GetComponent<CinemachineBrain>();
            if (brain == null) brain = mainCamObj.AddComponent<CinemachineBrain>();
            brain.enabled = true;
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);

            planeCtrl.targetCamera = unityCam;

            // 4. CINEMACHINE CAMERA 2D
            CinemachineCamera cmCam = Object.FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Include);
            if (cmCam == null)
            {
                GameObject cmObj = new GameObject("CinemachineCamera_2D");
                if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(cmObj, "Crear CinemachineCamera");
                cmCam = cmObj.AddComponent<CinemachineCamera>();
            }
            cmCam.gameObject.SetActive(true);

            // Cinemachine sigue al PUNTO GUÍA (Cinemachine_PathTarget) que recorre la carretera lentamente
            cmCam.Target.TrackingTarget = targetObj.transform;
            cmCam.Target.CustomLookAtTarget = false;
            cmCam.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            cmCam.Lens.OrthographicSize = 14f; // Cámara alejada para ver todas las ciudades y archipiélagos
            cmCam.Lens.NearClipPlane = 0.1f;
            cmCam.Lens.FarClipPlane = 5000f;

            var composer = cmCam.GetComponent<CinemachinePositionComposer>();
            if (composer == null) composer = cmCam.gameObject.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.Damping = new Vector3(0.55f, 0.55f, 0f);
            composer.Composition.ScreenPosition = Vector2.zero; // Punto guía centrado
            composer.CenterOnActivate = true;

            // 5. SINCRONIZAR Y POSICIONAR AL INICIO DEL CAMINO
            if (gen != null)
            {
                gen.shmupPlayer = planeObj.transform;
                gen.shmupCamera = unityCam;

                var pathList = (gen.optimalFlightPath != null && gen.optimalFlightPath.Count > 0) ? gen.optimalFlightPath : gen.roadPathPoints;
                if (pathList != null && pathList.Count > 0)
                {
                    planeCtrl.SetFlightPath(pathList, gen.roadStartPosition);
                    cmTarget.SetupAtPathStart(gen.roadStartPosition, pathList);
                }

                gen.PositionShmupAtRoadStart();
            }
            else
            {
                planeObj.transform.position = Vector3.zero;
                targetObj.transform.position = Vector3.zero;
                mainCamObj.transform.position = new Vector3(0f, 0f, -10f);
                cmCam.transform.position = new Vector3(0f, 0f, -10f);
            }

            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(planeObj);
                EditorUtility.SetDirty(targetObj);
                EditorUtility.SetDirty(mainCamObj);
                EditorUtility.SetDirty(cmCam.gameObject);
                if (gen != null) EditorUtility.SetDirty(gen);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            Selection.activeGameObject = cmCam.gameObject;

            Debug.Log("<color=green><b>[Cinemachine 2D Shmup]</b> ¡Configuración 100% Exitosa!</color>\n" +
                      "• Punto Guía 'Cinemachine_PathTarget': Recorre la carretera lentamente (scrollSpeed = 1.6).\n" +
                      "• CinemachineCamera_2D: Sigue a 'Cinemachine_PathTarget' con vista alejada (OrthographicSize = 14) para ver ciudades e islas.\n" +
                      "• Avión del Jugador: Encajado en la pantalla de la cámara con libertad total de movimiento (WASD) para combatir y limpiar zonas.\n" +
                      "• Main Camera: Controlada por CinemachineBrain.");
        }

        private void AddStructureAsset(MapGenerator gen, string assetName)
        {
            string[] guids = AssetDatabase.FindAssets($"{assetName} t:StructureData");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var asset = AssetDatabase.LoadAssetAtPath<StructureData>(path);
                if (asset != null)
                {
                    Undo.RecordObject(gen, "Añadir Estructura");
                    if (gen.structuresToPlace == null) gen.structuresToPlace = new System.Collections.Generic.List<StructureData>();
                    if (!gen.structuresToPlace.Contains(asset))
                    {
                        gen.structuresToPlace.Add(asset);
                    }
                    EditorUtility.SetDirty(gen);
                }
            }
        }

        private void AddNaturalAsset(MapGenerator gen, string assetName)
        {
            string[] guids = AssetDatabase.FindAssets($"{assetName} t:StructureData");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var asset = AssetDatabase.LoadAssetAtPath<StructureData>(path);
                if (asset != null)
                {
                    Undo.RecordObject(gen, "Añadir Elemento Natural");
                    if (gen.naturalFeatures == null) gen.naturalFeatures = new System.Collections.Generic.List<StructureData>();
                    if (!gen.naturalFeatures.Contains(asset))
                    {
                        gen.naturalFeatures.Add(asset);
                    }
                    EditorUtility.SetDirty(gen);
                }
            }
        }

        private void DrawDiagnostics(MapGenerator gen)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("🔍 Diagnóstico Rápido:", EditorStyles.miniBoldLabel);

            // 1. Tilemaps
            if (gen.groundMap == null)
            {
                EditorGUILayout.HelpBox("❌ Falta asignar 'Ground Map' (Tilemap de Suelo).", MessageType.Error);
            }
            if (gen.pathMap == null)
            {
                EditorGUILayout.HelpBox("⚠️ 'Path Map' no está asignado (no se verán caminos).", MessageType.Warning);
            }
            if (gen.structureMap == null)
            {
                EditorGUILayout.HelpBox("⚠️ 'Structure Map' no está asignado (no se verán tus edificios).", MessageType.Warning);
            }

            // 2. Base de Datos
            if (gen.wfcDatabase == null && gen.customGrassTile == null)
            {
                EditorGUILayout.HelpBox("❌ Falta asignar 'Wfc Database' o un 'Custom Grass Tile'.", MessageType.Error);
            }
            else if (gen.wfcDatabase != null)
            {
                int prado = gen.wfcDatabase.GetTilesByCategory("Prado").Count;
                int caminoPiedra = gen.wfcDatabase.GetTilesByCategory(gen.roadCategory).Count;
                int caminoTierra = gen.wfcDatabase.GetTilesByCategory(gen.dirtRoadCategory).Count;
                int agua = gen.wfcDatabase.GetTilesByCategory(gen.waterCategory).Count;
                int puente = gen.wfcDatabase.GetTilesByCategory(gen.bridgeCategory).Count;

                if (prado == 0 && gen.customGrassTile == null)
                {
                    EditorGUILayout.HelpBox($"⚠️ Tu base de datos '{gen.wfcDatabase.name}' no tiene tiles en la categoría 'Prado'. El pasto saldrá invisible.", MessageType.Warning);
                }
                else
                {
                    string roadInfo = gen.useHybridRoads 
                        ? $"Piedra [{gen.roadCategory}] ({caminoPiedra}), Tierra [{gen.dirtRoadCategory}] ({caminoTierra})" 
                        : $"Caminos [{gen.roadCategory}] ({caminoPiedra})";
                    EditorGUILayout.LabelField($"• DB: Prado ({prado}), {roadInfo}, Agua [{gen.waterCategory}] ({agua})", EditorStyles.miniLabel);
                }
            }

            // 3. Estilo de Terreno y Conectividad
            EditorGUILayout.LabelField($"• Geografía: {gen.terrainStyle} | Nivel de Agua: {gen.waterLevel:P0}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"• Puentes sobre Agua: {(gen.allowBridges ? "Activados (conecta islas)" : "Desactivados")}", EditorStyles.miniLabel);

            // 4. Chunk Map
            if (gen.useChunkMap)
            {
                EditorGUILayout.LabelField($"• Chunk Map: Activo | {gen.TotalChunks} chunks ({gen.chunkSize.x}x{gen.chunkSize.y}) | Visibles: {gen.LoadedChunksCount}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"• Modo: {gen.chunkStreamingMode} | Continente: {gen.GetContinentName(gen.activeContinentSection)}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("• Chunk Map: Desactivado (modo pantalla completa tradicional)", EditorStyles.miniLabel);
            }

            // 5. Estructuras
            if (gen.structuresToPlace == null || gen.structuresToPlace.Count == 0)
            {
                EditorGUILayout.LabelField("• Estructuras: Ninguna en la lista.", EditorStyles.miniLabel);
            }
            else
            {
                int validCount = 0;
                foreach (var s in gen.structuresToPlace) if (s != null) validCount++;
                EditorGUILayout.LabelField($"• Estructuras asignadas: {validCount} lista(s) para estampar.", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
