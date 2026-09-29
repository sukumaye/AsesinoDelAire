using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using WorldGen.Data;

namespace WorldGen.EditorTools
{
    public class AutotileWizardWindow : EditorWindow
    {
        public WfcVisualDatabase database;

        // Categoría y Bordes
        public string targetCategory = "Agua_Clara";
        public string borderCategory = "Prado";

        // Los 9 tiles del bloque 3x3 exterior
        // [0]=NO, [1]=N,  [2]=NE
        // [3]=O,  [4]=C,  [5]=E
        // [6]=SO, [7]=S,  [8]=SE
        public TileBase[] outer3x3 = new TileBase[9];

        // Los 4 tiles del bloque 2x2 interior
        // [0]=Int_NO, [1]=Int_NE
        // [2]=Int_SO, [3]=Int_SE
        public TileBase[] inner2x2 = new TileBase[4];

        private readonly string[] labels3x3 = {
            "Esquina NO", "Orilla Norte", "Esquina NE",
            "Orilla Oeste", "Centro (Fondo)", "Orilla Este",
            "Esquina SO", "Orilla Sur", "Esquina SE"
        };

        private readonly string[] labels2x2 = {
            "Interior NO", "Interior NE",
            "Interior SO", "Interior SE"
        };

        [MenuItem("Tools/WorldGen/Asistente Autotile (3x3 + 2x2)")]
        public static void Open()
        {
            var win = GetWindow<AutotileWizardWindow>("Asistente Autotile");
            win.minSize = new Vector2(650, 680);
        }

        public static void OpenWithDatabase(WfcVisualDatabase db)
        {
            var win = GetWindow<AutotileWizardWindow>("Asistente Autotile");
            win.database = db;
            win.minSize = new Vector2(650, 680);
            win.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("🌊 Asistente Visual de Autotile (3x3 + 2x2)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Arrastra los tiles a cada casilla para definir las 4 orillas, las 4 esquinas exteriores, el fondo y las 4 esquinas interiores (cóncavas).", MessageType.Info);

            EditorGUILayout.Space(6);

            // Base de Datos
            EditorGUILayout.BeginHorizontal();
            database = (WfcVisualDatabase)EditorGUILayout.ObjectField("Base de Datos:", database, typeof(WfcVisualDatabase), false);
            if (database == null)
            {
                // Intentar buscarla
                string[] guids = AssetDatabase.FindAssets("t:WfcVisualDatabase");
                if (guids != null && guids.Length > 0)
                {
                    database = AssetDatabase.LoadAssetAtPath<WfcVisualDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
            EditorGUILayout.EndHorizontal();

            if (database == null)
            {
                EditorGUILayout.HelpBox("Asigna o crea una WfcVisualDatabase para continuar.", MessageType.Warning);
                return;
            }

            database.EnsureCategories();

            // Categoría Destino
            EditorGUILayout.BeginHorizontal();
            int catIdx = Mathf.Max(0, database.categories.IndexOf(targetCategory));
            catIdx = EditorGUILayout.Popup("Categoría Destino:", catIdx, database.categories.ToArray());
            if (catIdx >= 0 && catIdx < database.categories.Count) targetCategory = database.categories[catIdx];
            targetCategory = EditorGUILayout.TextField(targetCategory, GUILayout.Width(140));
            EditorGUILayout.EndHorizontal();

            // Borde Exterior
            EditorGUILayout.BeginHorizontal();
            var borderList = new List<string>(database.categories);
            borderList.Insert(0, "[Sin Conexión Exterior]");
            int borderIdx = Mathf.Max(0, borderList.IndexOf(borderCategory));
            borderIdx = EditorGUILayout.Popup("Conectar Borde con:", borderIdx, borderList.ToArray());
            if (borderIdx >= 0 && borderIdx < borderList.Count) borderCategory = borderList[borderIdx];
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // CUADRÍCULAS VISUALES
            EditorGUILayout.BeginHorizontal();

            // Columna Izquierda: 3x3 Exterior & Centro
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(340));
            EditorGUILayout.LabelField("🏝️ Bloque Exterior & Centro (3x3)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            Draw3x3Grid();
            EditorGUILayout.Space(6);
            if (GUILayout.Button("🪄 Cargar Selección del Atlas (3x3)"))
            {
                LoadFromAtlasSelection(9);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // Columna Derecha: 2x2 Esquinas Interiores
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(240));
            EditorGUILayout.LabelField("🔄 Esquinas Interiores (2x2)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            Draw2x2Grid();
            EditorGUILayout.Space(6);
            if (GUILayout.Button("🪄 Cargar Selección del Atlas (2x2)"))
            {
                LoadFromAtlasSelection(4);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(14);

            // Botones de Acción
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.3f);
            if (GUILayout.Button("✅ APLICAR Y GUARDAR AUTOTILE", GUILayout.Height(40)))
            {
                ApplyAutotileConfiguration();
            }

            GUI.backgroundColor = new Color(0.9f, 0.35f, 0.35f);
            if (GUILayout.Button("🧹 Limpiar Casillas", GUILayout.Height(40), GUILayout.Width(130)))
            {
                for (int i = 0; i < 9; i++) outer3x3[i] = null;
                for (int i = 0; i < 4; i++) inner2x2[i] = null;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void Draw3x3Grid()
        {
            float cellSize = 80f;

            for (int r = 0; r < 3; r++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < 3; c++)
                {
                    int idx = r * 3 + c;
                    DrawSlot(ref outer3x3[idx], labels3x3[idx], cellSize, cellSize, 1000 + idx);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void Draw2x2Grid()
        {
            float cellSize = 80f;

            for (int r = 0; r < 2; r++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < 2; c++)
                {
                    int idx = r * 2 + c;
                    DrawSlot(ref inner2x2[idx], labels2x2[idx], cellSize, cellSize, 2000 + idx);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawSlot(ref TileBase tile, string label, float width, float height, int controlIdOffset)
        {
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));

            // Fondo
            Color bgColor = (tile != null) ? new Color(0.18f, 0.28f, 0.35f, 1f) : new Color(0.2f, 0.2f, 0.2f, 1f);
            EditorGUI.DrawRect(rect, bgColor);
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, new Color(0.35f, 0.35f, 0.35f, 1f));

            // Dibujar Sprite si existe
            if (tile != null)
            {
                Sprite spr = GetSprite(tile);
                if (spr != null && spr.texture != null)
                {
                    Rect texCoords = new Rect(
                        spr.rect.x / spr.texture.width,
                        spr.rect.y / spr.texture.height,
                        spr.rect.width / spr.texture.width,
                        spr.rect.height / spr.texture.height
                    );
                    Rect innerRect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 22);
                    GUI.DrawTextureWithTexCoords(innerRect, spr.texture, texCoords);
                }
            }

            // Etiqueta descriptiva abajo
            GUIStyle lblStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = 9
            };
            GUI.Label(new Rect(rect.x, rect.yMax - 18, rect.width, 16), label, lblStyle);

            // Botón X para borrar
            if (tile != null && GUI.Button(new Rect(rect.xMax - 14, rect.yMin + 2, 12, 12), "×", EditorStyles.miniButton))
            {
                tile = null;
                Event.current.Use();
                return;
            }

            // Drag & Drop y Clics
            Event evt = Event.current;
            if (rect.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (obj is TileBase tb) { tile = tb; break; }
                            else if (obj is Sprite spr) { tile = FindTileForSprite(spr); break; }
                        }
                        evt.Use();
                        Repaint();
                    }
                }
                else if (evt.type == EventType.MouseDown && evt.button == 0)
                {
                    int controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
                    EditorGUIUtility.ShowObjectPicker<TileBase>(tile, false, "", controlID);
                    activePickerSlotId = controlIdOffset;
                    evt.Use();
                }
            }

            // Object Picker Callback
            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "ObjectSelectorUpdated")
            {
                if (activePickerSlotId == controlIdOffset)
                {
                    tile = EditorGUIUtility.GetObjectPickerObject() as TileBase;
                    Repaint();
                }
            }
        }

        private static int activePickerSlotId = -1;

        private Sprite GetSprite(TileBase t)
        {
            if (t is Tile ut) return ut.sprite;
            return null;
        }

        private TileBase FindTileForSprite(Sprite spr)
        {
            if (spr == null) return null;
            string[] guids = AssetDatabase.FindAssets($"{spr.name} t:TileBase");
            if (guids != null && guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<TileBase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            return null;
        }

        private void LoadFromAtlasSelection(int count)
        {
            if (database == null) return;

            // Obtener ventana WFC Visual Studio si está abierta
            var wfcWindow = GetWindow<WfcVisualStudioWindow>();
            if (wfcWindow == null)
            {
                EditorUtility.DisplayDialog("Aviso", "Abre la ventana WFC Visual Studio y selecciona los tiles en el atlas.", "OK");
                return;
            }

            // Buscar tiles seleccionados
            var selIdsProperty = typeof(WfcVisualStudioWindow).GetField("multiSelectedIds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (selIdsProperty == null) return;

            var selIds = selIdsProperty.GetValue(wfcWindow) as List<int>;
            if (selIds == null || selIds.Count == 0)
            {
                EditorUtility.DisplayDialog("Aviso", "Selecciona los tiles en el atlas de la ventana WFC Visual Studio.", "OK");
                return;
            }

            var tiles = selIds.Select(id => database.GetTileById(id)).Where(t => t != null && t.tile != null).ToList();
            if (tiles.Count == 0) return;

            // Ordenar por coordenadas de arriba a abajo (Y desc) y de izquierda a derecha (X asc)
            tiles = tiles.OrderByDescending(t => t.rectInAtlas.y).ThenBy(t => t.rectInAtlas.x).ToList();

            if (count == 9)
            {
                for (int i = 0; i < 9 && i < tiles.Count; i++)
                {
                    outer3x3[i] = tiles[i].tile;
                }
            }
            else if (count == 4)
            {
                for (int i = 0; i < 4 && i < tiles.Count; i++)
                {
                    inner2x2[i] = tiles[i].tile;
                }
            }

            Repaint();
        }

        private void ApplyAutotileConfiguration()
        {
            if (database == null) return;

            // Validar que al menos el 3x3 esté completo
            for (int i = 0; i < 9; i++)
            {
                if (outer3x3[i] == null)
                {
                    EditorUtility.DisplayDialog("Faltan Tiles", $"Falta asignar la casilla '{labels3x3[i]}' en el bloque 3x3.", "OK");
                    return;
                }
            }

            Undo.RecordObject(database, $"Configurar Autotile {targetCategory}");

            // Asegurar que la categoría existe
            if (!database.categories.Contains(targetCategory)) database.categories.Add(targetCategory);

            // Mapear los 9 tiles del 3x3
            string[] roles3x3 = {
                "Outer_TL", "Top",    "Outer_TR",
                "Left",     "Center", "Right",
                "Outer_BL", "Bottom", "Outer_BR"
            };

            WfcTileEntry[] entries3x3 = new WfcTileEntry[9];
            for (int i = 0; i < 9; i++)
            {
                var entry = database.tiles.FirstOrDefault(t => t.tile == outer3x3[i]);
                if (entry != null)
                {
                    entry.category = targetCategory;
                    entry.role = roles3x3[i];
                    entry.weight = (roles3x3[i] == "Center") ? 2.5f : 1.0f;
                    entries3x3[i] = entry;
                }
            }

            // Mapear los 4 tiles del 2x2
            string[] roles2x2 = {
                "Inner_TL", "Inner_TR",
                "Inner_BL", "Inner_BR"
            };

            int innerAssigned = 0;
            for (int i = 0; i < 4; i++)
            {
                if (inner2x2[i] != null)
                {
                    var entry = database.tiles.FirstOrDefault(t => t.tile == inner2x2[i]);
                    if (entry != null)
                    {
                        entry.category = targetCategory;
                        entry.role = roles2x2[i];
                        entry.weight = 1.0f;
                        innerAssigned++;
                    }
                }
            }

            // Conectar vecinos en el 3x3
            var block = new WfcTileEntry[3, 3];
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    block[c, r] = entries3x3[r * 3 + c];
                }
            }

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    var t = block[c, r];
                    if (t == null) continue;

                    if (r > 0 && block[c, r - 1] != null) database.AddNeighbor(t.id, block[c, r - 1].id, Vector2Int.up, true);
                    if (r < 2 && block[c, r + 1] != null) database.AddNeighbor(t.id, block[c, r + 1].id, Vector2Int.down, true);
                    if (c > 0 && block[c - 1, r] != null) database.AddNeighbor(t.id, block[c - 1, r].id, Vector2Int.left, true);
                    if (c < 2 && block[c + 1, r] != null) database.AddNeighbor(t.id, block[c + 1, r].id, Vector2Int.right, true);

                    // Auto-repetición del centro
                    if (r == 1 && c == 1)
                    {
                        database.AddNeighbor(t.id, t.id, Vector2Int.up, true);
                        database.AddNeighbor(t.id, t.id, Vector2Int.down, true);
                        database.AddNeighbor(t.id, t.id, Vector2Int.left, true);
                        database.AddNeighbor(t.id, t.id, Vector2Int.right, true);
                    }
                }
            }

            // Conectar bordes hacia la categoría elegida (ej. Prado)
            bool hasBorder = !string.IsNullOrEmpty(borderCategory) && borderCategory != "[Sin Conexión Exterior]";
            if (hasBorder)
            {
                var borderTiles = database.GetTilesByCategory(borderCategory);
                foreach (var bt in borderTiles)
                {
                    for (int c = 0; c < 3; c++) database.AddNeighbor(block[c, 0].id, bt.id, Vector2Int.up, true);
                    for (int c = 0; c < 3; c++) database.AddNeighbor(block[c, 2].id, bt.id, Vector2Int.down, true);
                    for (int r = 0; r < 3; r++) database.AddNeighbor(block[0, r].id, bt.id, Vector2Int.left, true);
                    for (int r = 0; r < 3; r++) database.AddNeighbor(block[2, r].id, bt.id, Vector2Int.right, true);
                }
            }

            database.EnsureCategories();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("Autotile Guardado", 
                $"¡Autotile '{targetCategory}' configurado y guardado con éxito!\n\n" +
                $"• 9 tiles exteriores (3x3) enlazados.\n" +
                $"• {innerAssigned} esquinas interiores (2x2) asignadas.\n" +
                (hasBorder ? $"• Bordes conectados con '{borderCategory}'." : "• Sin bordes exteriores."), "¡Excelente!");
        }
    }
}
