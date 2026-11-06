using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using WorldGen.Core;
using WorldGen.Flow;
using WorldGen.Generators;

namespace WorldGen.UI
{
    /// <summary>
    /// Identificador de los 5 submenús tácticos de la barra inferior.
    /// </summary>
    public enum MenuSubTab
    {
        Casino = 0,
        Periodico = 1,
        Misiones = 2,
        Hangar = 3,
        Pasaporte = 4
    }

    /// <summary>
    /// Identificador de las 4 facciones del juego AK-47.
    /// </summary>
    public enum FactionType
    {
        Anarquista = 0,
        Capitalista = 1,
        Comunista = 2,
        Fascista = 3
    }

    /// <summary>
    /// Estructura modular de datos de mapa y zona táctica.
    /// </summary>
    [System.Serializable]
    public class MapZoneData
    {
        [Tooltip("Identificador único de la zona (ej: ZONA-01, SECTOR-B4).")]
        public string zoneId = "ZONA-01";

        [Tooltip("Nombre de la zona o sector táctico.")]
        public string zoneName = "Bahía de los Herejes";

        [Tooltip("Nombre del mapa regional al que pertenece la zona.")]
        public string mapName = "MAPA TÁCTICO REGIONAL";

        [Tooltip("Sprite personalizado del mapa si el usuario lo asigna.")]
        public Sprite mapSprite;

        [Tooltip("Coordenadas tácticas militares en el mapa [X, Y].")]
        public Vector2Int zoneCoordinates = new Vector2Int(34, 18);

        [Tooltip("Nivel de amenaza militar en la zona.")]
        public string threatLevel = "AMENAZA: ALTA";

        [TextArea(2, 4)]
        [Tooltip("Detalles geográficos o tácticos específicos de la zona.")]
        public string zoneDetails = "Costa escarpada con cañones submarinos y bancos de arena.";

        public MapZoneData() { }

        public MapZoneData(string id, string name, string map, Vector2Int coords, string threat, string details, Sprite sprite = null)
        {
            zoneId = id;
            zoneName = name;
            mapName = map;
            zoneCoordinates = coords;
            threatLevel = threat;
            zoneDetails = details;
            mapSprite = sprite;
        }
    }

    /// <summary>
    /// Representa un punto de radio / estado operativo ('State') y una misión específica.
    /// </summary>
    [System.Serializable]
    public class RadioSubMissionData
    {
        [Tooltip("Subfrecuencia exacta (ej: 89.60 MHz, 86.10 MHz).")]
        public float subFrequency = 89.60f;

        [Tooltip("Estado operativo de la señal ('State').")]
        public string stateName = "STATE: VANGUARDIA AÉREA";

        [Tooltip("Identificador militar de la misión.")]
        public string missionId = "MISIÓN 89-3";

        [Tooltip("Título o nombre clave de la operación.")]
        public string missionTitle = "Asalto al Estuario de Hierro";

        [TextArea(3, 5)]
        [Tooltip("Descripción del terreno, bioma y condiciones meteorológicas.")]
        public string terrainDescription = "Relieve kárstico con cañones submarinos, niebla espesa y vientos cruzados de 35 nudos.";

        [Tooltip("Objetivo táctico principal.")]
        public string tacticalObjective = "DESTRUIR GENERADOR DE RADAR COSTEIRO";

        [Tooltip("Datos del mapa y sector militar correspondiente.")]
        public MapZoneData mapZone = new MapZoneData();

        [Header("Configuración Procedural del Mundo (WorldGen)")]
        [Tooltip("Semilla aleatoria fija que genera el mapa exacto.")]
        public int seed = 407949;

        [Tooltip("Estilo de terreno procedural (Archipiélago, Continente, etc.).")]
        public TerrainStyle terrainStyle = TerrainStyle.Archipelago;

        [Tooltip("Ancho del mundo en baldosas.")]
        public int worldWidth = 100;

        [Tooltip("Alto del mundo en baldosas.")]
        public int worldHeight = 300;

        public RadioSubMissionData() { }

        public RadioSubMissionData(
            float freq, 
            string state, 
            string id, 
            string title, 
            string terrainDesc, 
            string objective, 
            MapZoneData zone, 
            int genSeed = 407949, 
            TerrainStyle style = TerrainStyle.Archipelago)
        {
            subFrequency = freq;
            stateName = state;
            missionId = id;
            missionTitle = title;
            terrainDescription = terrainDesc;
            tacticalObjective = objective;
            mapZone = zone ?? new MapZoneData();
            seed = genSeed;
            terrainStyle = style;
        }
    }

    /// <summary>
    /// Representa una emisora principal en el dial derecho (intervalos de 3 en 3: 86, 89, 92, 95...).
    /// </summary>
    [System.Serializable]
    public class RadioStationData
    {
        [Tooltip("Frecuencia base de la emisora (saltos de 3 en 3: 86, 89, 92, 95...).")]
        public float baseFrequency = 89.0f;

        [Tooltip("Nombre de llamada de la emisora.")]
        public string stationCallsign = "ESTACIÓN CENTRAL // 89 MHz";

        [Tooltip("Lista de sub-puntos (submenús) accesibles con el dial izquierdo.")]
        public List<RadioSubMissionData> subMissions = new List<RadioSubMissionData>();

        public RadioStationData() { }

        public RadioStationData(float baseFreq, string callsign, List<RadioSubMissionData> missions = null)
        {
            baseFrequency = baseFreq;
            stationCallsign = callsign;
            subMissions = missions ?? new List<RadioSubMissionData>();
        }
    }

    /// <summary>
    /// Definición de recursos y estaciones de una facción.
    /// </summary>
    [System.Serializable]
    public class FactionMenuDefinition
    {
        public FactionType factionType;
        public string factionDisplayName = "ANARQUISTAS";
        public string slogan = "VANGUARDIA CLANDESTINA // LIBERTAD O CENIZAS";

        [Header("🖼️ Textura de la Radio")]
        public Sprite radioSprite;

        [HideInInspector] public Sprite backgroundSprite;
        [HideInInspector] public Sprite logoSprite;
        [HideInInspector] public Sprite frameVertical;
        [HideInInspector] public Sprite frameHorizontal;
        [HideInInspector] public Sprite factionCover;

        [Header("🎨 Paleta de Color")]
        public Color themeColor = new Color(0.9f, 0.3f, 0.3f, 1f);
        public Color accentColor = new Color(1.0f, 0.85f, 0.4f, 1f);

        [Header("📻 Red de Radio y Misiones")]
        public List<RadioStationData> stations = new List<RadioStationData>();
    }

    /// <summary>
    /// Compatibilidad histórica de datos de reporte para HemerotecaManager.
    /// </summary>
    [System.Serializable]
    public class MissionReportData
    {
        public string missionId = "MISIÓN 1";
        public string missionName = "Invasión del Archipiélago";
        public string sectorName = "ARCHIPIÉLAGO CENTRAL";
        public bool isVictory = true;
        public float completionPercentage = 94f;
        public float accuracyPercentage = 84f;
        public int enemiesDestroyed = 16;
        public int alliedCasualties = 2;
        public string civilianImpact = "Refugios a salvo al 100%";
        public string warReason = "Control estratégico del combustible pesado.";
        public string date = "14 DE OCTUBRE, 1944";
        public string year = "1944";
        public string edition = "EDICIÓN N° 47";
        public string overrideHeadline = "";
        public string overrideSubtitle = "";
        public string overrideStory = "";
        public Sprite photoSprite;
        public string photoCaption = "Avión en combate.";
        public string stampText = "MILITARY CLASSIFIED";
        public string ad1Title = "TALLER";
        public string ad1Text = "Ajuste de bujías.";
        public string ad2Title = "VAPORES";
        public string ad2Text = "Línea de combustible.";
        public string ad3Title = "CHATARRA";
        public string ad3Text = "Se compran restos.";

        public MissionReportData() { }

        public MissionReportData(string id, string name, string sector, bool victory, float completion, int enemies, int casualties, string reason, string dt, string ed)
        {
            missionId = id;
            missionName = name;
            sectorName = sector;
            isVictory = victory;
            completionPercentage = completion;
            enemiesDestroyed = enemies;
            alliedCasualties = casualties;
            warReason = reason;
            date = dt;
            edition = ed;
        }
    }

    /// <summary>
    /// CONTROLADOR PRINCIPAL DEL MENÚ DE RADIO POR FACCIÓN:
    /// ----------------------------------------------------------------------------------------------------
    /// - Cada facción tiene su escena dedicada y única (menu_anarquista, menu_capitalista, etc.).
    /// - No hay botones ni opciones para cambiar de facción dentro del menú: la facción elegida es permanente.
    /// - Toda la pantalla está ocupada de forma limpia por la radio vintage y su pantalla CRT interior.
    /// - Dos diales invisibles sobre los knobs del arte:
    ///     * Derecho (btn-maindial): saltos de 3x3 MHz entre emisoras.
    ///     * Izquierdo (btn-subdial): navega entre los 4 States y misiones dentro de la emisora.
    /// - Pantalla CRT en el hueco transparente:
    ///     * Arriba: Mapa procedural en color (WorldGen) idéntico al de combate.
    ///     * Centro: Estructura modular de mapa con retícula y coordenadas tácticas [X, Y].
    ///     * Abajo: Menú de radio (frecuencia, State, misión, terreno y botón de despliegue).
    /// ----------------------------------------------------------------------------------------------------
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        [Header("🎮 Escenas del Juego")]
        public string gameplaySceneName = "gameplay";

        [Header("🚩 Facción de este Menú (Permanente)")]
        public FactionType activeFaction = FactionType.Capitalista;

        [Header("🖼️ Textura de la Radio (Auto-cargada si está vacía)")]
        public Sprite radioSprite;

        [Header("🔘 Textura del Botón/Dial (Auto-cargada si está vacía)")]
        public Sprite knobButtonSprite;

        [Header("📻 Sintonización Activa")]
        public int currentStationIndex = 1;     // e.g. 89.0 MHz
        public int currentSubMissionIndex = 2;  // e.g. 89.60 MHz (State 3)

        // Estado de rotación suave de los diales
        private Coroutine subdialAnimCoroutine;
        private Coroutine maindialAnimCoroutine;
        private float currentSubdialAngle = 0f;
        private float currentMaindialAngle = 0f;

        [Header("🔊 Audio Procedural de Radio")]
        public bool enableProceduralAudio = true;
        private AudioSource audioSource;
        private AudioClip knobClickSound;
        private AudioClip knobRatchetSound;

        [Header("📁 Definición de Facciones y Misiones")]
        public List<FactionMenuDefinition> factions = new List<FactionMenuDefinition>();

        // Referencias de UI Toolkit
        private UIDocument uiDocument;
        private VisualElement root;
        private VisualElement radioCabinetSkin;
        private VisualElement crtScreenContainer;

        // A. World Preview
        private VisualElement worldPreviewDisplay;
        private Label lblWorldStyle;
        private Label lblWorldCoords;
        private Label lblWorldSeed;
        private Texture2D generatedWorldTexture;

        // B. Map Structure
        private Label lblMapName;
        private Label lblThreatLevel;
        private VisualElement mapSpriteElement;
        private Label lblZoneName;
        private Label lblZoneId;
        private VisualElement mapZoneTarget;

        // C. Radio & Terrain Menu
        private Label lblRadioFrequency;
        private Label lblRadioState;
        private Label lblMissionId;
        private Label lblMissionTitle;
        private Label lblTacticalObjective;
        private Label lblTerrainDescription;
        private Button btnDeployMission;

        // Botones invisibles sobre los diales del arte
        private Button btnSubdial;
        private Button btnMaindial;

        // Barra Superior de Recursos
        private Label lblTopFaction;
        private Label lblResGold;
        private Label lblResFuel;

        [Header("🎛️ Submenús de la Barra Inferior")]
        public MenuSubTab currentActiveTab = MenuSubTab.Misiones;

        // Botones de la Barra Inferior
        private Button btnNavCasino;
        private Button btnNavPeriodico;
        private Button btnNavMisiones;
        private Button btnNavHangar;
        private Button btnNavPasaporte;

        // Contenedores de las 5 Vistas
        private VisualElement viewCasino;
        private VisualElement viewPeriodico;
        private VisualElement viewMisiones;
        private VisualElement viewHangar;
        private VisualElement viewPasaporte;

        // Vista 2: Periódico Histórico (Hemeroteca)
        private Label lblNewsDate;
        private Label lblNewsEdition;
        private Label lblNewsHeadline;
        private Label lblNewsSubtitle;
        private VisualElement newsPhotoImage;
        private Label lblNewsPhotoCaption;
        private Label lblNewsArticleText;
        private Label lblNewsKills;
        private Label lblNewsLosses;
        private Label lblNewsAccuracy;
        private Label lblNewsSector;
        private Label lblNewsPageIndicator;
        private Button btnNewsPrev;
        private Button btnNewsNext;
        private int currentReportIndex = 0;

        // Vista 3: Hangar de Aviones
        private Label lblHangarWallet;
        private Label lblHangarTier;
        private VisualElement hangarPlaneDisplay;
        private VisualElement barHstatFirepower;
        private Label lblHstatFirepower;
        private VisualElement barHstatArmor;
        private Label lblHstatArmor;
        private VisualElement barHstatSpeed;
        private Label lblHstatSpeed;
        private Button btnHangarUpgrade;
        private Button btnHangarArmor;

        // Vista 3: Carrusel de Naves
        private const int HangarSlideCount = 4;
        private int currentHangarSlide = 0;
        private Button btnHangarPrev;
        private Button btnHangarNext;
        private VisualElement hangarCarouselStage;
        private VisualElement[] hangarSlides;
        private VisualElement[] hangarDots;

        // Gestos de arrastre táctil para el carrusel de naves y periódico
        private float hangarDragStartX;
        private float hangarDragStartY;
        private bool isHangarDragging;
        private VisualElement newspaperSwipeContainer;
        private float newsDragStartX;
        private float newsDragStartY;
        private bool isNewsDragging;

        // Vista 4: Pasaporte Militar / Estadísticas
        private Label lblPassPilotFaction;
        private Label lblPstatMissions;
        private Label lblPstatKills;
        private Label lblPstatAccuracy;
        private Label lblPstatHours;
        private Label lblPstatLosses;
        private Label lblPstatKd;

        // Vista 5: Casino Clandestino / Tienda
        private Label lblCasinoWallet;
        private Label lblCasinoStatus;
        private VisualElement casinoRouletteWheel;
        private Button btnCasinoSpin;
        private Button btnBuyFuel;
        private Button btnBuyAmmo;
        private Button btnBuyEngine;
        private Coroutine rouletteAnimCoroutine;

        private void Awake()
        {
            // Leer la facción guardada del jugador como fuente de verdad
            activeFaction = (FactionType)GameFlowDataStore.SavedFactionId;

            uiDocument = GetComponent<UIDocument>();
            SetupAudioSystem();

            if (factions == null || factions.Count < 4)
            {
                InitializeDefaultFactions();
            }
        }

        private void OnEnable()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return;

            root = uiDocument.rootVisualElement;
            if (root == null) return;

            // Leer la facción guardada del jugador y aplicarla
            activeFaction = (FactionType)GameFlowDataStore.SavedFactionId;

            QueryUIElements();
            RegisterCallbacks();
            ApplyFactionTheme();   // ← aplica clases CSS, textos y frecuencia según la facción
            ApplyRadioVisual();

            // Si ya existe un mapa creado previamente, sincronizarlo para que se renderice directamente en la pantalla
            if (PlayerPrefs.HasKey("SavedWorldSeed"))
            {
                int savedSeed = PlayerPrefs.GetInt("SavedWorldSeed");
                TerrainStyle savedStyle = (TerrainStyle)PlayerPrefs.GetInt("SavedTerrainStyle", (int)TerrainStyle.Archipelago);
                var def = GetFactionDefinition(activeFaction);
                if (def != null && def.stations.Count > currentStationIndex)
                {
                    var st = def.stations[currentStationIndex];
                    if (st.subMissions.Count > currentSubMissionIndex)
                    {
                        st.subMissions[currentSubMissionIndex].seed = savedSeed;
                        st.subMissions[currentSubMissionIndex].terrainStyle = savedStyle;
                    }
                }
            }

            UpdateTuningDisplay();
            UpdateKnobRotations(false);
            SwitchTab(currentActiveTab);
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
            if (generatedWorldTexture != null)
            {
                Destroy(generatedWorldTexture);
                generatedWorldTexture = null;
            }
        }

        // ==========================================================================
        // TEMA DE FACCIÓN: aplica clases CSS, textos y datos según la facción guardada
        // Funciona tanto con el UXML unificado (MainMenu_Unified.uxml) como con los
        // UXML por facción heredados.
        // ==========================================================================
        private void ApplyFactionTheme()
        {
            if (root == null) return;

            string factionSuffix = activeFaction switch
            {
                FactionType.Anarquista  => "anarquista",
                FactionType.Capitalista => "capitalista",
                FactionType.Comunista   => "comunista",
                FactionType.Fascista    => "fascista",
                _                       => "capitalista"
            };

            // ── 1. Barra superior ──────────────────────────────────────────
            var topBar = root.Q<VisualElement>("top-resource-bar");
            if (topBar != null)
            {
                topBar.RemoveFromClassList("top-bar-anarquista");
                topBar.RemoveFromClassList("top-bar-capitalista");
                topBar.RemoveFromClassList("top-bar-comunista");
                topBar.RemoveFromClassList("top-bar-fascista");
                topBar.AddToClassList($"top-bar-{factionSuffix}");
            }

            // ── 2. Texto de facción en la barra superior ───────────────────
            if (lblTopFaction != null)
            {
                lblTopFaction.text = activeFaction switch
                {
                    FactionType.Anarquista  => "ANARQUISTAS // VANGUARDIA CLANDESTINA",
                    FactionType.Capitalista => "CAPITALISTAS // CONSORCIO MONETARIO",
                    FactionType.Comunista   => "COMUNISTAS // COLECTIVO ROJO",
                    FactionType.Fascista    => "FASCISTAS // DOCTRINA ZENIT",
                    _                       => "ANARQUISTAS // VANGUARDIA CLANDESTINA"
                };
            }

            // ── 3. Pantalla CRT ────────────────────────────────────────────
            var crtScreen = root.Q<VisualElement>("crt-screen-container");
            if (crtScreen != null)
            {
                crtScreen.RemoveFromClassList("crt-screen-anarquista");
                crtScreen.RemoveFromClassList("crt-screen-capitalista");
                crtScreen.RemoveFromClassList("crt-screen-comunista");
                crtScreen.RemoveFromClassList("crt-screen-fascista");
                crtScreen.AddToClassList($"crt-screen-{factionSuffix}");
            }

            // ── 4. Etiquetas del mapa táctico ──────────────────────────────
            var lblMapName = root.Q<Label>("lbl-map-name");
            if (lblMapName != null)
                lblMapName.text = $"MAPA TÁCTICO {factionSuffix.ToUpper()}";

            var lblFreq = root.Q<Label>("lbl-radio-frequency");
            if (lblFreq != null)
            {
                lblFreq.text = activeFaction switch
                {
                    FactionType.Anarquista  => "89.60 MHz",
                    FactionType.Capitalista => "95.30 MHz",
                    FactionType.Comunista   => "86.10 MHz",
                    FactionType.Fascista    => "101.90 MHz",
                    _                       => "95.30 MHz"
                };
            }

            var lblThreat = root.Q<Label>("lbl-threat-level");
            if (lblThreat != null)
            {
                lblThreat.text = activeFaction switch
                {
                    FactionType.Anarquista  => "AMENAZA: CRÍTICA",
                    FactionType.Capitalista => "AMENAZA: ALTA",
                    FactionType.Comunista   => "AMENAZA: EXTREMA",
                    FactionType.Fascista    => "AMENAZA: MÁXIMA",
                    _                       => "AMENAZA: ALTA"
                };
            }

            // ── 5. Diales de radio ─────────────────────────────────────────
            var btnSub  = root.Q<Button>("btn-subdial");
            var btnMain = root.Q<Button>("btn-maindial");
            string[] dialSuffixes = { "anarquista", "capitalista", "comunista", "fascista" };
            foreach (var btn in new[] { btnSub, btnMain })
            {
                if (btn == null) continue;
                foreach (var s in dialSuffixes)
                {
                    btn.RemoveFromClassList($"knob-skin-{s}");
                    btn.RemoveFromClassList($"knob-btn-{s}-left");
                    btn.RemoveFromClassList($"knob-btn-{s}-right");
                }
            }
            if (btnSub  != null) { btnSub.AddToClassList($"knob-skin-{factionSuffix}");  btnSub.AddToClassList($"knob-btn-{factionSuffix}-left"); }
            if (btnMain != null) { btnMain.AddToClassList($"knob-skin-{factionSuffix}"); btnMain.AddToClassList($"knob-btn-{factionSuffix}-right"); }

            // ── 5b. Gabinete de la Radio (Skin por facción) ───────────────
            var radioSkin = root.Q<VisualElement>("radio-cabinet-skin");
            if (radioSkin != null)
            {
                radioSkin.RemoveFromClassList("radio-skin-anarquista");
                radioSkin.RemoveFromClassList("radio-skin-capitalista");
                radioSkin.RemoveFromClassList("radio-skin-comunista");
                radioSkin.RemoveFromClassList("radio-skin-fascista");
                radioSkin.AddToClassList($"radio-skin-{factionSuffix}");
            }

            // ── 6. Carrusel del Hangar ─────────────────────────────────────
            var carousel = root.Q<VisualElement>("hangar-carousel-bg");
            if (carousel != null)
            {
                carousel.RemoveFromClassList("hangar-carousel-anarquista");
                carousel.RemoveFromClassList("hangar-carousel-capitalista");
                carousel.RemoveFromClassList("hangar-carousel-comunista");
                carousel.RemoveFromClassList("hangar-carousel-fascista");
                carousel.AddToClassList($"hangar-carousel-{factionSuffix}");
            }

            // ── 7. Barra de navegación inferior ───────────────────────────
            var navBar = root.Q<VisualElement>("bottom-navigation-bar");
            if (navBar != null)
            {
                navBar.RemoveFromClassList("nav-bar-anarquista");
                navBar.RemoveFromClassList("nav-bar-capitalista");
                navBar.RemoveFromClassList("nav-bar-comunista");
                navBar.RemoveFromClassList("nav-bar-fascista");
                navBar.AddToClassList($"nav-bar-{factionSuffix}");
            }

            // ── 8. Pasaporte: facción del piloto ───────────────────────────
            if (lblPassPilotFaction != null)
            {
                lblPassPilotFaction.text = activeFaction switch
                {
                    FactionType.Anarquista  => "FACCIÓN ANARQUISTA",
                    FactionType.Capitalista => "FACCIÓN CAPITALISTA",
                    FactionType.Comunista   => "FACCIÓN COMUNISTA",
                    FactionType.Fascista    => "FACCIÓN FASCISTA",
                    _                       => "FACCIÓN MILITAR DE ÉLITE"
                };
            }

            // ── 9. Auto-recargar sprites de radio y diales ─────────────────
            AutoLoadRadioSprite();
            AutoLoadKnobSprite();
        }

        // ==========================================================================
        // AUTO-CARGA ROBUSTA DEL ARTE DE LA RADIO Y BOTONES/DIALES
        // ==========================================================================
        private void AutoLoadRadioSprite()
        {
            // Siempre sincronizar con el sprite de la facción actual definida en la lista de facciones
            var def = GetFactionDefinition(activeFaction);
            if (def != null && def.radioSprite != null)
            {
                radioSprite = def.radioSprite;
                return;
            }

#if UNITY_EDITOR
            string folder = activeFaction switch
            {
                FactionType.Anarquista => "Anarquista",
                FactionType.Capitalista => "Capitalista",
                FactionType.Comunista => "Comunista",
                FactionType.Fascista => "Facista",
                _ => "Capitalista"
            };

            string fileName = activeFaction switch
            {
                FactionType.Anarquista => "radio_menu_final_2.png",
                FactionType.Capitalista => "radio_menu_final_1.png",
                FactionType.Comunista => "radio_menu_final_4.png",
                FactionType.Fascista => "radio_menu_final_3.png",
                _ => "radio_menu_final_1.png"
            };

            string path = $"Assets/Art/Interfaces/{folder}/menu/{fileName}";
            radioSprite = LoadSpriteAsset(path);
#endif
        }

        private void AutoLoadKnobSprite()
        {
#if UNITY_EDITOR
            string folder = activeFaction switch
            {
                FactionType.Anarquista => "Anarquista",
                FactionType.Capitalista => "Capitalista",
                FactionType.Comunista => "Comunista",
                FactionType.Fascista => "Facista",
                _ => "Capitalista"
            };

            string buttonFile = activeFaction switch
            {
                FactionType.Anarquista => "botones (2).png",
                FactionType.Capitalista => "botones (4).png",
                FactionType.Comunista => "botones (1).png",
                FactionType.Fascista => "botones (2).png",
                _ => "botones (4).png"
            };

            string path = $"Assets/Art/Interfaces/{folder}/botones/{buttonFile}";
            knobButtonSprite = LoadSpriteAsset(path);
#endif
        }

#if UNITY_EDITOR
        private static Sprite LoadSpriteAsset(string path)
        {
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            if (allAssets != null)
            {
                foreach (var obj in allAssets)
                {
                    if (obj is Sprite s) return s;
                }
            }
            return null;
        }

        private static Texture2D LoadTextureAsset(string path)
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
#endif

        // ==========================================================================
        // VINCULACIÓN DE ELEMENTOS UI TOOLKIT
        // ==========================================================================
        private void QueryUIElements()
        {
            // Barra Superior de Recursos
            lblTopFaction = root.Q<Label>("lbl-top-faction");
            lblResGold    = root.Q<Label>("lbl-res-gold");
            lblResFuel    = root.Q<Label>("lbl-res-fuel");

            radioCabinetSkin   = root.Q<VisualElement>("radio-cabinet-skin");
            crtScreenContainer = root.Q<VisualElement>("crt-screen-container");

            // A. World Preview
            worldPreviewDisplay = root.Q<VisualElement>("world-preview-display");
            lblWorldStyle       = root.Q<Label>("lbl-world-style");
            lblWorldCoords      = root.Q<Label>("lbl-world-coords");
            lblWorldSeed        = root.Q<Label>("lbl-world-seed");

            // B. Map Structure
            lblMapName       = root.Q<Label>("lbl-map-name");
            lblThreatLevel   = root.Q<Label>("lbl-threat-level");
            mapSpriteElement = root.Q<VisualElement>("map-sprite-element");
            lblZoneName      = root.Q<Label>("lbl-zone-name");
            lblZoneId        = root.Q<Label>("lbl-zone-id");
            mapZoneTarget    = root.Q<VisualElement>("map-zone-target");

            // C. Radio & Terrain Menu
            lblRadioFrequency     = root.Q<Label>("lbl-radio-frequency");
            lblRadioState         = root.Q<Label>("lbl-radio-state");
            lblMissionId          = root.Q<Label>("lbl-mission-id");
            lblMissionTitle       = root.Q<Label>("lbl-mission-title");
            lblTacticalObjective  = root.Q<Label>("lbl-tactical-objective");
            lblTerrainDescription = root.Q<Label>("lbl-terrain-description");
            btnDeployMission      = root.Q<Button>("btn-deploy-mission");

            // Botones invisibles sobre los diales del arte
            btnSubdial  = root.Q<Button>("btn-subdial");
            btnMaindial = root.Q<Button>("btn-maindial");

            // D. Botones de la Barra Inferior
            btnNavCasino    = root.Q<Button>("btn-nav-casino");
            btnNavPeriodico = root.Q<Button>("btn-nav-periodico");
            btnNavMisiones  = root.Q<Button>("btn-nav-misiones");
            btnNavHangar    = root.Q<Button>("btn-nav-hangar");
            btnNavPasaporte = root.Q<Button>("btn-nav-pasaporte");

            // E. Paneles de Vista de los 5 Submenús
            viewCasino    = root.Q<VisualElement>("view-casino");
            viewPeriodico = root.Q<VisualElement>("view-periodico");
            viewMisiones  = root.Q<VisualElement>("view-misiones");
            viewHangar    = root.Q<VisualElement>("view-hangar");
            viewPasaporte = root.Q<VisualElement>("view-pasaporte");

            // F. Elementos de Vista 2: Periódico
            lblNewsDate          = root.Q<Label>("lbl-news-date");
            lblNewsEdition       = root.Q<Label>("lbl-news-edition");
            lblNewsHeadline      = root.Q<Label>("lbl-news-headline");
            lblNewsSubtitle      = root.Q<Label>("lbl-news-subtitle");
            newsPhotoImage       = root.Q<VisualElement>("news-photo-image");
            lblNewsPhotoCaption  = root.Q<Label>("lbl-news-photo-caption");
            lblNewsArticleText   = root.Q<Label>("lbl-news-article-text");
            lblNewsKills         = root.Q<Label>("lbl-news-kills");
            lblNewsLosses        = root.Q<Label>("lbl-news-losses");
            lblNewsAccuracy      = root.Q<Label>("lbl-news-accuracy");
            lblNewsSector        = root.Q<Label>("lbl-news-sector");
            lblNewsPageIndicator = root.Q<Label>("lbl-news-page-indicator");
            btnNewsPrev          = root.Q<Button>("btn-news-prev");
            btnNewsNext          = root.Q<Button>("btn-news-next");

            // G. Elementos de Vista 3: Hangar
            lblHangarWallet   = root.Q<Label>("lbl-hangar-wallet");
            lblHangarTier     = root.Q<Label>("lbl-hangar-tier");
            hangarPlaneDisplay = root.Q<VisualElement>("hangar-plane-display");
            barHstatFirepower = root.Q<VisualElement>("bar-hstat-firepower");
            lblHstatFirepower = root.Q<Label>("lbl-hstat-firepower");
            barHstatArmor     = root.Q<VisualElement>("bar-hstat-armor");
            lblHstatArmor     = root.Q<Label>("lbl-hstat-armor");
            barHstatSpeed     = root.Q<VisualElement>("bar-hstat-speed");
            lblHstatSpeed     = root.Q<Label>("lbl-hstat-speed");
            btnHangarUpgrade  = root.Q<Button>("btn-hangar-upgrade");
            btnHangarArmor    = root.Q<Button>("btn-hangar-armor");

            // G2. Carrusel de naves del Hangar
            btnHangarPrev = root.Q<Button>("btn-hangar-prev");
            btnHangarNext = root.Q<Button>("btn-hangar-next");
            hangarCarouselStage = root.Q<VisualElement>("hangar-carousel-stage") ?? root.Q<VisualElement>("hangar-carousel-bg");
            newspaperSwipeContainer = root.Q<VisualElement>("newspaper-content-columns") ?? root.Q<VisualElement>("view-periodico");
            hangarSlides = new VisualElement[HangarSlideCount];
            hangarDots   = new VisualElement[HangarSlideCount];
            for (int i = 0; i < HangarSlideCount; i++)
            {
                hangarSlides[i] = root.Q<VisualElement>($"hangar-slide-{i}");
                hangarDots[i]   = root.Q<VisualElement>($"hangar-dot-{i}");
            }

            // H. Elementos de Vista 4: Pasaporte
            lblPassPilotFaction = root.Q<Label>("lbl-pass-pilot-faction");
            lblPstatMissions    = root.Q<Label>("lbl-pstat-missions");
            lblPstatKills       = root.Q<Label>("lbl-pstat-kills");
            lblPstatAccuracy    = root.Q<Label>("lbl-pstat-accuracy");
            lblPstatHours       = root.Q<Label>("lbl-pstat-hours");
            lblPstatLosses      = root.Q<Label>("lbl-pstat-losses");
            lblPstatKd          = root.Q<Label>("lbl-pstat-kd");

            // I. Elementos de Vista 5: Casino / Tienda
            lblCasinoWallet     = root.Q<Label>("lbl-casino-wallet");
            lblCasinoStatus     = root.Q<Label>("lbl-casino-status");
            casinoRouletteWheel = root.Q<VisualElement>("casino-roulette-wheel");
            btnCasinoSpin       = root.Q<Button>("btn-casino-spin");
            btnBuyFuel          = root.Q<Button>("btn-buy-fuel");
            btnBuyAmmo          = root.Q<Button>("btn-buy-ammo");
            btnBuyEngine        = root.Q<Button>("btn-buy-engine");
        }

        private void RegisterCallbacks()
        {
            if (btnMaindial != null) btnMaindial.clicked += OnMainDialClicked;
            if (btnSubdial  != null) btnSubdial.clicked  += OnSubDialClicked;
            if (btnDeployMission != null) btnDeployMission.clicked += OnDeployMissionClicked;

            // Navegación de pestañas inferiores
            if (btnNavCasino    != null) btnNavCasino.clicked    += () => SwitchTab(MenuSubTab.Casino);
            if (btnNavPeriodico != null) btnNavPeriodico.clicked += () => SwitchTab(MenuSubTab.Periodico);
            if (btnNavMisiones  != null) btnNavMisiones.clicked  += () => SwitchTab(MenuSubTab.Misiones);
            if (btnNavHangar    != null) btnNavHangar.clicked    += () => SwitchTab(MenuSubTab.Hangar);
            if (btnNavPasaporte != null) btnNavPasaporte.clicked += () => SwitchTab(MenuSubTab.Pasaporte);

            // Periódico
            if (btnNewsPrev != null) btnNewsPrev.clicked += OnNewsPrevClicked;
            if (btnNewsNext != null) btnNewsNext.clicked += OnNewsNextClicked;

            // Hangar
            if (btnHangarUpgrade != null) btnHangarUpgrade.clicked += OnHangarUpgradeClicked;
            if (btnHangarArmor   != null) btnHangarArmor.clicked   += OnHangarArmorClicked;

            // Carrusel del Hangar
            if (btnHangarPrev != null) btnHangarPrev.clicked += OnHangarPrevClicked;
            if (btnHangarNext != null) btnHangarNext.clicked += OnHangarNextClicked;

            if (hangarCarouselStage != null)
            {
                hangarCarouselStage.RegisterCallback<PointerDownEvent>(OnHangarPointerDown);
                hangarCarouselStage.RegisterCallback<PointerMoveEvent>(OnHangarPointerMove);
                hangarCarouselStage.RegisterCallback<PointerUpEvent>(OnHangarPointerUp);
                hangarCarouselStage.RegisterCallback<PointerCancelEvent>(OnHangarPointerCancel);
            }

            if (newspaperSwipeContainer != null)
            {
                newspaperSwipeContainer.RegisterCallback<PointerDownEvent>(OnNewsPointerDown);
                newspaperSwipeContainer.RegisterCallback<PointerMoveEvent>(OnNewsPointerMove);
                newspaperSwipeContainer.RegisterCallback<PointerUpEvent>(OnNewsPointerUp);
                newspaperSwipeContainer.RegisterCallback<PointerCancelEvent>(OnNewsPointerCancel);
            }

            // Casino
            if (btnCasinoSpin != null) btnCasinoSpin.clicked += OnCasinoSpinClicked;
            if (btnBuyFuel    != null) btnBuyFuel.clicked    += OnBuyFuelClicked;
            if (btnBuyAmmo    != null) btnBuyAmmo.clicked    += OnBuyAmmoClicked;
            if (btnBuyEngine  != null) btnBuyEngine.clicked  += OnBuyEngineClicked;
        }

        private void UnregisterCallbacks()
        {
            if (btnMaindial != null) btnMaindial.clicked -= OnMainDialClicked;
            if (btnSubdial  != null) btnSubdial.clicked  -= OnSubDialClicked;
            if (btnDeployMission != null) btnDeployMission.clicked -= OnDeployMissionClicked;

            if (btnNewsPrev != null) btnNewsPrev.clicked -= OnNewsPrevClicked;
            if (btnNewsNext != null) btnNewsNext.clicked -= OnNewsNextClicked;

            if (btnHangarUpgrade != null) btnHangarUpgrade.clicked -= OnHangarUpgradeClicked;
            if (btnHangarArmor   != null) btnHangarArmor.clicked   -= OnHangarArmorClicked;

            // Carrusel del Hangar
            if (btnHangarPrev != null) btnHangarPrev.clicked -= OnHangarPrevClicked;
            if (btnHangarNext != null) btnHangarNext.clicked -= OnHangarNextClicked;

            if (hangarCarouselStage != null)
            {
                hangarCarouselStage.UnregisterCallback<PointerDownEvent>(OnHangarPointerDown);
                hangarCarouselStage.UnregisterCallback<PointerMoveEvent>(OnHangarPointerMove);
                hangarCarouselStage.UnregisterCallback<PointerUpEvent>(OnHangarPointerUp);
                hangarCarouselStage.UnregisterCallback<PointerCancelEvent>(OnHangarPointerCancel);
            }

            if (newspaperSwipeContainer != null)
            {
                newspaperSwipeContainer.UnregisterCallback<PointerDownEvent>(OnNewsPointerDown);
                newspaperSwipeContainer.UnregisterCallback<PointerMoveEvent>(OnNewsPointerMove);
                newspaperSwipeContainer.UnregisterCallback<PointerUpEvent>(OnNewsPointerUp);
                newspaperSwipeContainer.UnregisterCallback<PointerCancelEvent>(OnNewsPointerCancel);
            }

            if (btnCasinoSpin != null) btnCasinoSpin.clicked -= OnCasinoSpinClicked;
            if (btnBuyFuel    != null) btnBuyFuel.clicked    -= OnBuyFuelClicked;
            if (btnBuyAmmo    != null) btnBuyAmmo.clicked    -= OnBuyAmmoClicked;
            if (btnBuyEngine  != null) btnBuyEngine.clicked  -= OnBuyEngineClicked;
        }

        private void ApplyRadioVisual()
        {
            if (radioCabinetSkin != null)
            {
                if (radioSprite != null)
                {
                    radioCabinetSkin.style.backgroundImage = new StyleBackground(radioSprite);
                }
                else
                {
                    // Limpiar imagen en línea para permitir que la clase CSS radio-skin-{facción} actúe
                    radioCabinetSkin.style.backgroundImage = StyleKeyword.Null;
                }
            }

            // Aplicar la textura del botón a los diales interactivos
            if (knobButtonSprite != null)
            {
                if (btnSubdial != null) btnSubdial.style.backgroundImage = new StyleBackground(knobButtonSprite);
                if (btnMaindial != null) btnMaindial.style.backgroundImage = new StyleBackground(knobButtonSprite);
            }
            else
            {
                // Limpiar imagen en línea para permitir que la clase CSS knob-skin-{facción} actúe
                if (btnSubdial != null) btnSubdial.style.backgroundImage = StyleKeyword.Null;
                if (btnMaindial != null) btnMaindial.style.backgroundImage = StyleKeyword.Null;
            }
        }

        // ==========================================================================
        // CONTROL DE DIALES Y ROTACIÓN SEGÚN EL PATRÓN DE CADA FACCIÓN
        // ==========================================================================

        /// <summary>
        /// DIAL PRINCIPAL (DERECHO, btn-maindial): Salto de 3 en 3 MHz.
        /// </summary>
        public void OnMainDialClicked()
        {
            var def = GetFactionDefinition(activeFaction);
            if (def == null || def.stations.Count == 0) return;

            currentStationIndex = (currentStationIndex + 1) % def.stations.Count;
            currentSubMissionIndex = 0;

            PlaySound(knobClickSound, 1f);
            UpdateTuningDisplay();
            UpdateKnobRotations(true);
        }

        /// <summary>
        /// SUBDIAL (IZQUIERDO, btn-subdial): Navega entre los 4 sub-puntos (States y misiones).
        /// </summary>
        public void OnSubDialClicked()
        {
            var def = GetFactionDefinition(activeFaction);
            if (def == null || def.stations.Count == 0) return;

            var station = def.stations[currentStationIndex];
            if (station.subMissions == null || station.subMissions.Count == 0) return;

            currentSubMissionIndex = (currentSubMissionIndex + 1) % station.subMissions.Count;

            PlaySound(knobRatchetSound, 0.85f);
            UpdateTuningDisplay();
            UpdateKnobRotations(true);
        }

        /// <summary>
        /// Calcula el ángulo de giro del dial izquierdo (subdial / 4 States) según el patrón de cada facción.
        /// </summary>
        public float GetSubdialTargetAngle(FactionType faction, int stateIndex)
        {
            stateIndex = Mathf.Clamp(stateIndex, 0, 3);
            return faction switch
            {
                // Anarquista: Aguja de cuadrante (-60° a +60° en pasos de 40°)
                FactionType.Anarquista => -60f + (stateIndex * 40f),

                // Capitalista: Selector de baquelita de 4 posiciones (-45°, 0°, +45°, +90°)
                FactionType.Capitalista => -45f + (stateIndex * 45f),

                // Comunista: Selector industrial soviético (rotación de 90° en cuadrantes: 0°, 90°, 180°, 270°)
                FactionType.Comunista => stateIndex * 90f,

                // Fascista: Selector de precisión VE 301 (-45°, 0°, +45°, +90°)
                FactionType.Fascista => -45f + (stateIndex * 45f),

                _ => -45f + (stateIndex * 45f)
            };
        }

        /// <summary>
        /// Calcula el ángulo de giro del dial derecho (maindial / frecuencias) según el patrón y escala de cada radio.
        /// </summary>
        public float GetMaindialTargetAngle(FactionType faction, int stationIndex, int totalStations)
        {
            if (totalStations <= 1) return 0f;
            float t = (float)stationIndex / (totalStations - 1);

            return faction switch
            {
                // Anarquista: Sintonizador en abanico (86 MHz a 110 MHz) -> barrido de -60° a +60°
                FactionType.Anarquista => Mathf.Lerp(-60f, 60f, t),

                // Capitalista: Dial circular continuo multiescala -> barrido continuo de -120° a +120°
                FactionType.Capitalista => Mathf.Lerp(-120f, 120f, t),

                // Comunista: Escala horizontal lineal (88 MHz a 110 MHz) -> barrido de -55° a +55°
                FactionType.Comunista => Mathf.Lerp(-55f, 55f, t),

                // Fascista: Escala curva Volksempfänger VE 301 (86 MHz a 110 MHz) -> barrido de -60° a +60°
                FactionType.Fascista => Mathf.Lerp(-60f, 60f, t),

                _ => Mathf.Lerp(-90f, 90f, t)
            };
        }

        private void UpdateKnobRotations(bool animated)
        {
            var def = GetFactionDefinition(activeFaction);
            int totalStations = def != null ? def.stations.Count : 1;

            float targetSub = GetSubdialTargetAngle(activeFaction, currentSubMissionIndex);
            float targetMain = GetMaindialTargetAngle(activeFaction, currentStationIndex, totalStations);

            if (!animated || !Application.isPlaying)
            {
                currentSubdialAngle = targetSub;
                currentMaindialAngle = targetMain;
                if (btnSubdial != null) btnSubdial.style.rotate = new Rotate(Angle.Degrees(targetSub));
                if (btnMaindial != null) btnMaindial.style.rotate = new Rotate(Angle.Degrees(targetMain));
                return;
            }

            if (btnSubdial != null)
            {
                if (subdialAnimCoroutine != null) StopCoroutine(subdialAnimCoroutine);
                subdialAnimCoroutine = StartCoroutine(AnimateKnobRotate(btnSubdial, currentSubdialAngle, targetSub, angle => currentSubdialAngle = angle));
            }

            if (btnMaindial != null)
            {
                if (maindialAnimCoroutine != null) StopCoroutine(maindialAnimCoroutine);
                maindialAnimCoroutine = StartCoroutine(AnimateKnobRotate(btnMaindial, currentMaindialAngle, targetMain, angle => currentMaindialAngle = angle));
            }
        }

        private System.Collections.IEnumerator AnimateKnobRotate(VisualElement knob, float startAngle, float targetAngle, Action<float> onUpdate)
        {
            float elapsed = 0f;
            float duration = 0.18f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                float cur = Mathf.LerpAngle(startAngle, targetAngle, ease);
                knob.style.rotate = new Rotate(Angle.Degrees(cur));
                onUpdate?.Invoke(cur);
                yield return null;
            }
            knob.style.rotate = new Rotate(Angle.Degrees(targetAngle));
            onUpdate?.Invoke(targetAngle);
        }
        private void UpdateTuningDisplay()
        {
            var def = GetFactionDefinition(activeFaction);
            if (def == null || def.stations.Count == 0) return;

            currentStationIndex = Mathf.Clamp(currentStationIndex, 0, def.stations.Count - 1);
            var station = def.stations[currentStationIndex];

            if (station.subMissions == null || station.subMissions.Count == 0) return;
            currentSubMissionIndex = Mathf.Clamp(currentSubMissionIndex, 0, station.subMissions.Count - 1);
            var mission = station.subMissions[currentSubMissionIndex];

            string freqStr = $"{mission.subFrequency:F2} MHz";

            // 1. Labels de Radio y Terreno
            if (lblRadioFrequency     != null) lblRadioFrequency.text     = freqStr;
            if (lblRadioState         != null) lblRadioState.text         = mission.stateName;
            if (lblMissionId          != null) lblMissionId.text          = mission.missionId;
            if (lblMissionTitle       != null) lblMissionTitle.text       = mission.missionTitle;
            if (lblTacticalObjective  != null) lblTacticalObjective.text  = mission.tacticalObjective;
            if (lblTerrainDescription != null) lblTerrainDescription.text = mission.terrainDescription;

            // 2. Estructura Modular de Mapa
            var zone = mission.mapZone ?? new MapZoneData();
            if (lblMapName     != null) lblMapName.text     = $"MAPA: {zone.mapName.ToUpper()}";
            if (lblThreatLevel != null) lblThreatLevel.text = zone.threatLevel.ToUpper();
            if (lblZoneName    != null) lblZoneName.text    = $"ZONA: {zone.zoneName.ToUpper()} [{zone.zoneCoordinates.x}, {zone.zoneCoordinates.y}]";
            if (lblZoneId      != null) lblZoneId.text      = $"ID: {zone.zoneId}";

            if (mapSpriteElement != null)
            {
                if (zone.mapSprite != null)
                {
                    mapSpriteElement.style.backgroundImage = new StyleBackground(zone.mapSprite);
                    mapSpriteElement.style.display = DisplayStyle.Flex;
                }
                else
                {
                    mapSpriteElement.style.backgroundImage = null;
                    mapSpriteElement.style.display = DisplayStyle.None;
                }
            }

            if (mapZoneTarget != null)
            {
                float targetLeftPct = Mathf.Clamp((zone.zoneCoordinates.x / 100f) * 80f + 10f, 10f, 85f);
                float targetTopPct  = Mathf.Clamp((zone.zoneCoordinates.y / 100f) * 70f + 15f, 15f, 80f);
                mapZoneTarget.style.left = Length.Percent(targetLeftPct);
                mapZoneTarget.style.top  = Length.Percent(targetTopPct);
            }

            // 3. Vista de Generación del Mundo (WorldGen en color vivo)
            if (lblWorldStyle  != null) lblWorldStyle.text  = mission.terrainStyle.ToString().ToUpper();
            if (lblWorldCoords != null) lblWorldCoords.text = $"SECTOR: [{mission.worldWidth}x{mission.worldHeight}]";
            if (lblWorldSeed   != null) lblWorldSeed.text   = $"SEED: #{mission.seed}";

            GenerateWorldPreviewTexture(mission.seed, mission.terrainStyle, zone.zoneCoordinates);

            // 4. Sincronizar datos para que gameplay genere este mismo mapa
            SyncDeploymentData(def, station, mission);
        }

        private void SyncDeploymentData(FactionMenuDefinition def, RadioStationData station, RadioSubMissionData mission)
        {
            MissionDeploymentData.SetDeployment(
                seed: mission.seed,
                style: mission.terrainStyle,
                w: mission.worldWidth,
                h: mission.worldHeight,
                chunkSize: new Vector2Int(100, 100),
                sector: station.stationCallsign,
                freq: mission.subFrequency,
                quarter: currentSubMissionIndex,
                obj: mission.tacticalObjective,
                pPlanet: "Sixela",
                pFormation: mission.mapZone?.zoneName ?? "Zona Táctica",
                pFaction: def.factionDisplayName,
                pBiome: mission.terrainDescription,
                pWeather: "Operación de Vuelo Activa",
                pReport: mission.tacticalObjective
            );

            PlayerPrefs.SetInt("SavedWorldSeed", mission.seed);
            PlayerPrefs.SetInt("SavedTerrainStyle", (int)mission.terrainStyle);
            PlayerPrefs.SetInt("SavedFactionId", (int)activeFaction);
            PlayerPrefs.SetString("SavedFactionName", def.factionDisplayName);
            PlayerPrefs.SetString("SavedMissionId", mission.missionId);
            PlayerPrefs.Save();
        }

        private void GenerateWorldPreviewTexture(int seed, TerrainStyle style, Vector2Int zoneCoords)
        {
            // Zoom out total: Se renderiza el mapa longitudinal largo entero (100x300 tiles)
            int texW = 100;
            int texH = 300;

            if (generatedWorldTexture == null || generatedWorldTexture.width != texW || generatedWorldTexture.height != texH)
            {
                if (generatedWorldTexture != null) Destroy(generatedWorldTexture);
                generatedWorldTexture = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
                generatedWorldTexture.filterMode = FilterMode.Point;
                generatedWorldTexture.wrapMode = TextureWrapMode.Clamp;
            }

            CellType[,] grid = TerrainGenerator.Generate(texW, texH, 0.44f, seed, style);

            Color colOceanDeep   = new Color(0.04f, 0.12f, 0.22f, 1f); // Azul marino profundo
            Color colOceanCoast  = new Color(0.10f, 0.38f, 0.48f, 1f); // Turquesa costero
            Color colSand        = new Color(0.76f, 0.68f, 0.40f, 1f); // Arena dorada
            Color colGrass       = new Color(0.18f, 0.48f, 0.20f, 1f); // Verde táctico
            Color colPlateau     = new Color(0.42f, 0.52f, 0.30f, 1f); // Meseta / elevación
            Color colRoute       = new Color(1.00f, 0.85f, 0.20f, 1f); // Ruta militar

            Color[] pixels = new Color[texW * texH];

            for (int y = 0; y < texH; y++)
            {
                for (int x = 0; x < texW; x++)
                {
                    CellType cell = grid[x, y];
                    Color c;
                    switch (cell)
                    {
                        case CellType.Water:     c = colOceanCoast; break;
                        case CellType.Grass:     c = colGrass; break;
                        case CellType.Path:      c = colRoute; break;
                        case CellType.Structure: c = colPlateau; break;
                        case CellType.Bridge:    c = colSand; break;
                        default:                 c = colOceanDeep; break;
                    }

                    // Cuadrícula táctica longitudinal cada 20x50 tiles
                    if (x % 20 == 0 || y % 50 == 0)
                    {
                        c = Color.Lerp(c, Color.black, 0.30f);
                    }

                    pixels[y * texW + x] = c;
                }
            }

            // 1. Pista de despegue / base de operaciones al sur (inicio del vuelo en y = 15)
            int spawnX = texW / 2;
            int spawnY = 15;
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int px = Mathf.Clamp(spawnX + dx, 0, texW - 1);
                    int py = Mathf.Clamp(spawnY + dy, 0, texH - 1);
                    pixels[py * texW + px] = new Color(0.2f, 1.0f, 0.4f, 1f); // Verde fósforo
                }
            }

            // 2. Objetivo táctico final / jefe al norte (y = 285)
            int bossX = texW / 2;
            int bossY = 285;
            for (int dy = -3; dy <= 3; dy++)
            {
                for (int dx = -3; dx <= 3; dx++)
                {
                    if (Mathf.Abs(dx) == 3 || Mathf.Abs(dy) == 3 || (dx == 0 && dy == 0))
                    {
                        int px = Mathf.Clamp(bossX + dx, 0, texW - 1);
                        int py = Mathf.Clamp(bossY + dy, 0, texH - 1);
                        pixels[py * texW + px] = new Color(1.0f, 0.2f, 0.2f, 1f); // Rojo alerta
                    }
                }
            }

            // 3. Corredor aéreo proyectado (línea discontinua amarilla entre sur y norte)
            for (int y = spawnY + 4; y < bossY - 4; y += 4)
            {
                int lineX = Mathf.RoundToInt(Mathf.Lerp(spawnX, bossX, (float)(y - spawnY) / (bossY - spawnY)));
                pixels[y * texW + lineX] = new Color(1.0f, 0.9f, 0.3f, 0.85f);
            }

            // 4. Zona de misión seleccionada con cruz cian
            int targetX = Mathf.Clamp((zoneCoords.x * texW) / 100, 4, texW - 5);
            int targetY = Mathf.Clamp((zoneCoords.y * texH) / 100, 4, texH - 5);

            for (int dy = -3; dy <= 3; dy++)
            {
                for (int dx = -3; dx <= 3; dx++)
                {
                    if (Mathf.Abs(dx) == 3 || Mathf.Abs(dy) == 3)
                    {
                        pixels[(targetY + dy) * texW + (targetX + dx)] = new Color(0.3f, 0.85f, 1f, 1f);
                    }
                    else if (dx == 0 && dy == 0)
                    {
                        pixels[(targetY + dy) * texW + (targetX + dx)] = Color.white;
                    }
                }
            }

            generatedWorldTexture.SetPixels(pixels);
            generatedWorldTexture.Apply();

            if (worldPreviewDisplay != null)
            {
                worldPreviewDisplay.style.backgroundImage = Background.FromTexture2D(generatedWorldTexture);
            }
        }

        // ==========================================================================
        // DESPLIEGUE A GAMEPLAY
        // ==========================================================================
        public void OnDeployMissionClicked()
        {
            var def = GetFactionDefinition(activeFaction);
            if (def == null || def.stations.Count == 0) return;

            var station = def.stations[currentStationIndex];
            var mission = station.subMissions[currentSubMissionIndex];

            SyncDeploymentData(def, station, mission);

            PlaySound(knobClickSound, 1.25f);
            Debug.Log($"<b>[MainMenu]</b> ⚔️ Desplegando misión: {mission.missionId} ({mission.missionTitle}) en {gameplaySceneName}");
            SceneManager.LoadScene(gameplaySceneName);
        }

        // ==========================================================================
        // AUDIO PROCEDURAL
        // ==========================================================================
        private void SetupAudioSystem()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            knobClickSound   = CreateToneClip(frequency: 820f, duration: 0.045f, decay: 35f);
            knobRatchetSound = CreateToneClip(frequency: 440f, duration: 0.065f, decay: 25f);
        }

        private AudioClip CreateToneClip(float frequency, float duration, float decay)
        {
            int sampleRate = 44100;
            int count = (int)(sampleRate * duration);
            float[] data = new float[count];

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-decay * t);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * env * 0.45f;
            }

            var clip = AudioClip.Create("RadioClick", count, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void PlaySound(AudioClip clip, float pitch)
        {
            if (!enableProceduralAudio || audioSource == null || clip == null) return;
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip);
        }

        // ==========================================================================
        // DATOS DE FACCIONES Y MISIONES
        // ==========================================================================
        public FactionMenuDefinition GetFactionDefinition(FactionType type)
        {
            if (factions == null || factions.Count == 0) InitializeDefaultFactions();
            return factions.Find(f => f.factionType == type);
        }

        public void InitializeDefaultFactions()
        {
            factions = new List<FactionMenuDefinition>
            {
                CreateFactionConfig(FactionType.Anarquista, "ANARQUISTAS", "VANGUARDIA CLANDESTINA // LIBERTAD O CENIZAS", new Color(0.85f, 0.25f, 0.25f)),
                CreateFactionConfig(FactionType.Capitalista, "CAPITALISTAS", "CONSORCIO MONETARIO // SUPREMACÍA INDUSTRIAL", new Color(0.85f, 0.72f, 0.25f)),
                CreateFactionConfig(FactionType.Comunista, "COMUNISTAS", "COLECTIVO ROJO // MAQUINARIA DEL PUEBLO", new Color(0.95f, 0.20f, 0.20f)),
                CreateFactionConfig(FactionType.Fascista, "FASCISTAS", "DOCTRINA ZENIT // ORDEN DE ACERO", new Color(0.40f, 0.65f, 0.85f))
            };
        }

        private FactionMenuDefinition CreateFactionConfig(FactionType type, string name, string slogan, Color color)
        {
            var def = new FactionMenuDefinition
            {
                factionType = type,
                factionDisplayName = name,
                slogan = slogan,
                themeColor = color,
                accentColor = Color.white
            };

            InitializeFactionStations(def);
            return def;
        }

        public void InitializeFactionStations(FactionMenuDefinition def)
        {
            def.stations = new List<RadioStationData>();

            float[] baseFreqs = new float[] { 86.0f, 89.0f, 92.0f, 95.0f, 98.0f, 101.0f };

            for (int s = 0; s < baseFreqs.Length; s++)
            {
                float baseF = baseFreqs[s];
                string callsign = $"EMISORA #{Mathf.RoundToInt(baseF)} // SECTOR {s + 1}";
                var station = new RadioStationData(baseF, callsign);

                float[] subOffsets = new float[] { 0.10f, 0.30f, 0.60f, 0.90f };
                string[] stateNames = new string[] {
                    "STATE: ALERTA TEMPRANA",
                    "STATE: PATRULLA DE RECONOCIMIENTO",
                    "STATE: VANGUARDIA AÉREA",
                    "STATE: ASALTO A LA REFINERÍA"
                };

                for (int m = 0; m < subOffsets.Length; m++)
                {
                    float subFreq = baseF + subOffsets[m];
                    string missionId = $"MISIÓN {Mathf.RoundToInt(baseF)}-{m + 1}";
                    string missionTitle = GetThematicMissionTitle(def.factionType, s, m);
                    string terrainDesc = GetThematicTerrainDescription(def.factionType, s, m);

                    var zone = new MapZoneData(
                        id: $"ZONA-{(s + 1):D2}-{m + 1}",
                        name: $"Sector {def.factionDisplayName} #{s + 1}",
                        map: $"MAPA TÁCTICO {def.factionDisplayName}",
                        coords: new Vector2Int(20 + s * 12 + m * 5, 15 + m * 20),
                        threat: m >= 2 ? "AMENAZA: CRÍTICA" : "AMENAZA: MODERADA",
                        details: terrainDesc
                    );

                    int seed = 407949 + (int)def.factionType * 1000 + s * 50 + m * 7;
                    TerrainStyle style = (TerrainStyle)(s % 5);

                    station.subMissions.Add(new RadioSubMissionData(
                        subFreq,
                        stateNames[m],
                        missionId,
                        missionTitle,
                        terrainDesc,
                        $"NEUTRALIZAR AMENAZA EN FRECUENCIA {subFreq:F2} MHz",
                        zone,
                        seed,
                        style
                    ));
                }

                def.stations.Add(station);
            }
        }

        private string GetThematicMissionTitle(FactionType faction, int stationIdx, int subIdx)
        {
            string[][] titles = new string[][]
            {
                new string[] { "Emboscada en el Estuario", "Ruptura del Bloqueo Estatal", "Infiltración en el Hangar Central", "Sabotaje de la Red de Radar" },
                new string[] { "Custodia de Convoy Petrolero", "Operación Escudo de Refinería", "Contrato de Supresión Aérea", "Recuperación de Cargamento Estratégico" },
                new string[] { "Vanguardia de la Columna Roja", "Defensa de la Fábrica Colectiva", "Barrido de Cazas Enemigos", "Asalto al Complejo Industrial" },
                new string[] { "Patrulla del Muro de Hierro", "Supremacía Aérea en la Frontera", "Ataque Quirúrgico a la Costa", "Operación Dominio Absoluto" }
            };

            int fIdx = (int)faction;
            if (fIdx < 0 || fIdx >= titles.Length) fIdx = 0;
            return titles[fIdx][subIdx % 4];
        }

        private string GetThematicTerrainDescription(FactionType faction, int stationIdx, int subIdx)
        {
            string[] descriptions = new string[]
            {
                "Archipiélago fractal kárstico con cañones escarpados, niebla espesa y vientos cruzados de 35 nudos. Emplazamientos antiaéreos en las crestas.",
                "Valle fluvial inundado con densa vegetación de manglar y lluvias torrenciales. Visibilidad reducida a baja altitud.",
                "Mesetas basálticas elevadas sobre un mar embravecido. Fuertes corrientes térmicas ascendentes y riesgo de tormenta eléctrica.",
                "Estrecho marítimo fortificado con bancos de arena traicioneros y plataformas artilladas costeras de gran calibre."
            };

            return descriptions[(stationIdx + subIdx) % descriptions.Length];
        }

        // ==========================================================================
        // GESTIÓN DE SUBMENÚS Y BARRA INFERIOR (5 PESTAÑAS)
        // ==========================================================================

        /// <summary>
        /// Cambia la vista activa de la pantalla CRT entre las 5 opciones tácticas.
        /// </summary>
        public void SwitchTab(MenuSubTab targetTab)
        {
            currentActiveTab = targetTab;
            UpdateNavTabsVisual();

            // Ocultar todas las vistas
            if (viewCasino    != null) viewCasino.style.display    = DisplayStyle.None;
            if (viewPeriodico != null) viewPeriodico.style.display = DisplayStyle.None;
            if (viewMisiones  != null) viewMisiones.style.display  = DisplayStyle.None;
            if (viewHangar    != null) viewHangar.style.display    = DisplayStyle.None;
            if (viewPasaporte != null) viewPasaporte.style.display = DisplayStyle.None;

            // Mostrar la vista seleccionada y refrescar su contenido
            switch (targetTab)
            {
                case MenuSubTab.Casino:
                    if (viewCasino != null) viewCasino.style.display = DisplayStyle.Flex;
                    UpdateCasinoView();
                    break;

                case MenuSubTab.Periodico:
                    if (viewPeriodico != null) viewPeriodico.style.display = DisplayStyle.Flex;
                    var repList = HemerotecaManager.GetReports();
                    if (repList != null && repList.Count > 0)
                    {
                        currentReportIndex = UnityEngine.Random.Range(0, repList.Count);
                    }
                    UpdatePeriodicoView();
                    break;

                case MenuSubTab.Misiones:
                    if (viewMisiones != null) viewMisiones.style.display = DisplayStyle.Flex;
                    UpdateTuningDisplay();
                    break;

                case MenuSubTab.Hangar:
                    if (viewHangar != null) viewHangar.style.display = DisplayStyle.Flex;
                    UpdateHangarView();
                    break;

                case MenuSubTab.Pasaporte:
                    if (viewPasaporte != null) viewPasaporte.style.display = DisplayStyle.Flex;
                    UpdatePasaporteView();
                    break;
            }

            UpdateTopResourceBar();
            PlaySound(knobClickSound, 1.15f);
        }

        private void UpdateTopResourceBar()
        {
            var def = GetFactionDefinition(activeFaction);
            if (lblTopFaction != null && def != null)
            {
                lblTopFaction.text = $"{def.factionDisplayName} // 1944";
            }
            if (lblResGold != null)
            {
                lblResGold.text = $"{GetCoins():N0} ORO";
            }
            if (lblResFuel != null)
            {
                lblResFuel.text = $"{GetFuel()}/100";
            }
        }

        private void UpdateNavTabsVisual()
        {
            SetTabBtnActive(btnNavCasino,    currentActiveTab == MenuSubTab.Casino);
            SetTabBtnActive(btnNavPeriodico, currentActiveTab == MenuSubTab.Periodico);
            SetTabBtnActive(btnNavMisiones,  currentActiveTab == MenuSubTab.Misiones);
            SetTabBtnActive(btnNavHangar,    currentActiveTab == MenuSubTab.Hangar);
            SetTabBtnActive(btnNavPasaporte, currentActiveTab == MenuSubTab.Pasaporte);
        }

        private void SetTabBtnActive(Button btn, bool isActive)
        {
            if (btn == null) return;
            if (isActive)
            {
                if (!btn.ClassListContains("nav-tab-active")) btn.AddToClassList("nav-tab-active");
            }
            else
            {
                if (btn.ClassListContains("nav-tab-active")) btn.RemoveFromClassList("nav-tab-active");
            }
        }

        // ==========================================================================
        // VISTA 2: PERIÓDICO HISTÓRICO (HEMEROTECA)
        // ==========================================================================
        private void UpdatePeriodicoView()
        {
            var reports = HemerotecaManager.GetReports();
            if (reports == null || reports.Count == 0) return;

            currentReportIndex = Mathf.Clamp(currentReportIndex, 0, reports.Count - 1);
            var rep = reports[currentReportIndex];

            if (lblNewsDate     != null) lblNewsDate.text     = rep.date;
            if (lblNewsEdition  != null) lblNewsEdition.text  = rep.edition;
            if (lblNewsHeadline != null) lblNewsHeadline.text = !string.IsNullOrEmpty(rep.overrideHeadline) ? rep.overrideHeadline : $"CRÓNICA BÉLICA: {rep.missionName.ToUpper()}";
            if (lblNewsSubtitle != null) lblNewsSubtitle.text = !string.IsNullOrEmpty(rep.overrideSubtitle) ? rep.overrideSubtitle : rep.warReason;

            if (lblNewsPhotoCaption != null) lblNewsPhotoCaption.text = !string.IsNullOrEmpty(rep.photoCaption) ? rep.photoCaption : "Avión de combate sobre el sector.";
            if (lblNewsArticleText  != null) lblNewsArticleText.text  = !string.IsNullOrEmpty(rep.overrideStory) ? rep.overrideStory : $"Operación militar ejecutada en {rep.sectorName}. Impacto: {rep.civilianImpact}. Victoria: {(rep.isVictory ? "SÍ" : "NO")}.";

            if (lblNewsKills    != null) lblNewsKills.text    = $"{rep.enemiesDestroyed} CAZAS";
            if (lblNewsLosses   != null) lblNewsLosses.text   = $"{rep.alliedCasualties} AVIONES";
            if (lblNewsAccuracy != null) lblNewsAccuracy.text = $"{rep.accuracyPercentage:F0}%";
            if (lblNewsSector   != null) lblNewsSector.text   = rep.sectorName;

            if (lblNewsPageIndicator != null) lblNewsPageIndicator.text = $"CRÓNICA {currentReportIndex + 1} / {reports.Count}";
        }

        private void OnNewsPrevClicked()
        {
            var reports = HemerotecaManager.GetReports();
            if (reports == null || reports.Count == 0) return;

            currentReportIndex = (currentReportIndex - 1 + reports.Count) % reports.Count;
            PlaySound(knobRatchetSound, 1f);
            UpdatePeriodicoView();
        }

        private void OnNewsNextClicked()
        {
            var reports = HemerotecaManager.GetReports();
            if (reports == null || reports.Count == 0) return;

            currentReportIndex = (currentReportIndex + 1) % reports.Count;
            PlaySound(knobRatchetSound, 1.1f);
            UpdatePeriodicoView();
        }

        // ==========================================================================
        // VISTA 3: HANGAR DE AVIONES Y TALLER
        // ==========================================================================
        private void UpdateHangarView()
        {
            UpdateTopResourceBar();
            int coins = GetCoins();
            int gems = GetGems();
            int fuel = GetFuel();
            int level = PlayerPrefs.GetInt("AK47_PlaneLevel", 1);
            int firepower = PlayerPrefs.GetInt("AK47_PlaneFirepower", 7);
            int armor = PlayerPrefs.GetInt("AK47_PlaneArmor", 5);
            int speed = PlayerPrefs.GetInt("AK47_PlaneSpeed", 8);

            if (lblHangarWallet != null) lblHangarWallet.text = $"COMB: {fuel}/100 | ORO: {coins:N0} | GEMAS: {gems}";
            if (lblHangarTier   != null) lblHangarTier.text   = $"NIVEL {level} // CAZA ACTIVO";

            if (lblHstatFirepower != null) lblHstatFirepower.text = $"NVL {firepower}";
            if (barHstatFirepower != null) barHstatFirepower.style.width = Length.Percent(Mathf.Clamp(firepower * 10f, 10f, 100f));

            if (lblHstatArmor != null) lblHstatArmor.text = $"NVL {armor}";
            if (barHstatArmor != null) barHstatArmor.style.width = Length.Percent(Mathf.Clamp(armor * 10f, 10f, 100f));

            if (lblHstatSpeed != null) lblHstatSpeed.text = $"NVL {speed}";
            if (barHstatSpeed != null) barHstatSpeed.style.width = Length.Percent(Mathf.Clamp(speed * 10f, 10f, 100f));

            // Asegurar carrusel en slide 0 al entrar a la vista
            NavigateHangarCarousel(0);
        }

        private void OnHangarUpgradeClicked()
        {
            int cost = 250;
            if (GetCoins() >= cost)
            {
                AddCoins(-cost);
                int lvl = PlayerPrefs.GetInt("AK47_PlaneLevel", 1) + 1;
                int fp = Mathf.Min(PlayerPrefs.GetInt("AK47_PlaneFirepower", 7) + 1, 10);
                PlayerPrefs.SetInt("AK47_PlaneLevel", lvl);
                PlayerPrefs.SetInt("AK47_PlaneFirepower", fp);
                PlayerPrefs.Save();

                PlaySound(knobClickSound, 1.3f);
                UpdateHangarView();
            }
            else
            {
                PlaySound(knobRatchetSound, 0.6f);
            }
        }

        private void OnHangarArmorClicked()
        {
            int cost = 200;
            if (GetCoins() >= cost)
            {
                AddCoins(-cost);
                int arm = Mathf.Min(PlayerPrefs.GetInt("AK47_PlaneArmor", 5) + 1, 10);
                PlayerPrefs.SetInt("AK47_PlaneArmor", arm);
                PlayerPrefs.Save();

                PlaySound(knobClickSound, 1.2f);
                UpdateHangarView();
            }
            else
            {
                PlaySound(knobRatchetSound, 0.6f);
            }
        }

        // ==========================================================================
        // CARRUSEL DE NAVES DEL HANGAR
        // ==========================================================================

        private void OnHangarPrevClicked()
        {
            PlaySound(knobRatchetSound, 1.0f);
            NavigateHangarCarousel((currentHangarSlide - 1 + HangarSlideCount) % HangarSlideCount);
        }

        private void OnHangarNextClicked()
        {
            PlaySound(knobRatchetSound, 1.0f);
            NavigateHangarCarousel((currentHangarSlide + 1) % HangarSlideCount);
        }

        private void NavigateHangarCarousel(int targetSlide)
        {
            if (hangarSlides == null || hangarDots == null) return;

            // Ocultar slide actual y desactivar dot actual
            if (hangarSlides[currentHangarSlide] != null)
                hangarSlides[currentHangarSlide].RemoveFromClassList("hangar-slide-active");
            if (hangarDots[currentHangarSlide] != null)
                hangarDots[currentHangarSlide].RemoveFromClassList("hangar-dot-active");

            currentHangarSlide = targetSlide;

            // Mostrar nuevo slide y activar su dot
            if (hangarSlides[currentHangarSlide] != null)
                hangarSlides[currentHangarSlide].AddToClassList("hangar-slide-active");
            if (hangarDots[currentHangarSlide] != null)
                hangarDots[currentHangarSlide].AddToClassList("hangar-dot-active");
        }

        // ==========================================================================
        // GESTOS TÁCTILES / SWIPE PARA EL CARRUSEL DEL HANGAR Y EL PERIÓDICO
        // ==========================================================================
        private const float SwipeThreshold = 35f;

        private void OnHangarPointerDown(PointerDownEvent evt)
        {
            hangarDragStartX = evt.position.x;
            hangarDragStartY = evt.position.y;
            isHangarDragging = true;
            if (hangarCarouselStage != null)
            {
                hangarCarouselStage.CapturePointer(evt.pointerId);
            }
        }

        private void OnHangarPointerMove(PointerMoveEvent evt)
        {
            // Opcional para feedback visual durante arrastre si se desea
        }

        private void OnHangarPointerUp(PointerUpEvent evt)
        {
            if (!isHangarDragging) return;
            isHangarDragging = false;
            if (hangarCarouselStage != null)
            {
                hangarCarouselStage.ReleasePointer(evt.pointerId);
            }

            float deltaX = evt.position.x - hangarDragStartX;
            float deltaY = evt.position.y - hangarDragStartY;

            // Solo procesar si el gesto fue predominantemente horizontal
            if (Mathf.Abs(deltaX) > SwipeThreshold && Mathf.Abs(deltaX) > Mathf.Abs(deltaY))
            {
                if (deltaX < 0)
                {
                    OnHangarNextClicked(); // Deslizar hacia la izquierda -> siguiente
                }
                else
                {
                    OnHangarPrevClicked(); // Deslizar hacia la derecha -> anterior
                }
            }
        }

        private void OnHangarPointerCancel(PointerCancelEvent evt)
        {
            isHangarDragging = false;
            if (hangarCarouselStage != null)
            {
                hangarCarouselStage.ReleasePointer(evt.pointerId);
            }
        }

        private void OnNewsPointerDown(PointerDownEvent evt)
        {
            newsDragStartX = evt.position.x;
            newsDragStartY = evt.position.y;
            isNewsDragging = true;
            if (newspaperSwipeContainer != null)
            {
                newspaperSwipeContainer.CapturePointer(evt.pointerId);
            }
        }

        private void OnNewsPointerMove(PointerMoveEvent evt)
        {
        }

        private void OnNewsPointerUp(PointerUpEvent evt)
        {
            if (!isNewsDragging) return;
            isNewsDragging = false;
            if (newspaperSwipeContainer != null)
            {
                newspaperSwipeContainer.ReleasePointer(evt.pointerId);
            }

            float deltaX = evt.position.x - newsDragStartX;
            float deltaY = evt.position.y - newsDragStartY;

            if (Mathf.Abs(deltaX) > SwipeThreshold && Mathf.Abs(deltaX) > Mathf.Abs(deltaY))
            {
                if (deltaX < 0)
                {
                    OnNewsNextClicked();
                }
                else
                {
                    OnNewsPrevClicked();
                }
            }
        }

        private void OnNewsPointerCancel(PointerCancelEvent evt)
        {
            isNewsDragging = false;
            if (newspaperSwipeContainer != null)
            {
                newspaperSwipeContainer.ReleasePointer(evt.pointerId);
            }
        }

        // ==========================================================================
        // VISTA 4: PASAPORTE MILITAR // ESTADÍSTICAS DEL JUGADOR
        // ==========================================================================
        private void UpdatePasaporteView()
        {
            var def = GetFactionDefinition(activeFaction);
            string factionName = def != null ? def.factionDisplayName : "VANGUARDIA AÉREA";

            if (lblPassPilotFaction != null) lblPassPilotFaction.text = $"FACCIÓN: {factionName}";

            int missions = PlayerPrefs.GetInt("AK47_StatMissions", 28);
            int kills    = PlayerPrefs.GetInt("AK47_StatKills", 142);
            int losses   = PlayerPrefs.GetInt("AK47_StatLosses", 3);
            float acc    = PlayerPrefs.GetFloat("AK47_StatAccuracy", 86.4f);
            float hours  = PlayerPrefs.GetFloat("AK47_StatFlightHours", 18.7f);

            float kd = losses > 0 ? (float)kills / losses : kills;

            if (lblPstatMissions != null) lblPstatMissions.text = missions.ToString();
            if (lblPstatKills    != null) lblPstatKills.text    = kills.ToString();
            if (lblPstatAccuracy != null) lblPstatAccuracy.text = $"{acc:F1}%";
            if (lblPstatHours    != null) lblPstatHours.text    = $"{hours:F0}h {Mathf.RoundToInt((hours % 1f) * 60)}m";
            if (lblPstatLosses   != null) lblPstatLosses.text   = losses.ToString();
            if (lblPstatKd       != null) lblPstatKd.text       = $"{kd:F1}";
        }

        // ==========================================================================
        // VISTA 5: CASINO CLANDESTINO // TIENDA RETRO DE LOS AÑOS 40
        // ==========================================================================
        private void UpdateCasinoView()
        {
            UpdateTopResourceBar();
            int coins = GetCoins();
            int gems = GetGems();
            if (lblCasinoWallet != null) lblCasinoWallet.text = $"ORO: {coins:N0} | GEMAS: {gems}";
        }

        private void OnCasinoSpinClicked()
        {
            int cost = 100;
            if (GetCoins() < cost)
            {
                if (lblCasinoStatus != null) lblCasinoStatus.text = "¡Oro insuficiente para girar la ruleta!";
                PlaySound(knobRatchetSound, 0.6f);
                return;
            }

            AddCoins(-cost);
            UpdateCasinoView();

            if (btnCasinoSpin != null) btnCasinoSpin.SetEnabled(false);
            if (lblCasinoStatus != null) lblCasinoStatus.text = "★ Girando la ruleta clandestina... ★";

            if (rouletteAnimCoroutine != null) StopCoroutine(rouletteAnimCoroutine);
            rouletteAnimCoroutine = StartCoroutine(AnimateRouletteSpin(1.5f, () =>
            {
                if (btnCasinoSpin != null) btnCasinoSpin.SetEnabled(true);

                // Recompensa aleatoria
                int prizeType = UnityEngine.Random.Range(0, 4);
                string prizeMsg = "";

                switch (prizeType)
                {
                    case 0:
                        AddCoins(250);
                        prizeMsg = "¡PREMIO MENOR! +250 MONEDAS DE ORO";
                        break;
                    case 1:
                        AddCoins(500);
                        prizeMsg = "★ ¡GOLPE DE SUERTE! +500 MONEDAS DE ORO ★";
                        break;
                    case 2:
                        AddGems(5);
                        prizeMsg = "💎 ¡PREMIO DE ÉLITE! +5 GEMAS MILITARES 💎";
                        break;
                    case 3:
                        AddFuel(50);
                        prizeMsg = "⛽ ¡SUMINISTRO! +50 BIDONES DE COMBUSTIBLE";
                        break;
                }

                if (lblCasinoStatus != null) lblCasinoStatus.text = prizeMsg;
                PlaySound(knobClickSound, 1.4f);
                UpdateCasinoView();
            }));
        }

        private System.Collections.IEnumerator AnimateRouletteSpin(float duration, Action onComplete)
        {
            float elapsed = 0f;
            float startAngle = 0f;
            float totalAngle = UnityEngine.Random.Range(4, 7) * 360f + UnityEngine.Random.Range(30f, 330f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                float cur = Mathf.Lerp(startAngle, totalAngle, ease);

                if (casinoRouletteWheel != null)
                {
                    casinoRouletteWheel.style.rotate = new Rotate(Angle.Degrees(cur));
                }

                if (UnityEngine.Random.value < 0.2f)
                {
                    PlaySound(knobRatchetSound, UnityEngine.Random.Range(0.9f, 1.3f));
                }

                yield return null;
            }

            onComplete?.Invoke();
        }

        private void OnBuyFuelClicked()
        {
            int cost = 50;
            if (GetCoins() >= cost)
            {
                AddCoins(-cost);
                AddFuel(50);
                if (lblCasinoStatus != null) lblCasinoStatus.text = "¡Adquirido: +50 Bidones de Combustible!";
                PlaySound(knobClickSound, 1.2f);
                UpdateCasinoView();
            }
            else
            {
                if (lblCasinoStatus != null) lblCasinoStatus.text = "¡Oro insuficiente para comprar combustible!";
                PlaySound(knobRatchetSound, 0.6f);
            }
        }

        private void OnBuyAmmoClicked()
        {
            int cost = 120;
            if (GetCoins() >= cost)
            {
                AddCoins(-cost);
                if (lblCasinoStatus != null) lblCasinoStatus.text = "¡Adquirida: Caja de Munición Perforante (+20% Daño)!";
                PlaySound(knobClickSound, 1.25f);
                UpdateCasinoView();
            }
            else
            {
                if (lblCasinoStatus != null) lblCasinoStatus.text = "¡Oro insuficiente para munición!";
                PlaySound(knobRatchetSound, 0.6f);
            }
        }

        private void OnBuyEngineClicked()
        {
            int cost = 250;
            if (GetCoins() >= cost)
            {
                AddCoins(-cost);
                int fp = Mathf.Min(PlayerPrefs.GetInt("AK47_PlaneFirepower", 7) + 1, 10);
                PlayerPrefs.SetInt("AK47_PlaneFirepower", fp);
                PlayerPrefs.Save();
                if (lblCasinoStatus != null) lblCasinoStatus.text = "¡Pieza de Motor instalada en Hangar! (+1 Potencia)";
                PlaySound(knobClickSound, 1.3f);
                UpdateCasinoView();
            }
            else
            {
                if (lblCasinoStatus != null) lblCasinoStatus.text = "¡Oro insuficiente para pieza de motor!";
                PlaySound(knobRatchetSound, 0.6f);
            }
        }

        // ==========================================================================
        // HELPERS DE ECONOMÍA Y RECURSOS DEL PILOTO
        // ==========================================================================
        private int GetCoins() => PlayerPrefs.GetInt("AK47_PlayerCoins", 18940);

        private void AddCoins(int delta)
        {
            int c = Mathf.Max(0, GetCoins() + delta);
            PlayerPrefs.SetInt("AK47_PlayerCoins", c);
            PlayerPrefs.Save();
        }

        private int GetGems() => PlayerPrefs.GetInt("AK47_PlayerGems", 17);

        private void AddGems(int delta)
        {
            int g = Mathf.Max(0, GetGems() + delta);
            PlayerPrefs.SetInt("AK47_PlayerGems", g);
            PlayerPrefs.Save();
        }

        private int GetFuel() => PlayerPrefs.GetInt("AK47_PlayerFuel", 90);

        private void AddFuel(int delta)
        {
            int f = Mathf.Clamp(GetFuel() + delta, 0, 100);
            PlayerPrefs.SetInt("AK47_PlayerFuel", f);
            PlayerPrefs.Save();
        }
    }
}
