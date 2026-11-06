using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using WorldGen.Core;

namespace WorldGen.Flow
{
    /// <summary>
    /// Controlador integral 100% basado en UI Toolkit (UIDocument / VisualElement).
    /// Ejecuta:
    /// 1. Logo y marca personalizable.
    /// 2. Selección de las 4 facciones utilizando el Sprite del caza japonés JAP_Ki21 con tintado y materiales.
    /// 3. Cinemática narrativa con texto ascendente suave.
    /// 4. Despliegue directo al propio juego (primera misión / tutorial in-game) mediante MissionDeploymentData.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameFlowToolkitController : MonoBehaviour
    {
        public enum FlowStep
        {
            BrandIntro = 0,
            FactionSelection = 1,
            StoryCinematic = 2
        }

        [Header("🎮 Navegación")]
        public FlowStep currentStep = FlowStep.BrandIntro;
        public string gameplaySceneName = "gameplay";
        public string menuSceneName = "menu";

        [Header("✈️ Sprite del Caza Japonés Ki-21")]
        public Sprite japaneseKi21Sprite;

        [Header("🏷️ Personalización de Logo y Marca")]
        public string brandTitle = "REIKO ESTUDIOS";
        public string brandSubtitle = "DIESELPUNK RETROFUTURISTA PRESENTA";
        public float brandDuration = 3.2f;

        // Elementos de UI Toolkit
        private UIDocument uiDocument;
        private VisualElement root;

        // 1. Brand
        private VisualElement stepBrand;
        private Label lblBrandTitle;
        private Label lblBrandSubtitle;

        // 2. Factions
        private VisualElement stepFactions;
        private VisualElement planeCardView;
        private VisualElement planeSpriteDisplay;
        private Label lblPlaneCode;
        private Label lblPlaneSlogan;
        private Label lblPhilosophy;
        private Label valEngine;
        private Label valArmor;
        private Label valAgility;
        private Label valDispersion;
        private Button btnConfirmFaction;
        private List<Button> factionButtons = new List<Button>();

        // 3. Cinematic
        private VisualElement stepCinematic;
        private VisualElement cinematicScroller;
        private Label lblCinematicStory;
        private Button btnPause;
        private Label lblPauseText;
        private Button btnFF;
        private Button btnSkip;

        private List<FactionPlaneDefinition> factions;
        private int selectedFactionIndex = 0;
        private bool isCinematicPaused = false;
        private bool isFastForward = false;
        private float scrollPos = 600f;
        private float scrollSpeed = 50f;
        private bool hasFinishedCinematic = false;

        private void OnEnable()
        {
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null || uiDocument.rootVisualElement == null) return;

            root = uiDocument.rootVisualElement;
            factions = GameFlowDataStore.GetAllFactions();

            // Si no se asignó manualmente el sprite del Ki-21, buscarlo en los assets
            if (japaneseKi21Sprite == null)
            {
                var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var s in sprites)
                {
                    if (s != null && s.name.Contains("JAP_Ki21"))
                    {
                        japaneseKi21Sprite = s;
                        break;
                    }
                }
            }

            BindUIElements();
            SetStep(currentStep);
        }

        private void BindUIElements()
        {
            // Paso 1: Logo
            stepBrand = root.Q<VisualElement>("step-brand");
            lblBrandTitle = root.Q<Label>("lbl-brand-title");
            lblBrandSubtitle = root.Q<Label>("lbl-brand-subtitle");

            if (lblBrandTitle != null) lblBrandTitle.text = brandTitle;
            if (lblBrandSubtitle != null) lblBrandSubtitle.text = brandSubtitle;

            if (stepBrand != null)
            {
                stepBrand.RegisterCallback<ClickEvent>(evt =>
                {
                    ProceduralAudio.PlayTick();
                    SetStep(FlowStep.FactionSelection);
                });
            }

            // Paso 2: Facciones
            stepFactions = root.Q<VisualElement>("step-factions");
            planeCardView = root.Q<VisualElement>("plane-card-view");
            planeSpriteDisplay = root.Q<VisualElement>("plane-sprite-display");
            lblPlaneCode = root.Q<Label>("lbl-plane-code");
            lblPlaneSlogan = root.Q<Label>("lbl-plane-slogan");
            lblPhilosophy = root.Q<Label>("lbl-philosophy");

            valEngine = root.Q<Label>("val-stat-engine");
            valArmor = root.Q<Label>("val-stat-armor");
            valAgility = root.Q<Label>("val-stat-agility");
            valDispersion = root.Q<Label>("val-stat-dispersion");
            btnConfirmFaction = root.Q<Button>("btn-confirm-faction");

            if (japaneseKi21Sprite != null && planeSpriteDisplay != null)
            {
                planeSpriteDisplay.style.backgroundImage = new StyleBackground(japaneseKi21Sprite);
            }

            factionButtons.Clear();
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                Button btn = root.Q<Button>($"btn-faction-{i}");
                if (btn != null)
                {
                    btn.clicked += () => SelectFaction(idx);
                    factionButtons.Add(btn);
                }
            }

            if (btnConfirmFaction != null)
            {
                btnConfirmFaction.clicked += () =>
                {
                    ProceduralAudio.PlayConfirm();
                    var chosen = (factions != null && selectedFactionIndex >= 0 && selectedFactionIndex < factions.Count)
                        ? factions[selectedFactionIndex]
                        : GameFlowDataStore.SelectedFaction;
                    GameFlowDataStore.SaveFaction(chosen);
                    var transition = Object.FindAnyObjectByType<CinematicFactionTransition>();
                    if (transition != null && transition.isActiveAndEnabled)
                    {
                        btnConfirmFaction.SetEnabled(false);
                        transition.PlayTransition(chosen, () =>
                        {
                            btnConfirmFaction.SetEnabled(true);
                            SetStep(FlowStep.StoryCinematic);
                        });
                        return;
                    }
                    SetStep(FlowStep.StoryCinematic);
                };
            }

            // Paso 3: Cinemática
            stepCinematic = root.Q<VisualElement>("step-cinematic");
            cinematicScroller = root.Q<VisualElement>("cinematic-scroller");
            lblCinematicStory = root.Q<Label>("lbl-cinematic-story");
            btnPause = root.Q<Button>("btn-cinematic-pause");
            lblPauseText = root.Q<Label>("lbl-pause-text");
            btnFF = root.Q<Button>("btn-cinematic-ff");
            btnSkip = root.Q<Button>("btn-cinematic-skip");

            if (btnPause != null)
            {
                btnPause.clicked += () =>
                {
                    isCinematicPaused = !isCinematicPaused;
                    ProceduralAudio.PlayTick();
                    if (lblPauseText != null) lblPauseText.text = isCinematicPaused ? "▶ REANUDAR" : "⏸ PAUSAR";
                };
            }

            if (btnFF != null)
            {
                btnFF.clicked += () =>
                {
                    isFastForward = !isFastForward;
                    ProceduralAudio.PlayTick();
                };
            }

            if (btnSkip != null)
            {
                btnSkip.clicked += DeployToGameTutorial;
            }
        }

        private void Update()
        {
            if (currentStep == FlowStep.StoryCinematic && !isCinematicPaused && !hasFinishedCinematic)
            {
                float speed = isFastForward ? (scrollSpeed * 3.5f) : scrollSpeed;
                scrollPos -= speed * Time.deltaTime;
                if (cinematicScroller != null)
                {
                    cinematicScroller.style.top = scrollPos;
                }

                if (scrollPos < -1500f)
                {
                    DeployToGameTutorial();
                }
            }
        }

        public void SetStep(FlowStep step)
        {
            currentStep = step;

            if (stepBrand != null) stepBrand.style.display = (step == FlowStep.BrandIntro) ? DisplayStyle.Flex : DisplayStyle.None;
            if (stepFactions != null) stepFactions.style.display = (step == FlowStep.FactionSelection) ? DisplayStyle.Flex : DisplayStyle.None;
            if (stepCinematic != null) stepCinematic.style.display = (step == FlowStep.StoryCinematic) ? DisplayStyle.Flex : DisplayStyle.None;

            if (step == FlowStep.BrandIntro)
            {
                StartCoroutine(BrandTimerRoutine());
            }
            else if (step == FlowStep.FactionSelection)
            {
                SelectFaction(selectedFactionIndex);
            }
            else if (step == FlowStep.StoryCinematic)
            {
                StartCinematic();
            }
        }

        private IEnumerator BrandTimerRoutine()
        {
            yield return new WaitForSeconds(brandDuration = 3.0f);
            if (currentStep == FlowStep.BrandIntro)
            {
                SetStep(FlowStep.FactionSelection);
            }
        }

        private void SelectFaction(int index)
        {
            selectedFactionIndex = index;
            var f = factions[selectedFactionIndex];
            GameFlowDataStore.SelectedFaction = f;

            ProceduralAudio.PlayTick();

            // Refrescar Textos
            if (lblPlaneCode != null)
            {
                lblPlaneCode.text = f.planeCode;
                lblPlaneCode.style.color = f.accentColor;
            }
            if (lblPlaneSlogan != null) lblPlaneSlogan.text = f.slogan;
            if (lblPhilosophy != null) lblPhilosophy.text = f.mechanicalPhilosophy;

            if (valEngine != null) valEngine.text = $"{f.enginePower:F1} / 10";
            if (valArmor != null) valArmor.text = $"{f.armorResistance:F1} / 10";
            if (valAgility != null) valAgility.text = $"{f.agilityHandling:F1} / 10";
            if (valDispersion != null) valDispersion.text = $"{f.fireDispersion:F1} / 10";

            // Tintado y recoloreo del caza japonés según la facción
            if (planeSpriteDisplay != null)
            {
                planeSpriteDisplay.style.unityBackgroundImageTintColor = f.primaryColor;
            }

            if (planeCardView != null)
            {
                planeCardView.style.borderTopColor = f.accentColor;
                planeCardView.style.borderBottomColor = f.accentColor;
                planeCardView.style.borderLeftColor = f.accentColor;
                planeCardView.style.borderRightColor = f.accentColor;
                planeCardView.style.backgroundColor = f.backgroundColor;
            }

            // Resaltar botón seleccionado
            for (int i = 0; i < factionButtons.Count; i++)
            {
                if (factionButtons[i] == null) continue;
                if (i == selectedFactionIndex)
                {
                    factionButtons[i].AddToClassList("selected");
                    factionButtons[i].style.borderTopColor = f.accentColor;
                    factionButtons[i].style.borderBottomColor = f.accentColor;
                    factionButtons[i].style.borderLeftColor = f.accentColor;
                    factionButtons[i].style.borderRightColor = f.accentColor;
                }
                else
                {
                    factionButtons[i].RemoveFromClassList("selected");
                    factionButtons[i].style.borderTopColor = new Color(0.17f, 0.22f, 0.27f);
                    factionButtons[i].style.borderBottomColor = new Color(0.17f, 0.22f, 0.27f);
                    factionButtons[i].style.borderLeftColor = new Color(0.17f, 0.22f, 0.27f);
                    factionButtons[i].style.borderRightColor = new Color(0.17f, 0.22f, 0.27f);
                }
            }
        }

        private void StartCinematic()
        {
            hasFinishedCinematic = false;
            isCinematicPaused = false;
            isFastForward = false;
            scrollPos = 650f;

            var f = GameFlowDataStore.SelectedFaction;
            if (lblCinematicStory != null && f != null)
            {
                lblCinematicStory.text = string.Join("\n\n---\n\n", f.cinematicParagraphs);
            }
            if (cinematicScroller != null)
            {
                cinematicScroller.style.top = scrollPos;
            }
        }

        private void DeployToGameTutorial()
        {
            if (hasFinishedCinematic) return;
            hasFinishedCinematic = true;

            ProceduralAudio.PlayEngineHum();

            // Configurar la primera misión usando el juego real
            var f = GameFlowDataStore.SelectedFaction;
            MissionDeploymentData.SetDeployment(
                seed: 407949,
                style: TerrainStyle.Archipelago,
                w: 60,
                h: 180,
                chunkSize: new Vector2Int(60, 60),
                sector: "PRIMERA MISIÓN // CALIBRACIÓN BALÍSTICA",
                freq: 95.0f,
                quarter: 0,
                obj: "TUTORIAL // NEUTRALIZAR VANGUARDIA",
                pPlanet: "Sixela",
                pFormation: "Archipiélago de Vandea",
                pFaction: f != null ? f.factionName : "Escuadrón Independiente",
                pBiome: "Océanos y Refinerías Fluviales",
                pWeather: "Bruma Industrial y Lluvia Ácida",
                pReport: "Vuelo de bautismo tras la fractura lunar. Calibra tus sistemas de tiro sobrevolando el sector antes de reportarte al Hangar Principal."
            );

            // Cargar directamente la escena del juego
            if (!string.IsNullOrEmpty(gameplaySceneName) && Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
            }
            else
            {
                Debug.LogWarning("[GameFlow] Escena gameplay no encontrada, cargando menú.");
                SceneManager.LoadScene(menuSceneName);
            }
        }
    }
}
