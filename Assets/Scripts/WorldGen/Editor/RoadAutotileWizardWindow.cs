using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using WorldGen.Data;

namespace WorldGen.EditorTools
{
    public class RoadAutotileWizardWindow : EditorWindow
    {
        public WfcVisualDatabase database;

        public enum RoadType
        {
            Dirt,   // Camino de Tierra: los extremos terminan en hierbajo/prado
            Stone   // Camino de Piedra: los extremos son transiciones a camino de tierra
        }

        public RoadType roadType = RoadType.Dirt;
        public string targetCategory = "Camino";
        public string borderCategory = "Prado";         // Hierbajo / pasto exterior
        public string transitionCategory = "Camino";   // Categoría de tierra para las transiciones de piedra

        // 1. Bloque 3x3 Principal (9 tiles)
        // [0]=Curva NO, [1]=T Arriba,   [2]=Curva NE
        // [3]=T Izq,    [4]=Cruz 4 vías,[5]=T Der
        // [6]=Curva SO, [7]=T Abajo,    [8]=Curva SE
        public TileBase[] block3x3 = new TileBase[9];

        // 2. Tira 3x1 Horizontal (3 tiles)
        // [0]=Extremo O / Transición O, [1]=Recta Horiz, [2]=Extremo E / Transición E
        public TileBase[] strip3x1 = new TileBase[3];

        // 3. Tira 1x3 Vertical (3 tiles)
        // [0]=Extremo N / Transición N, [1]=Recta Vert, [2]=Extremo S / Transición S
        public TileBase[] strip1x3 = new TileBase[3];

        private readonly string[] labels3x3 = {
            "Curva NO", "T Arriba", "Curva NE",
            "T Izquierda", "Cruz 4 Vías", "T Derecha",
            "Curva SO", "T Abajo", "Curva SE"
        };

        private Vector2 scrollPos;
        private static int activePickerSlotId = -1;

        [MenuItem("Tools/WorldGen/Asistente de Caminos (3x3 + 1x3 + 3x1)")]
        public static void Open()
        {
            var win = GetWindow<RoadAutotileWizardWindow>("Asistente de Caminos");
            win.minSize = new Vector2(720, 680);
        }

        public static void OpenWithDatabase(WfcVisualDatabase db)
        {
            var win = GetWindow<RoadAutotileWizardWindow>("Asistente de Caminos");
            win.database = db;
            win.minSize = new Vector2(720, 680);
            win.Show();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.Space(6);

            EditorGUILayout.LabelField("🛣️ Asistente Visual de Caminos (3x3 + 1x3 + 3x1 = 15 Tiles)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configura los 15 tiles esenciales de cualquier red vial:\n" +
                "• Bloque 3x3: Las 4 esquinas, las 4 intersecciones en T y el cruce central de 4 vías.\n" +
                "• Tira 3x1 Horizontal: Recta horizontal y sus 2 extremos (en tierra terminan en hierba; en piedra son transiciones a tierra).\n" +
                "• Tira 1x3 Vertical: Recta vertical y sus 2 extremos (en tierra terminan en hierba; en piedra son transiciones a tierra).",
                MessageType.Info
            );

            EditorGUILayout.Space(6);

            // Base de Datos
            EditorGUILayout.BeginHorizontal();
            database = (WfcVisualDatabase)EditorGUILayout.ObjectField("Base de Datos:", database, typeof(WfcVisualDatabase), false);
            if (database == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:WfcVisualDatabase");
                if (guids != null && guids.Length > 0)
                {
                    database = AssetDatabase.LoadAssetAtPath<WfcVisualDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
            EditorGUILayout.EndHorizontal();

            if (database == null)
            {
                EditorGUILayout.HelpBox("Asigna una WfcVisualDatabase para continuar.", MessageType.Warning);
                EditorGUILayout.EndScrollView();
                return;
            }

            database.EnsureCategories();

            // Configuración de Tipo y Categorías
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("⚙️ Parámetros de la Red de Caminos", EditorStyles.boldLabel);

            // Selector Tipo de Camino
            RoadType newRoadType = (RoadType)EditorGUILayout.EnumPopup("Tipo de Camino:", roadType);
            if (newRoadType != roadType)
            {
                roadType = newRoadType;
                if (roadType == RoadType.Dirt && targetCategory == "Camino_Piedra") targetCategory = "Camino";
                else if (roadType == RoadType.Stone && targetCategory == "Camino") targetCategory = "Camino_Piedra";
            }

            // Categoría Destino
            EditorGUILayout.BeginHorizontal();
            int catIdx = Mathf.Max(0, database.categories.IndexOf(targetCategory));
            catIdx = EditorGUILayout.Popup("Categoría Destino:", catIdx, database.categories.ToArray());
            if (catIdx >= 0 && catIdx < database.categories.Count) targetCategory = database.categories[catIdx];
            targetCategory = EditorGUILayout.TextField(targetCategory, GUILayout.Width(140));
            EditorGUILayout.EndHorizontal();

            // Terreno Borde Exterior (Hierbajo / Prado)
            EditorGUILayout.BeginHorizontal();
            var borderList = new List<string>(database.categories);
            borderList.Insert(0, "[Sin Borde Exterior]");
            int borderIdx = Mathf.Max(0, borderList.IndexOf(borderCategory));
            borderIdx = EditorGUILayout.Popup("Hierbajo / Prado Exterior:", borderIdx, borderList.ToArray());
            if (borderIdx >= 0 && borderIdx < borderList.Count) borderCategory = borderList[borderIdx];
            EditorGUILayout.EndHorizontal();

            // Si es camino de piedra, mostrar a qué camino conecta en las transiciones
            if (roadType == RoadType.Stone)
            {
                EditorGUILayout.BeginHorizontal();
                int transIdx = Mathf.Max(0, database.categories.IndexOf(transitionCategory));
                transIdx = EditorGUILayout.Popup("Conectar Transiciones con:", transIdx, database.categories.ToArray());
                if (transIdx >= 0 && transIdx < database.categories.Count) transitionCategory = database.categories[transIdx];
                transitionCategory = EditorGUILayout.TextField(transitionCategory, GUILayout.Width(140));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("En Modo Piedra, los extremos de las tiras horizontal y vertical actuarán como transiciones conectadas a '" + transitionCategory + "'.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("En Modo Tierra, los extremos de las tiras horizontal y vertical terminarán en '" + borderCategory + "' (hierbajo / final de camino cerrado).", MessageType.None);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // Barra de Botones de Autocarga Rápida
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.9f, 1f);
            if (GUILayout.Button("⚡ Autodetectar 15 Tiles de Selección", GUILayout.Height(26)))
            {
                AutoDetectAll15FromSelection();
            }

            GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);
            if (GUILayout.Button("📦 Preset Tierra (Atlas)", GUILayout.Height(26)))
            {
                LoadPreset(RoadType.Dirt);
            }

            GUI.backgroundColor = new Color(0.85f, 0.85f, 0.9f);
            if (GUILayout.Button("📦 Preset Piedra (Atlas)", GUILayout.Height(26)))
            {
                LoadPreset(RoadType.Stone);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // --- CUADRÍCULAS VISUALES ---
            EditorGUILayout.BeginHorizontal();

            // 1. COLUMNA IZQUIERDA: BLOQUE 3x3 (Intersecciones y Giros)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(310));
            EditorGUILayout.LabelField("🔲 1. Bloque 3x3 (Esquinas y Cruces)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            Draw3x3Grid();
            EditorGUILayout.Space(6);
            if (GUILayout.Button("🪄 Cargar Selección (3x3)"))
            {
                LoadFromAtlasSelection3x3();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(8);

            // 2. COLUMNA DERECHA: TIRA 3x1 HORIZONTAL & TIRA 1x3 VERTICAL
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Tira Horizontal (3x1)
            EditorGUILayout.LabelField("↔ 2. Tira 3x1 (Horizontal)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            Draw3x1HorizontalStrip();
            EditorGUILayout.Space(4);
            if (GUILayout.Button("🪄 Cargar Selección (3x1 Horiz)"))
            {
                LoadFromAtlasSelection3x1();
            }

            EditorGUILayout.Space(10);

            // Tira Vertical (1x3)
            EditorGUILayout.LabelField("↕ 3. Tira 1x3 (Vertical)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            Draw1x3VerticalStrip();
            EditorGUILayout.Space(4);
            if (GUILayout.Button("🪄 Cargar Selección (1x3 Vert)"))
            {
                LoadFromAtlasSelection1x3();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12);

            // BOTONES DE ACCIÓN
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.35f);
            if (GUILayout.Button("✅ APLICAR Y GUARDAR CAMINOS (15 TILES)", GUILayout.Height(42)))
            {
                ApplyRoadConfiguration();
            }

            GUI.backgroundColor = new Color(0.9f, 0.35f, 0.35f);
            if (GUILayout.Button("🧹 Limpiar Casillas", GUILayout.Height(42), GUILayout.Width(130)))
            {
                for (int i = 0; i < 9; i++) block3x3[i] = null;
                for (int i = 0; i < 3; i++) strip3x1[i] = null;
                for (int i = 0; i < 3; i++) strip1x3[i] = null;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        private void Draw3x3Grid()
        {
            float cellSize = 82f;
            for (int r = 0; r < 3; r++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < 3; c++)
                {
                    int idx = r * 3 + c;
                    DrawSlot(ref block3x3[idx], labels3x3[idx], cellSize, cellSize, 1000 + idx);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void Draw3x1HorizontalStrip()
        {
            float cellSize = 82f;
            string leftLabel = (roadType == RoadType.Dirt) ? "Fin Oeste (◄ Hierba)" : "Trans. O (Tierra ◄ Piedra)";
            string midLabel  = (roadType == RoadType.Dirt) ? "Recta Horiz (◄►)"     : "Recta Horiz (Piedra)";
            string rightLabel= (roadType == RoadType.Dirt) ? "Fin Este (Hierba ►)"  : "Trans. E (Piedra ► Tierra)";

            string[] hLabels = { leftLabel, midLabel, rightLabel };

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < 3; i++)
            {
                DrawSlot(ref strip3x1[i], hLabels[i], cellSize, cellSize, 2000 + i);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void Draw1x3VerticalStrip()
        {
            float cellSize = 82f;
            string topLabel = (roadType == RoadType.Dirt) ? "Fin Norte (▲ Hierba)"  : "Trans. N (Tierra ▲ Piedra)";
            string midLabel = (roadType == RoadType.Dirt) ? "Recta Vert (▲▼)"      : "Recta Vert (Piedra)";
            string botLabel = (roadType == RoadType.Dirt) ? "Fin Sur (Hierba ▼)"   : "Trans. S (Piedra ▼ Tierra)";

            string[] vLabels = { topLabel, midLabel, botLabel };

            for (int i = 0; i < 3; i++)
            {
                EditorGUILayout.BeginHorizontal();
                DrawSlot(ref strip1x3[i], vLabels[i], cellSize, cellSize, 3000 + i);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawSlot(ref TileBase tile, string label, float width, float height, int controlIdOffset)
        {
            Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));

            Color bgColor = (tile != null) ? new Color(0.18f, 0.28f, 0.35f, 1f) : new Color(0.2f, 0.2f, 0.2f, 1f);
            EditorGUI.DrawRect(rect, bgColor);
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, new Color(0.35f, 0.35f, 0.35f, 1f));

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
                    Rect innerRect = new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 24);
                    GUI.DrawTextureWithTexCoords(innerRect, spr.texture, texCoords);
                }
            }

            GUIStyle lblStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = 8,
                wordWrap = true
            };
            GUI.Label(new Rect(rect.x + 1, rect.yMax - 20, rect.width - 2, 18), label, lblStyle);

            if (tile != null && GUI.Button(new Rect(rect.xMax - 14, rect.yMin + 2, 12, 12), "×", EditorStyles.miniButton))
            {
                tile = null;
                Event.current.Use();
                return;
            }

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
                else if (evt.type == EventType.MouseDown)
                {
                    if (evt.button == 0)
                    {
                        // Clic izquierdo: si hay un tile seleccionado en WfcVisualStudioWindow, asignarlo directamente
                        var wfcWin = Resources.FindObjectsOfTypeAll<WfcVisualStudioWindow>().FirstOrDefault();
                        if (wfcWin != null && database != null)
                        {
                            var fld = typeof(WfcVisualStudioWindow).GetField("selectedTileId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            int selId = fld != null ? (int)fld.GetValue(wfcWin) : -1;
                            if (selId >= 0)
                            {
                                var entry = database.GetTileById(selId);
                                if (entry != null && entry.tile != null)
                                {
                                    tile = entry.tile;
                                    evt.Use();
                                    Repaint();
                                    return;
                                }
                            }
                        }

                        int controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
                        EditorGUIUtility.ShowObjectPicker<TileBase>(tile, false, "", controlID);
                        activePickerSlotId = controlIdOffset;
                        evt.Use();
                    }
                    else if (evt.button == 1)
                    {
                        int controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
                        EditorGUIUtility.ShowObjectPicker<TileBase>(tile, false, "", controlID);
                        activePickerSlotId = controlIdOffset;
                        evt.Use();
                    }
                }
            }

            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "ObjectSelectorUpdated")
            {
                if (activePickerSlotId == controlIdOffset)
                {
                    tile = EditorGUIUtility.GetObjectPickerObject() as TileBase;
                    Repaint();
                }
            }
        }

        private Sprite GetSprite(TileBase t)
        {
            if (t is Tile ut) return ut.sprite;
            if (database != null && database.tiles != null)
            {
                var entry = database.tiles.FirstOrDefault(e => e.tile == t);
                if (entry != null) return entry.sprite;
            }
            return null;
        }

        private TileBase FindTileForSprite(Sprite spr)
        {
            if (spr == null) return null;
            if (database != null && database.tiles != null)
            {
                var entry = database.tiles.FirstOrDefault(t => t.sprite == spr || t.spriteName == spr.name);
                if (entry != null && entry.tile != null) return entry.tile;
            }
            string[] guids = AssetDatabase.FindAssets($"{spr.name} t:TileBase");
            if (guids != null && guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<TileBase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            return null;
        }

        private List<WfcTileEntry> GetSelectedTilesFromWfcStudio()
        {
            if (database == null) return null;
            var wfcWindow = GetWindow<WfcVisualStudioWindow>();
            if (wfcWindow == null) return null;

            var selIdsProp = typeof(WfcVisualStudioWindow).GetField("multiSelectedIds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (selIdsProp == null) return null;

            var selIds = selIdsProp.GetValue(wfcWindow) as List<int>;
            if (selIds == null || selIds.Count == 0) return null;

            return selIds.Select(id => database.GetTileById(id)).Where(t => t != null && t.tile != null).ToList();
        }

        private void LoadFromAtlasSelection3x3()
        {
            var tiles = GetSelectedTilesFromWfcStudio();
            if (tiles == null || tiles.Count == 0)
            {
                EditorUtility.DisplayDialog("Aviso", "Abre la ventana WFC Visual Studio y selecciona los 9 tiles del 3x3 en el atlas.", "OK");
                return;
            }

            // Ordenar de arriba a abajo (Y desc) y de izquierda a derecha (X asc)
            tiles = tiles.OrderByDescending(t => t.rectInAtlas.y).ThenBy(t => t.rectInAtlas.x).ToList();
            for (int i = 0; i < 9 && i < tiles.Count; i++)
            {
                block3x3[i] = tiles[i].tile;
            }
            Repaint();
        }

        private void LoadFromAtlasSelection3x1()
        {
            var tiles = GetSelectedTilesFromWfcStudio();
            if (tiles == null || tiles.Count == 0)
            {
                EditorUtility.DisplayDialog("Aviso", "Selecciona los 3 tiles horizontales en el atlas de WFC Visual Studio.", "OK");
                return;
            }

            // Ordenar de izquierda a derecha (X asc)
            tiles = tiles.OrderBy(t => t.rectInAtlas.x).ToList();
            for (int i = 0; i < 3 && i < tiles.Count; i++)
            {
                strip3x1[i] = tiles[i].tile;
            }
            Repaint();
        }

        private void LoadFromAtlasSelection1x3()
        {
            var tiles = GetSelectedTilesFromWfcStudio();
            if (tiles == null || tiles.Count == 0)
            {
                EditorUtility.DisplayDialog("Aviso", "Selecciona los 3 tiles verticales en el atlas de WFC Visual Studio.", "OK");
                return;
            }

            // Ordenar de arriba a abajo (Y desc)
            tiles = tiles.OrderByDescending(t => t.rectInAtlas.y).ToList();
            for (int i = 0; i < 3 && i < tiles.Count; i++)
            {
                strip1x3[i] = tiles[i].tile;
            }
            Repaint();
        }

        private void AutoDetectAll15FromSelection()
        {
            var tiles = GetSelectedTilesFromWfcStudio();
            if (tiles == null || tiles.Count < 15)
            {
                EditorUtility.DisplayDialog("Aviso", "Selecciona los 15 tiles de camino en el atlas de WFC Studio (con arrastre o Mayús).", "OK");
                return;
            }

            // Detección geométrica:
            // 1. Identificar las coordenadas X e Y
            var xs = tiles.Select(t => t.rectInAtlas.x).Distinct().OrderBy(x => x).ToList();
            var ys = tiles.Select(t => t.rectInAtlas.y).Distinct().OrderByDescending(y => y).ToList();

            // Buscar el bloque 3x3: 3 X contiguas y 3 Y contiguas con un tile en cada una de las 9 posiciones
            List<WfcTileEntry> found3x3 = null;
            for (int yi = 0; yi <= ys.Count - 3; yi++)
            {
                for (int xi = 0; xi <= xs.Count - 3; xi++)
                {
                    float[] subXs = { xs[xi], xs[xi + 1], xs[xi + 2] };
                    float[] subYs = { ys[yi], ys[yi + 1], ys[yi + 2] };

                    var cand = new List<WfcTileEntry>();
                    bool all9 = true;
                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            var t = tiles.FirstOrDefault(entry =>
                                Mathf.Abs(entry.rectInAtlas.x - subXs[c]) < 0.5f &&
                                Mathf.Abs(entry.rectInAtlas.y - subYs[r]) < 0.5f);
                            if (t == null) { all9 = false; break; }
                            cand.Add(t);
                        }
                        if (!all9) break;
                    }
                    if (all9)
                    {
                        found3x3 = cand;
                        break;
                    }
                }
                if (found3x3 != null) break;
            }

            if (found3x3 != null)
            {
                for (int i = 0; i < 9; i++) block3x3[i] = found3x3[i].tile;

                var remaining = tiles.Where(t => !found3x3.Contains(t)).ToList();

                // De los restantes, buscar la tira horizontal (3 tiles con el mismo Y)
                var yGroups = remaining.GroupBy(t => Mathf.RoundToInt(t.rectInAtlas.y)).Where(g => g.Count() >= 3).ToList();
                if (yGroups.Count > 0)
                {
                    var hTiles = yGroups[0].OrderBy(t => t.rectInAtlas.x).Take(3).ToList();
                    for (int i = 0; i < 3; i++) strip3x1[i] = hTiles[i].tile;
                    remaining = remaining.Where(t => !hTiles.Contains(t)).ToList();
                }

                // De los restantes, buscar la tira vertical (3 tiles con el mismo X)
                var xGroups = remaining.GroupBy(t => Mathf.RoundToInt(t.rectInAtlas.x)).Where(g => g.Count() >= 3).ToList();
                if (xGroups.Count > 0)
                {
                    var vTiles = xGroups[0].OrderByDescending(t => t.rectInAtlas.y).Take(3).ToList();
                    for (int i = 0; i < 3; i++) strip1x3[i] = vTiles[i].tile;
                }

                Repaint();
                EditorUtility.DisplayDialog("Autodetección Exitosa", "¡Los 15 tiles han sido detectados y clasificados en 3x3, 3x1 y 1x3 correctamente!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Aviso", "No se encontró una cuadrícula 3x3 dentro de los tiles seleccionados. Asigna los bloques manualmente.", "OK");
            }
        }

        private void LoadPreset(RoadType type)
        {
            if (database == null || database.tiles == null) return;

            roadType = type;
            targetCategory = (type == RoadType.Dirt) ? "Camino" : "Camino_Piedra";

            WfcTileEntry FindT(int x, int y, string name)
            {
                var f = database.tiles.FirstOrDefault(t => Mathf.Abs(t.rectInAtlas.x - x) < 0.5f && Mathf.Abs(t.rectInAtlas.y - y) < 0.5f);
                if (f != null && f.tile != null) return f;
                f = database.tiles.FirstOrDefault(t => t.spriteName == name || t.spriteName == name + "_tile" || t.spriteName.EndsWith("_" + name.Replace("caminos_", "")));
                return f;
            }

            if (type == RoadType.Dirt)
            {
                // Bloque 3x3
                block3x3[0] = FindT(64,  304, "caminos_15")?.tile; // TL
                block3x3[1] = FindT(80,  304, "caminos_16")?.tile; // T_Up
                block3x3[2] = FindT(96,  304, "caminos_17")?.tile; // TR
                block3x3[3] = FindT(64,  288, "caminos_27")?.tile; // T_Left
                block3x3[4] = FindT(80,  288, "caminos_28")?.tile; // Cross
                block3x3[5] = FindT(96,  288, "caminos_29")?.tile; // T_Right
                block3x3[6] = FindT(64,  272, "caminos_39")?.tile; // BL
                block3x3[7] = FindT(80,  272, "caminos_40")?.tile; // T_Down
                block3x3[8] = FindT(96,  272, "caminos_41")?.tile; // BR

                // Tira 3x1 Horiz
                strip3x1[0] = FindT(64,  320, "caminos_4")?.tile;  // Cap_W
                strip3x1[1] = FindT(80,  320, "caminos_5")?.tile;  // Straight_H
                strip3x1[2] = FindT(96,  320, "caminos_6")?.tile;  // Cap_E

                // Tira 1x3 Vert
                strip1x3[0] = FindT(48,  304, "caminos_14")?.tile; // Cap_N
                strip1x3[1] = FindT(48,  288, "caminos_26")?.tile; // Straight_V
                strip1x3[2] = FindT(48,  272, "caminos_38")?.tile; // Cap_S
            }
            else
            {
                // Bloque 3x3 Piedra
                block3x3[0] = FindT(144, 304, "caminos_20")?.tile; // TL
                block3x3[1] = FindT(160, 304, "caminos_21")?.tile; // T_Up
                block3x3[2] = FindT(176, 304, "caminos_22")?.tile; // TR
                block3x3[3] = FindT(144, 288, "caminos_32")?.tile; // T_Left
                block3x3[4] = FindT(160, 288, "caminos_33")?.tile; // Cross
                block3x3[5] = FindT(176, 288, "caminos_34")?.tile; // T_Right
                block3x3[6] = FindT(144, 272, "caminos_44")?.tile; // BL
                block3x3[7] = FindT(160, 272, "caminos_45")?.tile; // T_Down
                block3x3[8] = FindT(176, 272, "caminos_46")?.tile; // BR

                // Tira 3x1 Horiz Piedra
                strip3x1[0] = FindT(144, 320, "caminos_8")?.tile;  // Trans_W
                strip3x1[1] = FindT(160, 320, "caminos_9")?.tile;  // Straight_H
                strip3x1[2] = FindT(176, 320, "caminos_10")?.tile; // Trans_E

                // Tira 1x3 Vert Piedra
                strip1x3[0] = FindT(128, 304, "caminos_19")?.tile; // Trans_N
                strip1x3[1] = FindT(128, 288, "caminos_31")?.tile; // Straight_V
                strip1x3[2] = FindT(128, 272, "caminos_43")?.tile; // Trans_S
            }

            Repaint();
        }

        private void ApplyRoadConfiguration()
        {
            if (database == null) return;

            // Validar que los 15 tiles estén asignados
            for (int i = 0; i < 9; i++)
            {
                if (block3x3[i] == null)
                {
                    EditorUtility.DisplayDialog("Faltan Tiles", $"Falta asignar '{labels3x3[i]}' en el Bloque 3x3.", "OK");
                    return;
                }
            }
            for (int i = 0; i < 3; i++)
            {
                if (strip3x1[i] == null)
                {
                    EditorUtility.DisplayDialog("Faltan Tiles", $"Falta asignar la casilla #{i+1} en la Tira 3x1 Horizontal.", "OK");
                    return;
                }
                if (strip1x3[i] == null)
                {
                    EditorUtility.DisplayDialog("Faltan Tiles", $"Falta asignar la casilla #{i+1} en la Tira 1x3 Vertical.", "OK");
                    return;
                }
            }

            Undo.RecordObject(database, $"Configurar Caminos {targetCategory}");

            if (!database.categories.Contains(targetCategory)) database.categories.Add(targetCategory);

            // Mapear los WfcTileEntries
            WfcTileEntry FindEntry(TileBase tb) => database.tiles.FirstOrDefault(t => t.tile == tb);

            var e3x3 = block3x3.Select(FindEntry).ToArray();
            var eH   = strip3x1.Select(FindEntry).ToArray();
            var eV   = strip1x3.Select(FindEntry).ToArray();

            // Asignar Categorías y Roles
            string[] roles3x3 = {
                "Road_Corner_TL", "Road_T_Up",   "Road_Corner_TR",
                "Road_T_Left",    "Road_Cross",  "Road_T_Right",
                "Road_Corner_BL", "Road_T_Down", "Road_Corner_BR"
            };

            for (int i = 0; i < 9; i++)
            {
                if (e3x3[i] != null)
                {
                    e3x3[i].category = targetCategory;
                    e3x3[i].role = roles3x3[i];
                    e3x3[i].weight = (roles3x3[i] == "Road_Cross") ? 1.0f : 1.2f;
                }
            }

            // Tira Horizontal
            if (eH[0] != null)
            {
                eH[0].category = targetCategory;
                eH[0].role = (roadType == RoadType.Dirt) ? "Road_Cap_W" : "Road_Trans_W";
                eH[0].weight = 1.0f;
            }
            if (eH[1] != null)
            {
                eH[1].category = targetCategory;
                eH[1].role = "Road_Straight_H";
                eH[1].weight = 2.0f; // Mayor peso para rectas
            }
            if (eH[2] != null)
            {
                eH[2].category = targetCategory;
                eH[2].role = (roadType == RoadType.Dirt) ? "Road_Cap_E" : "Road_Trans_E";
                eH[2].weight = 1.0f;
            }

            // Tira Vertical
            if (eV[0] != null)
            {
                eV[0].category = targetCategory;
                eV[0].role = (roadType == RoadType.Dirt) ? "Road_Cap_N" : "Road_Trans_N";
                eV[0].weight = 1.0f;
            }
            if (eV[1] != null)
            {
                eV[1].category = targetCategory;
                eV[1].role = "Road_Straight_V";
                eV[1].weight = 2.0f; // Mayor peso para rectas
            }
            if (eV[2] != null)
            {
                eV[2].category = targetCategory;
                eV[2].role = (roadType == RoadType.Dirt) ? "Road_Cap_S" : "Road_Trans_S";
                eV[2].weight = 1.0f;
            }

            // Vínculos WFC basados en Sockets
            // Definir qué lados de cada tile abren hacia camino
            var all15 = new List<WfcTileEntry>();
            all15.AddRange(e3x3.Where(t => t != null));
            all15.AddRange(eH.Where(t => t != null));
            all15.AddRange(eV.Where(t => t != null));
            all15 = all15.Distinct().ToList();

            // Limpiar conexiones previas de estos 15 tiles
            foreach (var t in all15)
            {
                t.allowedNorth.Clear();
                t.allowedSouth.Clear();
                t.allowedEast.Clear();
                t.allowedWest.Clear();
            }

            // Sockets de apertura vial: (N, S, W, E)
            var roadOpenings = new Dictionary<WfcTileEntry, (bool N, bool S, bool W, bool E)>();

            void SetOpen(WfcTileEntry t, bool n, bool s, bool w, bool e)
            {
                if (t != null) roadOpenings[t] = (n, s, w, e);
            }

            // 3x3
            SetOpen(e3x3[0], false, true,  false, true);  // TL: S, E
            SetOpen(e3x3[1], false, true,  true,  true);  // T_Up: S, W, E
            SetOpen(e3x3[2], false, true,  true,  false); // TR: S, W
            SetOpen(e3x3[3], true,  true,  false, true);  // T_Left: N, S, E
            SetOpen(e3x3[4], true,  true,  true,  true);  // Cross: N, S, W, E
            SetOpen(e3x3[5], true,  true,  true,  false); // T_Right: N, S, W
            SetOpen(e3x3[6], true,  false, false, true);  // BL: N, E
            SetOpen(e3x3[7], true,  false, true,  true);  // T_Down: N, W, E
            SetOpen(e3x3[8], true,  false, true,  false); // BR: N, W

            // Horizontal
            SetOpen(eH[0], false, false, false, true);  // Cap_W / Trans_W: Abre hacia el Este
            SetOpen(eH[1], false, false, true,  true);  // Straight_H: W, E
            SetOpen(eH[2], false, false, true,  false); // Cap_E / Trans_E: Abre hacia el Oeste

            // Vertical
            SetOpen(eV[0], false, true,  false, false); // Cap_N / Trans_N: Abre hacia el Sur
            SetOpen(eV[1], true,  true,  false, false); // Straight_V: N, S
            SetOpen(eV[2], true,  false, false, false); // Cap_S / Trans_S: Abre hacia el Norte

            // 1. Interconectar todos los tiles de este camino entre sí por sus lados abiertos
            foreach (var tA in all15)
            {
                if (!roadOpenings.TryGetValue(tA, out var oA)) continue;

                foreach (var tB in all15)
                {
                    if (!roadOpenings.TryGetValue(tB, out var oB)) continue;

                    if (oA.N && oB.S) database.AddNeighbor(tA.id, tB.id, Vector2Int.up, true);
                    if (oA.S && oB.N) database.AddNeighbor(tA.id, tB.id, Vector2Int.down, true);
                    if (oA.W && oB.E) database.AddNeighbor(tA.id, tB.id, Vector2Int.left, true);
                    if (oA.E && oB.W) database.AddNeighbor(tA.id, tB.id, Vector2Int.right, true);
                }
            }

            // 2. Conectar los lados cerrados al terreno exterior (Hierbajo / Prado)
            bool hasBorder = !string.IsNullOrEmpty(borderCategory) && borderCategory != "[Sin Borde Exterior]";
            if (hasBorder)
            {
                var borderTiles = database.GetTilesByCategory(borderCategory).Where(bt => !all15.Contains(bt)).ToList();
                foreach (var t in all15)
                {
                    if (!roadOpenings.TryGetValue(t, out var o)) continue;

                    foreach (var bt in borderTiles)
                    {
                        bool allowBorderN = !o.N;
                        bool allowBorderS = !o.S;
                        bool allowBorderW = !o.W;
                        bool allowBorderE = !o.E;

                        if (roadType == RoadType.Stone)
                        {
                            if (t == eH[0]) allowBorderW = false; // El Oeste conecta a tierra, no a hierba
                            if (t == eH[2]) allowBorderE = false; // El Este conecta a tierra, no a hierba
                            if (t == eV[0]) allowBorderN = false; // El Norte conecta a tierra, no a hierba
                            if (t == eV[2]) allowBorderS = false; // El Sur conecta a tierra, no a hierba
                        }

                        if (allowBorderN) database.AddNeighbor(t.id, bt.id, Vector2Int.up, true);
                        if (allowBorderS) database.AddNeighbor(t.id, bt.id, Vector2Int.down, true);
                        if (allowBorderW) database.AddNeighbor(t.id, bt.id, Vector2Int.left, true);
                        if (allowBorderE) database.AddNeighbor(t.id, bt.id, Vector2Int.right, true);
                    }
                }
            }

            // 3. En Modo Piedra: Conectar los 4 extremos de transición hacia la red de Tierra
            if (roadType == RoadType.Stone && !string.IsNullOrEmpty(transitionCategory))
            {
                var dirtTiles = database.GetTilesByCategory(transitionCategory).Where(dt => !all15.Contains(dt)).ToList();

                // Transición Oeste: Conecta al Oeste con tiles de tierra que abran hacia el Este
                if (eH[0] != null)
                {
                    foreach (var dt in dirtTiles)
                    {
                        if (dt.role == "Road_Straight_H" || dt.role == "Road_Cross" || dt.role == "Road_T_Left" || dt.role == "Road_T_Up" || dt.role == "Road_T_Down" || dt.role == "Road_Corner_TL" || dt.role == "Road_Corner_BL" || dt.role == "Road_Cap_W")
                        {
                            database.AddNeighbor(eH[0].id, dt.id, Vector2Int.left, true);
                        }
                    }
                }

                // Transición Este: Conecta al Este con tiles de tierra que abran hacia el Oeste
                if (eH[2] != null)
                {
                    foreach (var dt in dirtTiles)
                    {
                        if (dt.role == "Road_Straight_H" || dt.role == "Road_Cross" || dt.role == "Road_T_Right" || dt.role == "Road_T_Up" || dt.role == "Road_T_Down" || dt.role == "Road_Corner_TR" || dt.role == "Road_Corner_BR" || dt.role == "Road_Cap_E")
                        {
                            database.AddNeighbor(eH[2].id, dt.id, Vector2Int.right, true);
                        }
                    }
                }

                // Transición Norte: Conecta al Norte con tiles de tierra que abran hacia el Sur
                if (eV[0] != null)
                {
                    foreach (var dt in dirtTiles)
                    {
                        if (dt.role == "Road_Straight_V" || dt.role == "Road_Cross" || dt.role == "Road_T_Down" || dt.role == "Road_T_Left" || dt.role == "Road_T_Right" || dt.role == "Road_Corner_TL" || dt.role == "Road_Corner_TR" || dt.role == "Road_Cap_N")
                        {
                            database.AddNeighbor(eV[0].id, dt.id, Vector2Int.up, true);
                        }
                    }
                }

                // Transición Sur: Conecta al Sur con tiles de tierra que abran hacia el Norte
                if (eV[2] != null)
                {
                    foreach (var dt in dirtTiles)
                    {
                        if (dt.role == "Road_Straight_V" || dt.role == "Road_Cross" || dt.role == "Road_T_Up" || dt.role == "Road_T_Left" || dt.role == "Road_T_Right" || dt.role == "Road_Corner_BL" || dt.role == "Road_Corner_BR" || dt.role == "Road_Cap_S")
                        {
                            database.AddNeighbor(eV[2].id, dt.id, Vector2Int.down, true);
                        }
                    }
                }
            }

            database.EnsureCategories();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            // Refrescar y auditar WfcVisualStudioWindow si está abierta
            var wfcWindows = Resources.FindObjectsOfTypeAll<WfcVisualStudioWindow>();
            foreach (var win in wfcWindows)
            {
                var runAuditMethod = typeof(WfcVisualStudioWindow).GetMethod("RunAudit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (runAuditMethod != null) runAuditMethod.Invoke(win, null);
                win.Repaint();
            }

            string modeMsg = (roadType == RoadType.Dirt)
                ? $"Modo Tierra: Extremos configurados como finales cerrados a '{borderCategory}' (hierbajo)."
                : $"Modo Piedra: Extremos configurados como transiciones conectadas a '{transitionCategory}' (tierra).";

            EditorUtility.DisplayDialog(
                "¡Caminos Guardados con Éxito!",
                $"Red vial '{targetCategory}' configurada al 100%:\n\n" +
                $"• 9 tiles en Bloque 3x3 (Esquinas y Cruces).\n" +
                $"• 3 tiles en Tira Horizontal (Recta y Extremos/Transiciones).\n" +
                $"• 3 tiles en Tira Vertical (Recta y Extremos/Transiciones).\n\n" +
                $"{modeMsg}",
                "¡De lujo!"
            );
        }
    }
}
