using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace WorldGen.Flow
{
    public class GameFlowController : MonoBehaviour
    {
        public enum FlowStep
        {
            BrandIntro,       // 1. Logo y Marca personalizable
            FactionSelection, // 2. Selección de las 4 facciones/naves
            StoryCinematic,   // 3. Cinemática vertical ascendente con misterio
            TutorialMission,  // 4. Misión tutorial jugable previa al menú general
            HangarReady       // 5. Carga al menú ya existente
        }

        [Header("⚙️ Paso Inicial para Pruebas en Editor")]
        public FlowStep startingStep = FlowStep.BrandIntro;

        [Header("🏷️ Personalización de Logo y Marca (Paso 1)")]
        public string studioBrandName = "REIKO ESTUDIOS";
        public string studioBrandSubtitle = "DIESELPUNK ENTERTAINMENT PRESENTA";
        public Color brandAccentColor = new Color(0.95f, 0.7f, 0.2f);
        public float brandDurationSeconds = 3.2f;

        [Header("🎮 Navegación y Escenas")]
        public string nextSceneName = "menu"; // Carga el menú ya existente al finalizar

        // Elementos UI generados en tiempo de ejecución
        private Canvas _canvas;
        private RectTransform _rootPanel;

        // Subpaneles
        private GameObject _brandPanelObj;
        private GameObject _factionPanelObj;
        private GameObject _cinematicPanelObj;
        private GameObject _tutorialPanelObj;

        // Estado interno
        private List<FactionPlaneDefinition> _factions;
        private int _selectedFactionIndex = 0;
        private bool _isTransitioning = false;

        // Faction Selection UI Elements
        private Text _txtFactionTitle;
        private Text _txtPlaneCode;
        private Text _txtPhilosophy;
        private Text _txtSlogan;
        private Image _imgPlaneDisplay;
        private Image _imgBgArtDisplay;
        private Text _txtStatEngine;
        private Text _txtStatArmor;
        private Text _txtStatAgility;
        private Text _txtStatDispersion;
        private List<Image> _factionCardBorders = new List<Image>();

        // Cinematic UI Elements
        private RectTransform _cinematicScrollContainer;
        private Text _txtCinematicStory;
        private Text _txtCinematicSubtitle;
        private Image _imgCinematicBackdrop;
        private float _cinematicScrollSpeed = 40f;
        private bool _cinematicPaused = false;
        private bool _cinematicFastForward = false;
        private Coroutine _cinematicRoutine;

        // Tutorial UI Elements
        private RectTransform _tutorialPlayerPlane;
        private Vector2 _tutorialPlanePos = new Vector2(0, -220);
        private List<RectTransform> _tutorialTargets = new List<RectTransform>();
        private List<RectTransform> _tutorialBullets = new List<RectTransform>();
        private int _targetsDestroyed = 0;
        private Text _txtTutorialInstructions;
        private bool _tutorialActive = false;

        private void Awake()
        {
            _factions = GameFlowDataStore.GetAllFactions();
            SetupCanvasAndUI();
        }

        private void Start()
        {
            SetStep(startingStep);
        }

        private void Update()
        {
            if (_tutorialActive)
            {
                UpdateTutorialControls();
            }
        }

        private void SetupCanvasAndUI()
        {
            // Crear o buscar Canvas
            GameObject canvasObj = new GameObject("GameFlow_Canvas");
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 999; // Siempre al frente

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Portrait mobile / adaptable
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Root Panel
            GameObject rootObj = new GameObject("RootPanel", typeof(RectTransform));
            rootObj.transform.SetParent(canvasObj.transform, false);
            _rootPanel = rootObj.GetComponent<RectTransform>();
            _rootPanel.anchorMin = Vector2.zero;
            _rootPanel.anchorMax = Vector2.one;
            _rootPanel.offsetMin = Vector2.zero;
            _rootPanel.offsetMax = Vector2.zero;

            BuildBrandPanel();
            BuildFactionPanel();
            BuildCinematicPanel();
            BuildTutorialPanel();
        }

        // =========================================================================
        // 1. PANTALLA DE LOGO Y MARCA (Personalizable)
        // =========================================================================
        private void BuildBrandPanel()
        {
            _brandPanelObj = CreateSubPanel("Panel_Brand", new Color(0.05f, 0.05f, 0.06f, 1f));

            // Círculo Decorativo Mecánico (Engranaje simulado)
            GameObject ringObj = CreateUIObject("Brand_Ring", _brandPanelObj.transform);
            Image ringImg = ringObj.AddComponent<Image>();
            ringImg.sprite = ProceduralShapeGenerator.GetCircleSprite();
            ringImg.color = new Color(brandAccentColor.r, brandAccentColor.g, brandAccentColor.b, 0.15f);
            SetRect(ringObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 120), new Vector2(360, 360));

            // Cuadrado / Diamante interior del logo
            GameObject logoDiamond = CreateUIObject("Brand_Diamond", _brandPanelObj.transform);
            Image diamondImg = logoDiamond.AddComponent<Image>();
            diamondImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            diamondImg.color = brandAccentColor;
            SetRect(logoDiamond.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 120), new Vector2(140, 140));
            logoDiamond.transform.localRotation = Quaternion.Euler(0, 0, 45);

            // Icono central (Círculo negro de contraste)
            GameObject innerCircle = CreateUIObject("Brand_Inner", logoDiamond.transform);
            Image innerCircleImg = innerCircle.AddComponent<Image>();
            innerCircleImg.sprite = ProceduralShapeGenerator.GetCircleSprite();
            innerCircleImg.color = new Color(0.08f, 0.08f, 0.09f, 1f);
            SetRect(innerCircle.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, Vector2.zero, new Vector2(60, 60));

            // Texto Subtítulo Superior
            GameObject subTextObj = CreateUIObject("Brand_Subtitle", _brandPanelObj.transform);
            Text subText = subTextObj.AddComponent<Text>();
            subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subText.text = studioBrandSubtitle;
            subText.fontSize = 26;
            subText.alignment = TextAnchor.MiddleCenter;
            subText.color = new Color(0.7f, 0.7f, 0.75f, 0.9f);
            SetRect(subTextObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -60), new Vector2(800, 60));

            // Texto Nombre del Estudio / Marca Principal
            GameObject titleObj = CreateUIObject("Brand_Title", _brandPanelObj.transform);
            Text titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.text = studioBrandName;
            titleText.fontSize = 58;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = brandAccentColor;
            SetRect(titleObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -130), new Vector2(900, 90));

            // Botón interactivo para saltar logo al pulsar la pantalla
            Button brandBtn = _brandPanelObj.AddComponent<Button>();
            brandBtn.transition = Selectable.Transition.None;
            brandBtn.onClick.AddListener(() =>
            {
                if (!_isTransitioning) StartCoroutine(TransitionToStep(FlowStep.FactionSelection));
            });
        }

        // =========================================================================
        // 2. SELECCIÓN DE LAS CUATRO FACCIONES / NAVES
        // =========================================================================
        private void BuildFactionPanel()
        {
            _factionPanelObj = CreateSubPanel("Panel_FactionSelection", new Color(0.08f, 0.08f, 0.10f, 1f));

            // Encabezado Superior
            GameObject headerObj = CreateUIObject("Header_Title", _factionPanelObj.transform);
            Text headerText = headerObj.AddComponent<Text>();
            headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerText.text = "REGISTRO DE VUELO // SELECCIÓN DE VECTOR";
            headerText.fontSize = 28;
            headerText.fontStyle = FontStyle.Bold;
            headerText.alignment = TextAnchor.MiddleCenter;
            headerText.color = new Color(0.8f, 0.85f, 0.9f);
            SetRect(headerObj.GetComponent<RectTransform>(), 0.5f, 1f, 0.5f, 1f, new Vector2(0, -80), new Vector2(900, 60));

            // Recuadro Central: Ilustración / Telón de Fondo de la Facción
            GameObject bgArtObj = CreateUIObject("Bg_Art_Frame", _factionPanelObj.transform);
            _imgBgArtDisplay = bgArtObj.AddComponent<Image>();
            _imgBgArtDisplay.sprite = ProceduralShapeGenerator.GetSquareSprite();
            _imgBgArtDisplay.color = new Color(0.15f, 0.12f, 0.10f, 0.95f);
            SetRect(bgArtObj.GetComponent<RectTransform>(), 0.5f, 0.72f, 0.5f, 0.72f, Vector2.zero, new Vector2(940, 420));

            // Borde decorativo del marco
            GameObject bgArtBorder = CreateUIObject("Bg_Art_Border", bgArtObj.transform);
            Image borderImg = bgArtBorder.AddComponent<Image>();
            borderImg.sprite = ProceduralShapeGenerator.CreateBorderFrameSprite(256, 128, 4);
            borderImg.color = new Color(0.9f, 0.75f, 0.4f, 0.5f);
            SetRect(bgArtBorder.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            // Silueta de la nave (Cuadrados y círculos simulando fuselaje)
            GameObject planeContainer = CreateUIObject("Plane_Visual", bgArtObj.transform);
            _imgPlaneDisplay = planeContainer.AddComponent<Image>();
            _imgPlaneDisplay.sprite = ProceduralShapeGenerator.GetSquareSprite();
            _imgPlaneDisplay.color = Color.white;
            SetRect(planeContainer.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 20), new Vector2(160, 160));

            // Alas simuladas
            GameObject wingsObj = CreateUIObject("Plane_Wings", planeContainer.transform);
            Image wingsImg = wingsObj.AddComponent<Image>();
            wingsImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            wingsImg.color = new Color(1f, 1f, 1f, 0.6f);
            SetRect(wingsObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, Vector2.zero, new Vector2(320, 40));

            // Motor simulado (Círculo)
            GameObject engineObj = CreateUIObject("Plane_Engine", planeContainer.transform);
            Image engineImg = engineObj.AddComponent<Image>();
            engineImg.sprite = ProceduralShapeGenerator.GetCircleSprite();
            engineImg.color = new Color(1f, 0.4f, 0.1f, 0.8f);
            SetRect(engineObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -70), new Vector2(60, 60));

            // Nombre y Código de la Aeronave
            GameObject planeCodeObj = CreateUIObject("Txt_PlaneCode", bgArtObj.transform);
            _txtPlaneCode = planeCodeObj.AddComponent<Text>();
            _txtPlaneCode.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _txtPlaneCode.fontSize = 36;
            _txtPlaneCode.fontStyle = FontStyle.Bold;
            _txtPlaneCode.alignment = TextAnchor.UpperCenter;
            _txtPlaneCode.color = Color.white;
            SetRect(planeCodeObj.GetComponent<RectTransform>(), 0.5f, 0.15f, 0.5f, 0.15f, Vector2.zero, new Vector2(800, 50));

            // Lema de la facción
            GameObject sloganObj = CreateUIObject("Txt_Slogan", bgArtObj.transform);
            _txtSlogan = sloganObj.AddComponent<Text>();
            _txtSlogan.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _txtSlogan.fontSize = 22;
            _txtSlogan.fontStyle = FontStyle.Italic;
            _txtSlogan.alignment = TextAnchor.MiddleCenter;
            _txtSlogan.color = new Color(0.85f, 0.85f, 0.7f);
            SetRect(sloganObj.GetComponent<RectTransform>(), 0.5f, 0.05f, 0.5f, 0.05f, Vector2.zero, new Vector2(860, 40));

            // Selector horizontal de 4 botones para las 4 facciones
            GameObject cardsContainer = CreateUIObject("Faction_Cards_Row", _factionPanelObj.transform);
            SetRect(cardsContainer.GetComponent<RectTransform>(), 0.5f, 0.49f, 0.5f, 0.49f, Vector2.zero, new Vector2(960, 110));

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                GameObject cardBtnObj = CreateUIObject($"Btn_Faction_{i}", cardsContainer.transform);
                Image cardBg = cardBtnObj.AddComponent<Image>();
                cardBg.sprite = ProceduralShapeGenerator.GetSquareSprite();
                cardBg.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);

                float posX = -360f + (index * 240f);
                SetRect(cardBtnObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(posX, 0), new Vector2(210, 90));

                Button btn = cardBtnObj.AddComponent<Button>();
                btn.onClick.AddListener(() => SelectFaction(index));

                // Borde para marcar cuál está seleccionada
                GameObject cardBorder = CreateUIObject("Border", cardBtnObj.transform);
                Image border = cardBorder.AddComponent<Image>();
                border.sprite = ProceduralShapeGenerator.CreateBorderFrameSprite(128, 64, 4);
                border.color = (i == 0) ? Color.yellow : new Color(0.4f, 0.4f, 0.4f, 0.5f);
                SetRect(cardBorder.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);
                _factionCardBorders.Add(border);

                // Texto con el número romano y siglas
                GameObject cardTxtObj = CreateUIObject("Label", cardBtnObj.transform);
                Text cardTxt = cardTxtObj.AddComponent<Text>();
                cardTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                cardTxt.text = $"[ 0{i + 1} ]\n{_factions[i].planeCode.Split(' ')[0]}";
                cardTxt.fontSize = 20;
                cardTxt.fontStyle = FontStyle.Bold;
                cardTxt.alignment = TextAnchor.MiddleCenter;
                cardTxt.color = Color.white;
                SetRect(cardTxtObj.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);
            }

            // Recuadro Inferior Enmarcado: Filosofía Mecánica y Estadísticas
            GameObject statsBoxObj = CreateUIObject("Stats_Enclosure", _factionPanelObj.transform);
            Image statsBoxImg = statsBoxObj.AddComponent<Image>();
            statsBoxImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            statsBoxImg.color = new Color(0.11f, 0.11f, 0.13f, 0.95f);
            SetRect(statsBoxObj.GetComponent<RectTransform>(), 0.5f, 0.28f, 0.5f, 0.28f, Vector2.zero, new Vector2(940, 240));

            GameObject statsBorder = CreateUIObject("Stats_Border", statsBoxObj.transform);
            Image statsBrdImg = statsBorder.AddComponent<Image>();
            statsBrdImg.sprite = ProceduralShapeGenerator.CreateBorderFrameSprite(256, 128, 3);
            statsBrdImg.color = new Color(0.6f, 0.6f, 0.65f, 0.4f);
            SetRect(statsBorder.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            // Texto de Filosofía Mecánica
            GameObject philObj = CreateUIObject("Txt_Philosophy", statsBoxObj.transform);
            _txtPhilosophy = philObj.AddComponent<Text>();
            _txtPhilosophy.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _txtPhilosophy.fontSize = 21;
            _txtPhilosophy.alignment = TextAnchor.UpperLeft;
            _txtPhilosophy.color = new Color(0.85f, 0.85f, 0.85f);
            SetRect(philObj.GetComponent<RectTransform>(), 0.5f, 0.68f, 0.5f, 0.68f, Vector2.zero, new Vector2(880, 100));

            // Indicadores de Estadísticas (Motor, Blindaje, Agilidad, Dispersión)
            GameObject statRow = CreateUIObject("Stat_Row", statsBoxObj.transform);
            SetRect(statRow.GetComponent<RectTransform>(), 0.5f, 0.22f, 0.5f, 0.22f, Vector2.zero, new Vector2(880, 60));

            _txtStatEngine = CreateStatIndicator("POTENCIA", statRow.transform, -330f);
            _txtStatArmor = CreateStatIndicator("BLINDAJE", statRow.transform, -110f);
            _txtStatAgility = CreateStatIndicator("AGILIDAD", statRow.transform, 110f);
            _txtStatDispersion = CreateStatIndicator("DISPERSIÓN", statRow.transform, 330f);

            // Botón de Confirmación / Continuar a Cinemática
            GameObject confirmBtnObj = CreateUIObject("Btn_Confirm_Faction", _factionPanelObj.transform);
            Image confirmImg = confirmBtnObj.AddComponent<Image>();
            confirmImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            confirmImg.color = new Color(0.85f, 0.55f, 0.15f);
            SetRect(confirmBtnObj.GetComponent<RectTransform>(), 0.5f, 0.08f, 0.5f, 0.08f, Vector2.zero, new Vector2(620, 85));

            Button confirmBtn = confirmBtnObj.AddComponent<Button>();
            confirmBtn.onClick.AddListener(() =>
            {
                ProceduralAudio.PlayConfirm();
                StartCoroutine(TransitionToStep(FlowStep.StoryCinematic));
            });

            GameObject confirmTxtObj = CreateUIObject("Label", confirmBtnObj.transform);
            Text confirmTxt = confirmTxtObj.AddComponent<Text>();
            confirmTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            confirmTxt.text = "CONFIRMAR AERONAVE ➔";
            confirmTxt.fontSize = 30;
            confirmTxt.fontStyle = FontStyle.Bold;
            confirmTxt.alignment = TextAnchor.MiddleCenter;
            confirmTxt.color = Color.black;
            SetRect(confirmTxtObj.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            // Refrescar con datos de la primera facción
            RefreshFactionUI();
        }

        private Text CreateStatIndicator(string label, Transform parent, float posX)
        {
            GameObject container = CreateUIObject("Stat_" + label, parent);
            SetRect(container.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(posX, 0), new Vector2(180, 50));

            GameObject lblObj = CreateUIObject("Lbl", container.transform);
            Text lbl = lblObj.AddComponent<Text>();
            lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lbl.text = label;
            lbl.fontSize = 17;
            lbl.fontStyle = FontStyle.Bold;
            lbl.alignment = TextAnchor.UpperCenter;
            lbl.color = new Color(0.6f, 0.6f, 0.65f);
            SetRect(lblObj.GetComponent<RectTransform>(), 0.5f, 0.7f, 0.5f, 0.7f, Vector2.zero, new Vector2(180, 25));

            GameObject valObj = CreateUIObject("Val", container.transform);
            Text val = valObj.AddComponent<Text>();
            val.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            val.fontSize = 24;
            val.fontStyle = FontStyle.Bold;
            val.alignment = TextAnchor.LowerCenter;
            val.color = Color.white;
            SetRect(valObj.GetComponent<RectTransform>(), 0.5f, 0.2f, 0.5f, 0.2f, Vector2.zero, new Vector2(180, 30));

            return val;
        }

        private void SelectFaction(int index)
        {
            _selectedFactionIndex = index;
            GameFlowDataStore.SelectedFaction = _factions[_selectedFactionIndex];
            ProceduralAudio.PlayTick();
            RefreshFactionUI();
        }

        private void RefreshFactionUI()
        {
            var f = _factions[_selectedFactionIndex];
            _txtPlaneCode.text = f.planeCode;
            _txtPlaneCode.color = f.accentColor;
            _txtSlogan.text = f.slogan;
            _txtPhilosophy.text = f.mechanicalPhilosophy;

            _imgBgArtDisplay.color = f.backgroundColor;
            _imgPlaneDisplay.color = f.primaryColor;

            _txtStatEngine.text = $"{f.enginePower:F1} / 10";
            _txtStatArmor.text = $"{f.armorResistance:F1} / 10";
            _txtStatAgility.text = $"{f.agilityHandling:F1} / 10";
            _txtStatDispersion.text = $"{f.fireDispersion:F1} / 10";

            for (int i = 0; i < _factionCardBorders.Count; i++)
            {
                _factionCardBorders[i].color = (i == _selectedFactionIndex) ? f.accentColor : new Color(0.4f, 0.4f, 0.4f, 0.4f);
            }
        }

        // =========================================================================
        // 3. CINEMÁTICA CON TEXTO ASCENDENTE Y FONDOS
        // =========================================================================
        private void BuildCinematicPanel()
        {
            _cinematicPanelObj = CreateSubPanel("Panel_Cinematic", new Color(0.04f, 0.04f, 0.05f, 1f));

            // Fondo ilustrado procedural que cambia con cada fase de la cinemática
            GameObject backdropObj = CreateUIObject("Cinematic_Backdrop", _cinematicPanelObj.transform);
            _imgCinematicBackdrop = backdropObj.AddComponent<Image>();
            _imgCinematicBackdrop.sprite = ProceduralShapeGenerator.GetSquareSprite();
            _imgCinematicBackdrop.color = new Color(0.08f, 0.08f, 0.12f, 0.7f);
            SetRect(backdropObj.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            // Máscara central de proyección de lectura estilo documental
            GameObject maskObj = CreateUIObject("ScrollMask", _cinematicPanelObj.transform);
            maskObj.AddComponent<RectMask2D>();
            SetRect(maskObj.GetComponent<RectTransform>(), 0.5f, 0.55f, 0.5f, 0.55f, Vector2.zero, new Vector2(880, 1100));

            // Contenedor que ascenderá verticalmente
            GameObject scrollContainerObj = CreateUIObject("ScrollContainer", maskObj.transform);
            _cinematicScrollContainer = scrollContainerObj.GetComponent<RectTransform>();
            SetRect(_cinematicScrollContainer, 0.5f, 0f, 0.5f, 0f, new Vector2(0, -600), new Vector2(840, 2000));

            // Texto de la historia
            GameObject storyTextObj = CreateUIObject("Txt_Story", scrollContainerObj.transform);
            _txtCinematicStory = storyTextObj.AddComponent<Text>();
            _txtCinematicStory.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _txtCinematicStory.fontSize = 28;
            _txtCinematicStory.lineSpacing = 1.35f;
            _txtCinematicStory.alignment = TextAnchor.UpperCenter;
            _txtCinematicStory.color = new Color(0.95f, 0.95f, 0.88f);
            SetRect(storyTextObj.GetComponent<RectTransform>(), 0.5f, 1f, 0.5f, 1f, Vector2.zero, new Vector2(840, 1800));

            // Barra Inferior de Controles (Pausar, Acelerar, Saltar)
            GameObject controlsBar = CreateUIObject("Cinematic_Controls", _cinematicPanelObj.transform);
            SetRect(controlsBar.GetComponent<RectTransform>(), 0.5f, 0.08f, 0.5f, 0.08f, Vector2.zero, new Vector2(940, 80));

            // Botón Pausar / Reanudar
            CreateButton("Btn_Pause", "⏸ PAUSAR", controlsBar.transform, -300f, 220f, () =>
            {
                _cinematicPaused = !_cinematicPaused;
                ProceduralAudio.PlayTick();
            });

            // Botón Acelerar
            CreateButton("Btn_FastForward", "⏩ ACELERAR", controlsBar.transform, 0f, 240f, () =>
            {
                _cinematicFastForward = !_cinematicFastForward;
                ProceduralAudio.PlayTick();
            });

            // Botón Saltar
            CreateButton("Btn_Skip", "SALTO RÁPIDO ➔", controlsBar.transform, 300f, 240f, () =>
            {
                ProceduralAudio.PlayConfirm();
                EndCinematic();
            });
        }

        private void StartCinematicForFaction(FactionPlaneDefinition faction)
        {
            if (_cinematicRoutine != null) StopCoroutine(_cinematicRoutine);
            _cinematicRoutine = StartCoroutine(CinematicScrollRoutine(faction));
        }

        private IEnumerator CinematicScrollRoutine(FactionPlaneDefinition faction)
        {
            // Unir todos los párrafos de la facción con separadores atmosféricos
            string fullStory = string.Join("\n\n---\n\n", faction.cinematicParagraphs);
            _txtCinematicStory.text = fullStory;

            float yPos = -600f;
            _cinematicScrollContainer.anchoredPosition = new Vector2(0, yPos);
            float targetY = 1600f;

            int bgIndex = 0;
            float bgTimer = 0f;

            while (yPos < targetY)
            {
                if (!_cinematicPaused)
                {
                    float currentSpeed = _cinematicFastForward ? (_cinematicScrollSpeed * 3.5f) : _cinematicScrollSpeed;
                    yPos += currentSpeed * Time.deltaTime;
                    _cinematicScrollContainer.anchoredPosition = new Vector2(0, yPos);

                    bgTimer += Time.deltaTime;
                    if (bgTimer > 5.0f && faction.backgroundMockNames.Count > 0)
                    {
                        bgTimer = 0f;
                        bgIndex = (bgIndex + 1) % faction.backgroundMockNames.Count;
                        _imgCinematicBackdrop.color = Color.Lerp(faction.backgroundColor, Color.black, (bgIndex % 2 == 0) ? 0.3f : 0.6f);
                    }
                }
                yield return null;
            }

            EndCinematic();
        }

        private void EndCinematic()
        {
            if (_cinematicRoutine != null)
            {
                StopCoroutine(_cinematicRoutine);
                _cinematicRoutine = null;
            }
            StartCoroutine(TransitionToStep(FlowStep.TutorialMission));
        }

        // =========================================================================
        // 4. PASO TUTORIAL Y MISIÓN PREVIA
        // =========================================================================
        private void BuildTutorialPanel()
        {
            _tutorialPanelObj = CreateSubPanel("Panel_Tutorial", new Color(0.06f, 0.08f, 0.07f, 1f));

            // Instrucciones en pantalla
            GameObject instrObj = CreateUIObject("Txt_Tutorial_Instruction", _tutorialPanelObj.transform);
            _txtTutorialInstructions = instrObj.AddComponent<Text>();
            _txtTutorialInstructions.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _txtTutorialInstructions.text = "FASE DE PRUEBA DE BALÍSTICA // TOCA / HAZ CLIC PARA DISPARAR Y DESTRUIR 3 OBJETIVOS";
            _txtTutorialInstructions.fontSize = 24;
            _txtTutorialInstructions.fontStyle = FontStyle.Bold;
            _txtTutorialInstructions.alignment = TextAnchor.MiddleCenter;
            _txtTutorialInstructions.color = new Color(0.9f, 0.9f, 0.6f);
            SetRect(instrObj.GetComponent<RectTransform>(), 0.5f, 0.9f, 0.5f, 0.9f, Vector2.zero, new Vector2(920, 60));

            // Zona de Vuelo interactiva
            GameObject flightZone = CreateUIObject("FlightZone", _tutorialPanelObj.transform);
            SetRect(flightZone.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            // Avión del jugador (Generado procedimentalmente)
            GameObject playerObj = CreateUIObject("Player_Plane_Tutorial", flightZone.transform);
            _tutorialPlayerPlane = playerObj.GetComponent<RectTransform>();
            Image planeImg = playerObj.AddComponent<Image>();
            planeImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            planeImg.color = Color.white;
            SetRect(_tutorialPlayerPlane, 0.5f, 0.5f, 0.5f, 0.5f, _tutorialPlanePos, new Vector2(90, 90));

            // Crear 3 objetivos de entrenamiento (Círculos rojos flotantes)
            for (int i = 0; i < 3; i++)
            {
                GameObject targetObj = CreateUIObject($"Target_{i}", flightZone.transform);
                RectTransform rt = targetObj.GetComponent<RectTransform>();
                Image targetImg = targetObj.AddComponent<Image>();
                targetImg.sprite = ProceduralShapeGenerator.GetCircleSprite();
                targetImg.color = new Color(0.9f, 0.25f, 0.25f);

                float posX = -280f + (i * 280f);
                float posY = 240f + (i % 2 == 0 ? 60f : -40f);
                SetRect(rt, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(posX, posY), new Vector2(80, 80));
                _tutorialTargets.Add(rt);
            }
        }

        private void UpdateTutorialControls()
        {
            // Mover el avión suavemente con ratón/táctil horizontalmente
            if (Input.GetMouseButton(0))
            {
                Vector2 screenPos = Input.mousePosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootPanel, screenPos, null, out Vector2 localPoint);
                _tutorialPlanePos.x = Mathf.Lerp(_tutorialPlanePos.x, localPoint.x, Time.deltaTime * 12f);
                _tutorialPlayerPlane.anchoredPosition = _tutorialPlanePos;
            }

            // Disparo periódico al hacer clic o pulsar barra espaciadora
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                SpawnTutorialBullet();
            }

            // Actualizar movimiento de balas y comprobar colisiones
            for (int i = _tutorialBullets.Count - 1; i >= 0; i--)
            {
                RectTransform bullet = _tutorialBullets[i];
                if (bullet == null) continue;

                bullet.anchoredPosition += new Vector2(0, 900f * Time.deltaTime);

                // Comprobar colisión con objetivos
                for (int t = _tutorialTargets.Count - 1; t >= 0; t--)
                {
                    RectTransform target = _tutorialTargets[t];
                    if (target == null) continue;

                    if (Vector2.Distance(bullet.anchoredPosition, target.anchoredPosition) < 65f)
                    {
                        // Impacto
                        ProceduralAudio.PlayConfirm();
                        Destroy(target.gameObject);
                        _tutorialTargets.RemoveAt(t);

                        Destroy(bullet.gameObject);
                        _tutorialBullets.RemoveAt(i);

                        _targetsDestroyed++;
                        if (_targetsDestroyed >= 3)
                        {
                            CompleteTutorial();
                        }
                        break;
                    }
                }

                if (i < _tutorialBullets.Count && bullet != null && bullet.anchoredPosition.y > 900f)
                {
                    Destroy(bullet.gameObject);
                    _tutorialBullets.RemoveAt(i);
                }
            }
        }

        private void SpawnTutorialBullet()
        {
            ProceduralAudio.PlayTick();
            GameObject bulletObj = CreateUIObject("Bullet", _tutorialPanelObj.transform);
            RectTransform rt = bulletObj.GetComponent<RectTransform>();
            Image bulletImg = bulletObj.AddComponent<Image>();
            bulletImg.sprite = ProceduralShapeGenerator.GetCircleSprite();
            bulletImg.color = new Color(1f, 0.9f, 0.3f);
            SetRect(rt, 0.5f, 0.5f, 0.5f, 0.5f, _tutorialPlanePos + new Vector2(0, 60), new Vector2(24, 48));
            _tutorialBullets.Add(rt);
        }

        private void CompleteTutorial()
        {
            _tutorialActive = false;
            _txtTutorialInstructions.text = "¡VECTOR CALIBRADO CON ÉXITO! ACCEDIENDO AL MENÚ PRINCIPAL...";
            _txtTutorialInstructions.color = Color.green;

            StartCoroutine(FinishFlowAndLoadHangar());
        }

        private IEnumerator FinishFlowAndLoadHangar()
        {
            yield return new WaitForSeconds(1.4f);
            ProceduralAudio.PlayEngineHum();

            // Si estamos en la misma escena o queremos cargar menu.unity
            if (SceneManager.GetActiveScene().name == nextSceneName)
            {
                // Desactivar el overlay de flujo para revelar el menú UI Toolkit ya construido
                _rootPanel.gameObject.SetActive(false);
            }
            else if (!string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                SceneManager.LoadScene(nextSceneName);
            }
            else
            {
                // Fallback si no hay otra escena: deshabilitar el canvas del flujo
                _rootPanel.gameObject.SetActive(false);
            }
        }

        // =========================================================================
        // MÁQUINA DE ESTADOS Y TRANSICIONES
        // =========================================================================
        public void SetStep(FlowStep step)
        {
            _brandPanelObj.SetActive(step == FlowStep.BrandIntro);
            _factionPanelObj.SetActive(step == FlowStep.FactionSelection);
            _cinematicPanelObj.SetActive(step == FlowStep.StoryCinematic);
            _tutorialPanelObj.SetActive(step == FlowStep.TutorialMission);

            if (step == FlowStep.BrandIntro)
            {
                StartCoroutine(AutoAdvanceBrandIntro());
            }
            else if (step == FlowStep.FactionSelection)
            {
                RefreshFactionUI();
            }
            else if (step == FlowStep.StoryCinematic)
            {
                StartCinematicForFaction(GameFlowDataStore.SelectedFaction);
            }
            else if (step == FlowStep.TutorialMission)
            {
                _tutorialActive = true;
                _tutorialPlanePos = new Vector2(0, -220);
                if (_tutorialPlayerPlane != null)
                {
                    _tutorialPlayerPlane.anchoredPosition = _tutorialPlanePos;
                    Image pImg = _tutorialPlayerPlane.GetComponent<Image>();
                    if (pImg != null) pImg.color = GameFlowDataStore.SelectedFaction.primaryColor;
                }
            }
        }

        private IEnumerator AutoAdvanceBrandIntro()
        {
            yield return new WaitForSeconds(brandDurationSeconds);
            if (!_isTransitioning && _brandPanelObj.activeSelf)
            {
                StartCoroutine(TransitionToStep(FlowStep.FactionSelection));
            }
        }

        private IEnumerator TransitionToStep(FlowStep nextStep)
        {
            _isTransitioning = true;
            ProceduralAudio.PlayTick();

            // Fundido suave usando CanvasGroup temporal
            CanvasGroup cg = _rootPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = _rootPanel.gameObject.AddComponent<CanvasGroup>();

            float duration = 0.35f;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, timer / duration);
                yield return null;
            }

            SetStep(nextStep);

            timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                cg.alpha = Mathf.Lerp(0f, 1f, timer / duration);
                yield return null;
            }

            cg.alpha = 1f;
            _isTransitioning = false;
        }

        // =========================================================================
        // UTILIDADES DE CONSTRUCCIÓN DE UI
        // =========================================================================
        private GameObject CreateSubPanel(string name, Color color)
        {
            GameObject obj = CreateUIObject(name, _rootPanel.transform);
            Image img = obj.AddComponent<Image>();
            img.sprite = ProceduralShapeGenerator.GetSquareSprite();
            img.color = color;
            SetRect(obj.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);
            return obj;
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private void SetRect(RectTransform rt, float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
            rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }

        private GameObject CreateButton(string name, string text, Transform parent, float posX, float width, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = CreateUIObject(name, parent);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            btnImg.color = new Color(0.25f, 0.25f, 0.3f, 0.9f);
            SetRect(btnObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(posX, 0), new Vector2(width, 65));

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            GameObject txtObj = CreateUIObject("Label", btnObj.transform);
            Text t = txtObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = 20;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            SetRect(txtObj.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            return btnObj;
        }
    }
}
