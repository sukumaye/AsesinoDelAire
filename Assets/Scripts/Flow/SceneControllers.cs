using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace WorldGen.Flow
{
    // =========================================================================
    // ESCENA 0: LOGO Y MARCA (Presentación)
    // =========================================================================
    public class SceneBrandIntroController : MonoBehaviour
    {
        [Header("Navegación")]
        public string nextSceneName = "1_FactionSelection";
        public string menuSceneName = "menu";

        [Header("Estilo y Marca")]
        public string brandName = "REIKO ESTUDIOS";
        public string brandSubtitle = "DIESELPUNK RETROFUTURISTA";
        public Color brandAccent = new Color(0.95f, 0.65f, 0.2f);
        public float duration = 3.0f;

        private bool hasSkipped = false;
        private Canvas _canvas;

        private void Awake()
        {
            EnsureEventSystem();
            EnsureCanvasAndUI();
        }

        private void Start()
        {
            StartCoroutine(TimerRoutine());
        }

        private void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private void EnsureCanvasAndUI()
        {
            _canvas = GetComponentInChildren<Canvas>();
            if (_canvas == null)
            {
                _canvas = Object.FindAnyObjectByType<Canvas>();
            }

            if (_canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas_BrandIntro");
                _canvas = canvasObj.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 10;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Si no tiene elementos hijos, construir la UI
            if (_canvas.transform.childCount == 0)
            {
                SetupUI(_canvas.transform);
            }
        }

        public void SetupUI(Transform canvasTransform)
        {
            // Limpiar hijos existentes si los hubiera
            for (int i = canvasTransform.childCount - 1; i >= 0; i--)
            {
                Destroy(canvasTransform.GetChild(i).gameObject);
            }

            // Panel de fondo
            GameObject bgObj = CreateUI("Bg", canvasTransform);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            bgImg.color = new Color(0.04f, 0.04f, 0.05f, 1f);
            SetRect(bgObj.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            // Anillo exterior decorativo
            GameObject ringObj = CreateUI("Ring", canvasTransform);
            Image ringImg = ringObj.AddComponent<Image>();
            ringImg.sprite = ProceduralShapeGenerator.GetCircleSprite();
            ringImg.color = new Color(brandAccent.r, brandAccent.g, brandAccent.b, 0.18f);
            SetRect(ringObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 120), new Vector2(380, 380));

            // Diamante central
            GameObject diamondObj = CreateUI("Diamond", canvasTransform);
            Image diamondImg = diamondObj.AddComponent<Image>();
            diamondImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            diamondImg.color = brandAccent;
            SetRect(diamondObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 120), new Vector2(150, 150));
            diamondObj.transform.localRotation = Quaternion.Euler(0, 0, 45);

            // Círculo interior oscuro
            GameObject innerCircle = CreateUI("InnerCircle", diamondObj.transform);
            Image innerImg = innerCircle.AddComponent<Image>();
            innerImg.sprite = ProceduralShapeGenerator.GetCircleSprite();
            innerImg.color = new Color(0.05f, 0.05f, 0.06f, 1f);
            SetRect(innerCircle.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, Vector2.zero, new Vector2(70, 70));

            // Subtítulo
            GameObject subObj = CreateUI("Subtitle", canvasTransform);
            Text subTxt = subObj.AddComponent<Text>();
            subTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subTxt.text = brandSubtitle;
            subTxt.fontSize = 24;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.color = new Color(0.7f, 0.7f, 0.75f, 0.9f);
            SetRect(subObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -60), new Vector2(800, 50));

            // Título de Marca
            GameObject titleObj = CreateUI("Title", canvasTransform);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.text = brandName;
            titleTxt.fontSize = 58;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = brandAccent;
            SetRect(titleObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -130), new Vector2(900, 90));

            // Hint inferior
            GameObject hintObj = CreateUI("Hint", canvasTransform);
            Text hintTxt = hintObj.AddComponent<Text>();
            hintTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hintTxt.text = "[ TOCA LA PANTALLA PARA CONTINUAR ]";
            hintTxt.fontSize = 18;
            hintTxt.alignment = TextAnchor.MiddleCenter;
            hintTxt.color = new Color(0.5f, 0.5f, 0.55f, 0.6f);
            SetRect(hintObj.GetComponent<RectTransform>(), 0.5f, 0.08f, 0.5f, 0.08f, Vector2.zero, new Vector2(800, 40));

            // Botón en pantalla completa para avanzar con toque
            Button skipBtn = bgObj.AddComponent<Button>();
            skipBtn.transition = Selectable.Transition.None;
            skipBtn.onClick.AddListener(Advance);
        }

        private IEnumerator TimerRoutine()
        {
            yield return new WaitForSeconds(duration);
            Advance();
        }

        private void Advance()
        {
            if (hasSkipped) return;
            hasSkipped = true;
            ProceduralAudio.PlayTick();

            // Si el jugador ya completó el flujo introductorio, salta directo al menú principal
            if (GameFlowDataStore.HasCompletedIntroFlow)
            {
                Debug.Log("<color=cyan><b>[Flujo Lógico]</b> Jugador recurrente: selección y tutorial completados previamente. Cargando Menú Principal.</color>");
                SceneManager.LoadScene(menuSceneName);
            }
            else
            {
                Debug.Log("<color=yellow><b>[Flujo Lógico]</b> Primer inicio: Cargando Escena de Selección de Facción.</color>");
                SceneManager.LoadScene(nextSceneName);
            }
        }

        private GameObject CreateUI(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private void SetRect(RectTransform rt, float minX, float minY, float maxX, float maxY, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }

    // =========================================================================
    // ESCENA 1: SELECCIÓN DE LAS 4 FACCIONES Y NAVES (Sale SOLO una vez)
    // =========================================================================
    public class SceneFactionSelectionController : MonoBehaviour
    {
        [Header("Navegación")]
        public string nextSceneName = "2_StoryCinematic";
        public string menuSceneName = "menu";
        public bool allowRevisitInEditor = true;

        private List<FactionPlaneDefinition> factions;
        private int selectedIndex = 0;
        private Canvas _canvas;

        private Text txtPlaneCode;
        private Text txtSlogan;
        private Text txtPhilosophy;
        private Image imgBgArt;
        private Image imgPlane;
        private Text txtStatEngine;
        private Text txtStatArmor;
        private Text txtStatAgility;
        private Text txtStatDispersion;
        private List<Image> cardBorders = new List<Image>();

        private void Awake()
        {
            EnsureEventSystem();
            EnsureCanvasAndUI();
        }

        private void Start()
        {
            // Si el flujo ya está completado y no estamos en modo prueba, saltar directamente al menú
            if (GameFlowDataStore.HasCompletedIntroFlow && !Application.isEditor && !allowRevisitInEditor)
            {
                Debug.Log("[SceneFactionSelection] Flujo inicial ya completado. Redirigiendo a menu.unity.");
                SceneManager.LoadScene(menuSceneName);
                return;
            }
        }

        private void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private void EnsureCanvasAndUI()
        {
            _canvas = GetComponentInChildren<Canvas>();
            if (_canvas == null)
            {
                _canvas = Object.FindAnyObjectByType<Canvas>();
            }

            if (_canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas_FactionSelection");
                _canvas = canvasObj.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 10;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            if (_canvas.transform.childCount == 0)
            {
                SetupUI(_canvas.transform);
            }
        }

        public void SetupUI(Transform canvasTransform)
        {
            factions = GameFlowDataStore.GetAllFactions();
            cardBorders.Clear();

            for (int i = canvasTransform.childCount - 1; i >= 0; i--)
            {
                Destroy(canvasTransform.GetChild(i).gameObject);
            }

            // Fondo
            GameObject bgObj = CreateUI("Bg", canvasTransform);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            bgImg.color = new Color(0.06f, 0.06f, 0.08f, 1f);
            SetRect(bgObj.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            // Título
            GameObject titleObj = CreateUI("Header", canvasTransform);
            Text titleTxt = titleObj.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.text = "REGISTRO DE VUELO // SELECCIÓN DE VECTOR";
            titleTxt.fontSize = 28;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.85f, 0.88f, 0.92f);
            SetRect(titleObj.GetComponent<RectTransform>(), 0.5f, 1f, 0.5f, 1f, new Vector2(0, -80), new Vector2(900, 60));

            // Marco de exhibición de la aeronave
            GameObject artFrameObj = CreateUI("Art_Frame", canvasTransform);
            imgBgArt = artFrameObj.AddComponent<Image>();
            imgBgArt.sprite = ProceduralShapeGenerator.GetSquareSprite();
            imgBgArt.color = new Color(0.12f, 0.08f, 0.06f);
            SetRect(artFrameObj.GetComponent<RectTransform>(), 0.5f, 0.72f, 0.5f, 0.72f, Vector2.zero, new Vector2(940, 420));

            GameObject artBrd = CreateUI("Art_Border", artFrameObj.transform);
            Image artBrdImg = artBrd.AddComponent<Image>();
            artBrdImg.sprite = ProceduralShapeGenerator.CreateBorderFrameSprite(256, 128, 4);
            artBrdImg.color = new Color(0.85f, 0.7f, 0.35f, 0.6f);
            SetRect(artBrd.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            // Silueta de nave
            GameObject planeObj = CreateUI("Plane_Body", artFrameObj.transform);
            imgPlane = planeObj.AddComponent<Image>();
            imgPlane.sprite = ProceduralShapeGenerator.GetSquareSprite();
            imgPlane.color = Color.white;
            SetRect(planeObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, 20), new Vector2(160, 160));

            // Alas
            GameObject wingsObj = CreateUI("Wings", planeObj.transform);
            Image wingsImg = wingsObj.AddComponent<Image>();
            wingsImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            wingsImg.color = new Color(1f, 1f, 1f, 0.6f);
            SetRect(wingsObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, Vector2.zero, new Vector2(320, 40));

            // Motor
            GameObject engObj = CreateUI("Engine", planeObj.transform);
            Image engImg = engObj.AddComponent<Image>();
            engImg.sprite = ProceduralShapeGenerator.GetCircleSprite();
            engImg.color = new Color(1f, 0.45f, 0.15f);
            SetRect(engObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(0, -70), new Vector2(60, 60));

            // Código
            GameObject codeObj = CreateUI("Txt_Code", artFrameObj.transform);
            txtPlaneCode = codeObj.AddComponent<Text>();
            txtPlaneCode.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtPlaneCode.fontSize = 36;
            txtPlaneCode.fontStyle = FontStyle.Bold;
            txtPlaneCode.alignment = TextAnchor.MiddleCenter;
            SetRect(codeObj.GetComponent<RectTransform>(), 0.5f, 0.15f, 0.5f, 0.15f, Vector2.zero, new Vector2(800, 50));

            // Lema
            GameObject sloganObj = CreateUI("Txt_Slogan", artFrameObj.transform);
            txtSlogan = sloganObj.AddComponent<Text>();
            txtSlogan.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtSlogan.fontSize = 22;
            txtSlogan.fontStyle = FontStyle.Italic;
            txtSlogan.alignment = TextAnchor.MiddleCenter;
            SetRect(sloganObj.GetComponent<RectTransform>(), 0.5f, 0.05f, 0.5f, 0.05f, Vector2.zero, new Vector2(860, 40));

            // Fila de 4 tarjetas
            GameObject rowObj = CreateUI("Faction_Row", canvasTransform);
            SetRect(rowObj.GetComponent<RectTransform>(), 0.5f, 0.49f, 0.5f, 0.49f, Vector2.zero, new Vector2(960, 110));

            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                GameObject cardObj = CreateUI($"Card_{i}", rowObj.transform);
                Image cardBg = cardObj.AddComponent<Image>();
                cardBg.sprite = ProceduralShapeGenerator.GetSquareSprite();
                cardBg.color = new Color(0.18f, 0.18f, 0.22f);
                float posX = -360f + (i * 240f);
                SetRect(cardObj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(posX, 0), new Vector2(210, 90));

                Button btn = cardObj.AddComponent<Button>();
                btn.onClick.AddListener(() => SelectFaction(idx));

                GameObject brdObj = CreateUI("Brd", cardObj.transform);
                Image brd = brdObj.AddComponent<Image>();
                brd.sprite = ProceduralShapeGenerator.CreateBorderFrameSprite(128, 64, 4);
                SetRect(brdObj.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);
                cardBorders.Add(brd);

                GameObject lblObj = CreateUI("Lbl", cardObj.transform);
                Text lbl = lblObj.AddComponent<Text>();
                lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lbl.text = $"[ 0{i + 1} ]\n{factions[i].planeCode.Split(' ')[0]}";
                lbl.fontSize = 20;
                lbl.fontStyle = FontStyle.Bold;
                lbl.alignment = TextAnchor.MiddleCenter;
                lbl.color = Color.white;
                SetRect(lblObj.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);
            }

            // Recuadro de filosofía y stats
            GameObject statsObj = CreateUI("Stats_Box", canvasTransform);
            Image statsImg = statsObj.AddComponent<Image>();
            statsImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            statsImg.color = new Color(0.11f, 0.11f, 0.14f);
            SetRect(statsObj.GetComponent<RectTransform>(), 0.5f, 0.28f, 0.5f, 0.28f, Vector2.zero, new Vector2(940, 240));

            GameObject statsBrd = CreateUI("Border", statsObj.transform);
            Image statsBrdImg = statsBrd.AddComponent<Image>();
            statsBrdImg.sprite = ProceduralShapeGenerator.CreateBorderFrameSprite(256, 128, 3);
            statsBrdImg.color = new Color(0.6f, 0.6f, 0.65f, 0.4f);
            SetRect(statsBrd.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            GameObject philObj = CreateUI("Txt_Philosophy", statsObj.transform);
            txtPhilosophy = philObj.AddComponent<Text>();
            txtPhilosophy.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtPhilosophy.fontSize = 21;
            txtPhilosophy.color = new Color(0.85f, 0.85f, 0.85f);
            SetRect(philObj.GetComponent<RectTransform>(), 0.5f, 0.68f, 0.5f, 0.68f, Vector2.zero, new Vector2(880, 100));

            GameObject statRow = CreateUI("StatRow", statsObj.transform);
            SetRect(statRow.GetComponent<RectTransform>(), 0.5f, 0.22f, 0.5f, 0.22f, Vector2.zero, new Vector2(880, 60));

            txtStatEngine = CreateStatCol("POTENCIA", statRow.transform, -330f);
            txtStatArmor = CreateStatCol("BLINDAJE", statRow.transform, -110f);
            txtStatAgility = CreateStatCol("AGILIDAD", statRow.transform, 110f);
            txtStatDispersion = CreateStatCol("DISPERSIÓN", statRow.transform, 330f);

            // Botón Confirmar
            GameObject confirmObj = CreateUI("Btn_Confirm", canvasTransform);
            Image cImg = confirmObj.AddComponent<Image>();
            cImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            cImg.color = new Color(0.85f, 0.55f, 0.15f);
            SetRect(confirmObj.GetComponent<RectTransform>(), 0.5f, 0.08f, 0.5f, 0.08f, Vector2.zero, new Vector2(620, 85));

            Button cBtn = confirmObj.AddComponent<Button>();
            cBtn.onClick.AddListener(() =>
            {
                var chosen = factions[selectedIndex];
                ProceduralAudio.PlayConfirm();
                // Guardar permanentemente la facción elegida
                GameFlowDataStore.SaveFaction(chosen);
                var transition = Object.FindAnyObjectByType<CinematicFactionTransition>();
                if (transition != null && transition.isActiveAndEnabled)
                {
                    cBtn.interactable = false;
                    transition.PlayTransition(chosen, () => SceneManager.LoadScene(nextSceneName));
                    return;
                }
                Debug.Log($"<color=green><b>[Elección de Facción]</b> Facción confirmada: {chosen.factionName}. Avanzando al Tutorial.</color>");
                SceneManager.LoadScene(nextSceneName);
            });

            GameObject cTxtObj = CreateUI("Lbl", confirmObj.transform);
            Text cTxt = cTxtObj.AddComponent<Text>();
            cTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cTxt.text = "CONFIRMAR AERONAVE ➔";
            cTxt.fontSize = 30;
            cTxt.fontStyle = FontStyle.Bold;
            cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.color = Color.black;
            SetRect(cTxtObj.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            RefreshUI();
        }

        private Text CreateStatCol(string label, Transform parent, float x)
        {
            GameObject go = CreateUI("Col_" + label, parent);
            SetRect(go.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(x, 0), new Vector2(180, 50));

            GameObject lblObj = CreateUI("Lbl", go.transform);
            Text l = lblObj.AddComponent<Text>();
            l.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            l.text = label;
            l.fontSize = 17;
            l.fontStyle = FontStyle.Bold;
            l.alignment = TextAnchor.UpperCenter;
            l.color = new Color(0.6f, 0.6f, 0.65f);
            SetRect(lblObj.GetComponent<RectTransform>(), 0.5f, 0.7f, 0.5f, 0.7f, Vector2.zero, new Vector2(180, 25));

            GameObject valObj = CreateUI("Val", go.transform);
            Text v = valObj.AddComponent<Text>();
            v.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            v.fontSize = 24;
            v.fontStyle = FontStyle.Bold;
            v.alignment = TextAnchor.LowerCenter;
            v.color = Color.white;
            SetRect(valObj.GetComponent<RectTransform>(), 0.5f, 0.2f, 0.5f, 0.2f, Vector2.zero, new Vector2(180, 30));

            return v;
        }

        private void SelectFaction(int index)
        {
            selectedIndex = index;
            GameFlowDataStore.SaveFaction(factions[selectedIndex]);
            ProceduralAudio.PlayTick();
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (factions == null) factions = GameFlowDataStore.GetAllFactions();
            var f = factions[selectedIndex];
            txtPlaneCode.text = f.planeCode;
            txtPlaneCode.color = f.accentColor;
            txtSlogan.text = f.slogan;
            txtPhilosophy.text = f.mechanicalPhilosophy;

            imgBgArt.color = f.backgroundColor;
            imgPlane.color = f.primaryColor;

            txtStatEngine.text = $"{f.enginePower:F1} / 10";
            txtStatArmor.text = $"{f.armorResistance:F1} / 10";
            txtStatAgility.text = $"{f.agilityHandling:F1} / 10";
            txtStatDispersion.text = $"{f.fireDispersion:F1} / 10";

            for (int i = 0; i < cardBorders.Count; i++)
            {
                cardBorders[i].color = (i == selectedIndex) ? f.accentColor : new Color(0.4f, 0.4f, 0.4f, 0.4f);
            }
        }

        private GameObject CreateUI(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private void SetRect(RectTransform rt, float minX, float minY, float maxX, float maxY, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }

    // =========================================================================
    // ESCENA 2: CINEMÁTICA NARRATIVA ASCENDENTE (Tutorial Parte 1)
    // =========================================================================
    public class SceneStoryCinematicController : MonoBehaviour
    {
        [Header("Navegación")]
        public string nextSceneName = "3_TutorialMission";

        private RectTransform scrollContainer;
        private Text txtStory;
        private Image imgBackdrop;
        private Canvas _canvas;

        private float scrollSpeed = 40f;
        private bool isPaused = false;
        private bool isFastForward = false;

        private void Awake()
        {
            EnsureEventSystem();
            EnsureCanvasAndUI();
        }

        private void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private void EnsureCanvasAndUI()
        {
            _canvas = GetComponentInChildren<Canvas>();
            if (_canvas == null)
            {
                _canvas = Object.FindAnyObjectByType<Canvas>();
            }

            if (_canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas_StoryCinematic");
                _canvas = canvasObj.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 10;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            if (_canvas.transform.childCount == 0)
            {
                SetupUI(_canvas.transform);
            }
        }

        public void SetupUI(Transform canvasTransform)
        {
            var faction = GameFlowDataStore.SelectedFaction;

            for (int i = canvasTransform.childCount - 1; i >= 0; i--)
            {
                Destroy(canvasTransform.GetChild(i).gameObject);
            }

            // Fondo
            GameObject bgObj = CreateUI("Bg", canvasTransform);
            imgBackdrop = bgObj.AddComponent<Image>();
            imgBackdrop.sprite = ProceduralShapeGenerator.GetSquareSprite();
            imgBackdrop.color = faction != null ? faction.backgroundColor : new Color(0.04f, 0.04f, 0.05f);
            SetRect(bgObj.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            // Máscara
            GameObject maskObj = CreateUI("Mask", canvasTransform);
            maskObj.AddComponent<RectMask2D>();
            SetRect(maskObj.GetComponent<RectTransform>(), 0.5f, 0.55f, 0.5f, 0.55f, Vector2.zero, new Vector2(880, 1100));

            // Contenedor scroll
            GameObject scrollObj = CreateUI("ScrollContainer", maskObj.transform);
            scrollContainer = scrollObj.GetComponent<RectTransform>();
            SetRect(scrollContainer, 0.5f, 0f, 0.5f, 0f, new Vector2(0, -600), new Vector2(840, 2000));

            GameObject storyObj = CreateUI("Txt_Story", scrollObj.transform);
            txtStory = storyObj.AddComponent<Text>();
            txtStory.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtStory.fontSize = 28;
            txtStory.lineSpacing = 1.35f;
            txtStory.alignment = TextAnchor.UpperCenter;
            txtStory.color = new Color(0.95f, 0.95f, 0.88f);
            SetRect(storyObj.GetComponent<RectTransform>(), 0.5f, 1f, 0.5f, 1f, Vector2.zero, new Vector2(840, 1800));

            if (faction != null)
            {
                txtStory.text = string.Join("\n\n---\n\n", faction.cinematicParagraphs);
            }

            // Barra de controles
            GameObject barObj = CreateUI("ControlsBar", canvasTransform);
            SetRect(barObj.GetComponent<RectTransform>(), 0.5f, 0.08f, 0.5f, 0.08f, Vector2.zero, new Vector2(940, 80));

            CreateBtn("Btn_Pause", "⏸ PAUSA", barObj.transform, -300f, () => { isPaused = !isPaused; ProceduralAudio.PlayTick(); });
            CreateBtn("Btn_FF", "⏩ ACELERAR", barObj.transform, 0f, () => { isFastForward = !isFastForward; ProceduralAudio.PlayTick(); });
            CreateBtn("Btn_Skip", "CONTINUAR AL TUTORIAL ➔", barObj.transform, 300f, () => { Advance(); });
        }

        private void Start()
        {
            if (txtStory != null && GameFlowDataStore.SelectedFaction != null)
            {
                txtStory.text = string.Join("\n\n---\n\n", GameFlowDataStore.SelectedFaction.cinematicParagraphs);
            }
        }

        private void Update()
        {
            if (!isPaused && scrollContainer != null)
            {
                float speed = isFastForward ? (scrollSpeed * 3.5f) : scrollSpeed;
                Vector2 pos = scrollContainer.anchoredPosition;
                pos.y += speed * Time.deltaTime;
                scrollContainer.anchoredPosition = pos;

                if (pos.y > 1600f)
                {
                    Advance();
                }
            }
        }

        private void Advance()
        {
            ProceduralAudio.PlayConfirm();
            SceneManager.LoadScene(nextSceneName);
        }

        private void CreateBtn(string name, string text, Transform parent, float x, UnityEngine.Events.UnityAction action)
        {
            GameObject obj = CreateUI(name, parent);
            Image img = obj.AddComponent<Image>();
            img.sprite = ProceduralShapeGenerator.GetSquareSprite();
            img.color = new Color(0.22f, 0.22f, 0.28f);
            SetRect(obj.GetComponent<RectTransform>(), 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(x, 0), new Vector2(260, 65));

            Button btn = obj.AddComponent<Button>();
            btn.onClick.AddListener(action);

            GameObject txtObj = CreateUI("Lbl", obj.transform);
            Text t = txtObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = 20;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            SetRect(txtObj.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);
        }

        private GameObject CreateUI(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private void SetRect(RectTransform rt, float minX, float minY, float maxX, float maxY, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }

    // =========================================================================
    // ESCENA 3: MISIÓN TUTORIAL BALÍSTICA JUGABLE (Tutorial Parte 2)
    // =========================================================================
    public class SceneTutorialMissionController : MonoBehaviour
    {
        [Header("Navegación")]
        public string nextSceneName = "menu";

        private RectTransform rootPanel;
        private RectTransform playerPlane;
        private Vector2 planePos = new Vector2(0, -220);
        private List<RectTransform> targets = new List<RectTransform>();
        private List<RectTransform> bullets = new List<RectTransform>();
        private Text txtInstruction;
        private int destroyedCount = 0;
        private bool isActive = true;
        private Canvas _canvas;

        private void Awake()
        {
            EnsureEventSystem();
            EnsureCanvasAndUI();
        }

        private void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private void EnsureCanvasAndUI()
        {
            _canvas = GetComponentInChildren<Canvas>();
            if (_canvas == null)
            {
                _canvas = Object.FindAnyObjectByType<Canvas>();
            }

            if (_canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas_TutorialMission");
                _canvas = canvasObj.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 10;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;

                canvasObj.AddComponent<GraphicRaycaster>();
            }

            if (_canvas.transform.childCount == 0)
            {
                SetupUI(_canvas.transform);
            }
        }

        public void SetupUI(Transform canvasTransform)
        {
            targets.Clear();
            bullets.Clear();
            destroyedCount = 0;
            isActive = true;

            for (int i = canvasTransform.childCount - 1; i >= 0; i--)
            {
                Destroy(canvasTransform.GetChild(i).gameObject);
            }

            GameObject bgObj = CreateUI("Bg", canvasTransform);
            rootPanel = bgObj.GetComponent<RectTransform>();
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            bgImg.color = new Color(0.05f, 0.07f, 0.06f);
            SetRect(rootPanel, 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            // Instrucción
            GameObject instrObj = CreateUI("Txt_Instruction", canvasTransform);
            txtInstruction = instrObj.AddComponent<Text>();
            txtInstruction.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txtInstruction.text = "FASE DE CALIBRACIÓN BALÍSTICA // ARRASTRA O TOCA PARA DISPARAR A LOS 3 OBJETIVOS";
            txtInstruction.fontSize = 24;
            txtInstruction.fontStyle = FontStyle.Bold;
            txtInstruction.alignment = TextAnchor.MiddleCenter;
            txtInstruction.color = new Color(0.9f, 0.9f, 0.6f);
            SetRect(instrObj.GetComponent<RectTransform>(), 0.5f, 0.92f, 0.5f, 0.92f, Vector2.zero, new Vector2(920, 60));

            // Botón Saltar Tutorial
            GameObject skipObj = CreateUI("Btn_SkipTutorial", canvasTransform);
            Image sImg = skipObj.AddComponent<Image>();
            sImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            sImg.color = new Color(0.2f, 0.22f, 0.25f, 0.85f);
            SetRect(skipObj.GetComponent<RectTransform>(), 0.5f, 0.08f, 0.5f, 0.08f, Vector2.zero, new Vector2(400, 60));

            Button sBtn = skipObj.AddComponent<Button>();
            sBtn.onClick.AddListener(SkipTutorial);

            GameObject sTxtObj = CreateUI("Lbl", skipObj.transform);
            Text sTxt = sTxtObj.AddComponent<Text>();
            sTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sTxt.text = "SALTAR TUTORIAL AL HANGAR ➔";
            sTxt.fontSize = 20;
            sTxt.fontStyle = FontStyle.Bold;
            sTxt.alignment = TextAnchor.MiddleCenter;
            sTxt.color = new Color(0.85f, 0.85f, 0.9f);
            SetRect(sTxtObj.GetComponent<RectTransform>(), 0, 0, 1, 1, Vector2.zero, Vector2.zero);

            // Avión
            GameObject planeObj = CreateUI("PlayerPlane", canvasTransform);
            playerPlane = planeObj.GetComponent<RectTransform>();
            Image pImg = planeObj.AddComponent<Image>();
            pImg.sprite = ProceduralShapeGenerator.GetSquareSprite();
            pImg.color = (GameFlowDataStore.SelectedFaction != null) ? GameFlowDataStore.SelectedFaction.primaryColor : Color.white;
            SetRect(playerPlane, 0.5f, 0.5f, 0.5f, 0.5f, planePos, new Vector2(90, 90));

            // 3 Objetivos
            for (int i = 0; i < 3; i++)
            {
                GameObject tObj = CreateUI($"Target_{i}", canvasTransform);
                RectTransform rt = tObj.GetComponent<RectTransform>();
                Image tImg = tObj.AddComponent<Image>();
                tImg.sprite = ProceduralShapeGenerator.GetCircleSprite();
                tImg.color = new Color(0.95f, 0.25f, 0.25f);
                float x = -280f + (i * 280f);
                float y = 240f + (i % 2 == 0 ? 60f : -40f);
                SetRect(rt, 0.5f, 0.5f, 0.5f, 0.5f, new Vector2(x, y), new Vector2(80, 80));
                targets.Add(rt);
            }
        }

        private void Update()
        {
            if (!isActive) return;

            // Movimiento táctil o de ratón
            if (Input.GetMouseButton(0))
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rootPanel, Input.mousePosition, null, out Vector2 localPoint);
                planePos.x = Mathf.Lerp(planePos.x, localPoint.x, Time.deltaTime * 12f);
                if (playerPlane != null) playerPlane.anchoredPosition = planePos;
            }

            // Disparo
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                SpawnBullet();
            }

            // Actualización de balas
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                RectTransform b = bullets[i];
                if (b == null) continue;

                b.anchoredPosition += new Vector2(0, 950f * Time.deltaTime);

                for (int t = targets.Count - 1; t >= 0; t--)
                {
                    RectTransform trg = targets[t];
                    if (trg == null) continue;

                    if (Vector2.Distance(b.anchoredPosition, trg.anchoredPosition) < 65f)
                    {
                        ProceduralAudio.PlayConfirm();
                        Destroy(trg.gameObject);
                        targets.RemoveAt(t);

                        Destroy(b.gameObject);
                        bullets.RemoveAt(i);

                        destroyedCount++;
                        if (destroyedCount >= 3)
                        {
                            CompleteTutorial();
                        }
                        break;
                    }
                }

                if (i < bullets.Count && b != null && b.anchoredPosition.y > 900f)
                {
                    Destroy(b.gameObject);
                    bullets.RemoveAt(i);
                }
            }
        }

        private void SpawnBullet()
        {
            ProceduralAudio.PlayTick();
            GameObject bObj = CreateUI("Bullet", rootPanel);
            RectTransform rt = bObj.GetComponent<RectTransform>();
            Image img = bObj.AddComponent<Image>();
            img.sprite = ProceduralShapeGenerator.GetCircleSprite();
            img.color = new Color(1f, 0.9f, 0.3f);
            SetRect(rt, 0.5f, 0.5f, 0.5f, 0.5f, planePos + new Vector2(0, 60), new Vector2(24, 48));
            bullets.Add(rt);
        }

        private void CompleteTutorial()
        {
            isActive = false;
            txtInstruction.text = "¡VECTOR CALIBRADO CON ÉXITO! ACCEDIENDO AL HANGAR PRINCIPAL...";
            txtInstruction.color = Color.green;

            // Marcar flujo completado permanentemente en PlayerPrefs
            GameFlowDataStore.MarkIntroFlowCompleted();

            StartCoroutine(FinishRoutine());
        }

        public void SkipTutorial()
        {
            if (!isActive) return;
            isActive = false;
            txtInstruction.text = "OMITIENDO CALIBRACIÓN... ACCEDIENDO AL HANGAR PRINCIPAL...";
            txtInstruction.color = Color.yellow;

            // Marcar flujo completado permanentemente en PlayerPrefs
            GameFlowDataStore.MarkIntroFlowCompleted();

            StartCoroutine(FinishRoutine());
        }

        private IEnumerator FinishRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            ProceduralAudio.PlayEngineHum();
            SceneManager.LoadScene(nextSceneName);
        }

        private GameObject CreateUI(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private void SetRect(RectTransform rt, float minX, float minY, float maxX, float maxY, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
