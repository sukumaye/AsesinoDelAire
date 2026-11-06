using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace WorldGen.Flow
{
    /// <summary>
    /// Controlador interactivo de la misión tutorial preliminar (Escena 3_TutorialMission).
    /// Enseña al jugador las mecánicas de puntería táctil o por teclado, disparo continuo
    /// y destrucción de objetivos militares antes de conceder acceso al Hangar y Menú Principal.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class TutorialMissionToolkitController : MonoBehaviour
    {
        [Header("Navegación")]
        [Tooltip("Escena a cargar tras completar o saltar el tutorial.")]
        public string nextSceneName = "menu";

        [Header("Sprite del Caza")]
        [Tooltip("Sprite opcional del avión para el tutorial si se desea personalizar.")]
        public Sprite planeSprite;

        // Referencias a UI Toolkit
        private UIDocument _uiDocument;
        private VisualElement _root;
        private Label _lblInstruction;
        private VisualElement _gameArea;
        private VisualElement _playerPlane;
        private Button _btnSkip;

        /// <summary>
        /// Representa un proyectil en vuelo renderizado en el árbol visual de UI Toolkit.
        /// </summary>
        private class ActiveBullet
        {
            public VisualElement element;
            public float x;
            public float y;
        }

        // Listas de entidades activas
        private List<VisualElement> _targets = new List<VisualElement>();
        private List<ActiveBullet> _bullets = new List<ActiveBullet>();
        private int _destroyedCount = 0;
        private bool _isActive = true;
        private float _planePosX = 435f;
        private float _lastShootTime = 0f;
        private const float ShootCooldown = 0.22f;

        /// <summary>
        /// Inicialización de elementos visuales, callbacks y avión del tutorial.
        /// </summary>
        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null || _uiDocument.rootVisualElement == null) return;
            _root = _uiDocument.rootVisualElement;

            _lblInstruction = _root.Q<Label>("lbl-tutorial-instruction");
            _gameArea = _root.Q<VisualElement>("tutorial-game-area");
            _playerPlane = _root.Q<VisualElement>("tutorial-plane");
            _btnSkip = _root.Q<Button>("btn-skip-tutorial");

            if (_btnSkip != null)
            {
                _btnSkip.clicked += SkipTutorial;
            }

            var f = GameFlowDataStore.SelectedFaction;
            if (_playerPlane != null)
            {
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

                if (planeSprite != null)
                {
                    _playerPlane.style.backgroundImage = new StyleBackground(planeSprite);
                }

                if (f != null)
                {
                    _playerPlane.style.unityBackgroundImageTintColor = f.primaryColor;
                }
            }

            if (_gameArea != null)
            {
                _gameArea.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                _gameArea.RegisterCallback<PointerDownEvent>(OnPointerDown);
            }

            SpawnTargets();
        }

        /// <summary>
        /// Genera los objetivos flotantes enemigos (dianas bélicas) a diferentes coordenadas en pantalla.
        /// </summary>
        private void SpawnTargets()
        {
            if (_gameArea == null) return;

            _targets.Clear();
            _destroyedCount = 0;
            _isActive = true;

            // Coordenadas relativas iniciales de las tres dianas
            float[] xOffsets = new float[] { 180f, 440f, 700f };
            float[] yOffsets = new float[] { 100f, 150f, 90f };

            for (int i = 0; i < 3; i++)
            {
                var target = new VisualElement();
                target.AddToClassList("tutorial-target");
                target.style.left = xOffsets[i];
                target.style.top = yOffsets[i];
                _gameArea.Add(target);
                _targets.Add(target);
            }
        }

        /// <summary>
        /// Movimiento lateral continuo del avión arrastrando el dedo o cursor del ratón.
        /// </summary>
        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isActive || _gameArea == null || _playerPlane == null) return;
            float areaW = _gameArea.resolvedStyle.width;
            if (float.IsNaN(areaW) || areaW <= 50f) areaW = _gameArea.layout.width;
            if (float.IsNaN(areaW) || areaW <= 50f) areaW = 1000f;

            float localX = evt.localPosition.x;
            _planePosX = Mathf.Clamp(localX - 45f, 20f, areaW - 110f);
            _playerPlane.style.left = _planePosX;
        }

        /// <summary>
        /// Disparo instantáneo y reposicionamiento al pulsar en pantalla táctil.
        /// </summary>
        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!_isActive || _gameArea == null) return;
            float areaW = _gameArea.resolvedStyle.width;
            if (float.IsNaN(areaW) || areaW <= 50f) areaW = _gameArea.layout.width;
            if (float.IsNaN(areaW) || areaW <= 50f) areaW = 1000f;

            float localX = evt.localPosition.x;
            _planePosX = Mathf.Clamp(localX - 45f, 20f, areaW - 110f);
            if (_playerPlane != null) _playerPlane.style.left = _planePosX;

            SpawnBullet();
        }

        /// <summary>
        /// Bucle principal de físicas de proyectiles y detección de colisiones de precisión 2D.
        /// </summary>
        private void Update()
        {
            if (!_isActive || _gameArea == null) return;

            // Disparo con tecla espacio o manteniendo presionado
            if (Input.GetKeyDown(KeyCode.Space) || (Input.GetKey(KeyCode.Space) && Time.time - _lastShootTime >= ShootCooldown))
            {
                SpawnBullet();
            }

            // Actualización de proyectiles
            for (int i = _bullets.Count - 1; i >= 0; i--)
            {
                var b = _bullets[i];
                if (b == null || b.element == null)
                {
                    _bullets.RemoveAt(i);
                    continue;
                }

                b.y -= 950f * Time.deltaTime;
                b.element.style.top = b.y;

                float bx = b.x + 7f;
                float by = b.y + 18f;

                for (int t = _targets.Count - 1; t >= 0; t--)
                {
                    var trg = _targets[t];
                    if (trg == null) continue;

                    float tx = trg.resolvedStyle.left;
                    if (float.IsNaN(tx) || tx <= 0f) tx = trg.layout.x;
                    float ty = trg.resolvedStyle.top;
                    if (float.IsNaN(ty) || ty <= 0f) ty = trg.layout.y;

                    tx += 35f;
                    ty += 35f;

                    if (Vector2.Distance(new Vector2(bx, by), new Vector2(tx, ty)) < 52f)
                    {
                        ProceduralAudio.PlayConfirm();
                        _gameArea.Remove(trg);
                        _targets.RemoveAt(t);

                        _gameArea.Remove(b.element);
                        _bullets.RemoveAt(i);

                        _destroyedCount++;
                        if (_destroyedCount >= 3)
                        {
                            CompleteTutorial();
                        }
                        break;
                    }
                }

                // Eliminar proyectiles que salen del área visible superior
                if (i < _bullets.Count && b != null && b.y < -50f)
                {
                    if (b.element != null && b.element.parent != null)
                    {
                        _gameArea.Remove(b.element);
                    }
                    _bullets.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Instancia un nuevo proyectil en la punta del morro del avión y reproduce el chasquido sonoro.
        /// </summary>
        private void SpawnBullet()
        {
            if (_gameArea == null) return;
            _lastShootTime = Time.time;
            ProceduralAudio.PlayTick();

            // Calcular altura de salida justo en el morro/punta del avión
            float areaH = _gameArea.resolvedStyle.height;
            if (float.IsNaN(areaH) || areaH <= 50f) areaH = _gameArea.layout.height;
            if (float.IsNaN(areaH) || areaH <= 50f) areaH = 800f;

            float planeY;
            if (_playerPlane != null && !float.IsNaN(_playerPlane.layout.y) && _playerPlane.layout.y > 10f)
            {
                planeY = _playerPlane.layout.y;
            }
            else
            {
                // En USS bottom: 60px y height: 90px -> la punta superior está a areaH - 150px
                planeY = Mathf.Max(50f, areaH - 150f);
            }

            // El proyectil sale centrado respecto al caza (caza: 90px, bala: 14px -> offset: 38px)
            float bulletX = _planePosX + 38f;
            float bulletY = planeY - 10f;

            var bulletEl = new VisualElement();
            bulletEl.AddToClassList("tutorial-bullet");
            bulletEl.style.left = bulletX;
            bulletEl.style.top = bulletY;

            var f = GameFlowDataStore.SelectedFaction;
            if (f != null)
            {
                bulletEl.style.backgroundColor = f.accentColor;
            }

            _gameArea.Add(bulletEl);
            _bullets.Add(new ActiveBullet
            {
                element = bulletEl,
                x = bulletX,
                y = bulletY
            });
        }

        /// <summary>
        /// Marca el flujo inicial como completado y transiciona con éxito hacia el Menú Principal.
        /// </summary>
        private void CompleteTutorial()
        {
            _isActive = false;
            if (_lblInstruction != null)
            {
                _lblInstruction.text = "¡VECTOR CALIBRADO CON ÉXITO! ACCEDIENDO AL HANGAR PRINCIPAL...";
                _lblInstruction.style.color = new Color(0.3f, 0.95f, 0.4f);
            }

            GameFlowDataStore.MarkIntroFlowCompleted();
            StartCoroutine(FinishRoutine());
        }

        /// <summary>
        /// Botón de salto de tutorial para saltar directamente al Menú Principal.
        /// </summary>
        public void SkipTutorial()
        {
            if (!_isActive) return;
            _isActive = false;
            if (_lblInstruction != null)
            {
                _lblInstruction.text = "OMITIENDO CALIBRACIÓN... ACCEDIENDO AL HANGAR PRINCIPAL...";
                _lblInstruction.style.color = new Color(0.95f, 0.85f, 0.3f);
            }

            GameFlowDataStore.MarkIntroFlowCompleted();
            StartCoroutine(FinishRoutine());
        }

        /// <summary>
        /// Corrutina de retraso cinematográfico antes del cambio de escena.
        /// </summary>
        private IEnumerator FinishRoutine()
        {
            yield return new WaitForSeconds(1.4f);
            ProceduralAudio.PlayEngineHum();
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
