using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using WorldGen.Data;

namespace WorldGen.EditorTools
{
    [CustomEditor(typeof(StructureData))]
    public class StructureDataEditor : UnityEditor.Editor
    {
        private StructureData structure;
        private bool showDefaultList = false;
        private int selectedTool = 0; // 0 = Pintar/Asignar, 1 = Marcar Puerta, 2 = Borrar
        private readonly string[] toolNames = { "🎨 Pintar / Asignar", "🚪 Marcar Puerta", "🧹 Borrador" };
        private TileBase brushTile = null;

        private void OnEnable()
        {
            structure = (StructureData)target;
            EnsureValidArray();
        }

        private void EnsureValidArray()
        {
            if (structure == null) return;
            int targetLength = Mathf.Max(1, structure.size.x) * Mathf.Max(1, structure.size.y);
            if (structure.tiles == null || structure.tiles.Length != targetLength)
            {
                structure.Resize(structure.size);
                EditorUtility.SetDirty(structure);
            }
        }

        public override void OnInspectorGUI()
        {
            structure = (StructureData)target;
            serializedObject.Update();

            EnsureValidArray();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🏛️ Editor Visual de Estructura", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Arrastra tiles o sprites directamente a la cuadrícula. Arriba es el tejado y abajo es el suelo.", MessageType.Info);

            EditorGUILayout.Space(4);
            structure.structureName = EditorGUILayout.TextField("Nombre", structure.structureName);

            EditorGUI.BeginChangeCheck();
            bool newHasDoor = EditorGUILayout.Toggle("¿Tiene Puerta? (Estructura)", structure.hasDoor);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(structure, "Cambiar Tipo Estructura");
                structure.hasDoor = newHasDoor;
                if (!structure.hasDoor && selectedTool == 1) selectedTool = 0;
                EditorUtility.SetDirty(structure);
            }

            if (!structure.hasDoor)
            {
                EditorGUILayout.HelpBox("🌿 Elemento Natural (Bosque, Meseta): Sin puerta. La carretera rodeará esta zona sin conectarse a ella.", MessageType.Info);
            }

            // Dimensiones
            EditorGUI.BeginChangeCheck();
            Vector2Int newSize = EditorGUILayout.Vector2IntField("Dimensiones (Ancho x Alto)", structure.size);
            newSize.x = Mathf.Clamp(newSize.x, 1, 20);
            newSize.y = Mathf.Clamp(newSize.y, 1, 20);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(structure, "Cambiar Tamaño Estructura");
                structure.Resize(newSize);
                EditorUtility.SetDirty(structure);
            }

            // Botones rápidos de tamaño predeterminado
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Preajustes de Tamaño:");
            if (GUILayout.Button("8x6 (Isla)", EditorStyles.miniButton)) SetSizeAndDirty(new Vector2Int(8, 6));
            if (GUILayout.Button("10x7 (Media)", EditorStyles.miniButton)) SetSizeAndDirty(new Vector2Int(10, 7));
            if (GUILayout.Button("12x8 (Grande)", EditorStyles.miniButton)) SetSizeAndDirty(new Vector2Int(12, 8));
            if (GUILayout.Button("10x8 (Bosque)", EditorStyles.miniButton)) SetSizeAndDirty(new Vector2Int(10, 8));
            EditorGUILayout.EndHorizontal();

            // Puerta (solo para estructuras habitadas / urbanas)
            if (structure.hasDoor)
            {
                EditorGUI.BeginChangeCheck();
                Vector2Int newDoor = EditorGUILayout.Vector2IntField("Posición Puerta (X, Y)", structure.doorOffset);
                newDoor.x = Mathf.Clamp(newDoor.x, 0, structure.size.x - 1);
                newDoor.y = Mathf.Clamp(newDoor.y, 0, structure.size.y - 1);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(structure, "Cambiar Puerta Estructura");
                    structure.doorOffset = newDoor;
                    EditorUtility.SetDirty(structure);
                }
            }

            EditorGUILayout.Space(8);

            // Barra de herramientas
            EditorGUILayout.LabelField("Herramienta Activa:", EditorStyles.miniBoldLabel);
            if (structure.hasDoor)
            {
                selectedTool = GUILayout.Toolbar(selectedTool, toolNames, GUILayout.Height(28));
            }
            else
            {
                string[] toolsNoDoor = { "🎨 Pintar / Asignar", "🧹 Borrador" };
                int currentToolIdx = (selectedTool == 2) ? 1 : 0;
                int newToolIdx = GUILayout.Toolbar(currentToolIdx, toolsNoDoor, GUILayout.Height(28));
                selectedTool = (newToolIdx == 1) ? 2 : 0;
            }

            if (selectedTool == 0)
            {
                EditorGUILayout.BeginHorizontal();
                brushTile = (TileBase)EditorGUILayout.ObjectField("Pincel Rápido (Opcional):", brushTile, typeof(TileBase), false);
                if (brushTile != null && GUILayout.Button("✖", GUILayout.Width(24)))
                {
                    brushTile = null;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("Consejo: Arrastra cualquier Tile directamente desde tu carpeta a la casilla deseada.", MessageType.None);
            }
            else if (selectedTool == 1 && structure.hasDoor)
            {
                EditorGUILayout.HelpBox("Haz clic en cualquier casilla para establecer allí la PUERTA 🚪 de salida para los caminos.", MessageType.Warning);
            }
            else if (selectedTool == 2)
            {
                EditorGUILayout.HelpBox("Haz clic en cualquier casilla para borrar su tile y dejarla vacía.", MessageType.None);
            }

            EditorGUILayout.Space(10);

            // DIBUJAR CUADRÍCULA VISUAL 2D
            DrawVisualGrid();

            EditorGUILayout.Space(10);

            // Botones de Utilidad de Transformación
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🧹 Limpiar Todo"))
            {
                if (EditorUtility.DisplayDialog("Limpiar Estructura", "¿Seguro que quieres borrar todos los tiles colocados?", "Sí", "No"))
                {
                    Undo.RecordObject(structure, "Limpiar Estructura");
                    for (int i = 0; i < structure.tiles.Length; i++) structure.tiles[i] = null;
                    EditorUtility.SetDirty(structure);
                }
            }
            if (GUILayout.Button("↔ Invertir H"))
            {
                Undo.RecordObject(structure, "Invertir Horizontal");
                FlipHorizontal();
                EditorUtility.SetDirty(structure);
            }
            if (GUILayout.Button("↕ Invertir V"))
            {
                Undo.RecordObject(structure, "Invertir Vertical");
                FlipVertical();
                EditorUtility.SetDirty(structure);
            }
            if (GUILayout.Button("↷ Rotar 90°"))
            {
                Undo.RecordObject(structure, "Rotar 90°");
                Rotate90Clockwise();
                EditorUtility.SetDirty(structure);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // SECCIÓN: ASISTENTE DE MESETAS ADAPTABLES
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🌄 Asistente de Mesetas Adaptables", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Genera la meseta completa con su perímetro rocoso y su interior ya poblado (no quedará vacía ni ocupará toda la isla).", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.35f, 0.95f, 0.45f);
            if (GUILayout.Button("🏡 Auto-Generar Meseta Poblada (Casa + Camino)", GUILayout.Height(30)))
            {
                AutoBuildPlateauWithVillage();
            }
            GUI.backgroundColor = new Color(0.3f, 0.85f, 1f);
            if (GUILayout.Button("🌾 Auto-Generar Meseta Agrícola", GUILayout.Height(30)))
            {
                AutoBuildPlateauWithFarmland();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🧱 Solo Bordes (Vacía)", EditorStyles.miniButton))
            {
                AutoBuildPlateauBorders();
            }
            if (GUILayout.Button("🌱 Rellenar Solo Pasto", EditorStyles.miniButton))
            {
                AutofillInteriorGrass();
            }
            GUI.backgroundColor = new Color(1f, 0.45f, 0.3f);
            if (GUILayout.Button("🧹 Vaciar Solo Interior", EditorStyles.miniButton))
            {
                ClearInteriorOnly();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // SECCIÓN: ASISTENTE DE BOSQUES MASIVOS
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🌲 Asistente de Bosque Masivo (Canopy Tiling)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Construye una masa forestal continua de cualquier tamaño (NxM) usando las copas norte, laterales, troncos sur y canopy interior.", MessageType.None);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
            if (GUILayout.Button("🌲 Auto-Generar Bosque Denso Masivo (NxM)", GUILayout.Height(28)))
            {
                AutoBuildMassiveForest(false);
            }
            GUI.backgroundColor = new Color(0.3f, 0.9f, 0.6f);
            if (GUILayout.Button("🍄 Bosque Masivo con Claro Interior", GUILayout.Height(28)))
            {
                AutoBuildMassiveForest(true);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Lista técnica desplegable por si la quieren inspeccionar
            showDefaultList = EditorGUILayout.Foldout(showDefaultList, "Ver Array Técnico (Avanzado)");
            if (showDefaultList)
            {
                EditorGUI.indentLevel++;
                SerializedProperty tilesProp = serializedObject.FindProperty("tiles");
                EditorGUILayout.PropertyField(tilesProp, true);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SetSizeAndDirty(Vector2Int newSize)
        {
            Undo.RecordObject(structure, "Cambiar Tamaño Estructura");
            structure.Resize(newSize);
            if (structure.hasDoor)
            {
                structure.doorOffset = new Vector2Int(newSize.x / 2, 0);
            }
            EditorUtility.SetDirty(structure);
        }

        private void DrawVisualGrid()
        {
            int cols = structure.size.x;
            int rows = structure.size.y;

            float availableWidth = Mathf.Max(180f, EditorGUIUtility.currentViewWidth - 80f);
            float cellWidth = Mathf.Clamp(availableWidth / cols, 18f, 52f);
            float cellHeight = cellWidth;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"📐 Vista Previa Visual ({cols} x {rows} tiles) - [Arriba = Tejado, Abajo = Suelo]", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Filas: desde arriba (y = rows - 1) hacia abajo (y = 0)
            for (int y = rows - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                // Etiqueta de fila Y a la izquierda
                GUILayout.Label($"Y:{y}", GUILayout.Width(28), GUILayout.Height(cellHeight));

                for (int x = 0; x < cols; x++)
                {
                    DrawCell(x, y, cellWidth, cellHeight);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            // Etiquetas de columna X abajo
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Space(32); // Espacio para alinear con etiqueta Y
            for (int x = 0; x < cols; x++)
            {
                GUILayout.Label($"X:{x}", GUILayout.Width(cellWidth), GUILayout.Height(16));
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawCell(int x, int y, float width, float height)
        {
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            TileBase currentTile = structure.GetTile(x, y);
            bool isDoor = structure.hasDoor && (structure.doorOffset.x == x && structure.doorOffset.y == y);

            // Color de fondo
            Color bgColor = isDoor ? new Color(0.15f, 0.45f, 0.2f, 1f) : new Color(0.22f, 0.22f, 0.22f, 1f);
            EditorGUI.DrawRect(rect, bgColor);

            // Borde
            Color borderColor = isDoor ? new Color(0.4f, 1f, 0.4f, 1f) : new Color(0.12f, 0.12f, 0.12f, 1f);
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, borderColor);

            // Previsualización del Sprite / Tile
            if (currentTile != null)
            {
                Sprite spr = GetSpriteFromTile(currentTile);
                if (spr != null && spr.texture != null)
                {
                    Rect texCoords = new Rect(
                        spr.rect.x / spr.texture.width,
                        spr.rect.y / spr.texture.height,
                        spr.rect.width / spr.texture.width,
                        spr.rect.height / spr.texture.height
                    );
                    Rect innerRect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);
                    GUI.DrawTextureWithTexCoords(innerRect, spr.texture, texCoords);
                }
                else
                {
                    GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), currentTile.name, EditorStyles.miniLabel);
                }
            }
            else
            {
                // Indicador de casilla vacía
                var emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                emptyStyle.fontSize = 9;
                GUI.Label(rect, "vacío", emptyStyle);
            }

            // Indicador de puerta
            if (isDoor)
            {
                GUIStyle doorStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.LowerCenter,
                    fontSize = 11
                };
                doorStyle.normal.textColor = Color.green;
                GUI.Label(rect, "🚪 PUERTA", doorStyle);
            }

            // Botoncito rápido de borrar en la esquina superior derecha
            if (currentTile != null && GUI.Button(new Rect(rect.xMax - 14, rect.yMin + 2, 12, 12), "×", EditorStyles.miniButton))
            {
                Undo.RecordObject(structure, "Borrar Tile");
                structure.SetTile(x, y, null);
                EditorUtility.SetDirty(structure);
                Event.current.Use();
                return;
            }

            // Manejo de Eventos (Clics y Drag & Drop)
            Event evt = Event.current;
            if (rect.Contains(evt.mousePosition))
            {
                // Drag & Drop directo de Tiles o Sprites
                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        TileBase droppedTile = null;

                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (obj is TileBase tb)
                            {
                                droppedTile = tb;
                                break;
                            }
                            else if (obj is Sprite spr)
                            {
                                // Intentar buscar el TileBase con el mismo nombre
                                droppedTile = FindTileForSprite(spr);
                                break;
                            }
                        }

                        if (droppedTile != null)
                        {
                            Undo.RecordObject(structure, "Asignar Tile Drag&Drop");
                            structure.SetTile(x, y, droppedTile);
                            EditorUtility.SetDirty(structure);
                            evt.Use();
                            return;
                        }
                    }
                }

                // Clics con herramientas
                if (evt.type == EventType.MouseDown && evt.button == 0)
                {
                    if (selectedTool == 0) // Pintar / Asignar
                    {
                        if (brushTile != null)
                        {
                            Undo.RecordObject(structure, "Pintar Tile");
                            structure.SetTile(x, y, brushTile);
                            EditorUtility.SetDirty(structure);
                        }
                        else
                        {
                            // Abrir selector de Tile
                            int controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
                            EditorGUIUtility.ShowObjectPicker<TileBase>(currentTile, false, "", controlID);
                            activePickerCell = new Vector2Int(x, y);
                            activePickerControlId = controlID;
                        }
                        evt.Use();
                    }
                    else if (selectedTool == 1) // Marcar Puerta
                    {
                        Undo.RecordObject(structure, "Marcar Puerta");
                        structure.doorOffset = new Vector2Int(x, y);
                        EditorUtility.SetDirty(structure);
                        evt.Use();
                    }
                    else if (selectedTool == 2) // Borrar
                    {
                        Undo.RecordObject(structure, "Borrar Tile");
                        structure.SetTile(x, y, null);
                        EditorUtility.SetDirty(structure);
                        evt.Use();
                    }
                }
            }

            // Recibir resultado del Object Picker si se abrió para esta casilla
            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "ObjectSelectorUpdated")
            {
                if (EditorGUIUtility.GetObjectPickerControlID() == activePickerControlId && activePickerCell == new Vector2Int(x, y))
                {
                    Undo.RecordObject(structure, "Asignar Tile desde Selector");
                    structure.SetTile(x, y, EditorGUIUtility.GetObjectPickerObject() as TileBase);
                    EditorUtility.SetDirty(structure);
                    Repaint();
                }
            }
        }

        private static Vector2Int activePickerCell;
        private static int activePickerControlId;

        private Sprite GetSpriteFromTile(TileBase tile)
        {
            if (tile == null) return null;
            if (tile is Tile unityTile) return unityTile.sprite;
            return null;
        }

        private TileBase FindTileForSprite(Sprite spr)
        {
            if (spr == null) return null;
            string[] guids = AssetDatabase.FindAssets($"{spr.name} t:TileBase");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<TileBase>(path);
            }
            return null;
        }

        private void FlipHorizontal()
        {
            int cols = structure.size.x;
            int rows = structure.size.y;
            TileBase[] newTiles = new TileBase[cols * rows];

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    int oldIdx = y * cols + x;
                    int newIdx = y * cols + (cols - 1 - x);
                    newTiles[newIdx] = structure.tiles[oldIdx];
                }
            }

            structure.tiles = newTiles;
            structure.doorOffset.x = (cols - 1) - structure.doorOffset.x;
        }

        private void FlipVertical()
        {
            int cols = structure.size.x;
            int rows = structure.size.y;
            TileBase[] newTiles = new TileBase[cols * rows];

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    int oldIdx = y * cols + x;
                    int newIdx = (rows - 1 - y) * cols + x;
                    if (oldIdx < structure.tiles.Length && newIdx < newTiles.Length)
                    {
                        newTiles[newIdx] = structure.tiles[oldIdx];
                    }
                }
            }

            structure.tiles = newTiles;
            structure.doorOffset.y = (rows - 1) - structure.doorOffset.y;
        }

        private void Rotate90Clockwise()
        {
            int oldCols = structure.size.x;
            int oldRows = structure.size.y;
            int newCols = oldRows;
            int newRows = oldCols;
            TileBase[] newTiles = new TileBase[newCols * newRows];

            for (int y = 0; y < oldRows; y++)
            {
                for (int x = 0; x < oldCols; x++)
                {
                    int oldIdx = y * oldCols + x;
                    int newX = y;
                    int newY = (oldCols - 1) - x;
                    int newIdx = newY * newCols + newX;
                    if (oldIdx < structure.tiles.Length && newIdx < newTiles.Length)
                    {
                        newTiles[newIdx] = structure.tiles[oldIdx];
                    }
                }
            }

            structure.size = new Vector2Int(newCols, newRows);
            structure.tiles = newTiles;
            int oldDoorX = structure.doorOffset.x;
            int oldDoorY = structure.doorOffset.y;
            structure.doorOffset = new Vector2Int(oldDoorY, (oldCols - 1) - oldDoorX);
        }

        #region Asistente de Mesetas & Autorrelleno
        private TileBase LoadTileByName(string tileName)
        {
            if (string.IsNullOrEmpty(tileName)) return null;
            string[] guids = AssetDatabase.FindAssets($"{tileName} t:TileBase");
            if (guids != null && guids.Length > 0)
            {
                foreach (var guid in guids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    string fName = System.IO.Path.GetFileNameWithoutExtension(p);
                    if (string.Equals(fName, tileName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return AssetDatabase.LoadAssetAtPath<TileBase>(p);
                    }
                }
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<TileBase>(path);
            }
            return null;
        }

        private void BuildPlateauRockRim(int cols, int rows, int doorCol)
        {
            var t53 = LoadTileByName("Meseta_53");
            var t54 = LoadTileByName("Meseta_54");
            var t55 = LoadTileByName("Meseta_55");
            var t65 = LoadTileByName("Meseta_65");
            var t66 = LoadTileByName("Meseta_66");
            var t76 = LoadTileByName("Meseta_76");
            var t77 = LoadTileByName("Meseta_77");
            var t78 = LoadTileByName("Meseta_78");

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    // Fila Superior (Norte - Cornisa)
                    if (y == rows - 1)
                    {
                        if (x == 0) structure.SetTile(x, y, t53);
                        else if (x == cols - 1) structure.SetTile(x, y, t55);
                        else structure.SetTile(x, y, t54);
                    }
                    // Fila Inferior (Sur - Frente de Roca y Abertura de Puerta/Entrada)
                    else if (y == 0)
                    {
                        if (x == 0) structure.SetTile(x, y, t76);
                        else if (x == cols - 1) structure.SetTile(x, y, t78);
                        else if (x == doorCol)
                        {
                            // Abertura limpia de la puerta (pasto libre / transitable)
                            structure.SetTile(x, y, null);
                        }
                        else structure.SetTile(x, y, t77);
                    }
                    // Muro Lateral Oeste
                    else if (x == 0)
                    {
                        structure.SetTile(x, y, t65);
                    }
                    // Muro Lateral Este
                    else if (x == cols - 1)
                    {
                        structure.SetTile(x, y, t66);
                    }
                }
            }
        }

        private void AutoBuildPlateauWithVillage()
        {
            Undo.RecordObject(structure, "Auto-Generar Meseta Poblada");

            if (structure.size.x < 8 || structure.size.y < 6)
            {
                structure.Resize(new Vector2Int(Mathf.Max(8, structure.size.x), Mathf.Max(6, structure.size.y)));
            }

            int cols = structure.size.x;
            int rows = structure.size.y;
            int doorCol = cols / 2;

            // 1. Bordes de roca
            BuildPlateauRockRim(cols, rows, doorCol);

            // 2. Tiles
            var grassTile = LoadTileByName("Prado_0");
            var roadTile  = LoadTileByName("caminos_134_tile") ?? LoadTileByName("caminos_20_tile") ?? LoadTileByName("Camino_Piedra_10");
            var flower1   = LoadTileByName("Flores_1");
            var flower2   = LoadTileByName("Flores_2");

            // Tiles de la Casa (3x3)
            var hRoofL = LoadTileByName("casitas_182");
            var hRoofM = LoadTileByName("casitas_183");
            var hRoofR = LoadTileByName("casitas_184");
            var hWallL = LoadTileByName("casitas_193");
            var hWallM = LoadTileByName("casitas_194");
            var hWallR = LoadTileByName("casitas_195");
            var hBaseL = LoadTileByName("casitas_199");
            var hDoor  = LoadTileByName("casitas_200");
            var hBaseR = LoadTileByName("casitas_201");

            // Rellenar interior con pasto
            for (int y = 1; y < rows - 1; y++)
            {
                for (int x = 1; x < cols - 1; x++)
                {
                    structure.SetTile(x, y, grassTile);
                }
            }

            // Casa 3x3 ubicada en la parte superior
            int houseX = Mathf.Clamp(cols / 2 - 1, 1, cols - 4);
            int houseY = rows - 4;

            if (hBaseL != null && hRoofL != null)
            {
                structure.SetTile(houseX,     houseY,     hBaseL);
                structure.SetTile(houseX + 1, houseY,     hDoor);
                structure.SetTile(houseX + 2, houseY,     hBaseR);

                structure.SetTile(houseX,     houseY + 1, hWallL);
                structure.SetTile(houseX + 1, houseY + 1, hWallM);
                structure.SetTile(houseX + 2, houseY + 1, hWallR);

                structure.SetTile(houseX,     houseY + 2, hRoofL);
                structure.SetTile(houseX + 1, houseY + 2, hRoofM);
                structure.SetTile(houseX + 2, houseY + 2, hRoofR);
            }

            // Sendero desde la puerta sur (doorCol, 0) hasta la puerta de la casa
            int houseDoorX = houseX + 1;
            for (int y = 1; y < houseY; y++)
            {
                structure.SetTile(doorCol, y, roadTile);
            }
            if (doorCol != houseDoorX)
            {
                int minX = Mathf.Min(doorCol, houseDoorX);
                int maxX = Mathf.Max(doorCol, houseDoorX);
                for (int x = minX; x <= maxX; x++)
                {
                    structure.SetTile(x, houseY - 1, roadTile);
                }
            }

            // Flores decorativas
            if (flower1 != null && cols >= 8 && rows >= 6)
            {
                if (structure.GetTile(1, 1) == grassTile) structure.SetTile(1, 1, flower1);
                if (structure.GetTile(cols - 2, 1) == grassTile) structure.SetTile(cols - 2, 1, flower2 ?? flower1);
            }

            structure.hasDoor = true;
            structure.doorOffset = new Vector2Int(doorCol, 0);
            EditorUtility.SetDirty(structure);
            Debug.Log($"<color=green><b>[Meseta Poblada]</b> Meseta generada con éxito ({cols}x{rows}) con Casa interior y sendero.</color>");
        }

        private void AutoBuildPlateauWithFarmland()
        {
            Undo.RecordObject(structure, "Auto-Generar Meseta Agrícola");

            if (structure.size.x < 8 || structure.size.y < 6)
            {
                structure.Resize(new Vector2Int(Mathf.Max(8, structure.size.x), Mathf.Max(6, structure.size.y)));
            }

            int cols = structure.size.x;
            int rows = structure.size.y;
            int doorCol = cols / 2;

            BuildPlateauRockRim(cols, rows, doorCol);

            var grassTile = LoadTileByName("Prado_0");
            var roadTile  = LoadTileByName("caminos_134_tile") ?? LoadTileByName("caminos_20_tile") ?? LoadTileByName("Camino_Piedra_10");

            var c144 = LoadTileByName("Cultivo_144");
            var c145 = LoadTileByName("Cultivo_145");
            var c146 = LoadTileByName("Cultivo_146");
            var c156 = LoadTileByName("Cultivo_156");
            var c157 = LoadTileByName("Cultivo_157");
            var c158 = LoadTileByName("Cultivo_158");
            var c168 = LoadTileByName("Cultivo_168");
            var c169 = LoadTileByName("Cultivo_169");
            var c170 = LoadTileByName("Cultivo_170");

            // Rellenar interior con pasto
            for (int y = 1; y < rows - 1; y++)
            {
                for (int x = 1; x < cols - 1; x++)
                {
                    structure.SetTile(x, y, grassTile);
                }
            }

            // Camino vertical desde la entrada
            for (int y = 1; y < rows - 1; y++)
            {
                structure.SetTile(doorCol, y, roadTile);
            }

            // Parcela de cultivos (3x3)
            int cropX = (doorCol > 3) ? 1 : cols - 4;
            int cropY = rows - 4;

            if (c144 != null && c157 != null && cropX >= 1 && cropX + 2 < cols - 1 && cropY >= 1)
            {
                structure.SetTile(cropX,     cropY + 2, c144);
                structure.SetTile(cropX + 1, cropY + 2, c145);
                structure.SetTile(cropX + 2, cropY + 2, c146);

                structure.SetTile(cropX,     cropY + 1, c156);
                structure.SetTile(cropX + 1, cropY + 1, c157);
                structure.SetTile(cropX + 2, cropY + 1, c158);

                structure.SetTile(cropX,     cropY,     c168);
                structure.SetTile(cropX + 1, cropY,     c169);
                structure.SetTile(cropX + 2, cropY,     c170);
            }

            structure.hasDoor = true;
            structure.doorOffset = new Vector2Int(doorCol, 0);
            EditorUtility.SetDirty(structure);
            Debug.Log($"<color=green><b>[Meseta Agrícola]</b> Meseta generada con éxito ({cols}x{rows}) con huerto 3x3 y sendero.</color>");
        }

        private void AutoBuildPlateauBorders()
        {
            Undo.RecordObject(structure, "Auto-Bordes Meseta");

            int cols = structure.size.x;
            int rows = structure.size.y;
            int doorCol = Mathf.Clamp(structure.hasDoor ? structure.doorOffset.x : cols / 2, 1, cols - 2);

            BuildPlateauRockRim(cols, rows, doorCol);

            structure.hasDoor = true;
            structure.doorOffset = new Vector2Int(doorCol, 0);
            EditorUtility.SetDirty(structure);
            Debug.Log($"<color=cyan><b>[Meseta]</b> Bordes exteriores generados ({cols}x{rows}) con puerta limpia en ({doorCol}, 0).</color>");
        }

        private void AutofillInteriorCropsAndRoad()
        {
            AutoBuildPlateauWithFarmland();
        }

        private void AutofillInteriorGrass()
        {
            Undo.RecordObject(structure, "Autorrellenar Solo Pasto");
            var grassTile = LoadTileByName("Prado_0");
            int cols = structure.size.x;
            int rows = structure.size.y;

            for (int y = 1; y < rows - 1; y++)
            {
                for (int x = 1; x < cols - 1; x++)
                {
                    if (structure.GetTile(x, y) == null)
                    {
                        structure.SetTile(x, y, grassTile);
                    }
                }
            }

            EditorUtility.SetDirty(structure);
            Debug.Log("<color=green><b>[Meseta]</b> Interior rellenado con pasto limpio.</color>");
        }

        private void ClearInteriorOnly()
        {
            Undo.RecordObject(structure, "Vaciar Interior");
            int cols = structure.size.x;
            int rows = structure.size.y;

            for (int y = 1; y < rows - 1; y++)
            {
                for (int x = 1; x < cols - 1; x++)
                {
                    structure.SetTile(x, y, null);
                }
            }

            EditorUtility.SetDirty(structure);
            Debug.Log("<color=yellow><b>[Meseta]</b> Interior vaciado (bordes conservados intactos).</color>");
        }

        private void AutoBuildMassiveForest(bool addClearing)
        {
            Undo.RecordObject(structure, "Auto-Generar Bosque Masivo");

            var t56 = LoadTileByName("Bosque_56");
            var t57 = LoadTileByName("Bosque_57");
            var t58 = LoadTileByName("Bosque_58");
            var t67 = LoadTileByName("Bosque_67");
            var t68 = LoadTileByName("Bosque_68");
            var t69 = LoadTileByName("Bosque_69");
            var t79 = LoadTileByName("Bosque_79");
            var t80 = LoadTileByName("Bosque_80");
            var t81 = LoadTileByName("Bosque_81");

            int cols = structure.size.x;
            int rows = structure.size.y;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    // Fila Superior (Norte - Copas altas)
                    if (y == rows - 1)
                    {
                        if (x == 0) structure.SetTile(x, y, t56);
                        else if (x == cols - 1) structure.SetTile(x, y, t58);
                        else structure.SetTile(x, y, t57);
                    }
                    // Fila Inferior (Sur - Troncos y sombras)
                    else if (y == 0)
                    {
                        if (x == 0) structure.SetTile(x, y, t79);
                        else if (x == cols - 1) structure.SetTile(x, y, t81);
                        else structure.SetTile(x, y, t80);
                    }
                    // Filas Intermedias (Canopy denso)
                    else
                    {
                        if (x == 0) structure.SetTile(x, y, t67);
                        else if (x == cols - 1) structure.SetTile(x, y, t69);
                        else structure.SetTile(x, y, t68);
                    }
                }
            }

            // Opcional: Claro interior con setas y tocones si el bosque es amplio (>= 6x6)
            if (addClearing && cols >= 6 && rows >= 6)
            {
                int clearX = cols / 2 - 1;
                int clearY = rows / 2 - 1;

                var t90 = LoadTileByName("Bosque_90");
                var t91 = LoadTileByName("Bosque_91");
                var t101 = LoadTileByName("Bosque_101");
                var t102 = LoadTileByName("Bosque_102");
                var t103 = LoadTileByName("Decoracion_103"); // Setas
                var t112 = LoadTileByName("Decoracion_112"); // Tocón

                // Perímetro del claro interior
                if (t90 != null) structure.SetTile(clearX, clearY + 1, t90);
                if (t91 != null) structure.SetTile(clearX + 1, clearY + 1, t91);
                if (t101 != null) structure.SetTile(clearX, clearY, t101);
                if (t102 != null) structure.SetTile(clearX + 1, clearY, t102);

                if (t103 != null) structure.SetTile(clearX + 1, clearY, t103);
            }

            structure.hasDoor = false;
            EditorUtility.SetDirty(structure);
            Debug.Log($"<color=green><b>[Bosque]</b> Bosque masivo continuo generado con éxito ({cols}x{rows}).</color>");
        }
        #endregion
    }
}
