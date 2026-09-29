using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using WorldGen.Data;

namespace WorldGen.EditorTools
{
    public class TileRenameWizardWindow : EditorWindow
    {
        public enum NamingPattern
        {
            [InspectorName("Tile_{ID}  (ej: Tile_56)")]
            Tile_ID,

            [InspectorName("{Categoria}_{ID}  (ej: Bosque_56, Meseta_53)")]
            Category_ID,

            [InspectorName("{Prefijo}_{ID}  (ej: Overworld_56)")]
            CustomPrefix_ID,

            [InspectorName("{NombreActual}_ID{ID}  (ej: Sprite_ID56)")]
            OriginalName_ID
        }

        public enum RenameScope
        {
            [InspectorName("Todos los tiles de la Base de Datos")]
            AllTiles,

            [InspectorName("Solo los tiles de una Categoría")]
            ByCategory,

            [InspectorName("Solo los tiles Seleccionados")]
            SelectedOnly
        }

        [Header("Base de Datos")]
        public WfcVisualDatabase database;

        [Header("Configuración de Renombrado")]
        public NamingPattern namingPattern = NamingPattern.Category_ID;
        public RenameScope scope = RenameScope.AllTiles;
        public string selectedCategory = "Bosque";
        public string customPrefix = "Tile";

        [Header("Archivos en Disco")]
        [Tooltip("Si está activo, también renombra los archivos .asset de los tiles en Assets/Art/GeneratedTiles conservando sus GUIDs.")]
        public bool renameAssetFiles = true;

        [Tooltip("Relleno de ceros en el ID (ej: 056 en vez de 56). 0 = sin ceros a la izquierda.")]
        [Range(0, 4)]
        public int zeroPadding = 0;

        private List<int> selectedTileIds = new List<int>();
        private Vector2 scrollPos;

        [MenuItem("Tools/WorldGen/🏷️ Renombrar Tiles por ID")]
        public static void Open()
        {
            var win = GetWindow<TileRenameWizardWindow>("Renombrar Tiles");
            win.minSize = new Vector2(550, 520);
            win.AutoFindDatabase();
        }

        public static void OpenWithDatabase(WfcVisualDatabase db, List<int> selection = null)
        {
            var win = GetWindow<TileRenameWizardWindow>("Renombrar Tiles");
            win.database = db;
            win.minSize = new Vector2(550, 520);
            if (selection != null && selection.Count > 0)
            {
                win.selectedTileIds = new List<int>(selection);
                win.scope = RenameScope.SelectedOnly;
            }
            else
            {
                win.scope = RenameScope.AllTiles;
            }
        }

        private void OnEnable()
        {
            if (database == null) AutoFindDatabase();
        }

        private void AutoFindDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:WfcVisualDatabase");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                database = AssetDatabase.LoadAssetAtPath<WfcVisualDatabase>(path);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("🏷️ Asistente para Renombrar Tiles por ID", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Esta herramienta renombra los tiles basándose en su ID numérico y/o categoría. " +
                                  "Conserva los GUIDs intactos para no desvincular ninguna escena ni ScriptableObject.", MessageType.Info);

            EditorGUILayout.Space(6);

            // Selector de Base de Datos
            EditorGUILayout.BeginHorizontal();
            database = (WfcVisualDatabase)EditorGUILayout.ObjectField("Base de Datos:", database, typeof(WfcVisualDatabase), false);
            if (database == null && GUILayout.Button("Buscar", GUILayout.Width(70)))
            {
                AutoFindDatabase();
            }
            EditorGUILayout.EndHorizontal();

            if (database == null)
            {
                EditorGUILayout.HelpBox("Asigna o crea una base de datos WfcVisualDatabase para continuar.", MessageType.Warning);
                return;
            }

            database.EnsureCategories();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("1. Opciones de Formato", EditorStyles.boldLabel);

            namingPattern = (NamingPattern)EditorGUILayout.EnumPopup("Patrón de Nombre:", namingPattern);

            if (namingPattern == NamingPattern.CustomPrefix_ID)
            {
                customPrefix = EditorGUILayout.TextField("Prefijo Personalizado:", customPrefix);
            }

            zeroPadding = EditorGUILayout.IntSlider("Relleno de Ceros (Padding):", zeroPadding, 0, 4);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("2. Alcance (¿Qué tiles renombrar?)", EditorStyles.boldLabel);

            scope = (RenameScope)EditorGUILayout.EnumPopup("Alcance:", scope);

            if (scope == RenameScope.ByCategory)
            {
                int catIdx = database.categories.IndexOf(selectedCategory);
                if (catIdx < 0) catIdx = 0;
                int newIdx = EditorGUILayout.Popup("Categoría a Renombrar:", catIdx, database.categories.ToArray());
                if (newIdx >= 0 && newIdx < database.categories.Count)
                {
                    selectedCategory = database.categories[newIdx];
                }
            }
            else if (scope == RenameScope.SelectedOnly)
            {
                int count = selectedTileIds != null ? selectedTileIds.Count : 0;
                EditorGUILayout.LabelField($"Tiles actualmente seleccionados: {count}", EditorStyles.miniBoldLabel);
                if (count == 0)
                {
                    EditorGUILayout.HelpBox("No hay tiles seleccionados. Abre WFC Studio y selecciona los tiles deseados o cambia el alcance a 'Todos'.", MessageType.Warning);
                }
            }

            EditorGUILayout.Space(6);
            renameAssetFiles = EditorGUILayout.Toggle(new GUIContent("Renombrar Archivos .asset en Disco", "Renombra los archivos en Assets/Art/GeneratedTiles manteniendo intacto el GUID de Unity."), renameAssetFiles);

            EditorGUILayout.Space(12);

            // Lista filtrada para previsualización
            List<WfcTileEntry> targetTiles = GetTargetTiles();

            EditorGUILayout.LabelField($"3. Vista Previa ({targetTiles.Count} tiles afectados)", EditorStyles.boldLabel);

            DrawPreviewTable(targetTiles);

            EditorGUILayout.Space(12);

            // Botón de ejecución
            GUI.enabled = targetTiles.Count > 0;
            GUI.backgroundColor = new Color(0.2f, 0.95f, 0.4f);
            if (GUILayout.Button($"🚀 Renombrar {targetTiles.Count} Tiles por ID", GUILayout.Height(36)))
            {
                if (EditorUtility.DisplayDialog("Confirmar Renombrado de Tiles",
                    $"¿Estás seguro de que deseas renombrar {targetTiles.Count} tiles por ID?\n" +
                    $"Patrón seleccionado: {namingPattern}\n" +
                    $"Renombrar archivos en disco: {(renameAssetFiles ? "SÍ" : "NO")}",
                    "Sí, Renombrar", "Cancelar"))
                {
                    ExecuteRename(targetTiles);
                }
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }

        private List<WfcTileEntry> GetTargetTiles()
        {
            if (database == null || database.tiles == null) return new List<WfcTileEntry>();

            switch (scope)
            {
                case RenameScope.ByCategory:
                    return database.tiles.Where(t => t != null && string.Equals(t.category, selectedCategory, StringComparison.OrdinalIgnoreCase)).ToList();

                case RenameScope.SelectedOnly:
                    if (selectedTileIds == null || selectedTileIds.Count == 0) return new List<WfcTileEntry>();
                    return database.tiles.Where(t => t != null && selectedTileIds.Contains(t.id)).ToList();

                case RenameScope.AllTiles:
                default:
                    return database.tiles.Where(t => t != null).ToList();
            }
        }

        private string GenerateName(WfcTileEntry entry)
        {
            if (entry == null) return "";
            string idStr = zeroPadding > 0 ? entry.id.ToString($"D{zeroPadding + 1}") : entry.id.ToString();

            switch (namingPattern)
            {
                case NamingPattern.Tile_ID:
                    return $"Tile_{idStr}";

                case NamingPattern.Category_ID:
                    string cleanCat = string.IsNullOrEmpty(entry.category) ? "Tile" : entry.category.Replace(" ", "_");
                    return $"{cleanCat}_{idStr}";

                case NamingPattern.CustomPrefix_ID:
                    string cleanPrefix = string.IsNullOrEmpty(customPrefix) ? "Tile" : customPrefix.Trim().Replace(" ", "_");
                    return $"{cleanPrefix}_{idStr}";

                case NamingPattern.OriginalName_ID:
                    string orig = !string.IsNullOrEmpty(entry.spriteName) ? entry.spriteName : (entry.tile != null ? entry.tile.name : "Tile");
                    return $"{orig}_ID{idStr}";

                default:
                    return $"Tile_{idStr}";
            }
        }

        private void DrawPreviewTable(List<WfcTileEntry> targetTiles)
        {
            EditorGUILayout.BeginVertical("box");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(140));

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("ID", EditorStyles.miniBoldLabel, GUILayout.Width(40));
            EditorGUILayout.LabelField("Categoría", EditorStyles.miniBoldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("Nombre Actual", EditorStyles.miniBoldLabel, GUILayout.Width(160));
            EditorGUILayout.LabelField("➜ Nuevo Nombre", EditorStyles.miniBoldLabel, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            int displayCount = Mathf.Min(targetTiles.Count, 50);
            for (int i = 0; i < displayCount; i++)
            {
                var t = targetTiles[i];
                string currentName = !string.IsNullOrEmpty(t.spriteName) ? t.spriteName : (t.tile != null ? t.tile.name : "—");
                string newName = GenerateName(t);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"#{t.id}", GUILayout.Width(40));
                EditorGUILayout.LabelField(t.category ?? "Prado", GUILayout.Width(100));
                EditorGUILayout.LabelField(currentName, GUILayout.Width(160));
                EditorGUILayout.LabelField($"➜ {newName}", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
            }

            if (targetTiles.Count > displayCount)
            {
                EditorGUILayout.LabelField($"... y {targetTiles.Count - displayCount} tiles más ...", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void ExecuteRename(List<WfcTileEntry> targetTiles)
        {
            if (targetTiles == null || targetTiles.Count == 0) return;

            Undo.RecordObject(database, "Renombrar Tiles por ID");

            int renamedCount = 0;
            int fileRenamedCount = 0;

            try
            {
                for (int i = 0; i < targetTiles.Count; i++)
                {
                    var entry = targetTiles[i];
                    string newName = GenerateName(entry);

                    EditorUtility.DisplayProgressBar("Renombrando Tiles", $"Tile #{entry.id} -> {newName}", (float)i / targetTiles.Count);

                    // 1. Actualizar en Base de Datos
                    entry.spriteName = newName;

                    // 2. Actualizar TileBase en memoria y en disco
                    if (entry.tile != null)
                    {
                        entry.tile.name = newName;
                        EditorUtility.SetDirty(entry.tile);

                        if (renameAssetFiles)
                        {
                            string assetPath = AssetDatabase.GetAssetPath(entry.tile);
                            if (!string.IsNullOrEmpty(assetPath) && Path.GetFileNameWithoutExtension(assetPath) != newName)
                            {
                                string error = AssetDatabase.RenameAsset(assetPath, newName);
                                if (string.IsNullOrEmpty(error))
                                {
                                    fileRenamedCount++;
                                }
                                else
                                {
                                    Debug.LogWarning($"<b>[RenameAsset]</b> No se pudo renombrar el archivo '{assetPath}': {error}");
                                }
                            }
                        }
                    }

                    renamedCount++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green><b>[Renombrar Tiles]</b> ¡Éxito! Se renombraron {renamedCount} tiles en la base de datos y {fileRenamedCount} archivos en disco.</color>");
            ShowNotification(new GUIContent($"✅ {renamedCount} tiles renombrados"));
        }
    }
}
