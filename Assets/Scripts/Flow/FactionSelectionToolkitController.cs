using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace WorldGen.Flow
{
    [RequireComponent(typeof(UIDocument))]
    public class FactionSelectionToolkitController : MonoBehaviour
    {
        [Header("Navegación")]
        public string nextSceneName = "2_StoryCinematic";
        public string menuSceneName = "menu";
        public bool allowRevisitInEditor = true;

        [Header("Sprite del Caza")]
        public Sprite planeSprite;

        [Header("Sensibilidad Táctil")]
        [Tooltip("Distancia mínima en píxeles arrastrados para cambiar de nave")]
        public float swipeThreshold = 50f;

        [Header("Marcos / Interfaces de Facción (Fondo Completo)")]
        public Sprite anarquistaFrame;
        public Sprite capitalistaFrame;
        public Sprite comunistaFrame;
        public Sprite fascistaFrame;

        [Header("Marcos Horizontales para Estadísticas (1774 x 887)")]
        public Sprite anarquistaStatsFrame;
        public Sprite capitalistaStatsFrame;
        public Sprite comunistaStatsFrame;
        public Sprite fascistaStatsFrame;

        [Header("Marcos Verticales para la Nave (1024 x 1536)")]
        public Sprite anarquistaPlaneFrame;
        public Sprite capitalistaPlaneFrame;
        public Sprite comunistaPlaneFrame;
        public Sprite fascistaPlaneFrame;

        [Header("Transición Cinematográfica (DOTween)")]
        [Tooltip("Controlador de transición con Panel Izquierdo, Panel Derecho y Emblema Central")]
        public CinematicFactionTransition cinematicTransition;

        private UIDocument _uiDocument;
        private VisualElement _root;

        private VisualElement _bgFrame;
        private VisualElement _mainCard;
        private VisualElement _planeFrameBox;
        private VisualElement _descBox;
        private VisualElement _statsFrameBox;
        private Button _btnConfirmFaction;

        // Líneas de pestañeo en cascada
        private VisualElement _blinkLineBg;
        private VisualElement _blinkLinePlane;
        private VisualElement _blinkLineStats;

        private List<VisualElement> _dots = new List<VisualElement>();

        private Label _lblCounter;
        private Label _lblFactionTitle;
        private VisualElement _planeSpriteDisplay;
        private Label _lblPlaneCode;
        private Label _lblPlaneSlogan;
        private Label _lblPhilosophy;

        // Barras y valores de estadísticas
        private VisualElement _barSpeed;
        private Label _valSpeed;
        private VisualElement _barFirepower;
        private Label _valFirepower;
        private VisualElement _barArmor;
        private Label _valArmor;
        private VisualElement _barUpgrades;
        private Label _valUpgrades;

        private List<FactionPlaneDefinition> _factions;
        private int _selectedIndex = 0;

        // Seguimiento táctil y bloqueo de interacción durante animaciones
        private float _startX;
        private float _startY;
        private float _currentOffsetX = 0f;
        private bool _isDragging = false;
        private bool _isAnimating = false;
        private Coroutine _slideRoutine;

        private void SetCardOffset(float x)
        {
            if (_mainCard == null) return;
            _currentOffsetX = x;
            float screenW = (_root != null && _root.resolvedStyle.width > 10f && !float.IsNaN(_root.resolvedStyle.width)) ? _root.resolvedStyle.width : 1080f;
            float pctX = (x / screenW) * 100f;
            _mainCard.style.translate = new StyleTranslate(new Translate(Length.Percent(pctX), Length.Percent(0f)));
        }

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null || _uiDocument.rootVisualElement == null) return;
            _root = _uiDocument.rootVisualElement;

#if UNITY_EDITOR
            var flowStyleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI/Flow/FlowStyles.uss");
            if (flowStyleSheet != null && !_root.styleSheets.Contains(flowStyleSheet))
            {
                _root.styleSheets.Add(flowStyleSheet);
            }
#endif

            _factions = GameFlowDataStore.GetAllFactions();

            // Buscar sprite por defecto si no está asignado
            if (planeSprite == null)
            {
                var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var s in sprites)
                {
                    if (s != null && s.name.Contains("JAP_Ki21"))
                    {
                        planeSprite = s;
                        break;
                    }
                }
            }

            if (cinematicTransition == null)
            {
                cinematicTransition = GetComponent<CinematicFactionTransition>();
                if (cinematicTransition == null)
                {
                    cinematicTransition = Object.FindAnyObjectByType<CinematicFactionTransition>();
                }
                if (cinematicTransition == null)
                {
                    cinematicTransition = gameObject.AddComponent<CinematicFactionTransition>();
                }
            }
            cinematicTransition.selectionController = this;
            cinematicTransition.currentScreenUIDocument = _uiDocument;
            cinematicTransition.EnsureSceneHierarchySetup();

            EnsureFramesLoaded();
            BindUI();
            ApplyFactionData(_factions[_selectedIndex], false);

            if (_slideRoutine != null) StopCoroutine(_slideRoutine);
            _slideRoutine = StartCoroutine(InitialCascadeDeployRoutine());
        }

        /// <summary>
        /// Bloquea o desbloquea los controles de selección (deslizamiento, puntos y botón de confirmación)
        /// durante la transición cinematográfica.
        /// </summary>
        public void SetInteractionLocked(bool locked)
        {
            _isAnimating = locked;
            if (_btnConfirmFaction != null)
            {
                _btnConfirmFaction.SetEnabled(!locked);
            }
        }

        private void Update()
        {
            // Bloquear cualquier interacción mientras dure el pestañeo en cascada
            if (_isAnimating) return;

            // Soporte de navegación por teclado en Editor/PC
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                if (_slideRoutine != null) StopCoroutine(_slideRoutine);
                _slideRoutine = StartCoroutine(SequentialBlinkRoutine(1));
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                if (_slideRoutine != null) StopCoroutine(_slideRoutine);
                _slideRoutine = StartCoroutine(SequentialBlinkRoutine(-1));
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                OnConfirmFaction();
            }
        }

        private void EnsureFramesLoaded()
        {
#if UNITY_EDITOR
            // 1. Fondos principales de facciones
            if (anarquistaFrame == null)
                anarquistaFrame = LoadSubSprite("Assets/Art/Interfaces/Anarquista/Anarquismo.png");
            if (capitalistaFrame == null)
                capitalistaFrame = LoadSubSprite("Assets/Art/Interfaces/Capitalista/Capitalismo.png");
            if (comunistaFrame == null)
                comunistaFrame = LoadSubSprite("Assets/Art/Interfaces/Comunista/Comunismo.png");
            if (fascistaFrame == null)
                fascistaFrame = LoadSubSprite("Assets/Art/Interfaces/Facista/Facsimo.png");

            // 2. Marcos horizontales de estadísticas (1774 x 887)
            if (anarquistaStatsFrame == null)
                anarquistaStatsFrame = LoadSubSprite("Assets/Art/Interfaces/Anarquista/frame_horizontal_anarquista.png");
            if (capitalistaStatsFrame == null)
                capitalistaStatsFrame = LoadSubSprite("Assets/Art/Interfaces/Capitalista/frame_horizontal_capitalista.png");
            if (comunistaStatsFrame == null)
                comunistaStatsFrame = LoadSubSprite("Assets/Art/Interfaces/Comunista/frame_horizontal_comunismo.png");
            if (fascistaStatsFrame == null)
                fascistaStatsFrame = LoadSubSprite("Assets/Art/Interfaces/Facista/frame_horizontal_facista.png");

            // 3. Marcos verticales de la nave (1024 x 1536)
            if (anarquistaPlaneFrame == null)
                anarquistaPlaneFrame = LoadSubSprite("Assets/Art/Interfaces/Anarquista/frame_vertical_anarquista.png");
            if (capitalistaPlaneFrame == null)
                capitalistaPlaneFrame = LoadSubSprite("Assets/Art/Interfaces/Capitalista/frame_vertical_capitalista.png");
            if (comunistaPlaneFrame == null)
                comunistaPlaneFrame = LoadSubSprite("Assets/Art/Interfaces/Comunista/frame_vertical_comunista.png");
            if (fascistaPlaneFrame == null)
                fascistaPlaneFrame = LoadSubSprite("Assets/Art/Interfaces/Facista/frame_vertical_facista.png");
#endif

            // Fallback en runtime por Resources
            if (anarquistaStatsFrame == null || capitalistaStatsFrame == null || comunistaStatsFrame == null || fascistaStatsFrame == null ||
                anarquistaPlaneFrame == null || capitalistaPlaneFrame == null || comunistaPlaneFrame == null || fascistaPlaneFrame == null)
            {
                var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var s in sprites)
                {
                    if (s == null) continue;
                    string n = s.name.ToLower();

                    // Marcos horizontales
                    if (anarquistaStatsFrame == null && n.Contains("horizontal") && n.Contains("anarqu")) anarquistaStatsFrame = s;
                    if (capitalistaStatsFrame == null && n.Contains("horizontal") && n.Contains("capital")) capitalistaStatsFrame = s;
                    if (comunistaStatsFrame == null && n.Contains("horizontal") && n.Contains("comun")) comunistaStatsFrame = s;
                    if (fascistaStatsFrame == null && n.Contains("horizontal") && (n.Contains("facis") || n.Contains("fascis"))) fascistaStatsFrame = s;

                    // Marcos verticales
                    if (anarquistaPlaneFrame == null && n.Contains("vertical") && n.Contains("anarqu")) anarquistaPlaneFrame = s;
                    if (capitalistaPlaneFrame == null && n.Contains("vertical") && n.Contains("capital")) capitalistaPlaneFrame = s;
                    if (comunistaPlaneFrame == null && n.Contains("vertical") && n.Contains("comun")) comunistaPlaneFrame = s;
                    if (fascistaPlaneFrame == null && n.Contains("vertical") && (n.Contains("facis") || n.Contains("fascis"))) fascistaPlaneFrame = s;

                    // Fondos
                    if (anarquistaFrame == null && n.Contains("anarquismo")) anarquistaFrame = s;
                    if (capitalistaFrame == null && n.Contains("capitalismo")) capitalistaFrame = s;
                    if (comunistaFrame == null && n.Contains("comunismo")) comunistaFrame = s;
                    if (fascistaFrame == null && (n.Contains("facsimo") || n.Contains("fascismo"))) fascistaFrame = s;
                }
            }
        }

#if UNITY_EDITOR
        private static Sprite LoadSubSprite(string path)
        {
            var mainSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (mainSprite != null) return mainSprite;

            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is Sprite s) return s;
            }
            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            return null;
        }
#endif

        public Sprite GetStatsFrameForFaction(GameFactionId factionId)
        {
            if (anarquistaStatsFrame == null || capitalistaStatsFrame == null || comunistaStatsFrame == null || fascistaStatsFrame == null)
            {
                EnsureFramesLoaded();
            }

            switch (factionId)
            {
                case GameFactionId.Anarquistas:
                    return anarquistaStatsFrame;
                case GameFactionId.Capitalistas:
                    return capitalistaStatsFrame;
                case GameFactionId.Comunistas:
                    return comunistaStatsFrame;
                case GameFactionId.Fascistas:
                    return fascistaStatsFrame;
                default:
                    return anarquistaStatsFrame;
            }
        }

        public Sprite GetPlaneFrameForFaction(GameFactionId factionId)
        {
            if (anarquistaPlaneFrame == null || capitalistaPlaneFrame == null || comunistaPlaneFrame == null || fascistaPlaneFrame == null)
            {
                EnsureFramesLoaded();
            }

            switch (factionId)
            {
                case GameFactionId.Anarquistas:
                    return anarquistaPlaneFrame;
                case GameFactionId.Capitalistas:
                    return capitalistaPlaneFrame;
                case GameFactionId.Comunistas:
                    return comunistaPlaneFrame;
                case GameFactionId.Fascistas:
                    return fascistaPlaneFrame;
                default:
                    return anarquistaPlaneFrame;
            }
        }

        public Sprite GetFrameForFaction(GameFactionId factionId)
        {
            if (anarquistaFrame == null || capitalistaFrame == null || comunistaFrame == null || fascistaFrame == null)
            {
                EnsureFramesLoaded();
            }

            switch (factionId)
            {
                case GameFactionId.Anarquistas:
                    return anarquistaFrame;
                case GameFactionId.Capitalistas:
                    return capitalistaFrame;
                case GameFactionId.Comunistas:
                    return comunistaFrame;
                case GameFactionId.Fascistas:
                    return fascistaFrame;
                default:
                    return anarquistaFrame;
            }
        }

        private void Start()
        {
            // Si el jugador ya completó la selección anteriormente, se salta esta escena
            if (GameFlowDataStore.HasCompletedIntroFlow && !Application.isEditor && !allowRevisitInEditor)
            {
                Debug.Log("<color=cyan><b>[FactionSelection]</b> Flujo completado previamente. Saltando al Menú Principal.</color>");
                SceneManager.LoadScene(menuSceneName);
            }
        }

        private void BindUI()
        {
            _bgFrame = _root.Q<VisualElement>("faction-bg-frame");
            _mainCard = _root.Q<VisualElement>("carousel-swipe-area");
            _planeFrameBox = _root.Q<VisualElement>("plane-frame-box");
            _descBox = _root.Q<VisualElement>("faction-desc-box");
            _statsFrameBox = _root.Q<VisualElement>("stats-frame-box");
            _btnConfirmFaction = _root.Q<Button>("btn-confirm-faction");

            // Ocultar permanentemente cualquier línea central residual si existiera
            _blinkLineBg = _root.Q<VisualElement>("blink-line-bg");
            _blinkLinePlane = _root.Q<VisualElement>("blink-line-plane");
            _blinkLineStats = _root.Q<VisualElement>("blink-line-stats");
            if (_blinkLineBg != null) _blinkLineBg.style.display = DisplayStyle.None;
            if (_blinkLinePlane != null) _blinkLinePlane.style.display = DisplayStyle.None;
            if (_blinkLineStats != null) _blinkLineStats.style.display = DisplayStyle.None;

            _lblCounter = _root.Q<Label>("lbl-faction-counter");
            _lblFactionTitle = _root.Q<Label>("lbl-faction-title");
            _planeSpriteDisplay = _root.Q<VisualElement>("plane-sprite-display");
            _lblPlaneCode = _root.Q<Label>("lbl-plane-code");
            _lblPlaneSlogan = _root.Q<Label>("lbl-plane-slogan");
            _lblPhilosophy = _root.Q<Label>("lbl-philosophy");

            // Estadísticas
            _barSpeed = _root.Q<VisualElement>("bar-stat-speed");
            _valSpeed = _root.Q<Label>("val-stat-speed");
            _barFirepower = _root.Q<VisualElement>("bar-stat-firepower");
            _valFirepower = _root.Q<Label>("val-stat-firepower");
            _barArmor = _root.Q<VisualElement>("bar-stat-armor");
            _valArmor = _root.Q<Label>("val-stat-armor");
            _barUpgrades = _root.Q<VisualElement>("bar-stat-upgrades");
            _valUpgrades = _root.Q<Label>("val-stat-upgrades");

            // Paginación de puntos táctiles con soporte de clic interactivo
            _dots.Clear();
            for (int i = 0; i < 4; i++)
            {
                int targetIndex = i;
                var dot = _root.Q<VisualElement>($"dot-{i}");
                if (dot != null)
                {
                    _dots.Add(dot);
                    dot.RegisterCallback<ClickEvent>(evt =>
                    {
                        if (_isAnimating || targetIndex == _selectedIndex) return;
                        if (_slideRoutine != null) StopCoroutine(_slideRoutine);
                        _slideRoutine = StartCoroutine(SequentialBlinkToTargetRoutine(targetIndex));
                    });
                }
            }

            // GESTOS TÁCTILES PUROS
            if (_mainCard != null)
            {
                _mainCard.RegisterCallback<PointerDownEvent>(OnPointerDown);
                _mainCard.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                _mainCard.RegisterCallback<PointerUpEvent>(OnPointerUp);
                _mainCard.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            }

            if (_btnConfirmFaction != null)
            {
                _btnConfirmFaction.clicked += OnConfirmFaction;
                _btnConfirmFaction.RegisterCallback<ClickEvent>(_ => OnConfirmFaction());
                _btnConfirmFaction.RegisterCallback<PointerDownEvent>(evt =>
                {
                    evt.StopPropagation();
                });
                _btnConfirmFaction.RegisterCallback<PointerUpEvent>(evt =>
                {
                    evt.StopPropagation();
                    OnConfirmFaction();
                });
            }

            if (planeSprite != null && _planeSpriteDisplay != null)
            {
                _planeSpriteDisplay.style.backgroundImage = new StyleBackground(planeSprite);
            }

            // Mantener la relación de aspecto (941:1671) del marco exterior usando porcentajes en cualquier pantalla
            _root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            EnforceOuterFrameAspectRatio();
        }

        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            EnforceOuterFrameAspectRatio();
        }

        private void EnforceOuterFrameAspectRatio()
        {
            if (_root == null || _bgFrame == null) return;
            float screenW = _root.resolvedStyle.width;
            float screenH = _root.resolvedStyle.height;
            if (screenW <= 10f || screenH <= 10f || float.IsNaN(screenW) || float.IsNaN(screenH)) return;

            // Tasa exacta del marco principal (941 x 1671)
            const float targetAspect = 941f / 1671f;

            float maxW = screenW * 0.94f;
            float maxH = screenH * 0.92f;

            float finalW, finalH;
            if (maxW / maxH > targetAspect)
            {
                finalH = maxH;
                finalW = finalH * targetAspect;
            }
            else
            {
                finalW = maxW;
                finalH = finalW / targetAspect;
            }

            float pctW = (finalW / screenW) * 100f;
            float pctH = (finalH / screenH) * 100f;

            _bgFrame.style.width = Length.Percent(pctW);
            _bgFrame.style.height = Length.Percent(pctH);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            // Bloqueado hasta terminar la animación en cascada
            if (_isAnimating) return;

            // Si el toque ocurre sobre el botón ELEGIR NAVE, no capturar el puntero en el carrusel
            if (_btnConfirmFaction != null && (evt.target == _btnConfirmFaction || _btnConfirmFaction.Contains(evt.target as VisualElement)))
            {
                return;
            }

            if (_slideRoutine != null) StopCoroutine(_slideRoutine);

            _startX = evt.position.x;
            _startY = evt.position.y;
            _isDragging = true;

            if (_mainCard != null)
            {
                _mainCard.CapturePointer(evt.pointerId);
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || _mainCard == null || _isAnimating) return;

            float currentX = evt.position.x;
            float deltaX = currentX - _startX;

            // Arrastre elástico suave en tiempo real mientras el dedo se desplaza
            SetCardOffset(deltaX * 0.45f);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging || _mainCard == null || _isAnimating) return;
            _isDragging = false;

            _mainCard.ReleasePointer(evt.pointerId);

            float totalDeltaX = evt.position.x - _startX;
            float totalDeltaY = evt.position.y - _startY;

            // Comprobar si el gesto fue predominantemente horizontal y superó el umbral
            if (Mathf.Abs(totalDeltaX) > swipeThreshold && Mathf.Abs(totalDeltaX) > Mathf.Abs(totalDeltaY))
            {
                int direction = totalDeltaX < 0 ? 1 : -1; // Deslizar izquierda = siguiente nave (+1), derecha = anterior (-1)
                _slideRoutine = StartCoroutine(SequentialBlinkRoutine(direction));
            }
            else
            {
                // Arrastre corto: volver al centro suavemente
                _slideRoutine = StartCoroutine(SnapBackRoutine(_currentOffsetX));
            }
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (!_isDragging || _mainCard == null || _isAnimating) return;
            _isDragging = false;
            _mainCard.ReleasePointer(evt.pointerId);

            _slideRoutine = StartCoroutine(SnapBackRoutine(_currentOffsetX));
        }

        /// <summary>
        /// Despliegue inicial en cascada al entrar a la pantalla de selección:
        /// Se abre primero el marco grande de fondo (atrás) y, con tiempo entre cada uno,
        /// se despliegan la nave, la descripción, las estadísticas y el botón, sin línea en el medio.
        /// </summary>
        private IEnumerator InitialCascadeDeployRoutine()
        {
            _isAnimating = true;
            var faction = _factions[_selectedIndex];
            yield return StartCoroutine(PlayCascadeUnfoldRoutine(faction));
            _isAnimating = false;
        }

        /// <summary>
        /// Despliegue en Cascada Sucesiva:
        /// 1) Se cierran todos los marcos (incluido el marco grande de fondo) con el mismo pliegue vertical limpio que la nave.
        /// 2) Se abre primero el marco grande de fondo (el de atrás) exactamente igual que el de la nave, sin línea central.
        /// 3) Con tiempo entre pasos, se despliega el marco vertical de la nave.
        /// 4) Se despliega la descripción.
        /// 5) Se despliega el marco horizontal de estadísticas y se llenan sus barras.
        /// 6) Se despliega el botón de elegir nave.
        /// </summary>
        private IEnumerator SequentialBlinkRoutine(int direction)
        {
            _isAnimating = true;

            yield return StartCoroutine(CloseAllElementsRoutine(_currentOffsetX));

            _selectedIndex = (_selectedIndex + direction + _factions.Count) % _factions.Count;
            var faction = _factions[_selectedIndex];
            ApplyFactionData(faction, false);

            yield return StartCoroutine(PlayCascadeUnfoldRoutine(faction));

            _isAnimating = false;
        }

        private IEnumerator SequentialBlinkToTargetRoutine(int targetIndex)
        {
            _isAnimating = true;

            yield return StartCoroutine(CloseAllElementsRoutine(_currentOffsetX));

            _selectedIndex = targetIndex;
            var faction = _factions[_selectedIndex];
            ApplyFactionData(faction, false);

            yield return StartCoroutine(PlayCascadeUnfoldRoutine(faction));

            _isAnimating = false;
        }

        private IEnumerator CloseAllElementsRoutine(float initialOffset)
        {
            float tClose = 0f;
            float closeDur = 0.16f;

            while (tClose < closeDur)
            {
                tClose += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(tClose / closeDur);
                float ease = progress * progress;

                float s = Mathf.Lerp(1f, 0f, ease);
                float a = Mathf.Lerp(1f, 0f, ease);

                if (_statsFrameBox != null) { _statsFrameBox.style.scale = new StyleScale(new Scale(new Vector2(1f, s))); _statsFrameBox.style.opacity = a; }
                if (_descBox != null) { _descBox.style.scale = new StyleScale(new Scale(new Vector2(1f, s))); _descBox.style.opacity = a; }
                if (_planeFrameBox != null) { _planeFrameBox.style.scale = new StyleScale(new Scale(new Vector2(1f, s))); _planeFrameBox.style.opacity = a; }
                if (_bgFrame != null) { _bgFrame.style.scale = new StyleScale(new Scale(new Vector2(1f, s))); _bgFrame.style.opacity = a; }
                if (_btnConfirmFaction != null) { _btnConfirmFaction.style.scale = new StyleScale(new Scale(new Vector2(1f, s))); _btnConfirmFaction.style.opacity = a; }

                SetCardOffset(Mathf.Lerp(initialOffset, 0f, ease));
                yield return null;
            }

            SetCardOffset(0f);
        }

        private IEnumerator PlayCascadeUnfoldRoutine(FactionPlaneDefinition faction)
        {
            // Asegurar que no haya ninguna línea central visible
            if (_blinkLineBg != null) _blinkLineBg.style.display = DisplayStyle.None;
            if (_blinkLinePlane != null) _blinkLinePlane.style.display = DisplayStyle.None;
            if (_blinkLineStats != null) _blinkLineStats.style.display = DisplayStyle.None;

            // Preparar todos los elementos cerrados (escala vertical 0 y opacidad 0) para que el fondo se abra solo primero
            if (_bgFrame != null) { _bgFrame.style.scale = new StyleScale(new Scale(new Vector2(1f, 0f))); _bgFrame.style.opacity = 0f; }
            if (_planeFrameBox != null) { _planeFrameBox.style.scale = new StyleScale(new Scale(new Vector2(1f, 0f))); _planeFrameBox.style.opacity = 0f; }
            if (_descBox != null) { _descBox.style.scale = new StyleScale(new Scale(new Vector2(1f, 0f))); _descBox.style.opacity = 0f; }
            if (_statsFrameBox != null) { _statsFrameBox.style.scale = new StyleScale(new Scale(new Vector2(1f, 0f))); _statsFrameBox.style.opacity = 0f; }
            if (_btnConfirmFaction != null) { _btnConfirmFaction.style.scale = new StyleScale(new Scale(new Vector2(1f, 0f))); _btnConfirmFaction.style.opacity = 0f; }

            SetBarFillPercent(_barSpeed, 0f);
            SetBarFillPercent(_barFirepower, 0f);
            SetBarFillPercent(_barArmor, 0f);
            SetBarFillPercent(_barUpgrades, 0f);

            yield return new WaitForSecondsRealtime(0.05f);

            // 1. PASO 1: SE DESPLIEGA PRIMERO EL MARCO DE FONDO DE ATRÁS (exactamente igual que el de la nave, sin línea en el medio)
            ProceduralAudio.PlayBlinkHeavy();
            float t = 0f;
            float durBg = 0.28f;
            while (t < durBg)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / durBg);
                float easeOut = 1f - Mathf.Pow(1f - p, 3f);

                if (_bgFrame != null)
                {
                    _bgFrame.style.scale = new StyleScale(new Scale(new Vector2(1f, Mathf.Lerp(0f, 1f, easeOut))));
                    _bgFrame.style.opacity = Mathf.Lerp(0f, 1f, easeOut);
                }
                yield return null;
            }
            if (_bgFrame != null)
            {
                _bgFrame.style.scale = new StyleScale(new Scale(Vector2.one));
                _bgFrame.style.opacity = 1f;
            }

            // Tiempo claro tras abrirse el marco de fondo antes de desplegar la nave
            yield return new WaitForSecondsRealtime(0.22f);

            // 2. PASO 2: SE DESPLIEGA EL MARCO VERTICAL DE LA NAVE (sin línea central)
            ProceduralAudio.PlayBlinkMid();
            t = 0f;
            float durPlane = 0.26f;
            while (t < durPlane)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / durPlane);
                float easeOut = 1f - Mathf.Pow(1f - p, 3f);

                if (_planeFrameBox != null)
                {
                    _planeFrameBox.style.scale = new StyleScale(new Scale(new Vector2(1f, Mathf.Lerp(0f, 1f, easeOut))));
                    _planeFrameBox.style.opacity = Mathf.Lerp(0f, 1f, easeOut);
                }
                yield return null;
            }
            if (_planeFrameBox != null)
            {
                _planeFrameBox.style.scale = new StyleScale(new Scale(Vector2.one));
                _planeFrameBox.style.opacity = 1f;
            }

            // Tiempo entre la nave y la descripción
            yield return new WaitForSecondsRealtime(0.18f);

            // 3. PASO 3: SE DESPLIEGA LA DESCRIPCIÓN
            ProceduralAudio.PlayBlinkLight();
            t = 0f;
            float durDesc = 0.22f;
            while (t < durDesc)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / durDesc);
                float easeOut = 1f - Mathf.Pow(1f - p, 3f);

                if (_descBox != null)
                {
                    _descBox.style.scale = new StyleScale(new Scale(new Vector2(1f, Mathf.Lerp(0f, 1f, easeOut))));
                    _descBox.style.opacity = Mathf.Lerp(0f, 1f, easeOut);
                }
                yield return null;
            }
            if (_descBox != null)
            {
                _descBox.style.scale = new StyleScale(new Scale(Vector2.one));
                _descBox.style.opacity = 1f;
            }

            // Tiempo entre la descripción y las estadísticas
            yield return new WaitForSecondsRealtime(0.16f);

            // 4. PASO 4: SE DESPLIEGAN LAS ESTADÍSTICAS (sin línea central)
            ProceduralAudio.PlayBlinkHeavy();
            t = 0f;
            float durStats = 0.26f;
            while (t < durStats)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / durStats);
                float easeOut = 1f - Mathf.Pow(1f - p, 3f);

                if (_statsFrameBox != null)
                {
                    _statsFrameBox.style.scale = new StyleScale(new Scale(new Vector2(1f, Mathf.Lerp(0f, 1f, easeOut))));
                    _statsFrameBox.style.opacity = Mathf.Lerp(0f, 1f, easeOut);
                }
                yield return null;
            }
            if (_statsFrameBox != null)
            {
                _statsFrameBox.style.scale = new StyleScale(new Scale(Vector2.one));
                _statsFrameBox.style.opacity = 1f;
            }

            // Animación de llenado suave de las barras de estadísticas
            yield return StartCoroutine(AnimateStatBarsFillRoutine(faction));

            // Tiempo breve antes del botón
            yield return new WaitForSecondsRealtime(0.10f);

            // 5. PASO 5: SE DESPLIEGA EL BOTÓN ELEGIR NAVE
            t = 0f;
            float durBtn = 0.20f;
            while (t < durBtn)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / durBtn);
                float easeOut = 1f - Mathf.Pow(1f - p, 3f);
                if (_btnConfirmFaction != null)
                {
                    _btnConfirmFaction.style.opacity = easeOut;
                    _btnConfirmFaction.style.scale = new StyleScale(new Scale(new Vector2(1f, Mathf.Lerp(0f, 1f, easeOut))));
                }
                yield return null;
            }
            if (_btnConfirmFaction != null)
            {
                _btnConfirmFaction.style.opacity = 1f;
                _btnConfirmFaction.style.scale = new StyleScale(new Scale(Vector2.one));
            }
        }

        private IEnumerator AnimateStatBarsFillRoutine(FactionPlaneDefinition f)
        {
            float fillElapsed = 0f;
            float fillDur = 0.22f;

            while (fillElapsed < fillDur)
            {
                fillElapsed += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(fillElapsed / fillDur);
                float ease = 1f - Mathf.Pow(1f - p, 3f);

                SetBarFillPercent(_barSpeed, f.speed * ease);
                SetBarFillPercent(_barFirepower, f.firepower * ease);
                SetBarFillPercent(_barArmor, f.armor * ease);
                SetBarFillPercent(_barUpgrades, f.upgrades * ease);

                yield return null;
            }

            SetBarFillPercent(_barSpeed, f.speed);
            SetBarFillPercent(_barFirepower, f.firepower);
            SetBarFillPercent(_barArmor, f.armor);
            SetBarFillPercent(_barUpgrades, f.upgrades);
        }

        private void SetBarFillPercent(VisualElement bar, float statValue)
        {
            if (bar == null) return;
            float clamped = Mathf.Clamp(statValue, 0f, 10f);
            bar.style.width = Length.Percent(clamped * 10f);
        }

        private IEnumerator SnapBackRoutine(float currentOffset)
        {
            float t = 0f;
            float dur = 0.15f;

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(t / dur);
                float ease = 1f - Mathf.Pow(1f - progress, 3f);
                float x = Mathf.Lerp(currentOffset, 0f, ease);
                SetCardOffset(x);
                yield return null;
            }

            SetCardOffset(0f);
        }

        public void ApplyFactionData(FactionPlaneDefinition f, bool playSound)
        {
            if (f == null) return;

            if (playSound)
            {
                ProceduralAudio.PlayTick();
            }

            // 1. Textos identificativos
            if (_lblCounter != null)
            {
                _lblCounter.text = $"FACCIÓN 0{_selectedIndex + 1} DE 0{_factions.Count} // DESLIZA CON EL DEDO ↔";
            }
            if (_lblFactionTitle != null)
            {
                _lblFactionTitle.text = f.factionName.ToUpper();
                _lblFactionTitle.style.color = f.accentColor;
            }
            if (_lblPlaneCode != null)
            {
                _lblPlaneCode.text = f.planeCode;
            }
            if (_lblPlaneSlogan != null)
            {
                _lblPlaneSlogan.text = f.slogan;
            }
            if (_lblPhilosophy != null)
            {
                _lblPhilosophy.text = f.mechanicalPhilosophy;
            }

            // 2. Barras de estadísticas visuales y números
            UpdateStatBar(_barSpeed, _valSpeed, f.speed);
            UpdateStatBar(_barFirepower, _valFirepower, f.firepower);
            UpdateStatBar(_barArmor, _valArmor, f.armor);
            UpdateStatBar(_barUpgrades, _valUpgrades, f.upgrades);

            // 3. Indicadores de puntos (Dots)
            for (int i = 0; i < _dots.Count; i++)
            {
                if (i == _selectedIndex)
                {
                    _dots[i].AddToClassList("active");
                }
                else
                {
                    _dots[i].RemoveFromClassList("active");
                }
            }

            // 4. Color de acento temático para la silueta del avión
            if (_planeSpriteDisplay != null)
            {
                _planeSpriteDisplay.style.unityBackgroundImageTintColor = f.accentColor;
            }

            // 5. Marco horizontal para Estadísticas (1774 x 887)
            Sprite statsFrame = GetStatsFrameForFaction(f.factionId);
            if (_statsFrameBox != null && statsFrame != null)
            {
                _statsFrameBox.style.backgroundImage = new StyleBackground(statsFrame);
            }

            // 6. Marco vertical para la Nave (1024 x 1536)
            Sprite planeFrame = GetPlaneFrameForFaction(f.factionId);
            if (_planeFrameBox != null && planeFrame != null)
            {
                _planeFrameBox.style.backgroundImage = new StyleBackground(planeFrame);
            }

            // 7. Marco / Ilustración de fondo de la facción (a pantalla completa)
            Sprite bgFrame = GetFrameForFaction(f.factionId);
            if (_bgFrame != null)
            {
                if (bgFrame != null)
                {
                    _bgFrame.style.backgroundImage = new StyleBackground(bgFrame);
                    _bgFrame.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _bgFrame.style.display = DisplayStyle.None;
                }
            }
        }

        private void UpdateStatBar(VisualElement bar, Label lbl, float statValue)
        {
            float clamped = Mathf.Clamp(statValue, 1f, 10f);
            if (lbl != null)
            {
                lbl.text = $"{clamped:F1} / 10";
            }
            if (bar != null)
            {
                bar.style.width = Length.Percent(clamped * 10f);
            }
        }

        private void OnConfirmFaction()
        {
            if (_isAnimating) return;
            if (cinematicTransition != null && cinematicTransition.IsTransitionRunning) return;
            if (_factions == null || _selectedIndex < 0 || _selectedIndex >= _factions.Count) return;

            var chosen = _factions[_selectedIndex];
            GameFlowDataStore.SaveFaction(chosen);
            ProceduralAudio.PlayConfirm();

            if (cinematicTransition == null)
            {
                cinematicTransition = GetComponent<CinematicFactionTransition>();
                if (cinematicTransition == null)
                {
                    cinematicTransition = Object.FindAnyObjectByType<CinematicFactionTransition>();
                }
                if (cinematicTransition == null)
                {
                    cinematicTransition = gameObject.AddComponent<CinematicFactionTransition>();
                }
            }
            cinematicTransition.selectionController = this;
            cinematicTransition.currentScreenUIDocument = _uiDocument;
            cinematicTransition.EnsureSceneHierarchySetup();

            // Si existe el controlador de transición cinematográfica con DOTween, ejecutar las 5 fases antes de cambiar de pantalla
            if (cinematicTransition != null && cinematicTransition.isActiveAndEnabled)
            {
                SetInteractionLocked(true);
                Debug.Log($"<color=green><b>[FactionSelection]</b> Facción confirmada: {chosen.factionName} ({chosen.planeCode}). Iniciando transición cinematográfica DOTween...</color>");
                cinematicTransition.PlayTransition(chosen, () =>
                {
                    SceneManager.LoadScene(nextSceneName);
                });
                return;
            }

            Debug.Log($"<color=green><b>[FactionSelection]</b> Facción confirmada: {chosen.factionName} ({chosen.planeCode}). Avanzando a {nextSceneName}...</color>");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
