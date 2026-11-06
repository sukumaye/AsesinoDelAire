using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace WorldGen.Flow
{
    /// <summary>
    /// Controlador cinemático para la pantalla de presentación de marca y logotipo (Brand Intro).
    /// Ejecuta una secuencia con curvas suaves de aparición (Fade In), visibilidad sostenida
    /// y desvanecimiento (Fade Out), administrando la lógica de onboarding del jugador.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class BrandIntroToolkitController : MonoBehaviour
    {
        [Header("Navegación")]
        [Tooltip("Escena a cargar si es la primera vez que juega (Selección de Facción).")]
        public string nextSceneName = "1_FactionSelection";

        [Tooltip("Escena del Menú Principal a cargar si el jugador ya completó el tutorial.")]
        public string menuSceneName = "menu";

        [Tooltip("Si está activo, en el Editor de Unity SIEMPRE se mostrará la selección de facción y el tutorial para depurar.")]
        public bool allowRevisitInEditor = true;

        [Header("Personalización de Marca")]
        [Tooltip("Texto principal del estudio o título.")]
        public string brandName = "";

        [Tooltip("Subtítulo de la marca.")]
        public string brandSubtitle = "";

        [Header("Tiempos de Cinemática (Fundido)")]
        [Tooltip("Tiempo inicial de espera en negro antes de que aparezca.")]
        public float initialBlackDelay = 0.3f;

        [Tooltip("Duración de la aparición (Fade In) en segundos.")]
        public float fadeInDuration = 1.2f;

        [Tooltip("Tiempo que se mantiene totalmente visible en pantalla.")]
        public float visibleDuration = 2.0f;

        [Tooltip("Duración del desvanecimiento (Fade Out) en segundos.")]
        public float fadeOutDuration = 1.2f;

        [Tooltip("Tiempo en negro total antes de cambiar a la siguiente pantalla.")]
        public float finalBlackDuration = 0.5f;

        [Tooltip("Duración de reserva.")]
        public float duration = 2.0f;

        // Referencias a los elementos del árbol visual UI Toolkit
        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _stepBrand;
        private VisualElement _logo;
        private Label _title;

        // Variables de control de la animación y omisión
        private Coroutine _fadeRoutine;
        private bool _isSkipping = false;
        private bool _hasAdvanced = false;

        private void Awake()
        {
            // Verificación y saneamiento de tiempos mínimos de animación
            if (fadeInDuration <= 0.05f) fadeInDuration = 1.2f;
            if (visibleDuration <= 0.05f) visibleDuration = duration > 0.05f ? duration : 2.0f;
            if (fadeOutDuration <= 0.05f) fadeOutDuration = 1.2f;
            if (initialBlackDelay < 0f) initialBlackDelay = 0.3f;
            if (finalBlackDuration < 0f) finalBlackDuration = 0.5f;
        }

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null || _uiDocument.rootVisualElement == null) return;
            _root = _uiDocument.rootVisualElement;

#if UNITY_EDITOR
            // Enlazar estilos de flujo en el editor si no están asociados
            var flowStyleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI/Flow/FlowStyles.uss");
            if (flowStyleSheet != null && !_root.styleSheets.Contains(flowStyleSheet))
            {
                _root.styleSheets.Add(flowStyleSheet);
            }
#endif

            // Consultar elementos visuales del UXML
            _stepBrand = _root.Q<VisualElement>("step-brand");
            _logo = _root.Q<VisualElement>("brand-ram-logo");
            _title = _root.Q<Label>("lbl-brand-title");

            // Carga de seguridad del logo en el editor de Unity
            if (_logo != null)
            {
#if UNITY_EDITOR
                if (_logo.resolvedStyle.backgroundImage.sprite == null && _logo.resolvedStyle.backgroundImage.texture == null)
                {
                    var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Estudio/Calavera de carnero gótica pixelada.png");
                    foreach (var a in allAssets)
                    {
                        if (a is Sprite s)
                        {
                            _logo.style.backgroundImage = new StyleBackground(s);
                            break;
                        }
                    }
                }
#endif
            }

            // Aplicar textos de personalización
            if (_title != null && !string.IsNullOrEmpty(brandName))
            {
                _title.text = brandName;
            }

            var lblSub = _root.Q<Label>("lbl-brand-subtitle");
            if (lblSub != null)
            {
                if (!string.IsNullOrEmpty(brandSubtitle))
                    lblSub.text = brandSubtitle;
                else
                    lblSub.style.display = DisplayStyle.None;
            }

            // Iniciar totalmente invisible en pantalla negra
            SetOpacity(0f);

            // Permitir omitir la intro al hacer clic o tocar la pantalla
            if (_stepBrand != null)
            {
                _stepBrand.RegisterCallback<ClickEvent>(evt => OnUserClick());
            }

            // Iniciar corrutina de reproducción de la intro
            _fadeRoutine = StartCoroutine(IntroSequenceRoutine());
        }

        /// <summary>
        /// Aplica la opacidad a todos los elementos clave de la presentación de marca.
        /// </summary>
        /// <param name="alpha">Valor de transparencia entre 0 (invisible) y 1 (opaco).</param>
        private void SetOpacity(float alpha)
        {
            if (_stepBrand != null) _stepBrand.style.opacity = alpha;
            if (_logo != null) _logo.style.opacity = alpha;
            if (_title != null) _title.style.opacity = alpha;
        }

        /// <summary>
        /// Corrutina orquestadora de los 5 pasos de la cinemática de introducción:
        /// 1. Espera inicial en negro.
        /// 2. Fade in con curva Smoothstep.
        /// 3. Visibilidad plena sostenida.
        /// 4. Fade out suave.
        /// 5. Pausa en negro y transición a la siguiente escena.
        /// </summary>
        private IEnumerator IntroSequenceRoutine()
        {
            // 0. Pausa inicial en negro absoluto
            SetOpacity(0f);
            if (initialBlackDelay > 0f)
            {
                yield return new WaitForSeconds(initialBlackDelay);
            }

            // 1. Aparición suave (Fade In)
            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / Mathf.Max(0.01f, fadeInDuration));
                // Curva de aceleración/desaceleración suave (Smoothstep)
                float alpha = progress * progress * (3f - 2f * progress);
                SetOpacity(alpha);
                yield return null;
            }
            SetOpacity(1f);

            // 2. Mantenerse visible en pantalla
            yield return new WaitForSeconds(visibleDuration);

            // 3. Desvanecimiento suave (Fade Out)
            t = 0f;
            while (t < fadeOutDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / Mathf.Max(0.01f, fadeOutDuration));
                float alpha = 1f - (progress * progress * (3f - 2f * progress));
                SetOpacity(alpha);
                yield return null;
            }
            SetOpacity(0f);

            // 4. Pausa en pantalla negra antes del cambio
            if (finalBlackDuration > 0f)
            {
                yield return new WaitForSeconds(finalBlackDuration);
            }

            // 5. Transición a la siguiente pantalla
            Advance();
        }

        private void Update()
        {
            // Tecla 'R' en el editor para resetear el progreso del tutorial y facción inmediatamente
            if (Application.isEditor && Input.GetKeyDown(KeyCode.R))
            {
                GameFlowDataStore.ResetIntroFlow();
                Debug.Log("<color=green><b>[BrandIntro]</b> Progreso reseteado con tecla R. Volviendo a probar tutorial.</color>");
            }
        }

        /// <summary>
        /// Intercepta el clic o toque del usuario para omitir rápidamente la intro con un desvanecimiento acelerado.
        /// </summary>
        private void OnUserClick()
        {
            if (_hasAdvanced || _isSkipping) return;
            _isSkipping = true;

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }
            StartCoroutine(QuickFadeOutRoutine());
        }

        /// <summary>
        /// Desvanecimiento acelerado para omitir la animación sin cortes bruscos.
        /// </summary>
        private IEnumerator QuickFadeOutRoutine()
        {
            float currentAlpha = _logo != null ? _logo.style.opacity.value : 1f;
            float t = 0f;
            float quickDuration = 0.35f;

            while (t < quickDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / quickDuration);
                SetOpacity(Mathf.Lerp(currentAlpha, 0f, progress));
                yield return null;
            }
            SetOpacity(0f);

            yield return new WaitForSeconds(0.2f);
            Advance();
        }

        /// <summary>
        /// Decide hacia qué escena avanzar según el historial de juego:
        /// - Si es usuario nuevo: va a selección de facción.
        /// - Si ya completó el tutorial: va directo al menú principal.
        /// </summary>
        private void Advance()
        {
            if (_hasAdvanced) return;
            _hasAdvanced = true;

            ProceduralAudio.PlayTick();

            // Si el jugador ya completó la selección y el tutorial anteriormente, va directo al menú principal (a menos que estemos testeando en Editor con allowRevisitInEditor)
            if (GameFlowDataStore.HasCompletedIntroFlow && (!Application.isEditor || !allowRevisitInEditor))
            {
                Debug.Log("<color=cyan><b>[BrandIntro]</b> Jugador recurrente: selección y tutorial completados previamente. Cargando Menú Principal.</color>");
                SceneManager.LoadScene(menuSceneName);
            }
            else
            {
                Debug.Log("<color=yellow><b>[BrandIntro]</b> Modo prueba/Primer inicio: Cargando Selección de Facción y Tutorial.</color>");
                SceneManager.LoadScene(nextSceneName);
            }
        }
    }
}
