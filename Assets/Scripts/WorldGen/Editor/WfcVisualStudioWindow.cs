using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using WorldGen.Data;

namespace WorldGen.EditorTools
{
    public class WfcVisualStudioWindow : EditorWindow
    {
        private WfcVisualDatabase database;
        private Vector2 scrollAtlas;
        private Vector2 scrollRightPanel;
        private Vector2 scrollPicker;

        // Selección individual
        private int selectedTileId = -1;

        // Dirección activa para añadir sprites (0 = Norte, 1 = Sur, 2 = Oeste, 3 = Este)
        private int activeDirection = -1;
        private readonly string[] dirNames = { "▲ n", "▼ s", "◄ o", "► e" };

        // Scroll de cada una de las 4 direcciones periféricas
        private Vector2[] dirScrolls = new Vector2[4];

        // Estados de plegado para ocultar/abrir los scrolls cuando sea conveniente
        private bool[] dirExpanded = new bool[] { true, true, true, true };

        // Índices para la previsualización en cuadrícula 3x3 (cuál de los vecinos o esquinas se muestra en el preview)
        private int previewNorthIdx = 0;
        private int previewSouthIdx = 0;
        private int previewWestIdx  = 0;
        private int previewEastIdx  = 0;
        private int previewNWIdx    = 0;
        private int previewNEIdx    = 0;
        private int previewSWIdx    = 0;
        private int previewSEIdx    = 0;

        // Filtro de categoría en el selector visual
        private string activeCategoryFilter = "Todos";

        // Selección múltiple para estructuras
        private List<int> multiSelectedIds = new List<int>();

        // Nivel de zoom del Atlas principal
        private float zoom = 2.0f;
        private bool showCategoryColors = true;
        // Estados para selección por arrastre / recuadro (Marquee Selection)
        private bool isDraggingAtlas = false;
        private bool potentialAtlasDrag = false;
        private Vector2 atlasDragStart;
        private Vector2 atlasDragCurrent;
        private bool atlasDragIsShift = false;
        private bool atlasDragIsCtrl = false;
        private HashSet<int> initialMultiSelected = new HashSet<int>();
        private int selectionBorderCategoryIdx = 0;

        // Pestañas plegables y ampliables
        private bool foldoutCross = true;
        private bool foldoutPicker = false;

        // Estado para arrastrar / pintar en el selector inferior
        private bool isDraggingPicker = false;
        private bool pickerDragDisconnectMode = false;
        private HashSet<int> touchedPickerTilesInDrag = new HashSet<int>();

        // Control de división 50/50 y splitter interactivo (Mitad y Mitad por defecto)
        private float splitRatio = 0.5f;
        private float leftPanelWidth = 450f;
        private bool isResizingSplitter = false;
        private bool hideAtlasPanel = false;
        private bool foldoutModulePalette = true;

        [MenuItem("Tools/WorldGen/WFC Visual Studio")]
        public static void Open()
        {
            var win = GetWindow<WfcVisualStudioWindow>("WFC Studio");
            win.minSize = new Vector2(500, 400);
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            splitRatio = 0.5f;
            foldoutModulePalette = true;
        }

        private void SaveDatabase()
        {
            if (database == null) return;
            database.EnsureCategories();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        private void OnGUI()
        {
            Event currentEvt = Event.current;
            if (currentEvt.type == EventType.KeyDown && currentEvt.keyCode == KeyCode.Escape)
            {
                ClearSelection();
                currentEvt.Use();
            }

            if (database != null)
            {
                database.EnsureCategories();
            }

            DrawTopBar();

            if (database == null || database.atlasTexture == null)
            {
                EditorGUILayout.HelpBox("Asigna una 'WfcVisualDatabase' y arrastra la textura con tus sprites cortados en el Sprite Editor.", MessageType.Info);
                return;
            }

            if (database.tiles == null || database.tiles.Count == 0)
            {
                EditorGUILayout.HelpBox("Pulsa '1. Cargar Sprites del Sprite Editor' para sincronizar los tiles.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // Panel Izquierdo: Atlas Físico (Dividido al 50% por defecto, ajustable con splitter)
            if (!hideAtlasPanel)
            {
                if (splitRatio <= 0.05f || splitRatio >= 0.95f) splitRatio = 0.5f;
                float totalWidth = position.width;
                leftPanelWidth = Mathf.Clamp(totalWidth * splitRatio, 140f, Mathf.Max(140f, totalWidth - 140f));

                DrawAtlasPanel();

                // Barra separadora interactiva (Splitter arrastrable)
                Rect splitterRect = GUILayoutUtility.GetRect(5f, Screen.height, GUILayout.Width(5f), GUILayout.ExpandHeight(true));
                EditorGUI.DrawRect(splitterRect, new Color(0.15f, 0.15f, 0.15f, 0.7f));
                EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

                Event evt = Event.current;
                if (evt.type == EventType.MouseDown && splitterRect.Contains(evt.mousePosition))
                {
                    isResizingSplitter = true;
                    evt.Use();
                }

                if (isResizingSplitter)
                {
                    if (evt.type == EventType.MouseDrag)
                    {
                        splitRatio = Mathf.Clamp(evt.mousePosition.x / Mathf.Max(1f, totalWidth), 0.1f, 0.9f);
                        leftPanelWidth = Mathf.Clamp(totalWidth * splitRatio, 140f, Mathf.Max(140f, totalWidth - 140f));
                        Repaint();
                        evt.Use();
                    }
                    else if (evt.type == EventType.MouseUp)
                    {
                        isResizingSplitter = false;
                        evt.Use();
                    }
                }
            }

            // Panel Derecho: La Cruz y la Consola de Trabajo (Ocupa la otra mitad de la ventana)
            DrawRightCrossAndPickerPanel();

            EditorGUILayout.EndHorizontal();
        }

        private bool showAuditor = false;
        private int auditValidCount = 0;
        private int auditOrphanCount = 0;
        private int auditOneWayCount = 0;

        private void DrawTopBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Botón para Ocultar / Mostrar Atlas
            GUI.backgroundColor = hideAtlasPanel ? new Color(0.3f, 0.85f, 1f) : Color.white;
            if (GUILayout.Button(hideAtlasPanel ? "▶ Atlas" : "◀ Atlas", EditorStyles.toolbarButton, GUILayout.Width(62)))
            {
                hideAtlasPanel = !hideAtlasPanel;
            }
            GUI.backgroundColor = Color.white;

            // Botón para restablecer la división exacta al 50% / 50% (Mitad y Mitad)
            if (!hideAtlasPanel)
            {
                GUI.backgroundColor = Mathf.Approximately(splitRatio, 0.5f) ? new Color(0.4f, 1f, 0.6f) : Color.white;
                if (GUILayout.Button("⚖️ 50/50", EditorStyles.toolbarButton, GUILayout.Width(58)))
                {
                    splitRatio = 0.5f;
                    Repaint();
                }
                GUI.backgroundColor = Color.white;
            }

            database = (WfcVisualDatabase)EditorGUILayout.ObjectField(database, typeof(WfcVisualDatabase), false, GUILayout.Width(130));

            if (database != null)
            {
                database.atlasTexture = (Texture2D)EditorGUILayout.ObjectField(database.atlasTexture, typeof(Texture2D), false, GUILayout.Width(100));

                GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
                if (GUILayout.Button("1. Cargar", EditorStyles.toolbarButton, GUILayout.Width(65)))
                {
                    LoadSpritesFromSpriteEditor();
                }

                GUI.backgroundColor = new Color(0.2f, 0.95f, 0.4f);
                if (GUILayout.Button("🛣️ Asist. Caminos", EditorStyles.toolbarButton, GUILayout.Width(110)))
                {
                    RoadAutotileWizardWindow.OpenWithDatabase(database);
                }

                GUI.backgroundColor = new Color(0.85f, 0.6f, 1f);
                if (GUILayout.Button("🏷️ Renombrar ID", EditorStyles.toolbarButton, GUILayout.Width(110)))
                {
                    TileRenameWizardWindow.OpenWithDatabase(database, multiSelectedIds);
                }

                GUI.backgroundColor = showAuditor ? Color.yellow : Color.white;
                if (GUILayout.Button("🩺 Salud", EditorStyles.toolbarButton, GUILayout.Width(58)))
                {
                    showAuditor = !showAuditor;
                    if (showAuditor) RunAudit();
                }

                GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
                if (GUILayout.Button("💾 Guardar", EditorStyles.toolbarButton, GUILayout.Width(65)))
                {
                    SaveDatabase();
                    Debug.Log("<color=cyan><b>[WFC Database]</b> ¡Datos guardados en disco correctamente!</color>");
                }

                // BOTÓN DE LIMPIAR CONEXIONES (Tile activo o Selección)
                if (multiSelectedIds.Count > 0 || selectedTileId >= 0)
                {
                    GUI.backgroundColor = new Color(1f, 0.45f, 0.2f);
                    int count = multiSelectedIds.Count > 0 ? multiSelectedIds.Count : 1;
                    string btnTxt = count > 1 ? $"🧹 Limpiar Conexiones ({count})" : "🧹 Limpiar Conexiones";
                    if (GUILayout.Button(btnTxt, EditorStyles.toolbarButton, GUILayout.Width(170)))
                    {
                        ClearSelectionConnections();
                    }
                    GUI.backgroundColor = Color.white;
                }

                // BOTÓN DE LIMPIAR TODO / RESET COMPLETO
                GUI.backgroundColor = new Color(0.7f, 0.2f, 0.2f);
                if (GUILayout.Button("🗑️ Limpiar Todo", EditorStyles.toolbarButton, GUILayout.Width(92)))
                {
                    if (EditorUtility.DisplayDialog("Limpiar Toda la Base de Datos", 
                        "¿Estás seguro de que quieres borrar TODAS las conexiones de los 148 tiles y dejarlos completamente limpios y vacíos para empezar de cero?", 
                        "Sí, Limpiar Todo", "Cancelar"))
                    {
                        ClearAllConnections();
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            if (showAuditor && database != null)
            {
                DrawAuditorBar();
            }
        }

        private void DrawTopModulePaletteBar()
        {
            if (database == null) return;
            database.EnsureCategories();

            int selCount = (multiSelectedIds != null && multiSelectedIds.Count > 0)
                ? multiSelectedIds.Count
                : (selectedTileId >= 0 ? 1 : 0);

            EditorGUILayout.BeginVertical("helpBox");

            // Barra de título compacta con letra pequeña
            EditorGUILayout.BeginHorizontal();
            string titleText = selCount > 0 ? $"🎨 Paleta ({selCount} sel):" : "🎨 Paleta Módulos:";
            if (selCount > 0) GUI.color = Color.yellow;
            EditorGUILayout.LabelField(titleText, EditorStyles.miniBoldLabel, GUILayout.Width(selCount > 0 ? 115 : 100));
            if (selCount > 0) GUI.color = Color.white;

            // Campo para escribir y añadir nuevo módulo
            newCategoryInput = EditorGUILayout.TextField(newCategoryInput, EditorStyles.miniTextField, GUILayout.Width(70));
            GUI.backgroundColor = new Color(0.2f, 0.9f, 0.4f);
            if (GUILayout.Button("➕", EditorStyles.miniButton, GUILayout.Width(22), GUILayout.Height(16)))
            {
                if (!string.IsNullOrWhiteSpace(newCategoryInput))
                {
                    string trimmed = newCategoryInput.Trim();
                    if (!database.categories.Contains(trimmed))
                    {
                        database.categories.Add(trimmed);
                    }
                    if (selCount > 0)
                    {
                        ApplyCategoryToSelection(trimmed);
                    }
                    else
                    {
                        database.EnsureCategories();
                        SaveDatabase();
                    }
                    newCategoryInput = "";
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();

            // Botón Ocultar / Mostrar pequeño
            GUI.backgroundColor = foldoutModulePalette ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.3f, 0.85f, 1f);
            if (GUILayout.Button(foldoutModulePalette ? "👁 Ocultar" : "👁 Mostrar", EditorStyles.miniButton, GUILayout.Width(62), GUILayout.Height(16)))
            {
                foldoutModulePalette = !foldoutModulePalette;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Si está visible (por defecto SÍ aparece)
            if (foldoutModulePalette)
            {
                // Rejilla de botones de módulos coloreados adaptativa al ancho
                if (database.categories != null && database.categories.Count > 0)
                {
                    int cols = Mathf.Max(2, Mathf.FloorToInt((leftPanelWidth - 20) / 80f));
                    for (int i = 0; i < database.categories.Count; i += cols)
                    {
                        EditorGUILayout.BeginHorizontal();
                        for (int j = 0; j < cols && (i + j) < database.categories.Count; j++)
                        {
                            string cat = database.categories[i + j];
                            Color c = GetCategoryColor(cat);
                            int count = database.GetTilesByCategory(cat).Count;

                            GUI.backgroundColor = c;
                            string btnLabel = $"{cat} ({count})";
                            if (GUILayout.Button(btnLabel, EditorStyles.miniButton, GUILayout.Height(17)))
                            {
                                if (selCount > 0)
                                {
                                    ApplyCategoryToSelection(cat);
                                }
                                else
                                {
                                    multiSelectedIds = database.GetTilesByCategory(cat).Select(t => t.id).ToList();
                                    if (multiSelectedIds.Count > 0) selectedTileId = multiSelectedIds[0];
                                    Repaint();
                                }
                            }
                        }
                        GUI.backgroundColor = Color.white;
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAtlasPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(leftPanelWidth), GUILayout.MaxWidth(leftPanelWidth));

            // Barra superior del Atlas (Compacta y pequeña)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Atlas ({database.tiles.Count}):", EditorStyles.miniBoldLabel, GUILayout.Width(72));

            int emptyCount = database.tiles.Count(t => t.allowedNorth.Count + t.allowedSouth.Count + t.allowedEast.Count + t.allowedWest.Count == 0);
            GUI.backgroundColor = (emptyCount > 0) ? new Color(1f, 0.65f, 0.2f) : Color.white;
            if (GUILayout.Button($"🔍 {emptyCount}", EditorStyles.miniButton, GUILayout.Width(45), GUILayout.Height(16)))
            {
                SelectEmptyTiles();
                Repaint();
            }

            GUI.backgroundColor = showCategoryColors ? new Color(0.4f, 1f, 0.6f) : Color.white;
            if (GUILayout.Button("🎨 Color", EditorStyles.miniButton, GUILayout.Width(50), GUILayout.Height(16)))
            {
                showCategoryColors = !showCategoryColors;
                Repaint();
            }
            GUI.backgroundColor = Color.white;

            int totalSel = multiSelectedIds.Count > 0 ? multiSelectedIds.Count : (selectedTileId >= 0 ? 1 : 0);
            GUI.backgroundColor = totalSel > 0 ? new Color(1f, 0.45f, 0.2f) : new Color(0.9f, 0.9f, 0.9f);
            string cleanBtnText = totalSel > 1 ? $"🧹 Limpiar Conexiones ({totalSel})" : "🧹 Limpiar Conexiones";
            GUI.enabled = (totalSel > 0);
            if (GUILayout.Button(cleanBtnText, EditorStyles.miniButton, GUILayout.Height(16)))
            {
                ClearSelectionConnections();
            }
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Zoom:", EditorStyles.miniLabel, GUILayout.Width(35));
            zoom = EditorGUILayout.Slider(zoom, 0.5f, 4.0f, GUILayout.Width(65));
            EditorGUILayout.EndHorizontal();

            // PALETA DE MÓDULOS DE COLORES VISIBLE POR DEFECTO
            DrawTopModulePaletteBar();

            // Ayuda rápida de interacción (Texto pequeño y discreto)
            EditorGUILayout.LabelField("💡 Clic: Seleccionar tile | Shift+Clic: Multiselección | Arrastrar: Recuadro", EditorStyles.miniLabel);

            scrollAtlas = EditorGUILayout.BeginScrollView(scrollAtlas);

            Texture2D tex = database.atlasTexture;
            float displayW = tex.width * zoom;
            float displayH = tex.height * zoom;

            Rect rect = GUILayoutUtility.GetRect(displayW, displayH, GUILayout.Width(displayW), GUILayout.Height(displayH));
            GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill);

            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;

            if (e.type == EventType.ScrollWheel && (e.control || e.command || e.alt))
            {
                float oldZoom = zoom;
                zoom = Mathf.Clamp(zoom - e.delta.y * 0.15f, 0.5f, 5.0f);
                if (Mathf.Abs(zoom - oldZoom) > 0.01f)
                {
                    e.Use();
                    Repaint();
                }
            }

            var activeTile = database.GetTileById(selectedTileId);

            // 1. Dibujar resaltado de cada celda / tile con indicador de color de módulo
            foreach (var tile in database.tiles)
            {
                Rect r = tile.rectInAtlas;
                float guiY = rect.y + (tex.height - r.y - r.height) * zoom;
                float guiX = rect.x + r.x * zoom;
                Rect cellRect = new Rect(guiX, guiY, r.width * zoom, r.height * zoom);

                bool isSelected = (tile.id == selectedTileId);
                bool isMulti = multiSelectedIds.Contains(tile.id);
                bool isConnectedToActive = (activeTile != null && IsAlreadyConnected(activeTile, tile.id, activeDirection));

                // Color del módulo / categoría
                Color catColor = GetCategoryColor(tile.category);

                if (showCategoryColors)
                {
                    // Relleno sutil del color del módulo
                    EditorGUI.DrawRect(cellRect, new Color(catColor.r, catColor.g, catColor.b, 0.2f));

                    // Barra horizontal coloreada del módulo en la base del tile
                    float barH = Mathf.Max(3f, 4f * (zoom / 2.0f));
                    Rect tagRect = new Rect(cellRect.x, cellRect.yMax - barH, cellRect.width, barH);
                    EditorGUI.DrawRect(tagRect, new Color(catColor.r, catColor.g, catColor.b, 0.95f));

                    // Punto indicador en la esquina superior izquierda
                    float dotSize = Mathf.Max(4f, 5f * (zoom / 2.0f));
                    Rect dotRect = new Rect(cellRect.x + 1f, cellRect.y + 1f, dotSize, dotSize);
                    EditorGUI.DrawRect(dotRect, new Color(catColor.r, catColor.g, catColor.b, 1.0f));

                    // Contorno del color del módulo
                    Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, new Color(catColor.r, catColor.g, catColor.b, 0.65f));
                }

                if (isSelected)
                {
                    EditorGUI.DrawRect(cellRect, new Color(0f, 1f, 1f, 0.4f));
                    Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, Color.cyan);
                }
                else if (isMulti)
                {
                    EditorGUI.DrawRect(cellRect, new Color(1f, 0.9f, 0f, 0.4f));
                    Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, Color.yellow);
                }
                else if (isConnectedToActive)
                {
                    EditorGUI.DrawRect(cellRect, new Color(0f, 1f, 0f, 0.2f));
                    Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, new Color(0.2f, 1f, 0.2f, 0.95f));
                }
                else if (!showCategoryColors)
                {
                    Handles.DrawSolidRectangleWithOutline(cellRect, Color.clear, new Color(0f, 0.3f, 1f, 0.2f));
                }
            }

            // 2. Control de eventos de ratón: Arrastre fluido para selección múltiple
            if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 1) && rect.Contains(mousePos))
            {
                potentialAtlasDrag = true;
                isDraggingAtlas = false;
                atlasDragStart = mousePos;
                atlasDragCurrent = mousePos;
                atlasDragIsShift = e.shift;
                atlasDragIsCtrl = (e.control || e.command);
                initialMultiSelected = new HashSet<int>(multiSelectedIds);

                e.Use();
                Repaint();
            }
            else if ((e.type == EventType.MouseDrag || e.type == EventType.MouseMove) && potentialAtlasDrag)
            {
                atlasDragCurrent = mousePos;

                if (!isDraggingAtlas && (atlasDragCurrent - atlasDragStart).sqrMagnitude > 12f)
                {
                    isDraggingAtlas = true;
                }

                if (isDraggingAtlas)
                {
                    Rect marqueeRect = GetMarqueeRect(atlasDragStart, atlasDragCurrent);

                    List<int> inMarquee = new List<int>();
                    foreach (var tile in database.tiles)
                    {
                        Rect r = tile.rectInAtlas;
                        float guiY = rect.y + (tex.height - r.y - r.height) * zoom;
                        float guiX = rect.x + r.x * zoom;
                        Rect cellRect = new Rect(guiX, guiY, r.width * zoom, r.height * zoom);

                        if (marqueeRect.Overlaps(cellRect))
                        {
                            inMarquee.Add(tile.id);
                        }
                    }

                    if (atlasDragIsCtrl)
                    {
                        var remaining = new HashSet<int>(initialMultiSelected);
                        foreach (var id in inMarquee) remaining.Remove(id);
                        multiSelectedIds = remaining.ToList();
                    }
                    else if (atlasDragIsShift)
                    {
                        var combined = new HashSet<int>(initialMultiSelected);
                        foreach (var id in inMarquee) combined.Add(id);
                        multiSelectedIds = combined.ToList();
                    }
                    else
                    {
                        multiSelectedIds = inMarquee;
                    }

                    if (multiSelectedIds.Count > 0)
                    {
                        selectedTileId = multiSelectedIds[0];
                    }

                    Repaint();
                    e.Use();
                }
            }
            else if (e.type == EventType.MouseUp && potentialAtlasDrag)
            {
                if (!isDraggingAtlas)
                {
                    WfcTileEntry clickedTile = FindTileAtGuiPos(mousePos, rect, tex);

                    if (e.button == 1)
                    {
                        // Clic derecho: Limpiar selección
                        ClearSelection();
                        activeDirection = -1;
                    }
                    else if (clickedTile != null)
                    {
                        if (e.shift)
                        {
                            // Shift + Clic: Alternar en multiselección
                            if (!multiSelectedIds.Contains(clickedTile.id))
                            {
                                multiSelectedIds.Add(clickedTile.id);
                                selectedTileId = clickedTile.id;
                            }
                            else
                            {
                                multiSelectedIds.Remove(clickedTile.id);
                                selectedTileId = multiSelectedIds.Count > 0 ? multiSelectedIds[0] : -1;
                            }
                        }
                        else if (e.control || e.command)
                        {
                            // Ctrl/Cmd + Clic: Quitar de selección
                            multiSelectedIds.Remove(clickedTile.id);
                            if (selectedTileId == clickedTile.id)
                                selectedTileId = multiSelectedIds.Count > 0 ? multiSelectedIds[0] : -1;
                        }
                        else
                        {
                            // Clic normal: SELECCIONAR TILE DIRECTAMENTE
                            selectedTileId = clickedTile.id;
                            multiSelectedIds.Clear();
                            multiSelectedIds.Add(clickedTile.id);
                            activeDirection = -1; // Tile Centro seleccionado en la cruz
                        }

                        ClampPreviewIndex(clickedTile, 0);
                        ClampPreviewIndex(clickedTile, 1);
                        ClampPreviewIndex(clickedTile, 2);
                        ClampPreviewIndex(clickedTile, 3);
                    }
                    else if (!e.shift && !e.control)
                    {
                        // Clic en fondo vacío fuera de tiles: limpiar
                        multiSelectedIds.Clear();
                        selectedTileId = -1;
                        activeDirection = -1;
                    }
                }
                else
                {
                    // FIN DEL ARRASTRE DE RECUADRO: Selección múltiple visual limpia
                    if (multiSelectedIds.Count > 0)
                    {
                        selectedTileId = multiSelectedIds[0];
                        activeDirection = -1;
                    }
                }

                isDraggingAtlas = false;
                potentialAtlasDrag = false;
                Repaint();
                e.Use();
            }

            // Dibujar recuadro de arrastre (Marquee Box)
            if (isDraggingAtlas)
            {
                Rect marqueeRect = GetMarqueeRect(atlasDragStart, atlasDragCurrent);
                Color fillCol = atlasDragIsCtrl ? new Color(1f, 0.3f, 0.3f, 0.25f) : new Color(0.2f, 0.95f, 1f, 0.3f);
                Color outlineCol = atlasDragIsCtrl ? Color.red : Color.cyan;
                EditorGUI.DrawRect(marqueeRect, fillCol);
                Handles.DrawSolidRectangleWithOutline(marqueeRect, Color.clear, outlineCol);
            }

            EditorGUILayout.EndScrollView();

            // Panel de Acciones Rápidas sobre la selección actual
            DrawSelectionActionButtons();

            EditorGUILayout.EndVertical();
        }

        private string newCategoryInput = "";

        public static Color GetCategoryColor(string category)
        {
            if (string.IsNullOrEmpty(category)) return new Color(0.6f, 0.6f, 0.6f, 0.9f);
            switch (category)
            {
                case "Prado": return new Color(0.2f, 0.85f, 0.2f); // Verde hierba
                case "Flores": return new Color(0.95f, 0.4f, 0.8f); // Rosa
                case "Agua_Clara": return new Color(0.1f, 0.75f, 1f); // Azul cielo / cian
                case "Agua_Turbia": return new Color(0.1f, 0.5f, 0.7f); // Turquesa / azul oscuro
                case "Bosque": return new Color(0.05f, 0.55f, 0.15f); // Verde bosque oscuro
                case "Camino": return new Color(0.85f, 0.55f, 0.2f); // Marrón naranja tierra
                case "Camino_Piedra": return new Color(0.7f, 0.7f, 0.75f); // Gris piedra
                case "Cultivo": return new Color(0.9f, 0.85f, 0.2f); // Amarillo trigo
                case "Fuerte": return new Color(0.9f, 0.3f, 0.2f); // Rojo teja
                case "Iglesia": return new Color(0.7f, 0.3f, 0.9f); // Púrpura
                case "Torre": return new Color(0.4f, 0.6f, 0.9f); // Azul pizarra
                case "Cueva": return new Color(0.55f, 0.4f, 0.3f); // Marrón roca
                case "Meseta": return new Color(0.8f, 0.5f, 0.3f); // Terracota
                case "Decoracion": return new Color(0.95f, 0.7f, 0.1f); // Dorado
                default:
                    int hash = Mathf.Abs(category.GetHashCode());
                    float h = (hash % 1000) / 1000f;
                    return Color.HSVToRGB(h, 0.75f, 0.9f);
            }
        }

        private void DrawSelectionActionButtons()
        {
            var targetTiles = (multiSelectedIds != null && multiSelectedIds.Count > 0)
                ? multiSelectedIds.Select(id => database.GetTileById(id)).Where(t => t != null).ToList()
                : (selectedTileId >= 0 ? new List<WfcTileEntry> { database.GetTileById(selectedTileId) } : new List<WfcTileEntry>());

            targetTiles = targetTiles.Where(t => t != null).ToList();
            int selCount = targetTiles.Count;
            if (selCount == 0) return;

            EditorGUILayout.BeginVertical("helpBox");

            // Fila 0: Diagnóstico de categoría actual y asignación
            var distinctCats = targetTiles.Select(t => t.category).Distinct().ToList();
            string currentCatDisplay = (distinctCats.Count == 1) ? distinctCats[0] : $"[Mixto: {string.Join(", ", distinctCats)}]";
            Color currentCatColor = (distinctCats.Count == 1) ? GetCategoryColor(distinctCats[0]) : Color.yellow;

            EditorGUILayout.BeginHorizontal();
            GUI.color = currentCatColor;
            EditorGUILayout.LabelField($"🏷️ {currentCatDisplay} ({selCount})", EditorStyles.miniBoldLabel, GUILayout.Width(110));
            GUI.color = Color.white;

            var catList = database.categories != null ? database.categories.ToList() : new List<string>();
            int curPopupIdx = (distinctCats.Count == 1) ? catList.IndexOf(distinctCats[0]) : -1;
            if (curPopupIdx < 0) curPopupIdx = 0;

            int newSelIdx = EditorGUILayout.Popup(curPopupIdx, catList.ToArray(), EditorStyles.miniButton, GUILayout.Width(85));
            if (newSelIdx != curPopupIdx && newSelIdx >= 0 && newSelIdx < catList.Count)
            {
                ApplyCategoryToSelection(catList[newSelIdx]);
            }

            GUI.backgroundColor = selCount > 0 ? new Color(1f, 0.45f, 0.2f) : new Color(0.9f, 0.9f, 0.9f);
            string btnLimpiarText = selCount > 1 ? $"🧹 Limpiar Conexiones ({selCount})" : "🧹 Limpiar Conexiones";
            GUI.enabled = (selCount > 0);
            if (GUILayout.Button(btnLimpiarText, EditorStyles.miniButton, GUILayout.Width(165), GUILayout.Height(17)))
            {
                ClearSelectionConnections();
            }
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Paleta de Todos los Módulos con sus Colores (Rejilla compacta)
            if (database != null && database.categories != null)
            {
                int cols = Mathf.Max(2, Mathf.FloorToInt((leftPanelWidth - 20) / 80f));
                for (int i = 0; i < database.categories.Count; i += cols)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int j = 0; j < cols && (i + j) < database.categories.Count; j++)
                    {
                        string cat = database.categories[i + j];
                        Color qc = GetCategoryColor(cat);
                        int cCount = database.GetTilesByCategory(cat).Count;
                        GUI.backgroundColor = qc;
                        if (GUILayout.Button($"{cat} ({cCount})", EditorStyles.miniButton, GUILayout.Height(17)))
                        {
                            ApplyCategoryToSelection(cat);
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(2);

            // Fila 1: Configuración del Borde Exterior (Compacto)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🌐 Borde Ext:", EditorStyles.miniLabel, GUILayout.Width(68));

            var borderCleanNames = new List<string> { "[Sin Conexión Exterior / Solo Interno]" };
            var borderDisplayNames = new List<string> { "[Sin Conexión Exterior / Solo Interno]" };

            if (database != null && database.categories != null)
            {
                foreach (var cat in database.categories)
                {
                    if (!string.IsNullOrEmpty(cat) && !borderCleanNames.Contains(cat))
                    {
                        borderCleanNames.Add(cat);
                        int count = database.GetTilesByCategory(cat).Count;
                        borderDisplayNames.Add($"{cat} ({count} tiles)");
                    }
                }
            }

            selectionBorderCategoryIdx = Mathf.Clamp(selectionBorderCategoryIdx, 0, borderCleanNames.Count - 1);
            selectionBorderCategoryIdx = EditorGUILayout.Popup(selectionBorderCategoryIdx, borderDisplayNames.ToArray(), EditorStyles.miniButton, GUILayout.Width(110));
            string chosenBorder = borderCleanNames[selectionBorderCategoryIdx];

            GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
            if (GUILayout.Button("🔗 Bordes", EditorStyles.miniButton, GUILayout.Width(62), GUILayout.Height(16)))
            {
                AutoWireBordersOnly(chosenBorder);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Fila 2: Botones de Auto-Selección rápida (2 filas compactas)
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.95f, 0.2f);
            if (GUILayout.Button("🛣️ Caminos", EditorStyles.miniButton, GUILayout.Height(17))) AutoSelectRoads();
            if (GUILayout.Button("🧱 NxM", EditorStyles.miniButton, GUILayout.Height(17))) AutoSelectNxM();
            if (GUILayout.Button("🔲 3x3", EditorStyles.miniButton, GUILayout.Height(17))) AutoSelect3x3();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("↔ Tira H", EditorStyles.miniButton, GUILayout.Height(17))) AutoSelectHorizontalStrip();
            if (GUILayout.Button("↕ Tira V", EditorStyles.miniButton, GUILayout.Height(17))) AutoSelectVerticalStrip();
            GUI.backgroundColor = new Color(0.85f, 0.6f, 1f);
            if (GUILayout.Button("🏷️ Renombrar", EditorStyles.miniButton, GUILayout.Height(17)))
            {
                TileRenameWizardWindow.OpenWithDatabase(database, multiSelectedIds);
            }
            int c = multiSelectedIds.Count > 0 ? multiSelectedIds.Count : (selectedTileId >= 0 ? 1 : 0);
            GUI.backgroundColor = c > 0 ? new Color(1f, 0.45f, 0.2f) : new Color(0.9f, 0.9f, 0.9f);
            GUI.enabled = (c > 0);
            string tmplLimpiarTxt = c > 1 ? $"🧹 Limpiar Conexiones ({c})" : "🧹 Limpiar Conexiones";
            if (GUILayout.Button(tmplLimpiarTxt, EditorStyles.miniButton, GUILayout.Height(17)))
            {
                ClearSelectionConnections();
            }
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Fila 3: Botones de Cableado y Plantillas (2 filas compactas)
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.95f, 0.4f);
            if (GUILayout.Button("🛣️ Asist. Caminos", EditorStyles.miniButton, GUILayout.Height(17))) RoadAutotileWizardWindow.OpenWithDatabase(database);
            GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
            if (GUILayout.Button("🧱 Estructura", EditorStyles.miniButton, GUILayout.Height(17))) AutoWireStructureWithBorder(chosenBorder);
            GUI.backgroundColor = new Color(0.1f, 0.85f, 1f);
            if (GUILayout.Button("🌊 Asistente", EditorStyles.miniButton, GUILayout.Height(17))) AutotileWizardWindow.OpenWithDatabase(database);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.95f, 1f);
            if (GUILayout.Button("🏝️ 3x3", EditorStyles.miniButton, GUILayout.Height(17))) AutoWireAs3x3Template(chosenBorder);
            GUI.backgroundColor = new Color(1f, 0.7f, 0.3f);
            if (GUILayout.Button("↕ Vert", EditorStyles.miniButton, GUILayout.Height(17))) AutoWireSelectionAsVerticalLine(chosenBorder);
            GUI.backgroundColor = new Color(1f, 0.85f, 0.4f);
            if (GUILayout.Button("↔ Horiz", EditorStyles.miniButton, GUILayout.Height(17))) AutoWireSelectionAsHorizontalLine(chosenBorder);
            GUI.backgroundColor = new Color(0.8f, 0.6f, 1f);
            if (GUILayout.Button("🌱 1x1", EditorStyles.miniButton, GUILayout.Height(17))) AutoWireSelectionAsSingleObjects(chosenBorder);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private WfcTileEntry FindTileAtGuiPos(Vector2 guiPos, Rect rect, Texture2D tex)
        {
            if (database == null || database.tiles == null) return null;
            foreach (var tile in database.tiles)
            {
                Rect r = tile.rectInAtlas;
                float guiY = rect.y + (tex.height - r.y - r.height) * zoom;
                float guiX = rect.x + r.x * zoom;
                Rect cellRect = new Rect(guiX, guiY, r.width * zoom, r.height * zoom);
                if (cellRect.Contains(guiPos)) return tile;
            }
            return null;
        }

        private void ApplyCategoryToSelection(string cat)
        {
            if (database == null || database.tiles == null || string.IsNullOrEmpty(cat)) return;
            var targetIds = new HashSet<int>(multiSelectedIds);
            if (selectedTileId >= 0) targetIds.Add(selectedTileId);
            if (targetIds.Count == 0) return;

            Undo.RecordObject(database, $"Asignar Módulo {cat} a {targetIds.Count} tiles");
            if (!database.categories.Contains(cat)) database.categories.Add(cat);

            int count = 0;
            foreach (var id in targetIds)
            {
                var t = database.GetTileById(id);
                if (t != null)
                {
                    t.category = cat;
                    count++;
                }
            }

            database.EnsureCategories();
            SaveDatabase();
            Repaint();
            Debug.Log($"<color=cyan><b>[Módulo Asignado]</b> Se ha asignado el módulo <b>'{cat}'</b> a <b>{count}</b> tiles seleccionados con éxito.</color>");
        }

        private void DrawRightCrossAndPickerPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            scrollRightPanel = EditorGUILayout.BeginScrollView(scrollRightPanel);

            var activeTile = database.GetTileById(selectedTileId);
            if (activeTile == null)
            {
                EditorGUILayout.HelpBox("Haz clic en cualquier tile del atlas para abrir su consola en cruz.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            // Barra superior del tile activo
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Tile #{activeTile.id}", EditorStyles.boldLabel, GUILayout.Width(60));
            activeTile.spriteName = EditorGUILayout.TextField(activeTile.spriteName, GUILayout.Width(130));
            GUI.backgroundColor = new Color(0.85f, 0.6f, 1f);
            if (GUILayout.Button("🏷️", EditorStyles.miniButton, GUILayout.Width(24), GUILayout.Height(16)))
            {
                TileRenameWizardWindow.OpenWithDatabase(database, new List<int> { activeTile.id });
            }
            GUI.backgroundColor = Color.white;

            Color catCol = GetCategoryColor(activeTile.category);
            GUI.color = catCol;
            EditorGUILayout.LabelField("Módulo:", EditorStyles.boldLabel, GUILayout.Width(50));
            GUI.color = Color.white;

            int catIdx = database.categories.IndexOf(activeTile.category);
            if (catIdx < 0) { database.categories.Add(activeTile.category); catIdx = database.categories.IndexOf(activeTile.category); }
            int newTileCatIdx = EditorGUILayout.Popup(catIdx, database.categories.ToArray(), GUILayout.Width(100));
            if (newTileCatIdx != catIdx && newTileCatIdx >= 0 && newTileCatIdx < database.categories.Count)
            {
                ApplyCategoryToSelection(database.categories[newTileCatIdx]);
            }

            EditorGUILayout.LabelField("Peso:", GUILayout.Width(35));
            activeTile.weight = EditorGUILayout.Slider(activeTile.weight, 0.01f, 5.0f, GUILayout.Width(120));

            GUILayout.FlexibleSpace();

            bool anyClosed = dirExpanded.Any(x => !x);
            if (GUILayout.Button(anyClosed ? "Desplegar Menús" : "Plegar Menús", EditorStyles.miniButton, GUILayout.Width(110)))
            {
                for (int i = 0; i < dirExpanded.Length; i++) dirExpanded[i] = anyClosed;
            }

            GUI.backgroundColor = new Color(1f, 0.4f, 0.2f);
            if (GUILayout.Button("🧹 Limpiar Conexiones", EditorStyles.miniButton, GUILayout.Width(140), GUILayout.Height(18)))
            {
                ClearSingleTileConnections(activeTile.id);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);

            EditorGUILayout.Space(4);
            foldoutCross = EditorGUILayout.Foldout(foldoutCross, $"🎮 Consola en Cruz (Tile #{activeTile.id} - Módulo: {activeTile.category})", true, EditorStyles.foldoutHeader);
            if (foldoutCross)
            {
                string modeBanner = (activeDirection == -1)
                    ? "🎯 TILE CENTRO (Haz clic en el Atlas para seleccionar cualquier pieza)"
                    : $"● Dirección Activa: {dirNames[activeDirection]} (Usa los satélites o el selector inferior para conectar)";
                Color bannerCol = (activeDirection == -1) ? Color.cyan : new Color(0.2f, 1f, 0.3f);
                GUI.color = bannerCol;
                EditorGUILayout.LabelField(modeBanner, EditorStyles.boldLabel);
                GUI.color = Color.white;
                EditorGUILayout.Space(3);

                // --- LA CONSOLA EN CRUZ CON MOCKUP 3x3 ENCAJADO Y MENÚS SATÉLITES ---
                DrawSatanicCrossCockpit(activeTile);
            }

            EditorGUILayout.Space(10);
            foldoutPicker = EditorGUILayout.Foldout(foldoutPicker, "📋 Buscador / Selector Inferior del Atlas", true, EditorStyles.foldoutHeader);
            if (foldoutPicker)
            {
                // --- SELECTOR VISUAL CON LA FORMA DEL ATLAS ---
                DrawAtlasLayoutPicker(activeTile);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // DIBUJA LA CRUZ CON SU NÚCLEO ENCAJADO Y LOS SCROLLS SATÉLITES A LOS LADOS
        private void DrawSatanicCrossCockpit(WfcTileEntry activeTile)
        {
            EditorGUILayout.BeginVertical();

            // 1. ZONA SUPERIOR: Scroll Satélite de NORTE (arriba de la cruz)
            DrawTopSatellite(activeTile);

            EditorGUILayout.Space(5);

            // 2. ZONA MEDIA: [Satélite Oeste] - [CRUZ DE PREVIEW 3x3 ENCAJADA] - [Satélite Este]
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Satélite Izquierdo (Oeste)
            DrawLeftSatellite(activeTile);

            GUILayout.Space(10);

            // EL NÚCLEO: SIMULACIÓN VISUAL EN CRUZ 3x3 (192x192 px, piezas tocándose directamente)
            DrawSeamlessPreviewCore(activeTile);

            GUILayout.Space(10);

            // Satélite Derecho (Este)
            DrawRightSatellite(activeTile);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 3. ZONA INFERIOR: Scroll Satélite de SUR (abajo de la cruz)
            DrawBottomSatellite(activeTile);

            EditorGUILayout.EndVertical();
        }

        // NÚCLEO: Muestra el centro, los 4 vecinos cardinales y las 4 esquinas formando una cuadrícula 3x3 completa
        private void DrawSeamlessPreviewCore(WfcTileEntry activeTile)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(204), GUILayout.Height(204));
            
            // Obtener tiles activos actuales en las 4 direcciones cardinales
            int nId = GetCurrentPreviewId(activeTile, 0);
            int sId = GetCurrentPreviewId(activeTile, 1);
            int wId = GetCurrentPreviewId(activeTile, 2);
            int eId = GetCurrentPreviewId(activeTile, 3);

            WfcTileEntry northTile = nId >= 0 ? database.GetTileById(nId) : null;
            WfcTileEntry southTile = sId >= 0 ? database.GetTileById(sId) : null;
            WfcTileEntry westTile  = wId >= 0 ? database.GetTileById(wId) : null;
            WfcTileEntry eastTile  = eId >= 0 ? database.GetTileById(eId) : null;

            // FILA SUPERIOR DEL MOCKUP 3x3: Esquina NO - Norte - Esquina NE
            EditorGUILayout.BeginHorizontal();
            DrawCornerMockupSlot(northTile, westTile, 2, 0, ref previewNWIdx, "↖ no");
            DrawMockupSlot(activeTile, activeTile.allowedNorth, ref previewNorthIdx, 0);
            DrawCornerMockupSlot(northTile, eastTile, 3, 0, ref previewNEIdx, "↗ ne");
            EditorGUILayout.EndHorizontal();

            // FILA MEDIA DEL MOCKUP 3x3: Oeste - Tile Centro - Este
            EditorGUILayout.BeginHorizontal();
            DrawMockupSlot(activeTile, activeTile.allowedWest, ref previewWestIdx, 2);
            
            // Tile Centro (La pieza elegida)
            Rect centerRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));
            DrawSpriteDirect(centerRect, activeTile.sprite);

            bool isCenterActive = (activeDirection == -1);
            Color centerOutline = isCenterActive ? Color.cyan : new Color(0f, 0.7f, 1f, 0.5f);
            if (isCenterActive)
            {
                EditorGUI.DrawRect(centerRect, new Color(0f, 1f, 1f, 0.15f));
            }
            Handles.DrawSolidRectangleWithOutline(centerRect, Color.clear, centerOutline);

            Event curE = Event.current;
            if (curE.type == EventType.MouseDown && centerRect.Contains(curE.mousePosition))
            {
                activeDirection = -1; // Activar modo selección de tile centro
                curE.Use();
                Repaint();
            }

            DrawMockupSlot(activeTile, activeTile.allowedEast, ref previewEastIdx, 3);
            EditorGUILayout.EndHorizontal();

            // FILA INFERIOR DEL MOCKUP 3x3: Esquina SO - Sur - Esquina SE
            EditorGUILayout.BeginHorizontal();
            DrawCornerMockupSlot(southTile, westTile, 2, 1, ref previewSWIdx, "↙ so");
            DrawMockupSlot(activeTile, activeTile.allowedSouth, ref previewSouthIdx, 1);
            DrawCornerMockupSlot(southTile, eastTile, 3, 1, ref previewSEIdx, "↘ se");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // Dibuja un slot de esquina de la cuadrícula 3x3 inferido de los vecinos cardinales
        private void DrawCornerMockupSlot(
            WfcTileEntry primTile, 
            WfcTileEntry secTile, 
            int primDir, 
            int secDir, 
            ref int cornerIdx, 
            string cornerLabel)
        {
            Rect rect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));

            List<int> candidates = new List<int>();
            if (primTile != null && secTile != null)
            {
                var primList = GetNeighborList(primTile, primDir);
                var secList = GetNeighborList(secTile, secDir);
                if (primList != null && secList != null)
                {
                    candidates = primList.Intersect(secList).Distinct().ToList();
                }
            }
            else if (primTile != null)
            {
                var primList = GetNeighborList(primTile, primDir);
                if (primList != null) candidates = primList.Distinct().ToList();
            }
            else if (secTile != null)
            {
                var secList = GetNeighborList(secTile, secDir);
                if (secList != null) candidates = secList.Distinct().ToList();
            }

            if (candidates.Count == 0)
            {
                // Espacio vacío para la esquina
                EditorGUI.DrawRect(rect, new Color(0.08f, 0.08f, 0.08f, 0.4f));
                Handles.DrawSolidRectangleWithOutline(rect, Color.clear, new Color(0.3f, 0.3f, 0.3f, 0.3f));
                GUI.Label(rect, cornerLabel, EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                cornerIdx = Mathf.Clamp(cornerIdx, 0, candidates.Count - 1);
                var cornerTile = database.GetTileById(candidates[cornerIdx]);

                if (cornerTile != null && cornerTile.sprite != null)
                {
                    bool fullMatch = (primTile != null && secTile != null);
                    float alpha = fullMatch ? 1.0f : 0.5f;
                    DrawSpriteDirectWithAlpha(rect, cornerTile.sprite, alpha);
                }

                Color outlineColor = new Color(0.4f, 0.4f, 0.4f, 0.4f);
                Handles.DrawSolidRectangleWithOutline(rect, Color.clear, outlineColor);

                Rect badgeRect = new Rect(rect.xMax - 30, rect.yMax - 16, 28, 14);

                Event curE = Event.current;
                if (curE.type == EventType.MouseDown && rect.Contains(curE.mousePosition))
                {
                    if (curE.button == 0)
                    {
                        if (candidates.Count > 1)
                        {
                            cornerIdx = (cornerIdx + 1) % candidates.Count;
                        }
                        else if (cornerTile != null && curE.clickCount == 2)
                        {
                            // Doble clic: seleccionar esta esquina como tile activo
                            selectedTileId = cornerTile.id;
                        }
                    }
                    curE.Use();
                    Repaint();
                }

                if (candidates.Count > 1)
                {
                    EditorGUI.DrawRect(badgeRect, new Color(0f, 0f, 0f, 0.75f));
                    GUI.Label(badgeRect, $"{cornerIdx + 1}/{candidates.Count}", EditorStyles.miniLabel);
                }
            }
        }

        // Dibuja una celda de previsualización individual de la cruz
        private void DrawMockupSlot(WfcTileEntry activeTile, List<int> neighbors, ref int previewIdx, int dirIndex)
        {
            Rect rect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));

            bool isActiveDir = (activeDirection == dirIndex);

            if (neighbors == null || neighbors.Count == 0)
            {
                // Espacio vacío con marco sutil
                EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
                Color borderColor = isActiveDir ? Color.green : new Color(0.4f, 0.4f, 0.4f, 0.5f);
                Handles.DrawSolidRectangleWithOutline(rect, Color.clear, borderColor);

                Event eEmpty = Event.current;
                if (eEmpty.type == EventType.MouseDown && rect.Contains(eEmpty.mousePosition))
                {
                    activeDirection = dirIndex;
                    eEmpty.Use();
                    Repaint();
                }
                GUI.Label(rect, dirNames[dirIndex].Substring(0, 1), EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                previewIdx = Mathf.Clamp(previewIdx, 0, neighbors.Count - 1);
                var neighborTile = database.GetTileById(neighbors[previewIdx]);

                if (neighborTile != null && neighborTile.sprite != null)
                {
                    DrawSpriteDirect(rect, neighborTile.sprite);
                }

                Color outlineColor = isActiveDir ? Color.green : new Color(0f, 0.6f, 1f, 0.6f);
                Handles.DrawSolidRectangleWithOutline(rect, Color.clear, outlineColor);

                Rect xRect = new Rect(rect.xMax - 26, rect.yMin + 2, 24, 24);
                Rect badgeRect = new Rect(rect.xMax - 32, rect.yMax - 18, 30, 16);

                Event curE = Event.current;
                if (curE.type == EventType.MouseDown && rect.Contains(curE.mousePosition))
                {
                    if (xRect.Contains(curE.mousePosition))
                    {
                        // CLIC EN EL BOTÓN ROJO QUITAR
                        if (neighborTile != null)
                        {
                            Undo.RecordObject(database, "Quitar Vecino");
                            database.RemoveNeighbor(activeTile.id, neighborTile.id, GetVectorForDir(dirIndex), true);
                            ClampPreviewIndex(activeTile, dirIndex);
                            SaveDatabase();
                            Debug.Log($"<color=red>[WFC Vecinos]</color> Tile #{neighborTile.id} quitado de la dirección {dirNames[dirIndex]}.");
                        }
                    }
                    else if (badgeRect.Contains(curE.mousePosition) || activeDirection == dirIndex)
                    {
                        // Alternar al siguiente vecino si hay varios
                        if (neighbors.Count > 1)
                        {
                            previewIdx = (previewIdx + 1) % neighbors.Count;
                        }
                        activeDirection = dirIndex;
                    }
                    else
                    {
                        activeDirection = dirIndex;
                    }

                    curE.Use();
                    Repaint();
                }

                // Dibujar botón visual [✕] más grande y claro
                EditorGUI.DrawRect(xRect, new Color(0.95f, 0.15f, 0.15f, 0.95f));
                GUI.Label(new Rect(xRect.x + 6, xRect.y + 3, 18, 18), "✕", EditorStyles.whiteBoldLabel);

                // Si hay varios vecinos, mostrar contador
                if (neighbors.Count > 1)
                {
                    EditorGUI.DrawRect(badgeRect, new Color(0f, 0f, 0f, 0.85f));
                    GUI.Label(badgeRect, $"{previewIdx + 1}/{neighbors.Count}", EditorStyles.miniLabel);
                }
            }
        }

        // SATÉLITE SUPERIOR: Scroll de Norte (arriba)
        private void DrawTopSatellite(WfcTileEntry activeTile)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            bool isAct = (activeDirection == 0);
            GUI.backgroundColor = isAct ? new Color(0.4f, 0.95f, 0.4f) : Color.white;
            EditorGUILayout.BeginVertical("box", GUILayout.Width(360));
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"▲ n ({activeTile.allowedNorth.Count})", EditorStyles.boldLabel, GUILayout.Width(70));

            GUI.backgroundColor = isAct ? new Color(0.2f, 0.9f, 0.2f) : Color.white;
            if (GUILayout.Button(isAct ? "✔" : "Act", EditorStyles.miniButton, GUILayout.Width(35)))
            {
                activeDirection = 0;
                Repaint();
            }
            GUI.backgroundColor = Color.white;

            if (activeTile.allowedNorth.Count > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.35f, 0.35f);
                if (GUILayout.Button("🗑", EditorStyles.miniButton, GUILayout.Width(25)))
                {
                    int nId = activeTile.allowedNorth[Mathf.Clamp(previewNorthIdx, 0, activeTile.allowedNorth.Count - 1)];
                    Undo.RecordObject(database, "Quitar Vecino Norte");
                    database.RemoveNeighbor(activeTile.id, nId, Vector2Int.up, true);
                    ClampPreviewIndex(activeTile, 0);
                    SaveDatabase();
                    Repaint();
                }
                GUI.backgroundColor = Color.white;
            }

            if (GUILayout.Button(dirExpanded[0] ? "▲" : "▼", EditorStyles.miniButton, GUILayout.Width(24)))
            {
                dirExpanded[0] = !dirExpanded[0];
            }
            EditorGUILayout.EndHorizontal();

            if (dirExpanded[0])
            {
                DrawHorizontalSatelliteList(activeTile.id, activeTile.allowedNorth, Vector2Int.up, 0, ref previewNorthIdx);
            }

            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // SATÉLITE INFERIOR: Scroll de Sur (abajo)
        private void DrawBottomSatellite(WfcTileEntry activeTile)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            bool isAct = (activeDirection == 1);
            GUI.backgroundColor = isAct ? new Color(0.4f, 0.95f, 0.4f) : Color.white;
            EditorGUILayout.BeginVertical("box", GUILayout.Width(360));
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"▼ s ({activeTile.allowedSouth.Count})", EditorStyles.boldLabel, GUILayout.Width(70));

            GUI.backgroundColor = isAct ? new Color(0.2f, 0.9f, 0.2f) : Color.white;
            if (GUILayout.Button(isAct ? "✔" : "Act", EditorStyles.miniButton, GUILayout.Width(35)))
            {
                activeDirection = 1;
                Repaint();
            }
            GUI.backgroundColor = Color.white;

            if (activeTile.allowedSouth.Count > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.35f, 0.35f);
                if (GUILayout.Button("🗑", EditorStyles.miniButton, GUILayout.Width(25)))
                {
                    int nId = activeTile.allowedSouth[Mathf.Clamp(previewSouthIdx, 0, activeTile.allowedSouth.Count - 1)];
                    Undo.RecordObject(database, "Quitar Vecino Sur");
                    database.RemoveNeighbor(activeTile.id, nId, Vector2Int.down, true);
                    ClampPreviewIndex(activeTile, 1);
                    SaveDatabase();
                    Repaint();
                }
                GUI.backgroundColor = Color.white;
            }

            if (GUILayout.Button(dirExpanded[1] ? "▲" : "▼", EditorStyles.miniButton, GUILayout.Width(24)))
            {
                dirExpanded[1] = !dirExpanded[1];
            }
            EditorGUILayout.EndHorizontal();

            if (dirExpanded[1])
            {
                DrawHorizontalSatelliteList(activeTile.id, activeTile.allowedSouth, Vector2Int.down, 1, ref previewSouthIdx);
            }

            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // SATÉLITE IZQUIERDO: Scroll de Oeste (a la izquierda)
        private void DrawLeftSatellite(WfcTileEntry activeTile)
        {
            bool isAct = (activeDirection == 2);
            GUI.backgroundColor = isAct ? new Color(0.4f, 0.95f, 0.4f) : Color.white;
            EditorGUILayout.BeginVertical("box", GUILayout.Width(170), GUILayout.Height(204));
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"◄ o ({activeTile.allowedWest.Count})", EditorStyles.boldLabel, GUILayout.Width(65));
            if (GUILayout.Button(isAct ? "✔" : "Act", EditorStyles.miniButton, GUILayout.Width(28)))
            {
                activeDirection = 2;
                Repaint();
            }
            if (activeTile.allowedWest.Count > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.35f, 0.35f);
                if (GUILayout.Button("🗑", EditorStyles.miniButton, GUILayout.Width(24)))
                {
                    int nId = activeTile.allowedWest[Mathf.Clamp(previewWestIdx, 0, activeTile.allowedWest.Count - 1)];
                    Undo.RecordObject(database, "Quitar Vecino Oeste");
                    database.RemoveNeighbor(activeTile.id, nId, Vector2Int.left, true);
                    ClampPreviewIndex(activeTile, 2);
                    SaveDatabase();
                    Repaint();
                }
                GUI.backgroundColor = Color.white;
            }
            if (GUILayout.Button(dirExpanded[2] ? "▲" : "▼", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                dirExpanded[2] = !dirExpanded[2];
            }
            EditorGUILayout.EndHorizontal();

            if (dirExpanded[2])
            {
                DrawVerticalSatelliteList(activeTile.id, activeTile.allowedWest, Vector2Int.left, 2, ref previewWestIdx);
            }

            EditorGUILayout.EndVertical();
        }

        // SATÉLITE DERECHO: Scroll de Este (a la derecha)
        private void DrawRightSatellite(WfcTileEntry activeTile)
        {
            bool isAct = (activeDirection == 3);
            GUI.backgroundColor = isAct ? new Color(0.4f, 0.95f, 0.4f) : Color.white;
            EditorGUILayout.BeginVertical("box", GUILayout.Width(170), GUILayout.Height(204));
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"► e ({activeTile.allowedEast.Count})", EditorStyles.boldLabel, GUILayout.Width(65));
            if (GUILayout.Button(isAct ? "✔" : "Act", EditorStyles.miniButton, GUILayout.Width(28)))
            {
                activeDirection = 3;
                Repaint();
            }
            if (activeTile.allowedEast.Count > 0)
            {
                GUI.backgroundColor = new Color(1f, 0.35f, 0.35f);
                if (GUILayout.Button("🗑", EditorStyles.miniButton, GUILayout.Width(24)))
                {
                    int nId = activeTile.allowedEast[Mathf.Clamp(previewEastIdx, 0, activeTile.allowedEast.Count - 1)];
                    Undo.RecordObject(database, "Quitar Vecino Este");
                    database.RemoveNeighbor(activeTile.id, nId, Vector2Int.right, true);
                    ClampPreviewIndex(activeTile, 3);
                    SaveDatabase();
                    Repaint();
                }
                GUI.backgroundColor = Color.white;
            }
            if (GUILayout.Button(dirExpanded[3] ? "▲" : "▼", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                dirExpanded[3] = !dirExpanded[3];
            }
            EditorGUILayout.EndHorizontal();

            if (dirExpanded[3])
            {
                DrawVerticalSatelliteList(activeTile.id, activeTile.allowedEast, Vector2Int.right, 3, ref previewEastIdx);
            }

            EditorGUILayout.EndVertical();
        }

        // Dibuja el scroll horizontal de miniaturas para Norte y Sur
        private void DrawHorizontalSatelliteList(int tileId, List<int> list, Vector2Int dir, int dirIndex, ref int previewIdx)
        {
            if (list.Count == 0)
            {
                EditorGUILayout.LabelField("(Sin vecinos conectados)", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(40));
                return;
            }

            dirScrolls[dirIndex] = EditorGUILayout.BeginScrollView(dirScrolls[dirIndex], false, false, GUILayout.Height(50));
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < list.Count; i++)
            {
                int nId = list[i];
                var nTile = database.GetTileById(nId);
                if (nTile != null && nTile.sprite != null)
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(40));

                    Rect slotRect = GUILayoutUtility.GetRect(38, 38, GUILayout.Width(38), GUILayout.Height(38));
                    DrawSpriteDirect(slotRect, nTile.sprite);

                    // Si es el que está en el preview central, remarcar en amarillo
                    if (i == previewIdx)
                    {
                        Handles.DrawSolidRectangleWithOutline(slotRect, Color.clear, Color.yellow);
                    }

                    // Botón rojo [✕] en la esquina superior derecha (procesar PRIMERO para evitar intercepción)
                    Rect xBtn = new Rect(slotRect.xMax - 16, slotRect.yMin, 16, 16);
                    GUI.backgroundColor = new Color(1f, 0.2f, 0.2f, 0.95f);
                    if (GUI.Button(xBtn, "✕", EditorStyles.miniButton))
                    {
                        Undo.RecordObject(database, "Quitar Vecino");
                        database.RemoveNeighbor(tileId, nId, dir, true);
                        SaveDatabase();
                        Repaint();
                        GUI.backgroundColor = Color.white;
                        break;
                    }
                    GUI.backgroundColor = Color.white;

                    // Clic en el resto del slot para cambiar cuál se ve en el preview central
                    Rect clickBodyRect = new Rect(slotRect.x, slotRect.y, slotRect.width - 16, slotRect.height);
                    if (GUI.Button(clickBodyRect, GUIContent.none, GUIStyle.none))
                    {
                        previewIdx = i;
                        Repaint();
                    }

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        // Dibuja el scroll vertical de miniaturas para Oeste y Este
        private void DrawVerticalSatelliteList(int tileId, List<int> list, Vector2Int dir, int dirIndex, ref int previewIdx)
        {
            if (list.Count == 0)
            {
                EditorGUILayout.LabelField("(Sin vecinos)", EditorStyles.centeredGreyMiniLabel, GUILayout.Height(40));
                return;
            }

            dirScrolls[dirIndex] = EditorGUILayout.BeginScrollView(dirScrolls[dirIndex], false, false, GUILayout.Height(160));

            int itemsPerRow = 3;
            int count = 0;
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < list.Count; i++)
            {
                int nId = list[i];
                var nTile = database.GetTileById(nId);
                if (nTile != null && nTile.sprite != null)
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(40));

                    Rect slotRect = GUILayoutUtility.GetRect(38, 38, GUILayout.Width(38), GUILayout.Height(38));
                    DrawSpriteDirect(slotRect, nTile.sprite);

                    if (i == previewIdx)
                    {
                        Handles.DrawSolidRectangleWithOutline(slotRect, Color.clear, Color.yellow);
                    }

                    // Botón rojo [✕] en la esquina superior derecha (procesar PRIMERO)
                    Rect xBtn = new Rect(slotRect.xMax - 16, slotRect.yMin, 16, 16);
                    GUI.backgroundColor = new Color(1f, 0.2f, 0.2f, 0.95f);
                    if (GUI.Button(xBtn, "✕", EditorStyles.miniButton))
                    {
                        Undo.RecordObject(database, "Quitar Vecino");
                        database.RemoveNeighbor(tileId, nId, dir, true);
                        SaveDatabase();
                        Repaint();
                        GUI.backgroundColor = Color.white;
                        break;
                    }
                    GUI.backgroundColor = Color.white;

                    // Clic en el resto del slot para cambiar cuál se ve en el preview central
                    Rect clickBodyRect = new Rect(slotRect.x, slotRect.y, slotRect.width - 16, slotRect.height);
                    if (GUI.Button(clickBodyRect, GUIContent.none, GUIStyle.none))
                    {
                        previewIdx = i;
                        Repaint();
                    }

                    EditorGUILayout.EndVertical();

                    count++;
                    if (count % itemsPerRow == 0 && count < list.Count)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        // SELECTOR VISUAL CON LA FORMA FÍSICA REAL DEL ATLAS (12 columnas x 21 filas)
        private void DrawAtlasLayoutPicker(WfcTileEntry activeTile)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            bool isDirectionValid = (activeDirection >= 0 && activeDirection < 4);
            GUI.color = isDirectionValid ? new Color(0.3f, 1f, 0.3f) : Color.yellow;
            string dirLabel = isDirectionValid 
                ? $"● Conectando a: {dirNames[activeDirection]}" 
                : "⚠️ Ninguna dirección seleccionada en la cruz (haz clic en Norte, Sur, Este u Oeste arriba)";
            EditorGUILayout.LabelField(dirLabel, EditorStyles.boldLabel, GUILayout.Width(450));
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            // Barra de filtros limpia
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filtrar:", GUILayout.Width(50));
            bool isAll = (activeCategoryFilter == "Todos");
            GUI.backgroundColor = isAll ? Color.cyan : Color.white;
            if (GUILayout.Button("Todos", EditorStyles.miniButton, GUILayout.Width(60))) { activeCategoryFilter = "Todos"; Repaint(); }

            bool isVacios = (activeCategoryFilter == "🔘 Vacíos");
            GUI.backgroundColor = isVacios ? new Color(1f, 0.7f, 0.3f) : Color.white;
            if (GUILayout.Button("🔘 Sin Conexión", EditorStyles.miniButton, GUILayout.Width(110))) { activeCategoryFilter = "🔘 Vacíos"; Repaint(); }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("Clic: conectar/desconectar | Arrastrar: conectar múltiples al vuelo | Ctrl+Arrastrar: desconectar múltiples.", MessageType.None);

            scrollPicker = EditorGUILayout.BeginScrollView(scrollPicker, GUILayout.Height(240));

            Texture2D tex = database.atlasTexture;
            int tileSize = 16;
            int cols = tex.width / tileSize;
            int rows = tex.height / tileSize;

            Event e = Event.current;
            Vector2 mousePos = e.mousePosition;
            Vector2Int dir = GetVectorForDir(activeDirection);

            // Control de inicio/fin de arrastre en picker
            if (e.rawType == EventType.MouseUp)
            {
                isDraggingPicker = false;
                touchedPickerTilesInDrag.Clear();
            }

            if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 1))
            {
                isDraggingPicker = (e.button == 0);
                pickerDragDisconnectMode = (e.button == 1 || e.shift || e.control || e.command);
                touchedPickerTilesInDrag.Clear();
            }
            else if (e.type == EventType.MouseUp)
            {
                isDraggingPicker = false;
                touchedPickerTilesInDrag.Clear();
            }

            for (int r = 0; r < rows; r++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < cols; c++)
                {
                    float spriteY = tex.height - (r + 1) * tileSize;
                    float spriteX = c * tileSize;

                    var candidate = database.tiles.FirstOrDefault(t =>
                        Mathf.Approximately(t.rectInAtlas.x, spriteX) &&
                        Mathf.Approximately(t.rectInAtlas.y, spriteY));

                    Rect btnRect = GUILayoutUtility.GetRect(38, 38, GUILayout.Width(38), GUILayout.Height(38));

                    if (candidate != null && candidate.sprite != null)
                    {
                        bool isFilteredOut = (activeCategoryFilter == "🔘 Vacíos")
                            ? (candidate.allowedNorth.Count + candidate.allowedSouth.Count + candidate.allowedEast.Count + candidate.allowedWest.Count > 0)
                            : (activeCategoryFilter != "Todos" && candidate.category != activeCategoryFilter);
                        bool alreadyConnected = IsAlreadyConnected(activeTile, candidate.id, activeDirection);

                        if (!isFilteredOut && btnRect.Contains(mousePos))
                        {
                            bool isRightClick = (e.type == EventType.MouseDown && e.button == 1);
                            bool isLeftClick = (e.type == EventType.MouseDown && e.button == 0);

                            if (!isDirectionValid)
                            {
                                if (isLeftClick)
                                {
                                    selectedTileId = candidate.id;
                                    multiSelectedIds.Clear();
                                    multiSelectedIds.Add(candidate.id);
                                    e.Use();
                                    Repaint();
                                }
                                continue;
                            }

                            bool isDisconnect = (isRightClick || e.shift || e.control || e.command);

                            if (isLeftClick || isRightClick)
                            {
                                Undo.RecordObject(database, isDisconnect ? "Desconectar Vecino" : "Conectar Vecino");
                                if (isDisconnect)
                                {
                                    if (alreadyConnected)
                                    {
                                        database.RemoveNeighbor(activeTile.id, candidate.id, dir, true);
                                        ClampPreviewIndex(activeTile, activeDirection);
                                    }
                                }
                                else
                                {
                                    if (!alreadyConnected)
                                    {
                                        database.AddNeighbor(activeTile.id, candidate.id, dir, true);
                                    }
                                    SetPreviewToId(activeTile, activeDirection, candidate.id);
                                }

                                touchedPickerTilesInDrag.Add(candidate.id);
                                SaveDatabase();
                                e.Use();
                                Repaint();
                            }
                            else if (e.type == EventType.MouseDrag && isDraggingPicker && !touchedPickerTilesInDrag.Contains(candidate.id))
                            {
                                touchedPickerTilesInDrag.Add(candidate.id);
                                Undo.RecordObject(database, pickerDragDisconnectMode ? "Desconectar Vecinos" : "Conectar Vecinos");
                                if (pickerDragDisconnectMode)
                                {
                                    if (alreadyConnected)
                                    {
                                        database.RemoveNeighbor(activeTile.id, candidate.id, dir, true);
                                        ClampPreviewIndex(activeTile, activeDirection);
                                    }
                                }
                                else
                                {
                                    if (!alreadyConnected)
                                    {
                                        database.AddNeighbor(activeTile.id, candidate.id, dir, true);
                                    }
                                    SetPreviewToId(activeTile, activeDirection, candidate.id);
                                }
                                SaveDatabase();
                                e.Use();
                                Repaint();
                            }
                        }

                        float alpha = isFilteredOut ? 0.2f : 1.0f;
                        DrawSpriteDirectWithAlpha(btnRect, candidate.sprite, alpha);

                        if (alreadyConnected)
                        {
                            EditorGUI.DrawRect(btnRect, new Color(0f, 1f, 0f, 0.15f));
                            Handles.DrawSolidRectangleWithOutline(btnRect, Color.clear, new Color(0.2f, 1f, 0.2f, 0.95f));

                            Rect dotRect = new Rect(btnRect.xMax - 7, btnRect.yMin + 2, 5, 5);
                            EditorGUI.DrawRect(dotRect, Color.green);
                        }
                        else if (multiSelectedIds.Contains(candidate.id))
                        {
                            EditorGUI.DrawRect(btnRect, new Color(1f, 0.9f, 0f, 0.2f));
                            Handles.DrawSolidRectangleWithOutline(btnRect, Color.clear, Color.yellow);
                        }
                        else if (!isFilteredOut)
                        {
                            Handles.DrawSolidRectangleWithOutline(btnRect, Color.clear, new Color(0.25f, 0.25f, 0.25f, 0.35f));
                        }
                    }
                    else
                    {
                        EditorGUI.DrawRect(btnRect, new Color(0.12f, 0.12f, 0.12f, 0.2f));
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }


        private static Rect GetMarqueeRect(Vector2 p1, Vector2 p2)
        {
            return new Rect(
                Mathf.Min(p1.x, p2.x),
                Mathf.Min(p1.y, p2.y),
                Mathf.Max(1f, Mathf.Abs(p1.x - p2.x)),
                Mathf.Max(1f, Mathf.Abs(p1.y - p2.y))
            );
        }

        private void SelectContiguousTiles()
        {
            if (multiSelectedIds.Count == 0 && selectedTileId >= 0)
            {
                multiSelectedIds.Add(selectedTileId);
            }
            if (multiSelectedIds.Count == 0 || database == null || database.tiles == null) return;

            var firstTile = database.GetTileById(multiSelectedIds[0]);
            string targetCat = firstTile != null ? firstTile.category : null;

            Queue<WfcTileEntry> queue = new Queue<WfcTileEntry>();
            HashSet<int> visited = new HashSet<int>(multiSelectedIds);

            foreach (var id in multiSelectedIds)
            {
                var t = database.GetTileById(id);
                if (t != null) queue.Enqueue(t);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                Rect cr = current.rectInAtlas;

                foreach (var other in database.tiles)
                {
                    if (visited.Contains(other.id)) continue;
                    // Solo expandir si comparten la misma categoría (para no seleccionar todo el atlas)
                    if (!string.IsNullOrEmpty(targetCat) && other.category != targetCat) continue;

                    Rect or = other.rectInAtlas;
                    bool adjacentX = Mathf.Approximately(cr.y, or.y) && Mathf.Abs(cr.x - or.x) <= cr.width + 1.5f;
                    bool adjacentY = Mathf.Approximately(cr.x, or.x) && Mathf.Abs(cr.y - or.y) <= cr.height + 1.5f;

                    if (adjacentX || adjacentY)
                    {
                        visited.Add(other.id);
                        queue.Enqueue(other);
                    }
                }
            }

            multiSelectedIds = visited.ToList();
            Debug.Log($"<color=cyan>¡Selección expandida! Total: {multiSelectedIds.Count} tiles del módulo seleccionados.</color>");
        }

        private void SelectEmptyTiles()
        {
            if (database == null || database.tiles == null) return;
            multiSelectedIds = database.tiles
                .Where(t => t.allowedNorth.Count + t.allowedSouth.Count + t.allowedEast.Count + t.allowedWest.Count == 0)
                .Select(t => t.id)
                .ToList();

            if (multiSelectedIds.Count > 0)
            {
                selectedTileId = multiSelectedIds[0];
                Debug.Log($"<color=yellow><b>[WFC]</b> Se han seleccionado <b>{multiSelectedIds.Count}</b> tiles vacíos / sin conexión.</color>");
            }
            else
            {
                selectedTileId = -1;
                Debug.Log("<color=green><b>[WFC]</b> ¡No hay ningún tile vacío! Todos los tiles tienen conexiones configuradas.</color>");
            }
        }

        private static void DrawSpriteDirect(Rect rect, Sprite sprite)
        {
            DrawSpriteDirectWithAlpha(rect, sprite, 1.0f);
        }

        private static void DrawSpriteDirectWithAlpha(Rect rect, Sprite sprite, float alpha)
        {
            if (sprite == null || sprite.texture == null) return;

            Texture2D tex = sprite.texture;
            Rect r = sprite.rect;
            Rect uv = new Rect(r.x / tex.width, r.y / tex.height, r.width / tex.width, r.height / tex.height);

            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 0.6f * alpha));

            Color prevColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTextureWithTexCoords(rect, tex, uv);
            GUI.color = prevColor;
        }

        private Vector2Int GetVectorForDir(int dirIndex)
        {
            if (dirIndex == 0) return Vector2Int.up;
            if (dirIndex == 1) return Vector2Int.down;
            if (dirIndex == 2) return Vector2Int.left;
            if (dirIndex == 3) return Vector2Int.right;
            return Vector2Int.zero;
        }

        private bool IsAlreadyConnected(WfcTileEntry tile, int neighborId, int dirIndex)
        {
            if (tile == null || dirIndex < 0 || dirIndex > 3) return false;
            if (dirIndex == 0) return tile.allowedNorth != null && tile.allowedNorth.Contains(neighborId);
            if (dirIndex == 1) return tile.allowedSouth != null && tile.allowedSouth.Contains(neighborId);
            if (dirIndex == 2) return tile.allowedWest != null && tile.allowedWest.Contains(neighborId);
            if (dirIndex == 3) return tile.allowedEast != null && tile.allowedEast.Contains(neighborId);
            return false;
        }

        private void RenameSelectedTiles(string baseName)
        {
            if (string.IsNullOrEmpty(baseName)) return;
            var selectedTiles = multiSelectedIds.Select(id => database.GetTileById(id)).Where(t => t != null).ToList();
            if (selectedTiles.Count == 0 && selectedTileId >= 0)
            {
                var t = database.GetTileById(selectedTileId);
                if (t != null) selectedTiles.Add(t);
            }
            if (selectedTiles.Count == 0) return;

            if (selectedTiles.Count == 9)
            {
                var xs = selectedTiles.Select(t => t.rectInAtlas.x).Distinct().OrderBy(x => x).ToList();
                var ys = selectedTiles.Select(t => t.rectInAtlas.y).Distinct().OrderByDescending(y => y).ToList();
                if (xs.Count == 3 && ys.Count == 3)
                {
                    string[,] suffixes = new string[3, 3]
                    {
                        { "TL", "TC", "TR" },
                        { "ML", "MC", "MR" },
                        { "BL", "BC", "BR" }
                    };

                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            float targetX = xs[c];
                            float targetY = ys[r];
                            var tile = selectedTiles.FirstOrDefault(t => Mathf.Approximately(t.rectInAtlas.x, targetX) && Mathf.Approximately(t.rectInAtlas.y, targetY));
                            if (tile != null)
                            {
                                tile.spriteName = $"{baseName}_{suffixes[r, c]}";
                            }
                        }
                    }
                    EditorUtility.SetDirty(database);
                    Debug.Log($"<color=cyan><b>[Renombrar]</b> 9 tiles renombrados como {baseName}_TL...BR</color>");
                    return;
                }
            }

            for (int i = 0; i < selectedTiles.Count; i++)
            {
                selectedTiles[i].spriteName = $"{baseName}_{i}";
            }
            EditorUtility.SetDirty(database);
            Debug.Log($"<color=cyan><b>[Renombrar]</b> {selectedTiles.Count} tiles renombrados con prefijo '{baseName}'.</color>");
        }

        private void ClearSelection()
        {
            if (multiSelectedIds != null) multiSelectedIds.Clear();
            selectedTileId = -1;
            GUI.FocusControl(null);
            Repaint();
        }

        private void ClearSelectionConnections()
        {
            var targetIds = new HashSet<int>();
            if (multiSelectedIds != null && multiSelectedIds.Count > 0)
            {
                foreach (var id in multiSelectedIds) targetIds.Add(id);
            }
            else if (selectedTileId >= 0)
            {
                targetIds.Add(selectedTileId);
            }

            if (targetIds.Count == 0) return;
            Undo.RecordObject(database, "Reset Enlaces Seleccionados");

            foreach (var t in database.tiles)
            {
                if (targetIds.Contains(t.id))
                {
                    t.allowedNorth.Clear();
                    t.allowedSouth.Clear();
                    t.allowedEast.Clear();
                    t.allowedWest.Clear();
                }
                else
                {
                    t.allowedNorth.RemoveAll(id => targetIds.Contains(id));
                    t.allowedSouth.RemoveAll(id => targetIds.Contains(id));
                    t.allowedEast.RemoveAll(id => targetIds.Contains(id));
                    t.allowedWest.RemoveAll(id => targetIds.Contains(id));
                }
            }

            SaveDatabase();
            RunAudit();
            Repaint();
            ShowNotification(new GUIContent($"🧹 Conexiones limpiadas ({targetIds.Count} tiles)"));
            Debug.Log($"<color=orange><b>[WFC Reset]</b> Enlaces reseteados para {targetIds.Count} tiles seleccionados.</color>");
        }

        private void ClearAllConnections()
        {
            if (database == null || database.tiles == null) return;
            Undo.RecordObject(database, "Limpiar Toda la Base de Datos WFC");

            foreach (var t in database.tiles)
            {
                t.allowedNorth.Clear();
                t.allowedSouth.Clear();
                t.allowedEast.Clear();
                t.allowedWest.Clear();
            }

            SaveDatabase();
            RunAudit();
            Repaint();
            ShowNotification(new GUIContent("🗑️ Base de datos WFC reseteada por completo"));
            Debug.Log("<color=orange><b>[WFC Database]</b> ¡Se han borrado todas las conexiones de todos los tiles! Base de datos limpia desde cero.</color>");
        }

        private void ClearSingleTileConnections(int tileId)
        {
            var tile = database.GetTileById(tileId);
            if (tile == null) return;
            Undo.RecordObject(database, "Vaciar Conexiones de Tile");

            foreach (var other in database.tiles)
            {
                other.allowedNorth.Remove(tileId);
                other.allowedSouth.Remove(tileId);
                other.allowedEast.Remove(tileId);
                other.allowedWest.Remove(tileId);
            }
            tile.allowedNorth.Clear();
            tile.allowedSouth.Clear();
            tile.allowedEast.Clear();
            tile.allowedWest.Clear();

            SaveDatabase();
            RunAudit();
            Repaint();
            ShowNotification(new GUIContent($"🧹 Conexiones limpiadas: Tile #{tile.id}"));
            Debug.Log($"<color=orange><b>[WFC Database]</b> Se han vaciado todas las conexiones del tile #{tile.id} ({tile.spriteName}).</color>");
        }

        #region Métodos Inteligentes de Detección Geométrica y Auto-Selección

        private List<WfcTileEntry> Find3x3BlockContainingTile(WfcTileEntry activeT)
        {
            if (activeT == null || database == null || database.tiles == null) return null;

            float w = activeT.rectInAtlas.width;
            float h = activeT.rectInAtlas.height;
            if (w <= 0 || h <= 0) return null;

            // Presets conocidos de bioma para garantizar 100% de precisión instantánea
            int[][] presets = new int[][]
            {
                new int[] { 15, 16, 17, 27, 28, 29, 39, 40, 41 }, // Ventanal Tierra (caminos_15..41)
                new int[] { 20, 21, 22, 32, 33, 34, 44, 45, 46 }, // Ventanal Piedra (caminos_20..46)
                new int[] { 47, 48, 49, 59, 60, 61, 71, 72, 73 }, // Lago Agua Clara (caminos_47..73)
                new int[] { 50, 51, 52, 62, 63, 64, 74, 75, 76 }, // Lago Agua Turbia (caminos_50..76)
                new int[] { 56, 57, 58, 67, 68, 69, 83, 84, 85 }, // Bosque (caminos_56..85)
                new int[] { 108, 109, 110, 117, 118, 119, 128, 129, 130 } // Iglesia (caminos_108..130)
            };

            foreach (var preset in presets)
            {
                if (preset.Contains(activeT.id))
                {
                    var list = preset.Select(id => database.GetTileById(id)).Where(t => t != null).ToList();
                    if (list.Count == 9) return list;
                }
            }

            // Detección geométrica general: Probar los 9 posibles orígenes (activeT en cualquier posición del 3x3)
            List<List<WfcTileEntry>> candidates = new List<List<WfcTileEntry>>();
            List<float> scores = new List<float>();

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    float originX = activeT.rectInAtlas.x - c * w;
                    float originY = activeT.rectInAtlas.y + r * h;

                    var gridTiles = new List<WfcTileEntry>();
                    bool allFound = true;

                    for (int gr = 0; gr < 3; gr++)
                    {
                        for (int gc = 0; gc < 3; gc++)
                        {
                            float tx = originX + gc * w;
                            float ty = originY - gr * h;
                            var found = database.tiles.FirstOrDefault(t =>
                                Mathf.Abs(t.rectInAtlas.x - tx) < 0.5f &&
                                Mathf.Abs(t.rectInAtlas.y - ty) < 0.5f);
                            if (found == null)
                            {
                                allFound = false;
                                break;
                            }
                            gridTiles.Add(found);
                        }
                        if (!allFound) break;
                    }

                    if (allFound && gridTiles.Count == 9)
                    {
                        float score = 100f;
                        if (gridTiles.All(t => t.category == activeT.category)) score += 50f;

                        int r0_step = gridTiles[1].id - gridTiles[0].id;
                        int r1_step = gridTiles[4].id - gridTiles[3].id;
                        int r2_step = gridTiles[7].id - gridTiles[6].id;
                        int stride1 = gridTiles[3].id - gridTiles[0].id;
                        int stride2 = gridTiles[6].id - gridTiles[3].id;

                        if (r0_step == 1 && r1_step == 1 && r2_step == 1) score += 50f;
                        if (stride1 > 0 && stride1 == stride2) score += 100f; // Paso uniforme entre filas

                        if (c == 1 && r == 1) score += 10f; // Preferencia centro

                        candidates.Add(gridTiles);
                        scores.Add(score);
                    }
                }
            }

            if (candidates.Count > 0)
            {
                int bestIdx = 0;
                float maxScore = scores[0];
                for (int i = 1; i < scores.Count; i++)
                {
                    if (scores[i] > maxScore)
                    {
                        maxScore = scores[i];
                        bestIdx = i;
                    }
                }
                return candidates[bestIdx];
            }

            return null;
        }

        private List<WfcTileEntry> Find2x2BlockContainingTile(WfcTileEntry activeT)
        {
            if (activeT == null || database == null || database.tiles == null) return null;
            float w = activeT.rectInAtlas.width;
            float h = activeT.rectInAtlas.height;
            if (w <= 0 || h <= 0) return null;

            for (int r = 0; r < 2; r++)
            {
                for (int c = 0; c < 2; c++)
                {
                    float originX = activeT.rectInAtlas.x - c * w;
                    float originY = activeT.rectInAtlas.y + r * h;

                    var gridTiles = new List<WfcTileEntry>();
                    bool allFound = true;

                    for (int gr = 0; gr < 2; gr++)
                    {
                        for (int gc = 0; gc < 2; gc++)
                        {
                            float tx = originX + gc * w;
                            float ty = originY - gr * h;
                            var found = database.tiles.FirstOrDefault(t =>
                                Mathf.Abs(t.rectInAtlas.x - tx) < 0.5f &&
                                Mathf.Abs(t.rectInAtlas.y - ty) < 0.5f);
                            if (found == null)
                            {
                                allFound = false;
                                break;
                            }
                            gridTiles.Add(found);
                        }
                        if (!allFound) break;
                    }

                    if (allFound && gridTiles.Count == 4)
                    {
                        return gridTiles;
                    }
                }
            }
            return null;
        }

        private List<WfcTileEntry> FindHorizontalStripContainingTile(WfcTileEntry activeT)
        {
            if (activeT == null || database == null || database.tiles == null) return null;
            float y = activeT.rectInAtlas.y;
            float w = activeT.rectInAtlas.width;

            var rowTiles = database.tiles
                .Where(t => Mathf.Abs(t.rectInAtlas.y - y) < 0.5f)
                .OrderBy(t => t.rectInAtlas.x)
                .ToList();

            if (rowTiles.Count == 0) return null;

            int activeIdx = rowTiles.FindIndex(t => t.id == activeT.id);
            if (activeIdx < 0) return null;

            int left = activeIdx;
            while (left > 0 && Mathf.Abs((rowTiles[left].rectInAtlas.x - rowTiles[left - 1].rectInAtlas.x) - w) < 0.5f)
                left--;

            int right = activeIdx;
            while (right < rowTiles.Count - 1 && Mathf.Abs((rowTiles[right + 1].rectInAtlas.x - rowTiles[right].rectInAtlas.x) - w) < 0.5f)
                right++;

            var strip = rowTiles.GetRange(left, right - left + 1);
            return strip.Count >= 2 ? strip : null;
        }

        private List<WfcTileEntry> FindVerticalStripContainingTile(WfcTileEntry activeT)
        {
            if (activeT == null || database == null || database.tiles == null) return null;
            float x = activeT.rectInAtlas.x;
            float h = activeT.rectInAtlas.height;

            var colTiles = database.tiles
                .Where(t => Mathf.Abs(t.rectInAtlas.x - x) < 0.5f)
                .OrderByDescending(t => t.rectInAtlas.y) // De arriba hacia abajo
                .ToList();

            if (colTiles.Count == 0) return null;

            int activeIdx = colTiles.FindIndex(t => t.id == activeT.id);
            if (activeIdx < 0) return null;

            int top = activeIdx;
            while (top > 0 && Mathf.Abs((colTiles[top - 1].rectInAtlas.y - colTiles[top].rectInAtlas.y) - h) < 0.5f)
                top--;

            int bot = activeIdx;
            while (bot < colTiles.Count - 1 && Mathf.Abs((colTiles[bot].rectInAtlas.y - colTiles[bot + 1].rectInAtlas.y) - h) < 0.5f)
                bot++;

            var strip = colTiles.GetRange(top, bot - top + 1);
            return strip.Count >= 2 ? strip : null;
        }

        private readonly int[] dirtRoadTileIds = new int[] { 4, 5, 6, 14, 15, 16, 17, 26, 27, 28, 29, 38, 39, 40, 41 };
        private readonly int[] stoneRoadTileIds = new int[] { 8, 9, 10, 19, 20, 21, 22, 31, 32, 33, 34, 43, 44, 45, 46 };

        private List<WfcTileEntry> FindRoadSetContainingTile(WfcTileEntry activeT)
        {
            if (database == null || database.tiles == null || database.tiles.Count == 0) return null;

            WfcTileEntry FindT(int x, int y, string nameFallback = null)
            {
                var found = database.tiles.FirstOrDefault(t => Mathf.Abs(t.rectInAtlas.x - x) < 0.5f && Mathf.Abs(t.rectInAtlas.y - y) < 0.5f);
                if (found != null) return found;
                if (!string.IsNullOrEmpty(nameFallback))
                {
                    string numStr = nameFallback.Replace("caminos_", "");
                    var byName = database.tiles.FirstOrDefault(t =>
                        t.spriteName == nameFallback ||
                        t.spriteName == nameFallback + "_tile" ||
                        t.spriteName.EndsWith("_" + numStr) ||
                        t.spriteName.EndsWith("_" + numStr + "_tile") ||
                        (int.TryParse(numStr, out int num) && t.id == num));
                    if (byName != null) return byName;
                }
                return null;
            }

            var dirtList = new List<WfcTileEntry>
            {
                FindT(64,  304, "caminos_15"), // Curva TL (caminos_15)
                FindT(80,  304, "caminos_16"), // T Arriba (caminos_16)
                FindT(96,  304, "caminos_17"), // Curva TR (caminos_17)
                FindT(64,  288, "caminos_27"), // T Izq (caminos_27)
                FindT(80,  288, "caminos_28"), // Cruz 4 vías (caminos_28)
                FindT(96,  288, "caminos_29"), // T Der (caminos_29)
                FindT(64,  272, "caminos_39"), // Curva BL (caminos_39)
                FindT(80,  272, "caminos_40"), // T Abajo (caminos_40)
                FindT(96,  272, "caminos_41"), // Curva BR (caminos_41)
                FindT(80,  320, "caminos_5"),  // Recta Horiz (caminos_5)
                FindT(48,  288, "caminos_26"), // Recta Vert (caminos_26)
                FindT(64,  320, "caminos_4"),  // Final Izq (caminos_4)
                FindT(96,  320, "caminos_6"),  // Final Der (caminos_6)
                FindT(48,  304, "caminos_14"), // Final Arriba (caminos_14)
                FindT(48,  272, "caminos_38")  // Final Abajo (caminos_38)
            }.Where(t => t != null).ToList();

            var stoneList = new List<WfcTileEntry>
            {
                FindT(144, 304, "caminos_20"), // Curva TL Piedra (caminos_20)
                FindT(160, 304, "caminos_21"), // T Arriba Piedra (caminos_21)
                FindT(176, 304, "caminos_22"), // Curva TR Piedra (caminos_22)
                FindT(144, 288, "caminos_32"), // T Izq Piedra (caminos_32)
                FindT(160, 288, "caminos_33"), // Cruz 4 vías Piedra (caminos_33)
                FindT(176, 288, "caminos_34"), // T Der Piedra (caminos_34)
                FindT(144, 272, "caminos_44"), // Curva BL Piedra (caminos_44)
                FindT(160, 272, "caminos_45"), // T Abajo Piedra (caminos_45)
                FindT(176, 272, "caminos_46"), // Curva BR Piedra (caminos_46)
                FindT(160, 320, "caminos_9"),  // Recta Horiz Piedra (caminos_9)
                FindT(128, 288, "caminos_31"), // Recta Vert Piedra (caminos_31)
                FindT(144, 320, "caminos_8"),  // Transición Horiz Tierra->Piedra (caminos_8)
                FindT(176, 320, "caminos_10"), // Transición Horiz Piedra->Tierra (caminos_10)
                FindT(128, 304, "caminos_19"), // Transición Vert Tierra->Piedra (caminos_19)
                FindT(128, 272, "caminos_43")  // Transición Vert Piedra->Tierra (caminos_43)
            }.Where(t => t != null).ToList();

            if (activeT != null)
            {
                if (dirtList.Any(t => t.id == activeT.id || t.spriteName == activeT.spriteName)) return dirtList;
                if (stoneList.Any(t => t.id == activeT.id || t.spriteName == activeT.spriteName)) return stoneList;
            }

            return dirtList.Concat(stoneList).Distinct().ToList();
        }

        private void AutoSelectRoads()
        {
            var activeT = database.GetTileById(selectedTileId);
            var roadBlock = FindRoadSetContainingTile(activeT);
            if (roadBlock != null && roadBlock.Count > 0)
            {
                multiSelectedIds = roadBlock.Select(t => t.id).ToList();
                if (selectedTileId < 0 || !multiSelectedIds.Contains(selectedTileId))
                    selectedTileId = multiSelectedIds[0];
                Repaint();
                Debug.Log($"<color=cyan><b>[Selección de Caminos]</b> {roadBlock.Count} tiles de camino seleccionados ({string.Join(", ", roadBlock.Select(t => t.spriteName))}).</color>");
            }
            else
            {
                EditorUtility.DisplayDialog("Selección de Caminos", "No se encontraron tiles de camino en la base de datos.", "OK");
            }
        }

        private List<WfcTileEntry> FindContiguousNxMBlock(WfcTileEntry activeT)
        {
            if (activeT == null || database == null || database.tiles == null) return null;
            float w = activeT.rectInAtlas.width;
            float h = activeT.rectInAtlas.height;
            if (w <= 0 || h <= 0) return null;

            // Detección por Flood-Fill geométrico contiguo
            var visited = new HashSet<WfcTileEntry>();
            var queue = new Queue<WfcTileEntry>();
            queue.Enqueue(activeT);
            visited.Add(activeT);

            string targetCategory = activeT.category;

            while (queue.Count > 0)
            {
                var curr = queue.Dequeue();
                Vector2 pos = new Vector2(curr.rectInAtlas.x, curr.rectInAtlas.y);

                Vector2[] neighborPositions = new Vector2[]
                {
                    new Vector2(pos.x, pos.y + h),     // Arriba
                    new Vector2(pos.x, pos.y - h),     // Abajo
                    new Vector2(pos.x - w, pos.y),     // Izquierda
                    new Vector2(pos.x + w, pos.y)      // Derecha
                };

                foreach (var np in neighborPositions)
                {
                    var neighbor = database.tiles.FirstOrDefault(t =>
                        Mathf.Abs(t.rectInAtlas.x - np.x) < 0.5f &&
                        Mathf.Abs(t.rectInAtlas.y - np.y) < 0.5f);

                    if (neighbor != null && !visited.Contains(neighbor))
                    {
                        // Si pertenece a la misma categoría (o ambas son estructuras)
                        bool sameCat = (neighbor.category == targetCategory);
                        if (sameCat)
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return visited.ToList();
        }

        private void AutoSelectNxM()
        {
            var activeT = database.GetTileById(selectedTileId);
            if (activeT == null)
            {
                EditorUtility.DisplayDialog("Selección NxM", "Haz clic primero en un tile de la estructura en el atlas.", "OK");
                return;
            }

            var block = FindContiguousNxMBlock(activeT);
            if (block != null && block.Count > 0)
            {
                multiSelectedIds = block.Select(t => t.id).ToList();
                Repaint();
                Debug.Log($"<color=cyan><b>[Selección NxM]</b> Bloque NxM de {block.Count} tiles autoseleccionado.</color>");
            }
            else
            {
                EditorUtility.DisplayDialog("Selección NxM", $"No se pudo encontrar un bloque contiguo alrededor del tile #{activeT.id}.", "OK");
            }
        }

        private void AutoSelect3x3()
        {
            var activeT = database.GetTileById(selectedTileId);
            if (activeT == null)
            {
                EditorUtility.DisplayDialog("Selección 3x3", "Haz clic primero en un tile del bloque 3x3 en el atlas.", "OK");
                return;
            }

            var block = Find3x3BlockContainingTile(activeT);
            if (block != null && block.Count == 9)
            {
                multiSelectedIds = block.Select(t => t.id).ToList();
                selectedTileId = block[4].id; // Tile centro como activo
                Repaint();
                Debug.Log($"<color=cyan><b>[Selección 3x3]</b> Bloque 3x3 autoseleccionado ({string.Join(", ", block.Select(t => t.spriteName))}).</color>");
            }
            else
            {
                EditorUtility.DisplayDialog("Selección 3x3", $"No se pudo encontrar un bloque 3x3 contiguo alrededor del tile #{activeT.id} ({activeT.spriteName}).", "OK");
            }
        }

        private void AutoSelectHorizontalStrip()
        {
            var activeT = database.GetTileById(selectedTileId);
            if (activeT == null) return;
            var strip = FindHorizontalStripContainingTile(activeT);
            if (strip != null && strip.Count >= 2)
            {
                multiSelectedIds = strip.Select(t => t.id).ToList();
                Repaint();
                Debug.Log($"<color=cyan><b>[Selección Tira Horiz]</b> Tira horizontal de {strip.Count} tiles autoseleccionada.</color>");
            }
        }

        private void AutoSelectVerticalStrip()
        {
            var activeT = database.GetTileById(selectedTileId);
            if (activeT == null) return;
            var strip = FindVerticalStripContainingTile(activeT);
            if (strip != null && strip.Count >= 2)
            {
                multiSelectedIds = strip.Select(t => t.id).ToList();
                Repaint();
                Debug.Log($"<color=cyan><b>[Selección Tira Vert]</b> Tira vertical de {strip.Count} tiles autoseleccionada.</color>");
            }
        }

        #endregion

        #region Plantillas de Vinculación Automática (AutoWire)

        private void AutoWire13TileWaterAutotile(string borderCategory)
        {
            if (database == null || database.tiles == null || database.tiles.Count == 0) return;

            string targetCategory = "Agua_Clara";
            var selected = (multiSelectedIds != null) ? multiSelectedIds.Select(id => database.GetTileById(id)).Where(t => t != null).ToList() : new List<WfcTileEntry>();

            List<WfcTileEntry> outer9 = new List<WfcTileEntry>();
            List<WfcTileEntry> inner4 = new List<WfcTileEntry>();

            if (selected.Count == 13)
            {
                outer9 = selected.Take(9).ToList();
                inner4 = selected.Skip(9).Take(4).ToList();
            }
            else if (selected.Count == 9)
            {
                outer9 = selected;
            }
            else
            {
                // Cargar preset estándar de caminos.png
                string[] defOuter = { "caminos_47", "caminos_48", "caminos_49", "caminos_59", "caminos_60", "caminos_61", "caminos_71", "caminos_72", "caminos_73" };
                string[] defInner = { "caminos_53", "caminos_54", "caminos_65", "caminos_66" };

                foreach (var name in defOuter)
                {
                    var t = database.tiles.FirstOrDefault(x => x.spriteName == name || (x.tile != null && x.tile.name.StartsWith(name)));
                    if (t != null) outer9.Add(t);
                }
                foreach (var name in defInner)
                {
                    var t = database.tiles.FirstOrDefault(x => x.spriteName == name || (x.tile != null && x.tile.name.StartsWith(name)));
                    if (t != null) inner4.Add(t);
                }
            }

            if (outer9.Count < 9)
            {
                EditorUtility.DisplayDialog("Autotile Lago", "Selecciona los 9 tiles del 3x3 en el atlas (o los 13 tiles) antes de pulsar el botón.", "OK");
                return;
            }

            Undo.RecordObject(database, "Configurar Autotile de Agua 13 Tiles");

            // Ordenar los 9 tiles de arriba hacia abajo (Y descendente) y de izquierda a derecha (X ascendente)
            outer9 = outer9.OrderByDescending(t => t.rectInAtlas.y).ThenBy(t => t.rectInAtlas.x).ToList();

            string[] roles3x3 = {
                "Outer_TL", "Top",    "Outer_TR",
                "Left",     "Center", "Right",
                "Outer_BL", "Bottom", "Outer_BR"
            };

            for (int i = 0; i < 9; i++)
            {
                outer9[i].category = targetCategory;
                outer9[i].role = roles3x3[i];
                outer9[i].weight = (roles3x3[i] == "Center") ? 2.0f : 0.8f;
            }

            if (inner4.Count == 4)
            {
                inner4 = inner4.OrderByDescending(t => t.rectInAtlas.y).ThenBy(t => t.rectInAtlas.x).ToList();
                string[] roles2x2 = {
                    "Inner_TL", "Inner_TR",
                    "Inner_BL", "Inner_BR"
                };
                for (int i = 0; i < 4; i++)
                {
                    inner4[i].category = targetCategory;
                    inner4[i].role = roles2x2[i];
                    inner4[i].weight = 0.8f;
                }
            }

            // Conectar vecinos en el 3x3
            var block = new WfcTileEntry[3, 3];
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    block[c, r] = outer9[r * 3 + c];
                }
            }

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    var t = block[c, r];
                    if (r > 0) database.AddNeighbor(t.id, block[c, r - 1].id, Vector2Int.up, true);
                    if (r < 2) database.AddNeighbor(t.id, block[c, r + 1].id, Vector2Int.down, true);
                    if (c > 0) database.AddNeighbor(t.id, block[c - 1, r].id, Vector2Int.left, true);
                    if (c < 2) database.AddNeighbor(t.id, block[c + 1, r].id, Vector2Int.right, true);

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

            // Enlazar bordes hacia Prado
            var grassTiles = database.GetTilesByCategory("Prado");
            foreach (var g in grassTiles)
            {
                for (int c = 0; c < 3; c++) database.AddNeighbor(block[c, 0].id, g.id, Vector2Int.up, true);
                for (int c = 0; c < 3; c++) database.AddNeighbor(block[c, 2].id, g.id, Vector2Int.down, true);
                for (int r = 0; r < 3; r++) database.AddNeighbor(block[0, r].id, g.id, Vector2Int.left, true);
                for (int r = 0; r < 3; r++) database.AddNeighbor(block[2, r].id, g.id, Vector2Int.right, true);
            }

            database.EnsureCategories();
            SaveDatabase();
            RunAudit();
            Repaint();

            string innerMsg = inner4.Count == 4 ? " + 4 esquinas interiores (2x2)" : "";
            EditorUtility.DisplayDialog("Autotile Lago Configurado", $"¡Agua Clara configurada con éxito!\n\n• 9 tiles exteriores (3x3){innerMsg}\n• Roles de orillas y esquinas asignados\n• Conexiones con 'Prado' listas.", "Genial");
            Debug.Log($"<color=green><b>[Autotile Lago]</b> Agua Clara configurada con éxito ({outer9.Count + inner4.Count} tiles con roles y vecinos).</color>");
        }

        private void AutoWireAs3x3Template(string borderCategory)
        {
            var selectedTiles = multiSelectedIds.Select(id => database.GetTileById(id)).Where(t => t != null).ToList();
            
            // Si no hay 9 seleccionados, auto-detectar el bloque 3x3 exacto del tile activo
            if (selectedTiles.Count != 9)

            {
                var activeT = selectedTileId >= 0 ? database.GetTileById(selectedTileId) : (selectedTiles.Count > 0 ? selectedTiles[0] : null);
                if (activeT != null)
                {
                    var detected = Find3x3BlockContainingTile(activeT);
                    if (detected != null && detected.Count == 9)
                    {
                        selectedTiles = detected;
                        multiSelectedIds = detected.Select(t => t.id).ToList();
                        Repaint();
                    }
                }
            }

            if (selectedTiles.Count != 9)
            {
                EditorUtility.DisplayDialog("Plantilla 3x3", "Deben seleccionarse exactamente 9 tiles (3x3) o haz clic en cualquier tile de un bloque 3x3.", "OK");
                return;
            }

            Undo.RecordObject(database, "Vincular Plantilla 3x3 Autotile");

            bool hasBorder = !string.IsNullOrEmpty(borderCategory) && borderCategory != "[Sin Conexión Exterior / Solo Interno]" && borderCategory != "[ninguno]" && borderCategory != "Solo Interno";
            var borderTiles = hasBorder ? database.GetTilesByCategory(borderCategory) : new List<WfcTileEntry>();

            // Obtener columnas X ordenadas de izquierda a derecha (ascendente)
            var xs = selectedTiles.Select(t => t.rectInAtlas.x).Distinct().OrderBy(x => x).ToList();
            // Obtener filas Y ordenadas de arriba hacia abajo (descendente: Y mayor en textura = fila superior)
            var ys = selectedTiles.Select(t => t.rectInAtlas.y).Distinct().OrderByDescending(y => y).ToList();

            if (xs.Count != 3 || ys.Count != 3)
            {
                EditorUtility.DisplayDialog("Plantilla 3x3", "Los 9 tiles seleccionados deben formar una cuadrícula regular de 3 columnas por 3 filas.", "OK");
                return;
            }

            int internalCount = 0;
            int borderCount = 0;

            // Matriz 2D para acceso rápido a los tiles seleccionados
            WfcTileEntry[,] grid = new WfcTileEntry[3, 3];
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    float targetX = xs[c];
                    float targetY = ys[r];
                    grid[c, r] = selectedTiles.FirstOrDefault(t =>
                        Mathf.Abs(t.rectInAtlas.x - targetX) < 0.5f &&
                        Mathf.Abs(t.rectInAtlas.y - targetY) < 0.5f);
                }
            }

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    var tile = grid[c, r];
                    if (tile == null) continue;

                    bool isTop = (r == 0);
                    bool isBottom = (r == 2);
                    bool isLeft = (c == 0);
                    bool isRight = (c == 2);
                    bool isCenter = (!isTop && !isBottom && !isLeft && !isRight);

                    // 1. Conexiones internas directas con sus vecinos de la cuadrícula
                    if (r > 0 && grid[c, r - 1] != null)
                    {
                        database.AddNeighbor(tile.id, grid[c, r - 1].id, Vector2Int.up, true);
                        internalCount++;
                    }
                    if (r < 2 && grid[c, r + 1] != null)
                    {
                        database.AddNeighbor(tile.id, grid[c, r + 1].id, Vector2Int.down, true);
                        internalCount++;
                    }
                    if (c > 0 && grid[c - 1, r] != null)
                    {
                        database.AddNeighbor(tile.id, grid[c - 1, r].id, Vector2Int.left, true);
                        internalCount++;
                    }
                    if (c < 2 && grid[c + 1, r] != null)
                    {
                        database.AddNeighbor(tile.id, grid[c + 1, r].id, Vector2Int.right, true);
                        internalCount++;
                    }

                    // 2. Entrelazado y Continuidad Seamless (Auto-repetición según su rol en el autotile)
                    if (isCenter)
                    {
                        database.AddNeighbor(tile.id, tile.id, Vector2Int.up, true);
                        database.AddNeighbor(tile.id, tile.id, Vector2Int.down, true);
                        database.AddNeighbor(tile.id, tile.id, Vector2Int.left, true);
                        database.AddNeighbor(tile.id, tile.id, Vector2Int.right, true);
                        internalCount += 4;
                    }
                    else if (isTop && !isLeft && !isRight)
                    {
                        database.AddNeighbor(tile.id, tile.id, Vector2Int.left, true);
                        database.AddNeighbor(tile.id, tile.id, Vector2Int.right, true);
                        internalCount += 2;
                    }
                    else if (isBottom && !isLeft && !isRight)
                    {
                        database.AddNeighbor(tile.id, tile.id, Vector2Int.left, true);
                        database.AddNeighbor(tile.id, tile.id, Vector2Int.right, true);
                        internalCount += 2;
                    }
                    else if (isLeft && !isTop && !isBottom)
                    {
                        database.AddNeighbor(tile.id, tile.id, Vector2Int.up, true);
                        database.AddNeighbor(tile.id, tile.id, Vector2Int.down, true);
                        internalCount += 2;
                    }
                    else if (isRight && !isTop && !isBottom)
                    {
                        database.AddNeighbor(tile.id, tile.id, Vector2Int.up, true);
                        database.AddNeighbor(tile.id, tile.id, Vector2Int.down, true);
                        internalCount += 2;
                    }

                    // 3. Conexiones Perimetrales con el Módulo Exterior (si está activo)
                    if (hasBorder)
                    {
                        if (isTop)
                        {
                            foreach (var bt in borderTiles) { database.AddNeighbor(tile.id, bt.id, Vector2Int.up, true); borderCount++; }
                        }
                        if (isBottom)
                        {
                            foreach (var bt in borderTiles) { database.AddNeighbor(tile.id, bt.id, Vector2Int.down, true); borderCount++; }
                        }
                        if (isLeft)
                        {
                            foreach (var bt in borderTiles) { database.AddNeighbor(tile.id, bt.id, Vector2Int.left, true); borderCount++; }
                        }
                        if (isRight)
                        {
                            foreach (var bt in borderTiles) { database.AddNeighbor(tile.id, bt.id, Vector2Int.right, true); borderCount++; }
                        }
                    }
                }
            }

            database.EnsureCategories();
            SaveDatabase();
            RunAudit();
            Repaint();
            string extMsg = hasBorder ? $" y <b>{borderCount}</b> enlaces con '{borderCategory}'" : " (sin enlaces exteriores)";
            Debug.Log($"<color=green><b>[Plantilla 3x3 Autotile]</b> ¡Bloque 3x3 ({selectedTiles.Count} tiles) vinculado! <b>{internalCount}</b> enlaces internos{extMsg}.</color>");
        }

        private void AutoWireRoad3x3(string borderCategory)
        {
            var selectedTiles = multiSelectedIds.Select(id => database.GetTileById(id)).Where(t => t != null).ToList();
            if (selectedTiles.Count != 9)
            {
                var activeT = selectedTileId >= 0 ? database.GetTileById(selectedTileId) : (selectedTiles.Count > 0 ? selectedTiles[0] : null);
                if (activeT != null)
                {
                    var detected = Find3x3BlockContainingTile(activeT);
                    if (detected != null && detected.Count == 9)
                    {
                        selectedTiles = detected;
                        multiSelectedIds = detected.Select(t => t.id).ToList();
                        Repaint();
                    }
                }
            }

            if (selectedTiles.Count != 9)
            {
                EditorUtility.DisplayDialog("Camino 3x3", "Deben seleccionarse exactamente 9 tiles (3x3 del ventanal de caminos) o haz clic en cualquier tile del ventanal.", "OK");
                return;
            }

            Undo.RecordObject(database, "Vincular Ventanal de Camino 3x3");

            var xs = selectedTiles.Select(t => t.rectInAtlas.x).Distinct().OrderBy(x => x).ToList();
            var ys = selectedTiles.Select(t => t.rectInAtlas.y).Distinct().OrderByDescending(y => y).ToList();

            if (xs.Count != 3 || ys.Count != 3)
            {
                EditorUtility.DisplayDialog("Camino 3x3", "Los 9 tiles seleccionados deben formar una cuadrícula 3x3.", "OK");
                return;
            }

            WfcTileEntry[,] grid = new WfcTileEntry[3, 3];
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    float targetX = xs[c];
                    float targetY = ys[r];
                    grid[c, r] = selectedTiles.FirstOrDefault(t =>
                        Mathf.Abs(t.rectInAtlas.x - targetX) < 0.5f &&
                        Mathf.Abs(t.rectInAtlas.y - targetY) < 0.5f);
                }
            }

            var tl = grid[0, 0];
            var top_t = grid[1, 0];
            var tr = grid[2, 0];
            var left_t = grid[0, 1];
            var cross = grid[1, 1];
            var right_t = grid[2, 1];
            var bl = grid[0, 2];
            var bot_t = grid[1, 2];
            var br = grid[2, 2];

            int conns = 0;
            void Connect(WfcTileEntry a, WfcTileEntry b, Vector2Int dir)
            {
                if (a == null || b == null) return;
                database.AddNeighbor(a.id, b.id, dir, true);
                conns++;
            }

            // Top row
            Connect(tl, top_t, Vector2Int.right);
            Connect(tl, left_t, Vector2Int.down);
            Connect(top_t, tr, Vector2Int.right);
            Connect(top_t, cross, Vector2Int.down);
            Connect(tr, right_t, Vector2Int.down);

            // Mid row
            Connect(left_t, bl, Vector2Int.down);
            Connect(left_t, cross, Vector2Int.right);
            Connect(cross, right_t, Vector2Int.right);
            Connect(cross, bot_t, Vector2Int.down);
            Connect(right_t, br, Vector2Int.down);

            // Bot row
            Connect(bl, bot_t, Vector2Int.right);
            Connect(bot_t, br, Vector2Int.right);

            bool hasBorder = !string.IsNullOrEmpty(borderCategory) && borderCategory != "[Sin Conexión Exterior / Solo Interno]" && borderCategory != "[ninguno]" && borderCategory != "Solo Interno";
            var borderTiles = hasBorder ? database.GetTilesByCategory(borderCategory) : new List<WfcTileEntry>();
            if (hasBorder)
            {
                foreach (var b in borderTiles)
                {
                    database.AddNeighbor(tl.id, b.id, Vector2Int.up, true);
                    database.AddNeighbor(tl.id, b.id, Vector2Int.left, true);
                    database.AddNeighbor(top_t.id, b.id, Vector2Int.up, true);
                    database.AddNeighbor(tr.id, b.id, Vector2Int.up, true);
                    database.AddNeighbor(tr.id, b.id, Vector2Int.right, true);
                    database.AddNeighbor(left_t.id, b.id, Vector2Int.left, true);
                    database.AddNeighbor(right_t.id, b.id, Vector2Int.right, true);
                    database.AddNeighbor(bl.id, b.id, Vector2Int.down, true);
                    database.AddNeighbor(bl.id, b.id, Vector2Int.left, true);
                    database.AddNeighbor(bot_t.id, b.id, Vector2Int.down, true);
                    database.AddNeighbor(br.id, b.id, Vector2Int.down, true);
                    database.AddNeighbor(br.id, b.id, Vector2Int.right, true);
                }
            }

            database.EnsureCategories();
            SaveDatabase();
            RunAudit();
            Repaint();
            Debug.Log($"<color=green><b>[Camino 3x3]</b> Ventanal de caminos 3x3 configurado con éxito ({conns} enlaces internos).</color>");
        }

        private void AutoWireAs2x2Template(string borderCategory)
        {
            var selectedTiles = multiSelectedIds.Select(id => database.GetTileById(id)).Where(t => t != null).ToList();
            if (selectedTiles.Count != 4)
            {
                var activeT = selectedTileId >= 0 ? database.GetTileById(selectedTileId) : (selectedTiles.Count > 0 ? selectedTiles[0] : null);
                if (activeT != null)
                {
                    var detected = Find2x2BlockContainingTile(activeT);
                    if (detected != null && detected.Count == 4)
                    {
                        selectedTiles = detected;
                        multiSelectedIds = detected.Select(t => t.id).ToList();
                        Repaint();
                    }
                }
            }

            if (selectedTiles.Count != 4)
            {
                EditorUtility.DisplayDialog("Plantilla 2x2", "Deben seleccionarse 4 tiles (2x2) o haz clic en cualquier tile del bloque 2x2.", "OK");
                return;
            }

            bool hasBorder = !string.IsNullOrEmpty(borderCategory) && borderCategory != "[Sin Conexión Exterior / Solo Interno]";
            var borderTiles = hasBorder ? database.GetTilesByCategory(borderCategory) : new List<WfcTileEntry>();

            var xs = selectedTiles.Select(t => t.rectInAtlas.x).Distinct().OrderBy(x => x).ToList();
            var ys = selectedTiles.Select(t => t.rectInAtlas.y).Distinct().OrderByDescending(y => y).ToList();

            if (xs.Count < 2 || ys.Count < 2)
            {
                EditorUtility.DisplayDialog("Plantilla 2x2", "Los 4 tiles seleccionados deben formar una cuadrícula 2x2.", "OK");
                return;
            }

            WfcTileEntry[,] grid = new WfcTileEntry[2, 2];
            for (int r = 0; r < 2; r++)
            {
                for (int c = 0; c < 2; c++)
                {
                    float targetX = xs[c];
                    float targetY = ys[r];
                    grid[c, r] = selectedTiles.FirstOrDefault(t =>
                        Mathf.Abs(t.rectInAtlas.x - targetX) < 0.5f &&
                        Mathf.Abs(t.rectInAtlas.y - targetY) < 0.5f);
                }
            }

            Undo.RecordObject(database, "Vincular Plantilla 2x2");
            int internalCount = 0;
            int borderCount = 0;

            var tl = grid[0, 0]; // Top-Left
            var tr = grid[1, 0]; // Top-Right
            var bl = grid[0, 1]; // Bottom-Left
            var br = grid[1, 1]; // Bottom-Right

            if (tl != null && tr != null && bl != null && br != null)
            {
                database.AddNeighbor(tl.id, tr.id, Vector2Int.right, true);
                database.AddNeighbor(tl.id, bl.id, Vector2Int.down, true);
                database.AddNeighbor(tr.id, br.id, Vector2Int.down, true);
                database.AddNeighbor(bl.id, br.id, Vector2Int.right, true);
                internalCount += 4;

                if (hasBorder)
                {
                    foreach (var bt in borderTiles)
                    {
                        database.AddNeighbor(tl.id, bt.id, Vector2Int.up, true);
                        database.AddNeighbor(tl.id, bt.id, Vector2Int.left, true);
                        database.AddNeighbor(tr.id, bt.id, Vector2Int.up, true);
                        database.AddNeighbor(tr.id, bt.id, Vector2Int.right, true);
                        database.AddNeighbor(bl.id, bt.id, Vector2Int.down, true);
                        database.AddNeighbor(bl.id, bt.id, Vector2Int.left, true);
                        database.AddNeighbor(br.id, bt.id, Vector2Int.down, true);
                        database.AddNeighbor(br.id, bt.id, Vector2Int.right, true);
                        borderCount += 8;
                    }
                }
            }

            database.EnsureCategories();
            SaveDatabase();
            RunAudit();
            Repaint();
            string extMsg2 = hasBorder ? $" y <b>{borderCount}</b> de borde con '{borderCategory}'" : " (sin borde exterior)";
            Debug.Log($"<color=green><b>[Plantilla 2x2 Circular]</b> ¡Bloque 2x2 ({selectedTiles.Count} tiles) vinculado! <b>{internalCount}</b> internas{extMsg2}.</color>");
        }

        private void AutoWireAs2x2HoleTemplate(string surroundCategory)
        {
            var selectedTiles = multiSelectedIds.Select(id => database.GetTileById(id)).Where(t => t != null).ToList();
            if (selectedTiles.Count != 4)
            {
                var activeT = selectedTileId >= 0 ? database.GetTileById(selectedTileId) : (selectedTiles.Count > 0 ? selectedTiles[0] : null);
                if (activeT != null)
                {
                    var detected = Find2x2BlockContainingTile(activeT);
                    if (detected != null && detected.Count == 4)
                    {
                        selectedTiles = detected;
                        multiSelectedIds = detected.Select(t => t.id).ToList();
                        Repaint();
                    }
                }
            }

            if (selectedTiles.Count != 4)
            {
                EditorUtility.DisplayDialog("Hueco / Claro 2x2", "Deben seleccionarse 4 tiles (esquinas interiores del hueco 2x2).", "OK");
                return;
            }

            var xs = selectedTiles.Select(t => t.rectInAtlas.x).Distinct().OrderBy(x => x).ToList();
            var ys = selectedTiles.Select(t => t.rectInAtlas.y).Distinct().OrderByDescending(y => y).ToList();

            if (xs.Count < 2 || ys.Count < 2)
            {
                EditorUtility.DisplayDialog("Hueco 2x2", "Los 4 tiles seleccionados deben formar una cuadrícula 2x2.", "OK");
                return;
            }

            WfcTileEntry[,] grid = new WfcTileEntry[2, 2];
            for (int r = 0; r < 2; r++)
            {
                for (int c = 0; c < 2; c++)
                {
                    float targetX = xs[c];
                    float targetY = ys[r];
                    grid[c, r] = selectedTiles.FirstOrDefault(t =>
                        Mathf.Abs(t.rectInAtlas.x - targetX) < 0.5f &&
                        Mathf.Abs(t.rectInAtlas.y - targetY) < 0.5f);
                }
            }

            Undo.RecordObject(database, "Vincular Hueco/Claro 2x2");
            int internalCount = 0;
            int borderCount = 0;

            var tl = grid[0, 0];
            var tr = grid[1, 0];
            var bl = grid[0, 1];
            var br = grid[1, 1];

            string outerCat = string.IsNullOrEmpty(surroundCategory) || surroundCategory == "[ninguno]" ? "Bosque" : surroundCategory;
            var outerTiles = database.GetTilesByCategory(outerCat);
            var grassTiles = database.GetTilesByCategory("Prado");

            if (tl != null && tr != null && bl != null && br != null)
            {
                database.AddNeighbor(tl.id, tr.id, Vector2Int.right, true);
                database.AddNeighbor(tl.id, bl.id, Vector2Int.down, true);
                database.AddNeighbor(tr.id, br.id, Vector2Int.down, true);
                database.AddNeighbor(bl.id, br.id, Vector2Int.right, true);
                internalCount += 4;

                foreach (var g in grassTiles)
                {
                    database.AddNeighbor(tl.id, g.id, Vector2Int.down, true);
                    database.AddNeighbor(tl.id, g.id, Vector2Int.right, true);
                    database.AddNeighbor(tr.id, g.id, Vector2Int.down, true);
                    database.AddNeighbor(tr.id, g.id, Vector2Int.left, true);
                    database.AddNeighbor(bl.id, g.id, Vector2Int.up, true);
                    database.AddNeighbor(bl.id, g.id, Vector2Int.right, true);
                    database.AddNeighbor(br.id, g.id, Vector2Int.up, true);
                    database.AddNeighbor(br.id, g.id, Vector2Int.left, true);
                }

                foreach (var o in outerTiles)
                {
                    database.AddNeighbor(tl.id, o.id, Vector2Int.up, true);
                    database.AddNeighbor(tl.id, o.id, Vector2Int.left, true);
                    database.AddNeighbor(tr.id, o.id, Vector2Int.up, true);
                    database.AddNeighbor(tr.id, o.id, Vector2Int.right, true);
                    database.AddNeighbor(bl.id, o.id, Vector2Int.down, true);
                    database.AddNeighbor(bl.id, o.id, Vector2Int.left, true);
                    database.AddNeighbor(br.id, o.id, Vector2Int.down, true);
                    database.AddNeighbor(br.id, o.id, Vector2Int.right, true);
                    borderCount += 8;
                }
            }

            database.EnsureCategories();
            SaveDatabase();
            RunAudit();
            Repaint();
            Debug.Log($"<color=green><b>[Hueco/Claro 2x2]</b> ¡Hueco de bosque vinculado con éxito! Exterior: <b>{outerCat}</b>, Interior: <b>Prado</b>.</color>");
        }



        private Dictionary<WfcTileEntry, RoadTileSocket> GetRoadSocketsDictionary(List<WfcTileEntry> roadTiles = null)
        {
            var sockets = new Dictionary<WfcTileEntry, RoadTileSocket>();
            if (database == null || database.tiles == null) return sockets;

            WfcTileEntry FindT(int x, int y, string nameFallback = null)
            {
                var found = database.tiles.FirstOrDefault(t => Mathf.Abs(t.rectInAtlas.x - x) < 0.5f && Mathf.Abs(t.rectInAtlas.y - y) < 0.5f);
                if (found != null) return found;
                if (!string.IsNullOrEmpty(nameFallback))
                {
                    string numStr = nameFallback.Replace("caminos_", "");
                    var byName = database.tiles.FirstOrDefault(t =>
                        t.spriteName == nameFallback ||
                        t.spriteName == nameFallback + "_tile" ||
                        t.spriteName.EndsWith("_" + numStr) ||
                        t.spriteName.EndsWith("_" + numStr + "_tile") ||
                        (int.TryParse(numStr, out int num) && t.id == num));
                    if (byName != null) return byName;
                }
                return null;
            }

            void Register(WfcTileEntry t, RoadSocketType n, RoadSocketType s, RoadSocketType w, RoadSocketType e, string role = null)
            {
                if (t != null)
                {
                    sockets[t] = new RoadTileSocket(n, s, w, e);
                    if (!string.IsNullOrEmpty(role)) t.role = role;
                }
            }

            // 15 TILES DE TIERRA
            Register(FindT(64,  304, "caminos_15"), RoadSocketType.None,  RoadSocketType.Dirt,  RoadSocketType.None,  RoadSocketType.Dirt,  "Road_Corner_TL"); // Curva TL
            Register(FindT(80,  304, "caminos_16"), RoadSocketType.None,  RoadSocketType.Dirt,  RoadSocketType.Dirt,  RoadSocketType.Dirt,  "Road_T_Up");      // T Arriba
            Register(FindT(96,  304, "caminos_17"), RoadSocketType.None,  RoadSocketType.Dirt,  RoadSocketType.Dirt,  RoadSocketType.None,  "Road_Corner_TR"); // Curva TR
            Register(FindT(64,  288, "caminos_27"), RoadSocketType.Dirt,  RoadSocketType.Dirt,  RoadSocketType.None,  RoadSocketType.Dirt,  "Road_T_Left");    // T Izq
            Register(FindT(80,  288, "caminos_28"), RoadSocketType.Dirt,  RoadSocketType.Dirt,  RoadSocketType.Dirt,  RoadSocketType.Dirt,  "Road_Cross");     // Cruz 4 vías
            Register(FindT(96,  288, "caminos_29"), RoadSocketType.Dirt,  RoadSocketType.Dirt,  RoadSocketType.Dirt,  RoadSocketType.None,  "Road_T_Right");   // T Der
            Register(FindT(64,  272, "caminos_39"), RoadSocketType.Dirt,  RoadSocketType.None,  RoadSocketType.None,  RoadSocketType.Dirt,  "Road_Corner_BL"); // Curva BL
            Register(FindT(80,  272, "caminos_40"), RoadSocketType.Dirt,  RoadSocketType.None,  RoadSocketType.Dirt,  RoadSocketType.Dirt,  "Road_T_Down");    // T Abajo
            Register(FindT(96,  272, "caminos_41"), RoadSocketType.Dirt,  RoadSocketType.None,  RoadSocketType.Dirt,  RoadSocketType.None,  "Road_Corner_BR"); // Curva BR
            Register(FindT(80,  320, "caminos_5"),  RoadSocketType.None,  RoadSocketType.None,  RoadSocketType.Dirt,  RoadSocketType.Dirt,  "Road_Straight_H");// Recta Horiz
            Register(FindT(48,  288, "caminos_26"), RoadSocketType.Dirt,  RoadSocketType.Dirt,  RoadSocketType.None,  RoadSocketType.None,  "Road_Straight_V");// Recta Vert
            Register(FindT(64,  320, "caminos_4"),  RoadSocketType.None,  RoadSocketType.None,  RoadSocketType.None,  RoadSocketType.Dirt,  "Road_Cap_W");     // Final Izq (abre al Este)
            Register(FindT(96,  320, "caminos_6"),  RoadSocketType.None,  RoadSocketType.None,  RoadSocketType.Dirt,  RoadSocketType.None,  "Road_Cap_E");     // Final Der (abre al Oeste)
            Register(FindT(48,  304, "caminos_14"), RoadSocketType.None,  RoadSocketType.Dirt,  RoadSocketType.None,  RoadSocketType.None,  "Road_Cap_N");     // Final Arriba (abre al Sur)
            Register(FindT(48,  272, "caminos_38"), RoadSocketType.Dirt,  RoadSocketType.None,  RoadSocketType.None,  RoadSocketType.None,  "Road_Cap_S");     // Final Abajo (abre al Norte)

            // 15 TILES DE PIEDRA Y TRANSICIONES
            Register(FindT(144, 304, "caminos_20"), RoadSocketType.None,  RoadSocketType.Stone, RoadSocketType.None,  RoadSocketType.Stone, "Road_Corner_TL"); // Curva TL Piedra
            Register(FindT(160, 304, "caminos_21"), RoadSocketType.None,  RoadSocketType.Stone, RoadSocketType.Stone, RoadSocketType.Stone, "Road_T_Up");      // T Arriba Piedra
            Register(FindT(176, 304, "caminos_22"), RoadSocketType.None,  RoadSocketType.Stone, RoadSocketType.Stone, RoadSocketType.None,  "Road_Corner_TR"); // Curva TR Piedra
            Register(FindT(144, 288, "caminos_32"), RoadSocketType.Stone, RoadSocketType.Stone, RoadSocketType.None,  RoadSocketType.Stone, "Road_T_Left");    // T Izq Piedra
            Register(FindT(160, 288, "caminos_33"), RoadSocketType.Stone, RoadSocketType.Stone, RoadSocketType.Stone, RoadSocketType.Stone, "Road_Cross");     // Cruz 4 vías Piedra
            Register(FindT(176, 288, "caminos_34"), RoadSocketType.Stone, RoadSocketType.Stone, RoadSocketType.Stone, RoadSocketType.None,  "Road_T_Right");   // T Der Piedra
            Register(FindT(144, 272, "caminos_44"), RoadSocketType.Stone, RoadSocketType.None,  RoadSocketType.None,  RoadSocketType.Stone, "Road_Corner_BL"); // Curva BL Piedra
            Register(FindT(160, 272, "caminos_45"), RoadSocketType.Stone, RoadSocketType.None,  RoadSocketType.Stone, RoadSocketType.Stone, "Road_T_Down");    // T Abajo Piedra
            Register(FindT(176, 272, "caminos_46"), RoadSocketType.Stone, RoadSocketType.None,  RoadSocketType.Stone, RoadSocketType.None,  "Road_Corner_BR"); // Curva BR Piedra
            Register(FindT(160, 320, "caminos_9"),  RoadSocketType.None,  RoadSocketType.None,  RoadSocketType.Stone, RoadSocketType.Stone, "Road_Straight_H");// Recta Horiz Piedra
            Register(FindT(128, 288, "caminos_31"), RoadSocketType.Stone, RoadSocketType.Stone, RoadSocketType.None,  RoadSocketType.None,  "Road_Straight_V");// Recta Vert Piedra
            Register(FindT(144, 320, "caminos_8"),  RoadSocketType.None,  RoadSocketType.None,  RoadSocketType.Dirt,  RoadSocketType.Stone, "Road_Trans_W");    // Transición Horiz Tierra->Piedra
            Register(FindT(176, 320, "caminos_10"), RoadSocketType.None,  RoadSocketType.None,  RoadSocketType.Stone, RoadSocketType.Dirt,  "Road_Trans_E");    // Transición Horiz Piedra->Tierra
            Register(FindT(128, 304, "caminos_19"), RoadSocketType.Dirt,  RoadSocketType.Stone, RoadSocketType.None,  RoadSocketType.None,  "Road_Trans_N");    // Transición Vert Tierra->Piedra
            Register(FindT(128, 272, "caminos_43"), RoadSocketType.Stone, RoadSocketType.Dirt,  RoadSocketType.None,  RoadSocketType.None,  "Road_Trans_S");    // Transición Vert Piedra->Tierra

            return sockets;
        }

        private void AutoWireRoadNetwork(string borderCategory)
        {
            if (database == null || database.tiles == null || database.tiles.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No hay tiles cargados en la base de datos.", "OK");
                return;
            }

            Undo.RecordObject(database, "Vincular Red de Caminos WFC");

            var sockets = GetRoadSocketsDictionary();
            var allRoadTiles = sockets.Keys.ToList();
            var roadTileIds = new HashSet<int>(allRoadTiles.Select(t => t.id));

            // Si hay tiles seleccionados y son parte de caminos (por ejemplo los 15 de tierra o los 15 de piedra)
            var selectedRoads = multiSelectedIds.Where(id => roadTileIds.Contains(id)).Select(id => database.GetTileById(id)).Where(t => t != null).ToList();
            List<WfcTileEntry> targetTiles = (selectedRoads.Count > 0) ? selectedRoads : allRoadTiles;

            // Categorización y pesos
            foreach (var t in allRoadTiles)
            {
                bool isStone = stoneRoadTileIds.Contains(t.id) || (t.spriteName != null && t.spriteName.Contains("caminos_") && int.TryParse(t.spriteName.Replace("caminos_", "").Replace("_tile", ""), out int num) && stoneRoadTileIds.Contains(num));
                if (isStone)
                {
                    if (string.IsNullOrEmpty(t.category) || t.category == "Prado" || t.category == "Decoracion") t.category = "Camino_Piedra";
                }
                else
                {
                    if (string.IsNullOrEmpty(t.category) || t.category == "Prado" || t.category == "Decoracion") t.category = "Camino";
                }
            }

            foreach (var t in targetTiles)
            {
                t.allowedNorth.Clear();
                t.allowedSouth.Clear();
                t.allowedEast.Clear();
                t.allowedWest.Clear();
            }

            bool hasBorder = !string.IsNullOrEmpty(borderCategory) && borderCategory != "[Sin Conexión Exterior / Solo Interno]" && borderCategory != "[ninguno]" && borderCategory != "Solo Interno";
            var borderTiles = hasBorder
                ? database.GetTilesByCategory(borderCategory).Where(bt => !roadTileIds.Contains(bt.id) && !bt.category.ToLower().Contains("camino")).ToList()
                : new List<WfcTileEntry>();

            int roadConns = 0;
            int borderConns = 0;

            void Connect(WfcTileEntry from, WfcTileEntry to, Vector2Int dir)
            {
                if (from == null || to == null) return;
                database.AddNeighbor(from.id, to.id, dir, true);
                roadConns++;
            }

            void ConnectBorder(WfcTileEntry t, Vector2Int dir)
            {
                if (!hasBorder || t == null || borderTiles.Count == 0) return;
                foreach (var g in borderTiles)
                {
                    database.AddNeighbor(t.id, g.id, dir, true);
                    borderConns++;
                }
            }

            // Cableado estricto por socket matching
            foreach (var tA in targetTiles)
            {
                if (!sockets.TryGetValue(tA, out var sockA)) continue;

                foreach (var tB in allRoadTiles)
                {
                    if (!sockets.TryGetValue(tB, out var sockB)) continue;

                    // Norte de A conecta al Sur de B
                    if (sockA.north != RoadSocketType.None && sockA.north == sockB.south)
                        Connect(tA, tB, Vector2Int.up);

                    // Sur de A conecta al Norte de B
                    if (sockA.south != RoadSocketType.None && sockA.south == sockB.north)
                        Connect(tA, tB, Vector2Int.down);

                    // Oeste de A conecta al Este de B
                    if (sockA.west != RoadSocketType.None && sockA.west == sockB.east)
                        Connect(tA, tB, Vector2Int.left);

                    // Este de A conecta al Oeste de B
                    if (sockA.east != RoadSocketType.None && sockA.east == sockB.west)
                        Connect(tA, tB, Vector2Int.right);
                }

                // Conexión con terreno exterior en lados donde no hay camino (socket == None)
                if (hasBorder)
                {
                    if (sockA.north == RoadSocketType.None) ConnectBorder(tA, Vector2Int.up);
                    if (sockA.south == RoadSocketType.None) ConnectBorder(tA, Vector2Int.down);
                    if (sockA.west == RoadSocketType.None) ConnectBorder(tA, Vector2Int.left);
                    if (sockA.east == RoadSocketType.None) ConnectBorder(tA, Vector2Int.right);
                }
            }

            database.EnsureCategories();
            SaveDatabase();
            RunAudit();
            Repaint();

            string borderInfo = hasBorder ? $" y <b>{borderConns}</b> con '{borderCategory}'" : " (sin conexiones exteriores / caminos brutos)";
            Debug.Log($"<color=green><b>[Red de Caminos]</b> ¡{targetTiles.Count} tiles de camino configurados con topología exacta! <b>{roadConns}</b> enlaces entre caminos{borderInfo}.</color>");
            EditorUtility.DisplayDialog("Red de Caminos Configurada", 
                $"¡Se han vinculado {targetTiles.Count} tiles de camino con topología matemática modular!\n\n" +
                $"• Enlaces internos entre caminos: {roadConns}\n" +
                (hasBorder ? $"• Conexiones exteriores con '{borderCategory}': {borderConns}\n" : "• Conexiones exteriores: Ninguna (Caminos Brutos / Solo Interno)\n") +
                $"• Cobertura: Curvas, Cruces, Rectas, Tes, Finales y Transiciones.", "¡Genial!");
        }

        private void AutoWireStructureWithBorder(string borderCategory)
        {
            var selectedTiles = multiSelectedIds.Select(id => database.GetTileById(id)).Where(t => t != null).ToList();
            if (selectedTiles.Count == 0 && selectedTileId >= 0)
            {
                var activeT = database.GetTileById(selectedTileId);
                if (activeT != null)
                {
                    var block = FindContiguousNxMBlock(activeT);
                    if (block != null && block.Count > 0)
                    {
                        selectedTiles = block;
                        multiSelectedIds = block.Select(x => x.id).ToList();
                        Repaint();
                    }
                    else
                    {
                        selectedTiles.Add(activeT);
                    }
                }
            }

            if (selectedTiles.Count == 0)
            {
                EditorUtility.DisplayDialog("Estructura NxM", "Selecciona primero los tiles de la estructura en el atlas (arrastrando con el ratón o haciendo clic).", "OK");
                return;
            }

            bool hasBorder = !string.IsNullOrEmpty(borderCategory) && borderCategory != "[Sin Conexión Exterior / Solo Interno]" && borderCategory != "[ninguno]" && borderCategory != "Solo Interno";
            var borderTiles = hasBorder ? database.GetTilesByCategory(borderCategory) : new List<WfcTileEntry>();

            Undo.RecordObject(database, $"Vincular Estructura NxM ({selectedTiles.Count} tiles)");

            int internalCount = 0;
            int borderCount = 0;

            foreach (var a in selectedTiles)
            {
                bool hasNorth = false;
                bool hasSouth = false;
                bool hasEast  = false;
                bool hasWest  = false;

                foreach (var b in selectedTiles)
                {
                    if (a == b) continue;

                    // B está Arriba de A en el Atlas (Y de B es mayor por 1 tile)
                    if (Mathf.Abs(a.rectInAtlas.x - b.rectInAtlas.x) < 1.0f &&
                        Mathf.Abs((a.rectInAtlas.y + a.rectInAtlas.height) - b.rectInAtlas.y) < 1.0f)
                    {
                        database.AddNeighbor(a.id, b.id, Vector2Int.up, true);
                        hasNorth = true;
                        internalCount++;
                    }

                    // B está Abajo de A en el Atlas
                    if (Mathf.Abs(a.rectInAtlas.x - b.rectInAtlas.x) < 1.0f &&
                        Mathf.Abs((b.rectInAtlas.y + b.rectInAtlas.height) - a.rectInAtlas.y) < 1.0f)
                    {
                        hasSouth = true;
                    }

                    // B está a la Derecha de A en el Atlas
                    if (Mathf.Abs(a.rectInAtlas.y - b.rectInAtlas.y) < 1.0f &&
                        Mathf.Abs((a.rectInAtlas.x + a.rectInAtlas.width) - b.rectInAtlas.x) < 1.0f)
                    {
                        database.AddNeighbor(a.id, b.id, Vector2Int.right, true);
                        hasEast = true;
                        internalCount++;
                    }

                    // B está a la Izquierda de A en el Atlas
                    if (Mathf.Abs(a.rectInAtlas.y - b.rectInAtlas.y) < 1.0f &&
                        Mathf.Abs((b.rectInAtlas.x + b.rectInAtlas.width) - a.rectInAtlas.x) < 1.0f)
                    {
                        hasWest = true;
                    }
                }

                // Conectar bordes expuestos con el terreno exterior si está activo
                if (hasBorder)
                {
                    if (!hasNorth)
                    {
                        foreach (var bt in borderTiles) { database.AddNeighbor(a.id, bt.id, Vector2Int.up, true); borderCount++; }
                    }
                    if (!hasSouth)
                    {
                        foreach (var bt in borderTiles) { database.AddNeighbor(a.id, bt.id, Vector2Int.down, true); borderCount++; }
                    }
                    if (!hasWest)
                    {
                        foreach (var bt in borderTiles) { database.AddNeighbor(a.id, bt.id, Vector2Int.left, true); borderCount++; }
                    }
                    if (!hasEast)
                    {
                        foreach (var bt in borderTiles) { database.AddNeighbor(a.id, bt.id, Vector2Int.right, true); borderCount++; }
                    }
                }
            }

            database.EnsureCategories();
            SaveDatabase();
            RunAudit();
            Repaint();
            string extMsg = hasBorder ? $" y <b>{borderCount}</b> de borde con '{borderCategory}'" : " (sin borde exterior / prefab limpio)";
            Debug.Log($"<color=green><b>[Bloque NxM]</b> ¡Estructura de {selectedTiles.Count} tiles vinculada! <b>{internalCount}</b> conexiones internas{extMsg}.</color>");
            EditorUtility.DisplayDialog("Estructura NxM Vinculada", 
                $"¡Estructura de {selectedTiles.Count} tiles vinculada con éxito!\n\n" +
                $"• Conexiones internas: {internalCount}\n" +
                (hasBorder ? $"• Conexiones de borde con '{borderCategory}': {borderCount}\n" : "• Conexiones de borde: Ninguna (Sin conexiones exteriores / Prefab interno)\n"), "OK");
        }

        private void AutoWireSelectionAsVerticalLine(string borderCategory)
        {
            var selectedTiles = multiSelectedIds.Select(id => database.GetTileById(id)).Where(t => t != null).ToList();
            
            // Si solo hay 1 o 0 seleccionados, auto-detectar la tira vertical contigua
            if (selectedTiles.Count < 2)
            {
                var activeT = selectedTileId >= 0 ? database.GetTileById(selectedTileId) : (selectedTiles.Count > 0 ? selectedTiles[0] : null);
                if (activeT != null)
                {
                    var detected = FindVerticalStripContainingTile(activeT);
                    if (detected != null && detected.Count >= 2)
                    {
                        selectedTiles = detected;
                        multiSelectedIds = detected.Select(t => t.id).ToList();
                        Repaint();
                    }
                }
            }

            if (selectedTiles.Count < 2)
            {
                EditorUtility.DisplayDialog("Línea Vertical 1xN", "Selecciona al menos 2 tiles que formen una columna vertical (o haz clic en cualquier tile de la tira vertical).", "OK");
                return;
            }

            // Validación de seguridad: Asegurarse de que no estamos mezclando múltiples columnas (p. ej. si había un 3x3 seleccionado)
            var distinctXs = selectedTiles.Select(t => t.rectInAtlas.x).Distinct().ToList();
            if (distinctXs.Count > 1)
            {
                var activeT = database.GetTileById(selectedTileId);
                float targetX = activeT != null ? activeT.rectInAtlas.x : distinctXs[0];
                selectedTiles = selectedTiles.Where(t => Mathf.Abs(t.rectInAtlas.x - targetX) < 0.5f).ToList();
                multiSelectedIds = selectedTiles.Select(t => t.id).ToList();
                Repaint();

                if (selectedTiles.Count < 2)
                {
                    EditorUtility.DisplayDialog("Línea Vertical 1xN", "La selección contenía múltiples columnas. Selecciona solo tiles de una misma columna.", "OK");
                    return;
                }
            }

            selectedTiles = selectedTiles.OrderByDescending(t => t.rectInAtlas.y).ToList(); // De arriba hacia abajo

            Undo.RecordObject(database, $"Vincular Línea Vertical 1x{selectedTiles.Count}");
            bool hasBorder = !string.IsNullOrEmpty(borderCategory) && borderCategory != "[Sin Conexión Exterior / Solo Interno]" && borderCategory != "[ninguno]" && borderCategory != "Solo Interno";
            var borderTiles = hasBorder ? database.GetTilesByCategory(borderCategory) : new List<WfcTileEntry>();

            int internalCount = 0;
            int borderCount = 0;

            for (int i = 0; i < selectedTiles.Count; i++)
            {
                var t = selectedTiles[i];

                // Conexiones internas verticales
                if (i > 0)
                {
                    database.AddNeighbor(t.id, selectedTiles[i - 1].id, Vector2Int.up, true);
                    internalCount++;
                }
                if (i < selectedTiles.Count - 1)
                {
                    database.AddNeighbor(t.id, selectedTiles[i + 1].id, Vector2Int.down, true);
                    internalCount++;
                }

                // Si es una tira de 3 y es el centro (recta vertical de camino), permitir auto-repetición vertical
                if (selectedTiles.Count == 3 && i == 1)
                {
                    database.AddNeighbor(t.id, t.id, Vector2Int.up, true);
                    database.AddNeighbor(t.id, t.id, Vector2Int.down, true);
                    internalCount += 2;
                }

                // Conectar bordes laterales Oeste y Este a la categoría exterior
                if (hasBorder)
                {
                    foreach (var bt in borderTiles)
                    {
                        database.AddNeighbor(t.id, bt.id, Vector2Int.left, true);
                        database.AddNeighbor(t.id, bt.id, Vector2Int.right, true);
                        borderCount += 2;
                    }

                    // Extremos Norte y Sur
                    if (i == 0)
                    {
                        foreach (var bt in borderTiles) { database.AddNeighbor(t.id, bt.id, Vector2Int.up, true); borderCount++; }
                    }
                    if (i == selectedTiles.Count - 1)
                    {
                        foreach (var bt in borderTiles) { database.AddNeighbor(t.id, bt.id, Vector2Int.down, true); borderCount++; }
                    }
                }
            }

            SaveDatabase();
            RunAudit();
            Repaint();
            Debug.Log($"<color=green><b>[Línea Vertical 1x{selectedTiles.Count}]</b> ¡Columna de {selectedTiles.Count} tiles vinculada! <b>{internalCount}</b> internas, <b>{borderCount}</b> con '{borderCategory}'.</color>");
        }

        private void AutoWireSelectionAsHorizontalLine(string borderCategory)
        {
            var selectedTiles = multiSelectedIds.Select(id => database.GetTileById(id)).Where(t => t != null).ToList();
            
            // Si solo hay 1 o 0 seleccionados, auto-detectar la tira horizontal contigua
            if (selectedTiles.Count < 2)
            {
                var activeT = selectedTileId >= 0 ? database.GetTileById(selectedTileId) : (selectedTiles.Count > 0 ? selectedTiles[0] : null);
                if (activeT != null)
                {
                    var detected = FindHorizontalStripContainingTile(activeT);
                    if (detected != null && detected.Count >= 2)
                    {
                        selectedTiles = detected;
                        multiSelectedIds = detected.Select(t => t.id).ToList();
                        Repaint();
                    }
                }
            }

            if (selectedTiles.Count < 2)
            {
                EditorUtility.DisplayDialog("Fila Horizontal Nx1", "Selecciona al menos 2 tiles que formen una fila horizontal (o haz clic en cualquier tile de la tira horizontal).", "OK");
                return;
            }

            // Validación de seguridad: Asegurarse de que no estamos mezclando múltiples filas
            var distinctYs = selectedTiles.Select(t => t.rectInAtlas.y).Distinct().ToList();
            if (distinctYs.Count > 1)
            {
                var activeT = database.GetTileById(selectedTileId);
                float targetY = activeT != null ? activeT.rectInAtlas.y : distinctYs[0];
                selectedTiles = selectedTiles.Where(t => Mathf.Abs(t.rectInAtlas.y - targetY) < 0.5f).ToList();
                multiSelectedIds = selectedTiles.Select(t => t.id).ToList();
                Repaint();

                if (selectedTiles.Count < 2)
                {
                    EditorUtility.DisplayDialog("Fila Horizontal Nx1", "La selección contenía múltiples filas. Selecciona solo tiles de una misma fila.", "OK");
                    return;
                }
            }

            selectedTiles = selectedTiles.OrderBy(t => t.rectInAtlas.x).ToList(); // De izquierda a derecha

            Undo.RecordObject(database, $"Vincular Fila Horizontal {selectedTiles.Count}x1");
            bool hasBorder = !string.IsNullOrEmpty(borderCategory) && borderCategory != "[Sin Conexión Exterior / Solo Interno]" && borderCategory != "[ninguno]" && borderCategory != "Solo Interno";
            var borderTiles = hasBorder ? database.GetTilesByCategory(borderCategory) : new List<WfcTileEntry>();

            int internalCount = 0;
            int borderCount = 0;

            for (int i = 0; i < selectedTiles.Count; i++)
            {
                var t = selectedTiles[i];

                // Conexiones internas horizontales
                if (i > 0)
                {
                    database.AddNeighbor(t.id, selectedTiles[i - 1].id, Vector2Int.left, true);
                    internalCount++;
                }
                if (i < selectedTiles.Count - 1)
                {
                    database.AddNeighbor(t.id, selectedTiles[i + 1].id, Vector2Int.right, true);
                    internalCount++;
                }

                // Si es una tira de 3 y es el centro (recta horizontal de camino), permitir auto-repetición horizontal
                if (selectedTiles.Count == 3 && i == 1)
                {
                    database.AddNeighbor(t.id, t.id, Vector2Int.left, true);
                    database.AddNeighbor(t.id, t.id, Vector2Int.right, true);
                    internalCount += 2;
                }

                // Conectar bordes Norte y Sur a la categoría exterior
                if (hasBorder)
                {
                    foreach (var bt in borderTiles)
                    {
                        database.AddNeighbor(t.id, bt.id, Vector2Int.up, true);
                        database.AddNeighbor(t.id, bt.id, Vector2Int.down, true);
                        borderCount += 2;
                    }

                    // Extremos Oeste y Este
                    if (i == 0)
                    {
                        foreach (var bt in borderTiles) { database.AddNeighbor(t.id, bt.id, Vector2Int.left, true); borderCount++; }
                    }
                    if (i == selectedTiles.Count - 1)
                    {
                        foreach (var bt in borderTiles) { database.AddNeighbor(t.id, bt.id, Vector2Int.right, true); borderCount++; }
                    }
                }
            }

            SaveDatabase();
            RunAudit();
            Repaint();
            Debug.Log($"<color=green><b>[Fila Horizontal {selectedTiles.Count}x1]</b> ¡Fila de {selectedTiles.Count} tiles vinculada! <b>{internalCount}</b> internas, <b>{borderCount}</b> con '{borderCategory}'.</color>");
        }

        private void AutoWireSelectionAsSingleObjects(string borderCategory)
        {
            var selectedTiles = (multiSelectedIds.Count > 0 ? multiSelectedIds : new List<int> { selectedTileId })
                .Select(id => database.GetTileById(id))
                .Where(t => t != null)
                .ToList();

            if (selectedTiles.Count == 0)
            {
                EditorUtility.DisplayDialog("Objetos Sueltos 1x1", "Selecciona al menos 1 tile en el atlas.", "OK");
                return;
            }

            Undo.RecordObject(database, "Vincular Objetos Sueltos 1x1");
            bool hasBorder = !string.IsNullOrEmpty(borderCategory) && borderCategory != "[Sin Conexión Exterior / Solo Interno]";
            var borderTiles = hasBorder ? database.GetTilesByCategory(borderCategory) : new List<WfcTileEntry>();

            int count = 0;
            foreach (var t in selectedTiles)
            {
                if (t.weight >= 1.0f) t.weight = 0.08f; // Probabilidad de spawn moderada para objetos decorativos

                if (hasBorder)
                {
                    foreach (var bt in borderTiles)
                    {
                        database.AddNeighbor(t.id, bt.id, Vector2Int.up, true);
                        database.AddNeighbor(t.id, bt.id, Vector2Int.down, true);
                        database.AddNeighbor(t.id, bt.id, Vector2Int.left, true);
                        database.AddNeighbor(t.id, bt.id, Vector2Int.right, true);
                        count += 4;
                    }
                }
            }

            SaveDatabase();
            Debug.Log($"<color=green><b>[Objetos Sueltos 1x1]</b> ¡{selectedTiles.Count} objetos vinculados en 4 direcciones con '{borderCategory}' ({count} enlaces)!</color>");
        }

        private void AutoWireBordersOnly(string borderCategory)
        {
            var selectedTiles = multiSelectedIds.Select(id => database.GetTileById(id)).Where(t => t != null).ToList();
            if (selectedTiles.Count == 0 && selectedTileId >= 0)
            {
                var t = database.GetTileById(selectedTileId);
                if (t != null) selectedTiles.Add(t);
            }

            if (selectedTiles.Count == 0)
            {
                EditorUtility.DisplayDialog("Enlazar Bordes", "Selecciona primero los tiles de la estructura o bloque en el atlas.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(borderCategory) || borderCategory == "[Sin Conexión Exterior / Solo Interno]" || borderCategory == "[ninguno]" || borderCategory == "Solo Interno")
            {
                EditorUtility.DisplayDialog("Enlazar Bordes", "Selecciona una categoría de borde válida en el desplegable 'Borde Ext:' (por ejemplo 'Prado').", "OK");
                return;
            }

            var borderTiles = database.GetTilesByCategory(borderCategory);
            if (borderTiles.Count == 0)
            {
                EditorUtility.DisplayDialog("Enlazar Bordes", $"La categoría '{borderCategory}' no tiene ningún tile asignado en el atlas.\n\nAsigna primero algunos tiles a '{borderCategory}' antes de enlazar.", "OK");
                return;
            }

            Undo.RecordObject(database, $"Enlazar Bordes Exteriores con {borderCategory}");

            int borderConns = 0;
            int borderTilesCount = 0;

            var sockets = GetRoadSocketsDictionary();

            foreach (var a in selectedTiles)
            {
                // Si el tile es un tile de camino reconocido, usar sus sockets
                if (sockets.TryGetValue(a, out var sock))
                {
                    bool isRoadBorder = false;
                    if (sock.north == RoadSocketType.None) { foreach (var bt in borderTiles) { database.AddNeighbor(a.id, bt.id, Vector2Int.up, true); borderConns++; } isRoadBorder = true; }
                    if (sock.south == RoadSocketType.None) { foreach (var bt in borderTiles) { database.AddNeighbor(a.id, bt.id, Vector2Int.down, true); borderConns++; } isRoadBorder = true; }
                    if (sock.west == RoadSocketType.None)  { foreach (var bt in borderTiles) { database.AddNeighbor(a.id, bt.id, Vector2Int.left, true); borderConns++; } isRoadBorder = true; }
                    if (sock.east == RoadSocketType.None)  { foreach (var bt in borderTiles) { database.AddNeighbor(a.id, bt.id, Vector2Int.right, true); borderConns++; } isRoadBorder = true; }
                    if (isRoadBorder) borderTilesCount++;
                }
                else
                {
                    bool hasNorth = false;
                    bool hasSouth = false;
                    bool hasEast  = false;
                    bool hasWest  = false;

                    foreach (var b in selectedTiles)
                    {
                        if (a == b) continue;

                        // B está Arriba de A en el Atlas
                        if (Mathf.Abs(a.rectInAtlas.x - b.rectInAtlas.x) < 1.0f &&
                            Mathf.Abs((a.rectInAtlas.y + a.rectInAtlas.height) - b.rectInAtlas.y) < 1.0f)
                        {
                            hasNorth = true;
                        }

                        // B está Abajo de A en el Atlas
                        if (Mathf.Abs(a.rectInAtlas.x - b.rectInAtlas.x) < 1.0f &&
                            Mathf.Abs((b.rectInAtlas.y + b.rectInAtlas.height) - a.rectInAtlas.y) < 1.0f)
                        {
                            hasSouth = true;
                        }

                        // B está a la Derecha de A en el Atlas
                        if (Mathf.Abs(a.rectInAtlas.y - b.rectInAtlas.y) < 1.0f &&
                            Mathf.Abs((a.rectInAtlas.x + a.rectInAtlas.width) - b.rectInAtlas.x) < 1.0f)
                        {
                            hasEast = true;
                        }

                        // B está a la Izquierda de A en el Atlas
                        if (Mathf.Abs(a.rectInAtlas.y - b.rectInAtlas.y) < 1.0f &&
                            Mathf.Abs((b.rectInAtlas.width + b.rectInAtlas.x) - a.rectInAtlas.x) < 1.0f)
                        {
                            hasWest = true;
                        }
                    }

                    bool isBorder = (!hasNorth || !hasSouth || !hasEast || !hasWest);
                    if (isBorder) borderTilesCount++;

                    if (!hasNorth)
                    {
                        foreach (var bt in borderTiles) { database.AddNeighbor(a.id, bt.id, Vector2Int.up, true); borderConns++; }
                    }
                    if (!hasSouth)
                    {
                        foreach (var bt in borderTiles) { database.AddNeighbor(a.id, bt.id, Vector2Int.down, true); borderConns++; }
                    }
                    if (!hasWest)
                    {
                        foreach (var bt in borderTiles) { database.AddNeighbor(a.id, bt.id, Vector2Int.left, true); borderConns++; }
                    }
                    if (!hasEast)
                    {
                        foreach (var bt in borderTiles) { database.AddNeighbor(a.id, bt.id, Vector2Int.right, true); borderConns++; }
                    }
                }
            }

            SaveDatabase();
            RunAudit();
            Repaint();

            Debug.Log($"<color=green><b>[Bordes Exteriores]</b> ¡Se enlazaron {borderTilesCount} tiles perimetrales/expuestos con <b>{borderTiles.Count}</b> tiles de '{borderCategory}' ({borderConns} enlaces creados)!</color>");
            EditorUtility.DisplayDialog("Bordes Enlazados", 
                $"¡Bordes exteriores enlazados con éxito!\n\n" +
                $"• Tiles perimetrales / expuestos: {borderTilesCount}\n" +
                $"• Tiles de '{borderCategory}' enlazados: {borderTiles.Count}\n" +
                $"• Enlaces bidireccionales creados: {borderConns}", "OK");
        }

        private void LoadSpritesFromSpriteEditor()
        {
            if (database == null || database.atlasTexture == null) return;

            string path = AssetDatabase.GetAssetPath(database.atlasTexture);
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                                            .OfType<Sprite>()
                                            .OrderByDescending(s => s.rect.y)
                                            .ThenBy(s => s.rect.x)
                                            .ToArray();

            if (sprites.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No se encontraron sprites cortados en la textura.", "OK");
                return;
            }

            string folder = "Assets/Art/GeneratedTiles";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            Undo.RecordObject(database, "Sincronizar Sprites de Atlas");

            // Preservar datos existentes indexados por nombre de sprite o posición en atlas
            var existingBySpriteName = new Dictionary<string, WfcTileEntry>();
            var existingByRect = new Dictionary<Vector2, WfcTileEntry>();
            if (database.tiles != null)
            {
                foreach (var t in database.tiles)
                {
                    if (!string.IsNullOrEmpty(t.spriteName) && !existingBySpriteName.ContainsKey(t.spriteName))
                        existingBySpriteName[t.spriteName] = t;
                    Vector2 pos = new Vector2(t.rectInAtlas.x, t.rectInAtlas.y);
                    if (!existingByRect.ContainsKey(pos))
                        existingByRect[pos] = t;
                }
            }

            var updatedTiles = new List<WfcTileEntry>();

            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite s = sprites[i];

                string tileAssetPath = $"{folder}/{s.name}_tile.asset";
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tileAssetPath);
                if (tile == null)
                {
                    tile = CreateInstance<Tile>();
                    tile.sprite = s;
                    AssetDatabase.CreateAsset(tile, tileAssetPath);
                }
                else if (tile.sprite != s)
                {
                    tile.sprite = s;
                    EditorUtility.SetDirty(tile);
                }

                // Buscar si ya existía para mantener su categoría, conexiones, etc.
                WfcTileEntry existing = null;
                if (existingBySpriteName.TryGetValue(s.name, out var byName))
                {
                    existing = byName;
                }
                else if (existingByRect.TryGetValue(new Vector2(s.rect.x, s.rect.y), out var byRect))
                {
                    existing = byRect;
                }

                WfcTileEntry entry;
                if (existing != null)
                {
                    entry = existing;
                    entry.id = i;
                    entry.sprite = s;
                    entry.tile = tile;
                    entry.rectInAtlas = s.rect;
                    if (string.IsNullOrEmpty(entry.category)) entry.category = "Prado";
                }
                else
                {
                    entry = new WfcTileEntry
                    {
                        id = i,
                        spriteName = s.name,
                        sprite = s,
                        tile = tile,
                        rectInAtlas = s.rect,
                        category = "Prado",
                        weight = 1.0f
                    };
                }

                updatedTiles.Add(entry);
            }

            database.tiles = updatedTiles;
            database.EnsureCategories();

            SaveDatabase();
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>¡Éxito! Se sincronizaron {database.tiles.Count} sprites conservando categorías y conexiones existentes.</color>");
        }

        private int GetCurrentPreviewId(WfcTileEntry tile, int dirIndex)
        {
            var list = GetNeighborList(tile, dirIndex);
            if (list == null || list.Count == 0) return -1;
            int idx = GetPreviewIdx(dirIndex);
            idx = Mathf.Clamp(idx, 0, list.Count - 1);
            return list[idx];
        }

        private void SetPreviewToId(WfcTileEntry tile, int dirIndex, int targetId)
        {
            var list = GetNeighborList(tile, dirIndex);
            if (list == null) return;
            int found = list.IndexOf(targetId);
            if (found >= 0)
            {
                if (dirIndex == 0) previewNorthIdx = found;
                else if (dirIndex == 1) previewSouthIdx = found;
                else if (dirIndex == 2) previewWestIdx = found;
                else if (dirIndex == 3) previewEastIdx = found;
            }
        }

        private void ClampPreviewIndex(WfcTileEntry tile, int dirIndex)
        {
            var list = GetNeighborList(tile, dirIndex);
            int count = (list != null) ? list.Count : 0;
            if (count <= 1)
            {
                if (dirIndex == 0) previewNorthIdx = 0;
                else if (dirIndex == 1) previewSouthIdx = 0;
                else if (dirIndex == 2) previewWestIdx = 0;
                else if (dirIndex == 3) previewEastIdx = 0;
            }
            else
            {
                if (dirIndex == 0) previewNorthIdx = Mathf.Clamp(previewNorthIdx, 0, count - 1);
                else if (dirIndex == 1) previewSouthIdx = Mathf.Clamp(previewSouthIdx, 0, count - 1);
                else if (dirIndex == 2) previewWestIdx = Mathf.Clamp(previewWestIdx, 0, count - 1);
                else if (dirIndex == 3) previewEastIdx = Mathf.Clamp(previewEastIdx, 0, count - 1);
            }
        }

        private List<int> GetNeighborList(WfcTileEntry tile, int dirIndex)
        {
            if (dirIndex == 0) return tile.allowedNorth;
            if (dirIndex == 1) return tile.allowedSouth;
            if (dirIndex == 2) return tile.allowedWest;
            return tile.allowedEast;
        }

        private int GetPreviewIdx(int dirIndex)
        {
            if (dirIndex == 0) return previewNorthIdx;
            if (dirIndex == 1) return previewSouthIdx;
            if (dirIndex == 2) return previewWestIdx;
            return previewEastIdx;
        }

        #endregion

        #region Auditor y Diagnóstico de Salud

        private void DrawAuditorBar()
        {
            EditorGUILayout.BeginVertical("helpBox");
            EditorGUILayout.BeginHorizontal();

            GUI.color = (auditOrphanCount == 0 && auditOneWayCount == 0) ? Color.green : Color.yellow;
            EditorGUILayout.LabelField($"🩺 Auditor WFC:", EditorStyles.boldLabel, GUILayout.Width(110));
            GUI.color = Color.white;

            EditorGUILayout.LabelField($"🟢 Operativos: <b>{auditValidCount}</b>", new GUIStyle(EditorStyles.label) { richText = true }, GUILayout.Width(130));

            if (auditOrphanCount > 0)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                EditorGUILayout.LabelField($"⚠️ Sin Conexión (Huérfanos): <b>{auditOrphanCount}</b>", new GUIStyle(EditorStyles.label) { richText = true }, GUILayout.Width(210));
                GUI.color = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField($"✅ 0 Huérfanos", GUILayout.Width(110));
            }

            if (auditOneWayCount > 0)
            {
                GUI.color = new Color(1f, 0.6f, 0.2f);
                EditorGUILayout.LabelField($"⚠️ Unilaterales: <b>{auditOneWayCount}</b>", new GUIStyle(EditorStyles.label) { richText = true }, GUILayout.Width(150));
                GUI.color = Color.white;

                GUI.backgroundColor = new Color(0.4f, 1f, 0.6f);
                if (GUILayout.Button("🛠️ Reparar Reciprocidad", EditorStyles.miniButton, GUILayout.Width(160)))
                {
                    RepairReciprocity();
                    RunAudit();
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.LabelField($"✅ 100% Recíproco", GUILayout.Width(130));
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("🔄 Re-analizar", EditorStyles.miniButton, GUILayout.Width(90)))
            {
                RunAudit();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void RunAudit()
        {
            if (database == null || database.tiles == null) return;

            auditValidCount = 0;
            auditOrphanCount = 0;
            auditOneWayCount = 0;

            foreach (var tile in database.tiles)
            {
                int totalNeighbors = tile.allowedNorth.Count + tile.allowedSouth.Count + tile.allowedEast.Count + tile.allowedWest.Count;
                if (totalNeighbors == 0)
                {
                    auditOrphanCount++;
                }
                else
                {
                    auditValidCount++;
                }

                // Chequear asimetrías
                foreach (int nId in tile.allowedNorth)
                {
                    var nTile = database.GetTileById(nId);
                    if (nTile != null && !nTile.allowedSouth.Contains(tile.id)) auditOneWayCount++;
                }
                foreach (int sId in tile.allowedSouth)
                {
                    var sTile = database.GetTileById(sId);
                    if (sTile != null && !sTile.allowedNorth.Contains(tile.id)) auditOneWayCount++;
                }
                foreach (int eId in tile.allowedEast)
                {
                    var eTile = database.GetTileById(eId);
                    if (eTile != null && !eTile.allowedWest.Contains(tile.id)) auditOneWayCount++;
                }
                foreach (int wId in tile.allowedWest)
                {
                    var wTile = database.GetTileById(wId);
                    if (wTile != null && !wTile.allowedEast.Contains(tile.id)) auditOneWayCount++;
                }
            }
        }

        private void RepairReciprocity()
        {
            if (database == null || database.tiles == null) return;
            Undo.RecordObject(database, "Reparar Reciprocidad WFC");

            int repaired = 0;
            foreach (var tile in database.tiles)
            {
                foreach (int nId in tile.allowedNorth.ToList())
                {
                    var nTile = database.GetTileById(nId);
                    if (nTile != null && !nTile.allowedSouth.Contains(tile.id)) { nTile.allowedSouth.Add(tile.id); repaired++; }
                }
                foreach (int sId in tile.allowedSouth.ToList())
                {
                    var sTile = database.GetTileById(sId);
                    if (sTile != null && !sTile.allowedNorth.Contains(tile.id)) { sTile.allowedNorth.Add(tile.id); repaired++; }
                }
                foreach (int eId in tile.allowedEast.ToList())
                {
                    var eTile = database.GetTileById(eId);
                    if (eTile != null && !eTile.allowedWest.Contains(tile.id)) { eTile.allowedWest.Add(tile.id); repaired++; }
                }
                foreach (int wId in tile.allowedWest.ToList())
                {
                    var wTile = database.GetTileById(wId);
                    if (wTile != null && !wTile.allowedEast.Contains(tile.id)) { wTile.allowedEast.Add(tile.id); repaired++; }
                }
            }

            SaveDatabase();
            Debug.Log($"<color=green><b>[Auditor WFC]</b> ¡Reciprocidad reparada con éxito! Se ajustaron <b>{repaired}</b> conexiones asimétricas.</color>");
        }

        #endregion
    }
}

