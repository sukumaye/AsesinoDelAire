using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace WorldGen.Flow
{
    /// <summary>
    /// Transición cinematográfica reutilizable de confirmación de nave y facción con DOTween.
    /// Utiliza automáticamente los recursos existentes del proyecto (Assets/Art/Interfaces/...)
    /// si no se han asignado sprites personalizados manualmente en el Inspector, y sincroniza
    /// tanto el Canvas uGUI como el UIDocument para garantizar visibilidad en Game View y Device Simulator.
    /// </summary>
    public class CinematicFactionTransition : MonoBehaviour
    {
        [Serializable]
        public class FactionTransitionVisuals
        {
            public GameFactionId factionId;
            [Tooltip("Emblema central específico de esta facción")]
            public Sprite emblemSprite;
            [Tooltip("Opcional: mitad izquierda específica de esta facción")]
            public Sprite leftPanelOverride;
            [Tooltip("Opcional: mitad derecha específica de esta facción")]
            public Sprite rightPanelOverride;
        }

        [Header("1. Referencias UI Independientes (Canvas / Image)")]
        [Tooltip("Canvas contenedor de la transición (por encima de la UI de selección)")]
        public Canvas transitionCanvas;

        [Tooltip("Panel izquierdo: mitad izquierda del fondo")]
        public UnityEngine.UI.Image leftPanelImage;

        [Tooltip("RectTransform del panel izquierdo")]
        public RectTransform leftPanelRect;

        [Tooltip("Panel derecho: mitad derecha del fondo")]
        public UnityEngine.UI.Image rightPanelImage;

        [Tooltip("RectTransform del panel derecho")]
        public RectTransform rightPanelRect;

        [Tooltip("Emblema central: situado por encima de ambos paneles en el orden visual del Canvas")]
        public UnityEngine.UI.Image centerEmblemImage;

        [Tooltip("RectTransform del emblema central")]
        public RectTransform centerEmblemRect;

        [Header("2. Fondos de Transición por Facción (Se cortan a la mitad automáticamente)")]
        [Tooltip("Cartel de transición para la Facción Anarquista (Assets/Art/Interfaces/Anarquista/fondo (2).png)")]
        public Sprite anarquistaTransitionBg;

        [Tooltip("Cartel de transición para la Facción Capitalista (Assets/Art/Interfaces/Capitalista/fondo (3).png)")]
        public Sprite capitalistaTransitionBg;

        [Tooltip("Cartel de transición para la Facción Comunista (Assets/Art/Interfaces/Comunista/fondo (1).png)")]
        public Sprite comunistaTransitionBg;

        [Tooltip("Cartel de transición para la Facción Fascista (Assets/Art/Interfaces/Facista/fondo (4).png)")]
        public Sprite fascistaTransitionBg;

        [Header("2b. Logos / Emblemas Centrales por Facción")]
        [Tooltip("Logo central para la Facción Anarquista (Assets/Art/Interfaces/Anarquista/logo (2).png)")]
        public Sprite anarquistaEmblem;

        [Tooltip("Logo central para la Facción Capitalista (Assets/Art/Interfaces/Capitalista/logo (1).png)")]
        public Sprite capitalistaEmblem;

        [Tooltip("Logo central para la Facción Comunista (Assets/Art/Interfaces/Comunista/logo (4).png)")]
        public Sprite comunistaEmblem;

        [Tooltip("Logo central para la Facción Fascista (Assets/Art/Interfaces/Facista/logo (3).png)")]
        public Sprite fascistaEmblem;

        [Tooltip("Configuración opcional por facción")]
        public List<FactionTransitionVisuals> factionVisualOverrides = new List<FactionTransitionVisuals>();

        [Header("3. Fase 2 — Cierre de los Dos Paneles")]
        [Tooltip("Duración del desplazamiento de los dos paneles desde los bordes exteriores hasta el centro")]
        [Min(0.05f)] public float closeDuration = 0.85f;

        [Tooltip("Curva de aceleración de DOTween para el cierre de paneles")]
        public Ease closeEase = Ease.InOutCubic;

        [Tooltip("Activar para usar las AnimationCurve personalizadas del Inspector en lugar del selector Ease")]
        public bool useCustomInspectorCurves = false;

        [Tooltip("Curva de aceleración/desaceleración personalizada para el cierre de paneles")]
        public AnimationCurve closeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Intensidad del pequeño impacto al encontrarse ambos paneles en el centro")]
        [Range(0f, 0.15f)] public float closeImpactScalePunch = 0.03f;

        [Tooltip("Duración del pequeño impacto al unirse los paneles")]
        [Min(0.02f)] public float closeImpactDuration = 0.25f;

        [Tooltip("Solapamiento subpíxel en el centro (px) para impedir cualquier grieta visible entre las dos mitades")]
        [Range(0f, 4f)] public float centerSeamOverlap = 1.5f;

        [Tooltip("Margen extra fuera de pantalla (px) desde donde parten los paneles izquierdo y derecho")]
        [Min(0f)] public float offscreenExtraMargin = 60f;

        [Tooltip("Si está activo, ajusta automáticamente el tamaño de cada panel a la mitad exacta del Canvas")]
        public bool autoFitPanelsToHalfCanvas = true;

        [Header("4. Fase 3 — Aparición, Rotación e Impacto del Emblema")]
        [Tooltip("Duración de la aparición y escalado del emblema central")]
        [Min(0.05f)] public float emblemAppearDuration = 0.85f;

        [Tooltip("Escala inicial reducida desde la que emerge el emblema")]
        [Range(0f, 1f)] public float emblemInitialScale = 0.15f;

        [Tooltip("Intensidad del rebote al aparecer el emblema (usado en Ease.OutBack)")]
        [Range(0.5f, 4f)] public float bounceIntensity = 1.85f;

        [Tooltip("Ease de DOTween para la aparición con rebote del emblema")]
        public Ease emblemAppearEase = Ease.OutBack;

        [Tooltip("Curva personalizada para la aparición del emblema (si useCustomInspectorCurves está activo)")]
        public AnimationCurve emblemAppearCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Duración de la rotación breve y controlada del emblema")]
        [Min(0.05f)] public float rotationDuration = 0.80f;

        [Tooltip("Ángulo inicial de rotación controlada del emblema (termina asentado en 0 grados)")]
        public float rotationAngle = -28f;

        [Tooltip("Variación de escala del golpe visual del emblema tras aparecer")]
        public Vector3 emblemImpactPunchScale = new Vector3(0.15f, 0.15f, 0f);

        [Tooltip("Duración del golpe visual y movimiento de asentamiento del emblema")]
        [Min(0.05f)] public float emblemSettleDuration = 0.35f;

        [Tooltip("Desplazamiento vertical breve del movimiento de asentamiento (px)")]
        public float emblemSettleVerticalOffset = -18f;

        [Tooltip("Tiempo de espera con el emblema visible antes de iniciar el movimiento ascendente")]
        [Min(0f)] public float waitBeforeAscend = 1.15f;

        [Header("5. Fase 4 — Movimiento Ascendente")]
        [Tooltip("Duración del movimiento ascendente coordinado de los dos paneles y el emblema")]
        [Min(0.05f)] public float ascendDuration = 1.10f;

        [Tooltip("Distancia de desplazamiento hacia arriba (px)")]
        [Min(100f)] public float ascendDistance = 2200f;

        [Tooltip("Ease de DOTween para el movimiento ascendente")]
        public Ease ascendEase = Ease.InCubic;

        [Tooltip("Curva de aceleración/desaceleración personalizada para el movimiento ascendente")]
        public AnimationCurve ascendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("6. Fase 5 — Cambio de Pantalla y Navegación Existente")]
        [Tooltip("Referencia al controlador de selección de facciones existente")]
        public FactionSelectionToolkitController selectionController;

        [Tooltip("Referencia al UIDocument de la pantalla de selección actual")]
        public UIDocument currentScreenUIDocument;

        [Tooltip("Opcional: objeto de la pantalla siguiente si se encuentra dentro de la misma escena")]
        public GameObject nextScreenObject;

        [Tooltip("Nombre de la escena siguiente dentro del flujo existente del proyecto")]
        public string nextSceneName = "2_StoryCinematic";

        [Tooltip("Botón uGUI opcional de confirmación")]
        public UnityEngine.UI.Button optionalConfirmButton;

        // Estado interno de la secuencia DOTween
        private Sequence _activeSequence;
        private bool _isTransitionRunning = false;
        private bool _hasCapturedBasePositions = false;
        private Vector2 _leftClosedBasePos;
        private Vector2 _rightClosedBasePos;
        private Vector2 _emblemCenterBasePos;
        private Vector3 _emblemNormalScale = Vector3.one;

        // Caché de mitades izquierda/derecha y emblemas generados desde los recursos existentes del proyecto
        private readonly Dictionary<GameFactionId, Sprite> _cachedLeftHalfSprites = new Dictionary<GameFactionId, Sprite>();
        private readonly Dictionary<GameFactionId, Sprite> _cachedRightHalfSprites = new Dictionary<GameFactionId, Sprite>();
        private readonly Dictionary<GameFactionId, Sprite> _cachedEmblemSprites = new Dictionary<GameFactionId, Sprite>();
        private Sprite _userCustomLeftSprite;
        private Sprite _userCustomRightSprite;
        private bool _checkedUserCustomSprites = false;

        // Espejo en UI Toolkit para garantizar visibilidad sobre Device Simulator (X 3 5G, etc.)
        private VisualElement _toolkitOverlayRoot;
        private VisualElement _toolkitLeftPanel;
        private VisualElement _toolkitLeftInnerImage;
        private VisualElement _toolkitRightPanel;
        private VisualElement _toolkitRightInnerImage;
        private VisualElement _toolkitEmblemBox;
        private VisualElement _toolkitEmblemInnerPlane;
        private bool _usingAutoSlicedTransitionPoster = true;
        private bool _panelsUnifiedAfterJoin = false;
        private Sprite _currentFullPosterSprite;

        public bool IsTransitionRunning => _isTransitionRunning;

        private void Awake()
        {
            EnsureSceneHierarchySetup();
            ResolveReferences();
            CaptureBaseLayoutAndPlaceOffscreen();

            if (optionalConfirmButton != null)
            {
                optionalConfirmButton.onClick.RemoveListener(TriggerFromCurrentSelection);
                optionalConfirmButton.onClick.AddListener(TriggerFromCurrentSelection);
            }
        }

        private void Update()
        {
            if (_isTransitionRunning)
            {
                SyncToolkitOverlayFromRectTransforms();
            }
        }

        private void OnDisable()
        {
            KillActiveSequence();
        }

        private void OnDestroy()
        {
            KillActiveSequence();
            if (optionalConfirmButton != null)
            {
                optionalConfirmButton.onClick.RemoveListener(TriggerFromCurrentSelection);
            }
        }

        public void ResolveReferences()
        {
            if (selectionController == null)
            {
                selectionController = GetComponent<FactionSelectionToolkitController>();
                if (selectionController == null)
                {
                    selectionController = UnityEngine.Object.FindAnyObjectByType<FactionSelectionToolkitController>();
                }
            }

            if (currentScreenUIDocument == null && selectionController != null)
            {
                currentScreenUIDocument = selectionController.GetComponent<UIDocument>();
            }
            if (currentScreenUIDocument == null)
            {
                currentScreenUIDocument = GetComponent<UIDocument>();
            }

            if (transitionCanvas == null)
            {
                var existingObj = GameObject.Find("Canvas_CinematicTransition");
                if (existingObj != null)
                {
                    transitionCanvas = existingObj.GetComponent<Canvas>();
                }
            }

            if (leftPanelImage != null && leftPanelRect == null) leftPanelRect = leftPanelImage.rectTransform;
            if (rightPanelImage != null && rightPanelRect == null) rightPanelRect = rightPanelImage.rectTransform;
            if (centerEmblemImage != null && centerEmblemRect == null) centerEmblemRect = centerEmblemImage.rectTransform;

            EnsureTransitionBackgroundsLoaded();

            if (!_checkedUserCustomSprites)
            {
                _checkedUserCustomSprites = true;
                if (leftPanelImage != null && leftPanelImage.sprite != null && !leftPanelImage.sprite.name.EndsWith("_LeftHalf"))
                    _userCustomLeftSprite = leftPanelImage.sprite;
                if (rightPanelImage != null && rightPanelImage.sprite != null && !rightPanelImage.sprite.name.EndsWith("_RightHalf"))
                    _userCustomRightSprite = rightPanelImage.sprite;
            }

            if (centerEmblemRect != null && leftPanelRect != null && rightPanelRect != null)
            {
                if (centerEmblemRect.parent == leftPanelRect.parent)
                {
                    centerEmblemRect.SetAsLastSibling();
                }
            }
        }

        public void CaptureBaseLayoutAndPlaceOffscreen()
        {
            ResolveReferences();
            if (leftPanelRect == null || rightPanelRect == null || centerEmblemRect == null) return;

            float canvasW = GetCanvasWidth();
            float canvasH = GetCanvasHeight();
            float halfW = canvasW * 0.5f;

            if (autoFitPanelsToHalfCanvas)
            {
                leftPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
                leftPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
                leftPanelRect.pivot = new Vector2(1f, 0.5f);
                leftPanelRect.sizeDelta = new Vector2(halfW, canvasH);

                rightPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
                rightPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
                rightPanelRect.pivot = new Vector2(0f, 0.5f);
                rightPanelRect.sizeDelta = new Vector2(halfW, canvasH);

                _leftClosedBasePos = Vector2.zero;
                _rightClosedBasePos = Vector2.zero;

                centerEmblemRect.anchorMin = new Vector2(0.5f, 0.5f);
                centerEmblemRect.anchorMax = new Vector2(0.5f, 0.5f);
                centerEmblemRect.pivot = new Vector2(0.5f, 0.5f);
                centerEmblemRect.sizeDelta = new Vector2(560f, 560f);
                _emblemCenterBasePos = Vector2.zero;
                _emblemNormalScale = Vector3.one;
                _hasCapturedBasePositions = true;
            }
            else if (!_hasCapturedBasePositions)
            {
                _leftClosedBasePos = leftPanelRect.anchoredPosition;
                _rightClosedBasePos = rightPanelRect.anchoredPosition;
                _emblemCenterBasePos = centerEmblemRect.anchoredPosition;
                _emblemNormalScale = centerEmblemRect.localScale.sqrMagnitude > 0.001f ? centerEmblemRect.localScale : Vector3.one;
                _hasCapturedBasePositions = true;
            }

            PlaceElementsInInitialOffscreenState();
        }

        private void PlaceElementsInInitialOffscreenState()
        {
            if (leftPanelRect == null || rightPanelRect == null || centerEmblemRect == null) return;

            float canvasW = GetCanvasWidth();
            float leftWidth = Mathf.Max(canvasW * 0.5f, leftPanelRect.rect.width);
            float rightWidth = Mathf.Max(canvasW * 0.5f, rightPanelRect.rect.width);

            Vector2 leftOffscreen = new Vector2(_leftClosedBasePos.x - leftWidth - offscreenExtraMargin, _leftClosedBasePos.y);
            Vector2 rightOffscreen = new Vector2(_rightClosedBasePos.x + rightWidth + offscreenExtraMargin, _rightClosedBasePos.y);

            leftPanelRect.anchoredPosition = leftOffscreen;
            rightPanelRect.anchoredPosition = rightOffscreen;
            leftPanelRect.localScale = Vector3.one;
            rightPanelRect.localScale = Vector3.one;

            centerEmblemRect.anchoredPosition = _emblemCenterBasePos;
            centerEmblemRect.localScale = Vector3.zero;
            centerEmblemRect.localRotation = Quaternion.identity;

            if (centerEmblemImage != null) centerEmblemImage.gameObject.SetActive(false);
            if (leftPanelImage != null) leftPanelImage.gameObject.SetActive(false);
            if (rightPanelImage != null) rightPanelImage.gameObject.SetActive(false);

            if (_toolkitOverlayRoot != null)
            {
                _toolkitOverlayRoot.style.display = DisplayStyle.None;
            }
        }

        public void TriggerFromCurrentSelection()
        {
            var faction = GameFlowDataStore.SelectedFaction;
            PlayTransition(faction, null);
        }

        /// <summary>
        /// Ejecuta las 5 fases de la transición cinematográfica con DOTween usando los recursos del proyecto (o los asignados en el Inspector).
        /// </summary>
        public void PlayTransition(FactionPlaneDefinition selectedFaction, Action onCompleteOverride = null)
        {
            if (_isTransitionRunning) return;

            EnsureSceneHierarchySetup();
            ResolveReferences();

            if (leftPanelRect == null || rightPanelRect == null || centerEmblemRect == null)
            {
                ExecuteScreenChange(onCompleteOverride);
                return;
            }

            // =========================================================================
            // FASE 1 — CONFIRMACIÓN DE LA NAVE
            // =========================================================================
            _isTransitionRunning = true;
            KillActiveSequence();

            // Garantizar tiempos cinematográficos pausados si la escena tenía cacheados los valores rápidos anteriores
            if (closeDuration < 0.6f) closeDuration = 0.85f;
            if (closeImpactDuration < 0.2f) closeImpactDuration = 0.25f;
            if (emblemAppearDuration < 0.6f) emblemAppearDuration = 0.85f;
            if (rotationDuration < 0.6f) rotationDuration = 0.80f;
            if (emblemSettleDuration < 0.28f) emblemSettleDuration = 0.35f;
            if (waitBeforeAscend < 0.8f) waitBeforeAscend = 1.15f;
            if (ascendDuration < 0.8f) ascendDuration = 1.10f;

            if (selectedFaction != null)
            {
                GameFlowDataStore.SaveFaction(selectedFaction);
            }
            else
            {
                selectedFaction = GameFlowDataStore.SelectedFaction;
            }

            SetSelectionControlsLocked(true);

            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(true);
                transitionCanvas.sortingOrder = 500;
            }

            // Asignar sprites (usa los asignados en el Inspector o toma automáticamente los recursos existentes del proyecto)
            ApplyFactionVisuals(selectedFaction);

            // Asegurar capa espejo en UIDocument para que se vea también en el Device Simulator
            EnsureToolkitOverlayElements(selectedFaction);

            CaptureBaseLayoutAndPlaceOffscreen();

            _panelsUnifiedAfterJoin = false;
            leftPanelImage.gameObject.SetActive(true);
            leftPanelImage.enabled = true;
            rightPanelImage.gameObject.SetActive(true);
            rightPanelImage.enabled = true;
            centerEmblemImage.gameObject.SetActive(true);
            centerEmblemRect.SetAsLastSibling();

            if (_toolkitOverlayRoot != null)
            {
                _toolkitOverlayRoot.style.display = DisplayStyle.Flex;
                _toolkitOverlayRoot.BringToFront();
            }
            if (_toolkitRightPanel != null)
            {
                _toolkitRightPanel.style.display = DisplayStyle.Flex;
            }

            float canvasW = GetCanvasWidth();
            float canvasH = GetCanvasHeight();
            float leftWidth = Mathf.Max(canvasW * 0.5f, leftPanelRect.rect.width);
            float rightWidth = Mathf.Max(canvasW * 0.5f, rightPanelRect.rect.width);

            Vector2 leftStartPos = new Vector2(_leftClosedBasePos.x - leftWidth - offscreenExtraMargin, _leftClosedBasePos.y);
            Vector2 rightStartPos = new Vector2(_rightClosedBasePos.x + rightWidth + offscreenExtraMargin, _rightClosedBasePos.y);

            leftPanelRect.anchoredPosition = leftStartPos;
            rightPanelRect.anchoredPosition = rightStartPos;
            leftPanelRect.localScale = Vector3.one;
            rightPanelRect.localScale = Vector3.one;

            centerEmblemRect.anchoredPosition = _emblemCenterBasePos;
            centerEmblemRect.localScale = Vector3.zero;
            centerEmblemRect.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);
            SetImageAlpha(centerEmblemImage, 0f);

            SyncToolkitOverlayFromRectTransforms();

            float effectiveAscendDistance = Mathf.Max(ascendDistance, canvasH + 400f);
            Vector2 ascendOffset = new Vector2(0f, effectiveAscendDistance);

            // =========================================================================
            // SECUENCIA DOTWEEN (FASES 2, 3, 4 y 5)
            // =========================================================================
            _activeSequence = DOTween.Sequence().SetUpdate(true);

            // -------------------------------------------------------------------------
            // FASE 2 — CIERRE DE LOS DOS PANELES HACIA EL CENTRO
            // -------------------------------------------------------------------------
            Tweener leftCloseTween = leftPanelRect.DOAnchorPos(_leftClosedBasePos, closeDuration);
            Tweener rightCloseTween = rightPanelRect.DOAnchorPos(_rightClosedBasePos, closeDuration);

            if (useCustomInspectorCurves && closeCurve != null && closeCurve.length > 0)
            {
                leftCloseTween.SetEase(closeCurve);
                rightCloseTween.SetEase(closeCurve);
            }
            else
            {
                leftCloseTween.SetEase(closeEase);
                rightCloseTween.SetEase(closeEase);
            }

            _activeSequence.Append(leftCloseTween);
            _activeSequence.Join(rightCloseTween);

            // Al unirse en el centro: unificar ambos lados en el cartel completo para eliminar cualquier línea divisoria central
            _activeSequence.AppendCallback(() =>
            {
                ProceduralAudio.PlayBlinkHeavy();
                leftPanelRect.anchoredPosition = _leftClosedBasePos;
                rightPanelRect.anchoredPosition = _rightClosedBasePos;
                leftPanelRect.localScale = Vector3.one;
                rightPanelRect.localScale = Vector3.one;

                if (_usingAutoSlicedTransitionPoster && _currentFullPosterSprite != null)
                {
                    _panelsUnifiedAfterJoin = true;
                    leftPanelRect.pivot = new Vector2(0.5f, 0.5f);
                    leftPanelRect.sizeDelta = new Vector2(canvasW, canvasH);
                    leftPanelRect.anchoredPosition = _leftClosedBasePos;
                    leftPanelImage.sprite = _currentFullPosterSprite;
                    rightPanelImage.enabled = false;
                }

                HidePreviousScreenBehindPanels();
                SyncToolkitOverlayFromRectTransforms();
            });

            // Pequeña pausa tras el cierre de paneles antes de que aparezca el emblema central
            _activeSequence.AppendInterval(0.22f);

            // -------------------------------------------------------------------------
            // FASE 3 — APARICIÓN DEL EMBLEMA CENTRAL (ESCALA + REBOTE + ROTACIÓN + GOLPE VISUAL)
            // -------------------------------------------------------------------------
            _activeSequence.AppendCallback(() =>
            {
                ProceduralAudio.PlayConfirm();
                centerEmblemRect.anchoredPosition = _emblemCenterBasePos;
                centerEmblemRect.localScale = _emblemNormalScale * Mathf.Max(0.01f, emblemInitialScale);
                centerEmblemRect.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);
                SetImageAlpha(centerEmblemImage, 1f);
                SyncToolkitOverlayFromRectTransforms();
            });

            Tweener emblemScaleTween = centerEmblemRect.DOScale(_emblemNormalScale, emblemAppearDuration);
            if (useCustomInspectorCurves && emblemAppearCurve != null && emblemAppearCurve.length > 0)
            {
                emblemScaleTween.SetEase(emblemAppearCurve);
            }
            else
            {
                emblemScaleTween.SetEase(emblemAppearEase, bounceIntensity);
            }

            Tweener emblemRotateTween = centerEmblemRect
                .DORotate(Vector3.zero, rotationDuration, RotateMode.Fast)
                .SetEase(Ease.OutCubic);

            _activeSequence.Append(emblemScaleTween);
            _activeSequence.Join(emblemRotateTween);

            // Pequeño golpe visual y movimiento de asentamiento
            if (emblemSettleDuration > 0.01f)
            {
                _activeSequence.AppendCallback(() => ProceduralAudio.PlayBlinkMid());
                _activeSequence.Append(centerEmblemRect.DOPunchScale(emblemImpactPunchScale, emblemSettleDuration, 6, 0.7f));
                _activeSequence.Join(centerEmblemRect.DOPunchAnchorPos(new Vector2(0f, emblemSettleVerticalOffset), emblemSettleDuration, 4, 0.5f));
            }

            // Mantener el emblema claramente visible durante un breve instante
            if (waitBeforeAscend > 0.001f)
            {
                _activeSequence.AppendInterval(waitBeforeAscend);
            }

            // -------------------------------------------------------------------------
            // FASE 4 — MOVIMIENTO ASCENDENTE COORDINADO (PANELES + EMBLEMA)
            // -------------------------------------------------------------------------
            _activeSequence.AppendCallback(() =>
            {
                ProceduralAudio.PlayBlinkLight();
                leftPanelRect.anchoredPosition = _leftClosedBasePos;
                rightPanelRect.anchoredPosition = _rightClosedBasePos;
                centerEmblemRect.anchoredPosition = _emblemCenterBasePos;

                if (nextScreenObject != null)
                {
                    nextScreenObject.SetActive(true);
                }
                SyncToolkitOverlayFromRectTransforms();
            });

            Tweener leftAscendTween = leftPanelRect.DOAnchorPos(_leftClosedBasePos + ascendOffset, ascendDuration);
            Tweener rightAscendTween = rightPanelRect.DOAnchorPos(_rightClosedBasePos + ascendOffset, ascendDuration);
            Tweener emblemAscendTween = centerEmblemRect.DOAnchorPos(_emblemCenterBasePos + ascendOffset, ascendDuration);

            if (useCustomInspectorCurves && ascendCurve != null && ascendCurve.length > 0)
            {
                leftAscendTween.SetEase(ascendCurve);
                rightAscendTween.SetEase(ascendCurve);
                emblemAscendTween.SetEase(ascendCurve);
            }
            else
            {
                leftAscendTween.SetEase(ascendEase);
                rightAscendTween.SetEase(ascendEase);
                emblemAscendTween.SetEase(ascendEase);
            }

            _activeSequence.Append(leftAscendTween);
            _activeSequence.Join(rightAscendTween);
            _activeSequence.Join(emblemAscendTween);

            // -------------------------------------------------------------------------
            // FASE 5 — CAMBIO DE PANTALLA Y LIMPIEZA DE ESTADOS
            // -------------------------------------------------------------------------
            _activeSequence.OnComplete(() =>
            {
                ExecuteScreenChange(onCompleteOverride);
            });
        }

        /// <summary>
        /// Asigna los sprites de los paneles izquierdo/derecho y del emblema central.
        /// Toma automáticamente la imagen "fondo" de la facción seleccionada, la corta a la mitad (50% izquierda / 50% derecha)
        /// y une ambas mitades en el centro durante la transición.
        /// </summary>
        public void ApplyFactionVisuals(FactionPlaneDefinition faction)
        {
            if (faction == null) return;

            EnsureTransitionBackgroundsLoaded();

            Sprite chosenEmblem = null;
            Sprite leftOverride = _userCustomLeftSprite;
            Sprite rightOverride = _userCustomRightSprite;

            if (factionVisualOverrides != null)
            {
                for (int i = 0; i < factionVisualOverrides.Count; i++)
                {
                    var entry = factionVisualOverrides[i];
                    if (entry != null && entry.factionId == faction.factionId)
                    {
                        if (entry.emblemSprite != null) chosenEmblem = entry.emblemSprite;
                        if (entry.leftPanelOverride != null) leftOverride = entry.leftPanelOverride;
                        if (entry.rightPanelOverride != null) rightOverride = entry.rightPanelOverride;
                        break;
                    }
                }
            }

            if (chosenEmblem == null)
            {
                chosenEmblem = GetEmblemForFactionId(faction.factionId);
            }
            if (chosenEmblem == null && faction.factionEmblemSprite != null)
            {
                chosenEmblem = faction.factionEmblemSprite;
            }

            // Si no se asignaron mitades manuales, cortar automáticamente la imagen "fondo" de la facción a la mitad
            if (leftOverride == null || rightOverride == null)
            {
                _usingAutoSlicedTransitionPoster = true;
                _currentFullPosterSprite = GetTransitionBgForFactionId(faction.factionId);
                if (_currentFullPosterSprite == null && selectionController != null)
                {
                    _currentFullPosterSprite = selectionController.GetFrameForFaction(faction.factionId);
                }
                if (_currentFullPosterSprite != null && _currentFullPosterSprite.texture != null)
                {
                    Texture2D tex = _currentFullPosterSprite.texture;
                    _currentFullPosterSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }

                GetOrCreateFactionHalfSpritesFromProjectResources(faction.factionId, out Sprite autoLeft, out Sprite autoRight);
                if (leftOverride == null) leftOverride = autoLeft;
                if (rightOverride == null) rightOverride = autoRight;
            }
            else
            {
                _usingAutoSlicedTransitionPoster = false;
                _currentFullPosterSprite = null;
            }

            if (chosenEmblem == null)
            {
                chosenEmblem = GetOrCreateFactionEmblemFromProjectResources(faction.factionId);
            }

            if (leftPanelImage != null)
            {
                leftPanelImage.sprite = leftOverride;
                leftPanelImage.color = Color.white;
            }

            if (rightPanelImage != null)
            {
                rightPanelImage.sprite = rightOverride;
                rightPanelImage.color = Color.white;
            }

            if (centerEmblemImage != null)
            {
                centerEmblemImage.sprite = chosenEmblem;
                centerEmblemImage.color = Color.white;
            }
        }

        public void EnsureTransitionBackgroundsLoaded()
        {
#if UNITY_EDITOR
            if (anarquistaTransitionBg == null)
                anarquistaTransitionBg = LoadSpriteAtPath("Assets/Art/Interfaces/Anarquista/fondo (2).png");
            if (capitalistaTransitionBg == null)
                capitalistaTransitionBg = LoadSpriteAtPath("Assets/Art/Interfaces/Capitalista/fondo (3).png");
            if (comunistaTransitionBg == null)
                comunistaTransitionBg = LoadSpriteAtPath("Assets/Art/Interfaces/Comunista/fondo (1).png");
            if (fascistaTransitionBg == null)
                fascistaTransitionBg = LoadSpriteAtPath("Assets/Art/Interfaces/Facista/fondo (4).png");

            // Cargar automáticamente las imágenes "logo" de cada carpeta de facción (reemplazando si tenían el marco vertical provisional)
            if (IsMissingOrLegacyEmblem(anarquistaEmblem))
                anarquistaEmblem = LoadCenteredFullSpriteAtPath("Assets/Art/Interfaces/Anarquista/logo (2).png");
            if (IsMissingOrLegacyEmblem(capitalistaEmblem))
                capitalistaEmblem = LoadCenteredFullSpriteAtPath("Assets/Art/Interfaces/Capitalista/logo (1).png");
            if (IsMissingOrLegacyEmblem(comunistaEmblem))
                comunistaEmblem = LoadCenteredFullSpriteAtPath("Assets/Art/Interfaces/Comunista/logo (4).png");
            if (IsMissingOrLegacyEmblem(fascistaEmblem))
                fascistaEmblem = LoadCenteredFullSpriteAtPath("Assets/Art/Interfaces/Facista/logo (3).png");
#endif
            if (anarquistaTransitionBg == null || capitalistaTransitionBg == null || comunistaTransitionBg == null || fascistaTransitionBg == null ||
                IsMissingOrLegacyEmblem(anarquistaEmblem) || IsMissingOrLegacyEmblem(capitalistaEmblem) ||
                IsMissingOrLegacyEmblem(comunistaEmblem) || IsMissingOrLegacyEmblem(fascistaEmblem))
            {
                var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var s in sprites)
                {
                    if (s == null) continue;
                    string n = s.name.ToLower();
                    if (comunistaTransitionBg == null && n.Contains("fondo (1)")) comunistaTransitionBg = s;
                    if (anarquistaTransitionBg == null && n.Contains("fondo (2)")) anarquistaTransitionBg = s;
                    if (capitalistaTransitionBg == null && n.Contains("fondo (3)")) capitalistaTransitionBg = s;
                    if (fascistaTransitionBg == null && n.Contains("fondo (4)")) fascistaTransitionBg = s;

                    if (IsMissingOrLegacyEmblem(capitalistaEmblem) && n.Contains("logo (1)")) capitalistaEmblem = CreateCenteredFullSpriteFromTexture(s);
                    if (IsMissingOrLegacyEmblem(anarquistaEmblem) && n.Contains("logo (2)")) anarquistaEmblem = CreateCenteredFullSpriteFromTexture(s);
                    if (IsMissingOrLegacyEmblem(fascistaEmblem) && n.Contains("logo (3)")) fascistaEmblem = CreateCenteredFullSpriteFromTexture(s);
                    if (IsMissingOrLegacyEmblem(comunistaEmblem) && n.Contains("logo (4)")) comunistaEmblem = CreateCenteredFullSpriteFromTexture(s);
                }
            }
        }

        private static bool IsMissingOrLegacyEmblem(Sprite s)
        {
            if (s == null) return true;
            string n = s.name.ToLower();
            return n.Contains("frame_vertical") || n.Contains("vertical_");
        }

        private static Sprite CreateCenteredFullSpriteFromTexture(Sprite sourceSprite)
        {
            if (sourceSprite == null) return null;
            if (sourceSprite.texture != null)
            {
                Texture2D tex = sourceSprite.texture;
                Sprite centered = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                centered.name = $"{tex.name}_CenteredLogo";
                return centered;
            }
            return sourceSprite;
        }

#if UNITY_EDITOR
        private static Sprite LoadCenteredFullSpriteAtPath(string path)
        {
            var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                Sprite centered = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                centered.name = $"{tex.name}_CenteredLogo";
                return centered;
            }
            return LoadSpriteAtPath(path);
        }

        private static Sprite LoadSpriteAtPath(string path)
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

        public Sprite GetTransitionBgForFactionId(GameFactionId factionId)
        {
            EnsureTransitionBackgroundsLoaded();
            switch (factionId)
            {
                case GameFactionId.Anarquistas:
                    return anarquistaTransitionBg;
                case GameFactionId.Capitalistas:
                    return capitalistaTransitionBg;
                case GameFactionId.Comunistas:
                    return comunistaTransitionBg;
                case GameFactionId.Fascistas:
                    return fascistaTransitionBg;
                default:
                    return anarquistaTransitionBg;
            }
        }

        public Sprite GetEmblemForFactionId(GameFactionId factionId)
        {
            EnsureTransitionBackgroundsLoaded();
            Sprite raw = null;
            switch (factionId)
            {
                case GameFactionId.Anarquistas:
                    raw = anarquistaEmblem;
                    break;
                case GameFactionId.Capitalistas:
                    raw = capitalistaEmblem;
                    break;
                case GameFactionId.Comunistas:
                    raw = comunistaEmblem;
                    break;
                case GameFactionId.Fascistas:
                    raw = fascistaEmblem;
                    break;
                default:
                    raw = anarquistaEmblem;
                    break;
            }

            if (raw != null && raw.texture != null && !raw.name.EndsWith("_CenteredLogo"))
            {
                if (_cachedEmblemSprites.TryGetValue(factionId, out Sprite cachedCentered) && cachedCentered != null)
                {
                    return cachedCentered;
                }
                Sprite centered = CreateCenteredFullSpriteFromTexture(raw);
                _cachedEmblemSprites[factionId] = centered;
                return centered;
            }
            return raw;
        }

        private void GetOrCreateFactionHalfSpritesFromProjectResources(GameFactionId factionId, out Sprite leftHalf, out Sprite rightHalf)
        {
            if (_cachedLeftHalfSprites.TryGetValue(factionId, out leftHalf) &&
                _cachedRightHalfSprites.TryGetValue(factionId, out rightHalf) &&
                leftHalf != null && rightHalf != null)
            {
                return;
            }

            // Priorizar la imagen "fondo (1..4).png" de transición de cada facción; si no existe, usar el marco de respaldo
            Sprite fullPoster = GetTransitionBgForFactionId(factionId);
            if (fullPoster == null && selectionController != null)
            {
                fullPoster = selectionController.GetFrameForFaction(factionId);
            }

            if (fullPoster != null && fullPoster.texture != null)
            {
                Texture2D tex = fullPoster.texture;
                float halfW = tex.width * 0.5f;
                float fullH = tex.height;

                leftHalf = Sprite.Create(tex, new Rect(0f, 0f, halfW, fullH), new Vector2(1f, 0.5f), 100f);
                leftHalf.name = $"{tex.name}_LeftHalf";

                rightHalf = Sprite.Create(tex, new Rect(halfW, 0f, halfW, fullH), new Vector2(0f, 0.5f), 100f);
                rightHalf.name = $"{tex.name}_RightHalf";

                _cachedLeftHalfSprites[factionId] = leftHalf;
                _cachedRightHalfSprites[factionId] = rightHalf;
                return;
            }

            leftHalf = ProceduralShapeGenerator.GetSquareSprite();
            rightHalf = ProceduralShapeGenerator.GetSquareSprite();
        }

        private Sprite GetOrCreateFactionEmblemFromProjectResources(GameFactionId factionId)
        {
            if (_cachedEmblemSprites.TryGetValue(factionId, out Sprite cached) && cached != null)
            {
                return cached;
            }

            // Usar el marco vertical de la facción existente en el proyecto como base del emblema
            Sprite verticalFrame = selectionController != null ? selectionController.GetPlaneFrameForFaction(factionId) : null;
            if (verticalFrame != null)
            {
                _cachedEmblemSprites[factionId] = verticalFrame;
                return verticalFrame;
            }

            return selectionController != null && selectionController.planeSprite != null
                ? selectionController.planeSprite
                : ProceduralShapeGenerator.GetCircleSprite();
        }

        // =========================================================================
        // SINCRONIZACIÓN ESPEJO CON UI TOOLKIT (Para soporte en Device Simulator)
        // =========================================================================
        private void EnsureToolkitOverlayElements(FactionPlaneDefinition faction)
        {
            if (currentScreenUIDocument == null || currentScreenUIDocument.rootVisualElement == null) return;
            VisualElement root = currentScreenUIDocument.rootVisualElement;

            if (_toolkitOverlayRoot == null || _toolkitOverlayRoot.parent != root)
            {
                _toolkitOverlayRoot = root.Q<VisualElement>("cinematic-transition-overlay");
                if (_toolkitOverlayRoot == null)
                {
                    _toolkitOverlayRoot = new VisualElement { name = "cinematic-transition-overlay" };
                    _toolkitOverlayRoot.style.position = Position.Absolute;
                    _toolkitOverlayRoot.style.left = 0;
                    _toolkitOverlayRoot.style.right = 0;
                    _toolkitOverlayRoot.style.top = 0;
                    _toolkitOverlayRoot.style.bottom = 0;
                    _toolkitOverlayRoot.style.overflow = Overflow.Hidden;
                    _toolkitOverlayRoot.pickingMode = PickingMode.Position;

                    _toolkitLeftPanel = new VisualElement { name = "transition-left-panel" };
                    _toolkitLeftPanel.style.position = Position.Absolute;
                    _toolkitLeftPanel.style.backgroundColor = Color.black;
                    _toolkitLeftPanel.style.overflow = Overflow.Hidden;

                    _toolkitLeftInnerImage = new VisualElement { name = "transition-left-inner" };
                    _toolkitLeftInnerImage.style.position = Position.Absolute;
                    _toolkitLeftInnerImage.style.top = 0;
                    _toolkitLeftInnerImage.style.bottom = 0;
                    _toolkitLeftInnerImage.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(Length.Percent(100f), Length.Percent(100f)));
                    _toolkitLeftInnerImage.style.backgroundRepeat = new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat));
                    _toolkitLeftPanel.Add(_toolkitLeftInnerImage);

                    _toolkitRightPanel = new VisualElement { name = "transition-right-panel" };
                    _toolkitRightPanel.style.position = Position.Absolute;
                    _toolkitRightPanel.style.backgroundColor = Color.black;
                    _toolkitRightPanel.style.overflow = Overflow.Hidden;

                    _toolkitRightInnerImage = new VisualElement { name = "transition-right-inner" };
                    _toolkitRightInnerImage.style.position = Position.Absolute;
                    _toolkitRightInnerImage.style.top = 0;
                    _toolkitRightInnerImage.style.bottom = 0;
                    _toolkitRightInnerImage.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(Length.Percent(100f), Length.Percent(100f)));
                    _toolkitRightInnerImage.style.backgroundRepeat = new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat));
                    _toolkitRightPanel.Add(_toolkitRightInnerImage);

                    _toolkitEmblemBox = new VisualElement { name = "transition-center-emblem" };
                    _toolkitEmblemBox.style.position = Position.Absolute;
                    _toolkitEmblemBox.style.backgroundColor = Color.clear;
                    _toolkitEmblemBox.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
                    _toolkitEmblemBox.style.backgroundRepeat = new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat));
                    _toolkitEmblemBox.style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
                    _toolkitEmblemBox.style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
                    _toolkitEmblemBox.style.alignItems = Align.Center;
                    _toolkitEmblemBox.style.justifyContent = Justify.Center;
                    _toolkitEmblemBox.style.transformOrigin = new StyleTransformOrigin(new TransformOrigin(Length.Percent(50f), Length.Percent(50f), 0f));

                    _toolkitEmblemInnerPlane = new VisualElement { name = "transition-emblem-plane" };
                    _toolkitEmblemInnerPlane.style.width = Length.Percent(58f);
                    _toolkitEmblemInnerPlane.style.height = Length.Percent(58f);
                    _toolkitEmblemInnerPlane.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
                    _toolkitEmblemInnerPlane.style.backgroundRepeat = new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat));
                    _toolkitEmblemInnerPlane.style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
                    _toolkitEmblemInnerPlane.style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Center));
                    _toolkitEmblemBox.Add(_toolkitEmblemInnerPlane);

                    _toolkitOverlayRoot.Add(_toolkitLeftPanel);
                    _toolkitOverlayRoot.Add(_toolkitRightPanel);
                    _toolkitOverlayRoot.Add(_toolkitEmblemBox);
                    root.Add(_toolkitOverlayRoot);
                }
                else
                {
                    _toolkitLeftPanel = _toolkitOverlayRoot.Q<VisualElement>("transition-left-panel");
                    _toolkitLeftInnerImage = _toolkitOverlayRoot.Q<VisualElement>("transition-left-inner");
                    _toolkitRightPanel = _toolkitOverlayRoot.Q<VisualElement>("transition-right-panel");
                    _toolkitRightInnerImage = _toolkitOverlayRoot.Q<VisualElement>("transition-right-inner");
                    _toolkitEmblemBox = _toolkitOverlayRoot.Q<VisualElement>("transition-center-emblem");
                    _toolkitEmblemInnerPlane = _toolkitOverlayRoot.Q<VisualElement>("transition-emblem-plane");
                }
            }

            Sprite fullTransitionBg = faction != null ? GetTransitionBgForFactionId(faction.factionId) : null;
            if (fullTransitionBg == null && faction != null && selectionController != null)
            {
                fullTransitionBg = selectionController.GetFrameForFaction(faction.factionId);
            }

            if (_usingAutoSlicedTransitionPoster && fullTransitionBg != null && fullTransitionBg.texture != null)
            {
                // Usar la textura completa recortada al 50% izquierdo y 50% derecho para que encajen con precisión de píxel al juntarse
                if (_toolkitLeftInnerImage != null)
                {
                    _toolkitLeftInnerImage.style.backgroundImage = new StyleBackground(fullTransitionBg.texture);
                }
                if (_toolkitRightInnerImage != null)
                {
                    _toolkitRightInnerImage.style.backgroundImage = new StyleBackground(fullTransitionBg.texture);
                }
            }
            else
            {
                if (_toolkitLeftInnerImage != null && leftPanelImage != null && leftPanelImage.sprite != null)
                {
                    _toolkitLeftInnerImage.style.backgroundImage = new StyleBackground(leftPanelImage.sprite);
                }
                if (_toolkitRightInnerImage != null && rightPanelImage != null && rightPanelImage.sprite != null)
                {
                    _toolkitRightInnerImage.style.backgroundImage = new StyleBackground(rightPanelImage.sprite);
                }
            }

            if (_toolkitEmblemBox != null && centerEmblemImage != null && centerEmblemImage.sprite != null)
            {
                _toolkitEmblemBox.style.backgroundColor = Color.clear;
                if (centerEmblemImage.sprite.texture != null)
                {
                    _toolkitEmblemBox.style.backgroundImage = new StyleBackground(centerEmblemImage.sprite.texture);
                }
                else
                {
                    _toolkitEmblemBox.style.backgroundImage = new StyleBackground(centerEmblemImage.sprite);
                }
            }

            // Si el emblema actual es el logo de la facción, ocultar el caza superpuesto
            bool isVerticalFrameEmblem = centerEmblemImage != null && centerEmblemImage.sprite != null && centerEmblemImage.sprite.name.ToLower().Contains("vertical");
            if (_toolkitEmblemInnerPlane != null)
            {
                if (isVerticalFrameEmblem && selectionController != null && selectionController.planeSprite != null && faction != null)
                {
                    _toolkitEmblemInnerPlane.style.display = DisplayStyle.Flex;
                    _toolkitEmblemInnerPlane.style.backgroundImage = new StyleBackground(selectionController.planeSprite);
                    _toolkitEmblemInnerPlane.style.unityBackgroundImageTintColor = faction.accentColor;
                }
                else
                {
                    _toolkitEmblemInnerPlane.style.display = DisplayStyle.None;
                }
            }
        }

        private void SyncToolkitOverlayFromRectTransforms()
        {
            if (_toolkitOverlayRoot == null || leftPanelRect == null || rightPanelRect == null || centerEmblemRect == null) return;
            if (currentScreenUIDocument == null || currentScreenUIDocument.rootVisualElement == null) return;

            VisualElement root = currentScreenUIDocument.rootVisualElement;
            float uiW = root.resolvedStyle.width;
            float uiH = root.resolvedStyle.height;
            if (float.IsNaN(uiW) || uiW <= 10f) uiW = 1080f;
            if (float.IsNaN(uiH) || uiH <= 10f) uiH = 1920f;

            float canvasW = GetCanvasWidth();
            float canvasH = GetCanvasHeight();
            float scaleX = uiW / canvasW;
            float scaleY = uiH / canvasH;

            float halfUiW = uiW * 0.5f;
            float splitX = Mathf.Floor(halfUiW);

            if (_panelsUnifiedAfterJoin && _usingAutoSlicedTransitionPoster)
            {
                // Una vez unidos en el centro, mostrar el cartel completo de una sola pieza sin costura ni línea central
                if (_toolkitLeftPanel != null)
                {
                    float topY = -(leftPanelRect.anchoredPosition.y * scaleY);
                    _toolkitLeftPanel.style.width = uiW;
                    _toolkitLeftPanel.style.height = uiH;
                    _toolkitLeftPanel.style.left = 0f;
                    _toolkitLeftPanel.style.top = topY;
                    _toolkitLeftPanel.style.scale = new StyleScale(new Scale(Vector3.one));

                    if (_toolkitLeftInnerImage != null)
                    {
                        _toolkitLeftInnerImage.style.left = 0f;
                        _toolkitLeftInnerImage.style.width = uiW;
                        _toolkitLeftInnerImage.style.height = uiH;
                    }
                }
                if (_toolkitRightPanel != null)
                {
                    _toolkitRightPanel.style.display = DisplayStyle.None;
                }
            }
            else
            {
                // Sincronizar Panel Izquierdo (mitad izquierda 0%..50% de "fondo")
                if (_toolkitLeftPanel != null)
                {
                    float panelW = Mathf.Ceil(halfUiW);
                    float panelH = uiH;
                    float offsetLeft = leftPanelRect.anchoredPosition.x * scaleX;
                    float topY = -(leftPanelRect.anchoredPosition.y * scaleY);

                    _toolkitLeftPanel.style.width = panelW;
                    _toolkitLeftPanel.style.height = panelH;
                    _toolkitLeftPanel.style.left = offsetLeft;
                    _toolkitLeftPanel.style.top = topY;
                    _toolkitLeftPanel.style.scale = new StyleScale(new Scale(leftPanelRect.localScale));

                    if (_toolkitLeftInnerImage != null)
                    {
                        _toolkitLeftInnerImage.style.left = 0f;
                        _toolkitLeftInnerImage.style.width = _usingAutoSlicedTransitionPoster ? uiW : panelW;
                        _toolkitLeftInnerImage.style.height = panelH;
                    }
                }

                // Sincronizar Panel Derecho (mitad derecha 50%..100% de "fondo")
                if (_toolkitRightPanel != null)
                {
                    _toolkitRightPanel.style.display = DisplayStyle.Flex;
                    float panelW = uiW - splitX;
                    float panelH = uiH;
                    float offsetRight = rightPanelRect.anchoredPosition.x * scaleX;
                    float topY = -(rightPanelRect.anchoredPosition.y * scaleY);

                    _toolkitRightPanel.style.width = panelW;
                    _toolkitRightPanel.style.height = panelH;
                    _toolkitRightPanel.style.left = splitX + offsetRight;
                    _toolkitRightPanel.style.top = topY;
                    _toolkitRightPanel.style.scale = new StyleScale(new Scale(rightPanelRect.localScale));

                    if (_toolkitRightInnerImage != null)
                    {
                        _toolkitRightInnerImage.style.left = _usingAutoSlicedTransitionPoster ? -splitX : 0f;
                        _toolkitRightInnerImage.style.width = _usingAutoSlicedTransitionPoster ? uiW : panelW;
                        _toolkitRightInnerImage.style.height = panelH;
                    }
                }
            }

            // Sincronizar Logo / Emblema Central (centrado en pantalla, con escala, rebote, rotación y ascenso)
            if (_toolkitEmblemBox != null)
            {
                float embW = uiW * 0.56f;
                float embH = embW;
                float centerX = halfUiW + (centerEmblemRect.anchoredPosition.x * scaleX);
                float centerY = (uiH * 0.5f) - (centerEmblemRect.anchoredPosition.y * scaleY);

                _toolkitEmblemBox.style.width = embW;
                _toolkitEmblemBox.style.height = embH;
                _toolkitEmblemBox.style.left = centerX - (embW * 0.5f);
                _toolkitEmblemBox.style.top = centerY - (embH * 0.5f);
                _toolkitEmblemBox.style.scale = new StyleScale(new Scale(centerEmblemRect.localScale));
                _toolkitEmblemBox.style.rotate = new StyleRotate(new Rotate(new Angle(centerEmblemRect.localEulerAngles.z, AngleUnit.Degree)));
                _toolkitEmblemBox.style.opacity = centerEmblemImage != null ? centerEmblemImage.color.a : 1f;
            }
        }

        private void HidePreviousScreenBehindPanels()
        {
            if (currentScreenUIDocument != null && currentScreenUIDocument.rootVisualElement != null)
            {
                var root = currentScreenUIDocument.rootVisualElement;
                var carousel = root.Q<VisualElement>("carousel-container");
                var dots = root.Q<VisualElement>("carousel-dots-row");
                if (carousel != null) carousel.style.visibility = Visibility.Hidden;
                if (dots != null) dots.style.visibility = Visibility.Hidden;
            }
        }

        private void ExecuteScreenChange(Action onCompleteOverride)
        {
            ResetTransitionState(restorePreviousScreenVisibility: false);

            if (onCompleteOverride != null)
            {
                onCompleteOverride.Invoke();
                return;
            }

            if (nextScreenObject != null)
            {
                nextScreenObject.SetActive(true);
            }

            string targetScene = !string.IsNullOrEmpty(nextSceneName)
                ? nextSceneName
                : (selectionController != null ? selectionController.nextSceneName : "2_StoryCinematic");

            if (!string.IsNullOrEmpty(targetScene))
            {
                SceneManager.LoadScene(targetScene);
            }
        }

        public void ResetTransitionState(bool restorePreviousScreenVisibility = true)
        {
            KillActiveSequence();
            _isTransitionRunning = false;
            _panelsUnifiedAfterJoin = false;

            PlaceElementsInInitialOffscreenState();
            SetSelectionControlsLocked(false);

            if (restorePreviousScreenVisibility && currentScreenUIDocument != null && currentScreenUIDocument.rootVisualElement != null)
            {
                var root = currentScreenUIDocument.rootVisualElement;
                var carousel = root.Q<VisualElement>("carousel-container");
                var dots = root.Q<VisualElement>("carousel-dots-row");
                if (carousel != null) carousel.style.visibility = Visibility.Visible;
                if (dots != null) dots.style.visibility = Visibility.Visible;
            }
        }

        private void SetSelectionControlsLocked(bool locked)
        {
            if (selectionController != null)
            {
                selectionController.SetInteractionLocked(locked);
            }
            if (optionalConfirmButton != null)
            {
                optionalConfirmButton.interactable = !locked;
            }
        }

        private void KillActiveSequence()
        {
            if (_activeSequence != null)
            {
                _activeSequence.Kill(false);
                _activeSequence = null;
            }

            if (leftPanelRect != null) DOTween.Kill(leftPanelRect, false);
            if (rightPanelRect != null) DOTween.Kill(rightPanelRect, false);
            if (centerEmblemRect != null) DOTween.Kill(centerEmblemRect, false);
        }

        private float GetCanvasWidth()
        {
            if (transitionCanvas != null)
            {
                RectTransform canvasRt = transitionCanvas.GetComponent<RectTransform>();
                if (canvasRt != null && canvasRt.rect.width > 10f)
                {
                    return canvasRt.rect.width;
                }
            }
            return 1080f;
        }

        private float GetCanvasHeight()
        {
            if (transitionCanvas != null)
            {
                RectTransform canvasRt = transitionCanvas.GetComponent<RectTransform>();
                if (canvasRt != null && canvasRt.rect.height > 10f)
                {
                    return canvasRt.rect.height;
                }
            }
            return 1920f;
        }

        private static void SetImageAlpha(UnityEngine.UI.Image img, float alpha)
        {
            if (img == null) return;
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }

        public void EnsureSceneHierarchySetup()
        {
            if (transitionCanvas == null)
            {
                GameObject existingCanvasObj = GameObject.Find("Canvas_CinematicTransition");
                if (existingCanvasObj != null)
                {
                    transitionCanvas = existingCanvasObj.GetComponent<Canvas>();
                }

                if (transitionCanvas == null)
                {
                    GameObject canvasObj = new GameObject("Canvas_CinematicTransition");
                    transitionCanvas = canvasObj.AddComponent<Canvas>();
                    transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    transitionCanvas.sortingOrder = 500;

                    CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1080f, 1920f);
                    scaler.matchWidthOrHeight = 0.5f;

                    canvasObj.AddComponent<GraphicRaycaster>();
                }
            }

            Transform canvasTr = transitionCanvas.transform;

            if (leftPanelImage == null)
            {
                Transform existingLeft = canvasTr.Find("LeftPanel");
                GameObject leftObj = existingLeft != null ? existingLeft.gameObject : new GameObject("LeftPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                if (existingLeft == null) leftObj.transform.SetParent(canvasTr, false);
                leftPanelImage = leftObj.GetComponent<UnityEngine.UI.Image>();
                leftPanelImage.raycastTarget = true;
            }
            leftPanelRect = leftPanelImage.rectTransform;

            if (rightPanelImage == null)
            {
                Transform existingRight = canvasTr.Find("RightPanel");
                GameObject rightObj = existingRight != null ? existingRight.gameObject : new GameObject("RightPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                if (existingRight == null) rightObj.transform.SetParent(canvasTr, false);
                rightPanelImage = rightObj.GetComponent<UnityEngine.UI.Image>();
                rightPanelImage.raycastTarget = true;
            }
            rightPanelRect = rightPanelImage.rectTransform;

            if (centerEmblemImage == null)
            {
                Transform existingEmblem = canvasTr.Find("CenterEmblem");
                GameObject emblemObj = existingEmblem != null ? existingEmblem.gameObject : new GameObject("CenterEmblem", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                if (existingEmblem == null) emblemObj.transform.SetParent(canvasTr, false);
                centerEmblemImage = emblemObj.GetComponent<UnityEngine.UI.Image>();
                centerEmblemImage.preserveAspect = true;
                centerEmblemImage.raycastTarget = false;
                centerEmblemImage.rectTransform.sizeDelta = new Vector2(460f, 620f);
            }
            centerEmblemRect = centerEmblemImage.rectTransform;

            centerEmblemRect.SetAsLastSibling();
        }
    }
}
