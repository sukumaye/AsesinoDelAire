using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace WorldGen.Flow
{
    /// <summary>
    /// Controlador de la cinemática narrativa ascendente (Story Cinematic) con estilo de crónica histórica.
    /// Desplaza verticalmente el manifiesto bélico de la facción elegida, permitiendo al usuario
    /// pausar, acelerar el texto (Fast-Forward) u omitirlo directamente hacia el tutorial.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class StoryCinematicToolkitController : MonoBehaviour
    {
        [Header("Navegación")]
        [Tooltip("Escena siguiente tras finalizar o saltar la historia (Tutorial jugable).")]
        public string nextSceneName = "3_TutorialMission";

        [Header("Configuración de Desplazamiento")]
        [Tooltip("Velocidad normal de desplazamiento del texto en píxeles por segundo.")]
        public float scrollSpeed = 50f;

        // Referencias del DOM UI Toolkit
        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _cinematicScroller;
        private Label _lblCinematicStory;

        // Botones interactivos de control de reproducción
        private Button _btnPause;
        private Label _lblPauseText;
        private Button _btnFF;
        private Button _btnSkip;

        // Variables de estado de reproducción
        private bool _isPaused = false;
        private bool _isFastForward = false;
        private float _scrollPos = 650f;
        private bool _hasFinished = false;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null || _uiDocument.rootVisualElement == null) return;
            _root = _uiDocument.rootVisualElement;

            // Vincular contenedor y etiqueta de texto
            _cinematicScroller = _root.Q<VisualElement>("cinematic-scroller");
            _lblCinematicStory = _root.Q<Label>("lbl-cinematic-story");

            // Vincular controles de interfaz
            _btnPause = _root.Q<Button>("btn-cinematic-pause");
            _lblPauseText = _root.Q<Label>("lbl-pause-text");
            _btnFF = _root.Q<Button>("btn-cinematic-ff");
            _btnSkip = _root.Q<Button>("btn-cinematic-skip");

            // Configuración del botón de pausa / reanudación
            if (_btnPause != null)
            {
                _btnPause.clicked += () =>
                {
                    _isPaused = !_isPaused;
                    ProceduralAudio.PlayTick();
                    if (_lblPauseText != null) _lblPauseText.text = _isPaused ? "▶ REANUDAR" : "⏸ PAUSAR";
                };
            }

            // Configuración del botón de avance rápido (3.5x velocidad)
            if (_btnFF != null)
            {
                _btnFF.clicked += () =>
                {
                    _isFastForward = !_isFastForward;
                    ProceduralAudio.PlayTick();
                };
            }

            // Configuración del botón de salto (Skip)
            if (_btnSkip != null)
            {
                _btnSkip.clicked += Advance;
            }

            // Inyectar el texto de historia correspondiente a la facción elegida
            var f = GameFlowDataStore.SelectedFaction;
            if (_lblCinematicStory != null && f != null)
            {
                _lblCinematicStory.text = string.Join("\n\n---\n\n", f.cinematicParagraphs);
            }

            // Posicionar el scroller en la parte inferior para iniciar el ascenso
            _scrollPos = 650f;
            if (_cinematicScroller != null) _cinematicScroller.style.top = _scrollPos;
        }

        private void Update()
        {
            // Desplazamiento continuo del bloque de texto mientras no esté en pausa
            if (!_isPaused && !_hasFinished)
            {
                float speed = _isFastForward ? (scrollSpeed * 3.5f) : scrollSpeed;
                _scrollPos -= speed * Time.deltaTime;
                if (_cinematicScroller != null)
                {
                    _cinematicScroller.style.top = _scrollPos;
                }

                // Cuando el texto sale por completo del marco superior, avanzar automáticamente
                if (_scrollPos < -1500f)
                {
                    Advance();
                }
            }
        }

        /// <summary>
        /// Finaliza la cinemática con sonido de confirmación y carga la escena del tutorial de vuelo.
        /// </summary>
        private void Advance()
        {
            if (_hasFinished) return;
            _hasFinished = true;

            ProceduralAudio.PlayConfirm();
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
