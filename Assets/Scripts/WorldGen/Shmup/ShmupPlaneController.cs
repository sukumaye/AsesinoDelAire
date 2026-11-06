using System.Collections.Generic;
using UnityEngine;
using WorldGen.Core;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WorldGen.Shmup
{
    public enum PlaneRotationMode
    {
        [Tooltip("Clásico Shmup: El avión mira hacia arriba de la pantalla con alabeo suave al girar con A/D.")]
        FixedUp,
        [Tooltip("Omnidireccional: El avión rota hacia la dirección de WASD (360° para limpiar zonas en cualquier dirección).")]
        OmnidirectionalWASD,
        [Tooltip("Alineado con el Camino: El avión orienta su morro según el avance de la carretera procedimental.")]
        AlignWithPath
    }

    /// <summary>
    /// Controlador sólido y arcade del avión del jugador para modo Shoot 'em Up.
    /// - En modo Vuelo Libre (por defecto): El avión está anclado a la pantalla de la cámara con coordenadas relativas ultra sólidas.
    ///   Se mueve libremente con WASD por toda la pantalla, esquivando y disparando sin tirones, vibraciones ni bugs.
    /// - En modo Autopiloto: Sigue la carretera procedimental como un dron guiado.
    /// - Dispara ráfagas arcade orientadas al morro del avión (Espacio / Clic izquierdo).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DefaultExecutionOrder(100)]
    public class ShmupPlaneController : MonoBehaviour
    {
        [Header("✈️ Modo de Vuelo")]
        [Tooltip("Si está activo, el avión vuela automáticamente sobre la carretera. Si está desactivado (Recomendado), tienes control libre con WASD dentro de la pantalla.")]
        public bool followFlightPath = false;

        [Header("🎮 Control Libre en Pantalla (WASD)")]
        [Tooltip("Velocidad de maniobra del avión dentro de la pantalla.")]
        [Range(4f, 25f)]
        public float freeMoveSpeed = 12f;

        [Tooltip("Desaceleración/Freno cuando se suelta WASD (más alto = frenada más sólida e instantánea).")]
        [Range(10f, 80f)]
        public float freeDeceleration = 40f;

        [Header("📷 Confinamiento en Pantalla")]
        public Camera targetCamera;
        [Tooltip("Margen respecto a los bordes de la pantalla (0.05 = 5%).")]
        public Vector2 viewportMargin = new Vector2(0.05f, 0.06f);

        [Header("🎯 Modo de Rotación y Apuntado")]
        [Tooltip("Cómo se orienta el avión durante el vuelo libre.")]
        public PlaneRotationMode rotationMode = PlaneRotationMode.FixedUp;

        [Header("🔄 Alabeo e Inclinación")]
        [Tooltip("Ángulo máximo de inclinación al virar a los lados (grados).")]
        [Range(5f, 35f)]
        public float maxBankAngle = 18f;

        [Tooltip("Velocidad de respuesta del alabeo en grados/segundo.")]
        [Range(60f, 300f)]
        public float bankSpeed = 140f;

        [Header("🛣️ Autopiloto por Camino")]
        public float flightSpeed = 4.5f;
        public float lateralDodgeSpeed = 6.5f;
        public float maxLateralDodge = 3.5f;
        public List<Vector3> flightPath = new List<Vector3>();

        [Header("💥 Disparo Arcade")]
        public bool canShoot = true;
        public float fireRate = 0.14f;
        public float bulletSpeed = 24f;
        public GameObject bulletPrefab;

        [Header("📱 Control Móvil / Táctil 100%")]
        [Tooltip("Habilitar detección de gestos táctiles en pantalla completa para móviles, Unity Remote y ratón en Editor.")]
        public bool enableTouchControls = true;
        [Tooltip("Multiplicador de respuesta del arrastre táctil (1.0 = 1:1 directo al movimiento del dedo).")]
        [Range(0.5f, 2.5f)]
        public float touchDragMultiplier = 1.0f;
        [Tooltip("Velocidad de seguimiento suave al dedo (valores altos = respuesta instantánea sin retraso).")]
        [Range(20f, 100f)]
        public float touchFollowSpeed = 60f;
        [Tooltip("Disparar automáticamente al mantener el dedo en la pantalla (estándar Shmup arcade).")]
        public bool autoFireOnTouch = true;

        // Estado táctil interno
        private bool isTouchDragging = false;
        private Vector2 lastTouchCamSpace = Vector2.zero;
        private Vector2 touchVirtualVelocity = Vector2.zero;

        // Soporte de Inyección de Input (Local, Bluetooth Multijugador, Bots)
        private IPlaneInputProvider customInputProvider;
        public IPlaneInputProvider CurrentInputProvider => customInputProvider;
        public void SetInputProvider(IPlaneInputProvider provider) => customInputProvider = provider;

        // Estado expuesto para Bluetooth / Sincronización en red
        public Vector2 CurrentMoveInput { get; private set; }
        public bool IsFiring { get; private set; }

        // Estado interno en pantalla (offset respecto al centro de la cámara)
        [SerializeField]
        private Vector2 screenOffset = new Vector2(0f, -5f);
        private Vector2 currentMoveVelocity = Vector2.zero;
        private float currentBankAngle = 0f;
        private float currentFacingAngle = 0f;
        public bool hasInitializedOffset { get; private set; } = false;

        // Estado interno del autopiloto
        private float currentPathDist = 0f;
        private float currentLateralOffset = 0f;
        private float[] segmentLengths;
        private float totalPathLength = 0f;

        private float nextFireTime = 0f;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            enableTouchControls = true;
            autoFireOnTouch = true;
            spriteRenderer = GetComponent<SpriteRenderer>();
            EnsureTargetCamera();
            MobileTouchInput.InitializeTouchSubsystem();
        }

        private void Start()
        {
            EnsureTargetCamera();
            ComputePathLengths();
            // Aplicar personalización de facción seleccionada (recoloreo del caza japonés y ajustes)
            if (WorldGen.Flow.GameFlowDataStore.SelectedFaction != null && spriteRenderer != null)
            {
                var f = WorldGen.Flow.GameFlowDataStore.SelectedFaction;
                spriteRenderer.color = f.primaryColor;
                freeMoveSpeed = Mathf.Lerp(8f, 16f, f.agilityHandling / 10f);
                fireRate = Mathf.Lerp(0.22f, 0.08f, f.fireDispersion / 10f);
            }
        }

        private void OnDisable()
        {
            if (transform.parent != null && !Application.isPlaying)
            {
                transform.SetParent(null);
            }
        }

        private void EnsureTargetCamera()
        {
            if (targetCamera != null && targetCamera.gameObject.scene == gameObject.scene && targetCamera.gameObject.activeInHierarchy) return;

            targetCamera = null;
            if (gameObject.scene.isLoaded)
            {
                var roots = gameObject.scene.GetRootGameObjects();
                foreach (var r in roots)
                {
                    var cam = r.GetComponentInChildren<Camera>(true);
                    if (cam != null && cam.CompareTag("MainCamera"))
                    {
                        targetCamera = cam;
                        break;
                    }
                    if (cam != null && targetCamera == null && !cam.name.Contains("Preview"))
                    {
                        targetCamera = cam;
                    }
                }
            }

            if (targetCamera == null)
            {
                var allCams = FindObjectsByType<Camera>(FindObjectsInactive.Include);
                foreach (var cam in allCams)
                {
                    if (cam != null && cam.gameObject.scene == gameObject.scene && !cam.name.Contains("Preview"))
                    {
                        targetCamera = cam;
                        break;
                    }
                }
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        public void InitializeScreenOffset()
        {
            EnsureTargetCamera();
            if (targetCamera != null)
            {
                float halfH = targetCamera.orthographicSize;
                screenOffset.x = 0f;
                screenOffset.y = -halfH * 0.42f;

                if (!followFlightPath)
                {
                    if (targetCamera.gameObject.scene == gameObject.scene && transform.parent != targetCamera.transform)
                    {
                        transform.SetParent(targetCamera.transform, false);
                    }
                    transform.localPosition = new Vector3(screenOffset.x, screenOffset.y, 10f);
                    transform.localRotation = Quaternion.identity;
                }
                hasInitializedOffset = true;
            }
        }

        private void Update()
        {
            Vector2 input = ReadMovementInput();
            CurrentMoveInput = input;
            IsFiring = ReadFireInput();

            if (followFlightPath && flightPath != null && flightPath.Count >= 2)
            {
                HandleAutopilotPathMovement(input);
            }
            else
            {
                HandleFreeScreenMovement(input);
                HandleRotation(input);
            }

            HandleShooting();
        }

        private void HandleFreeScreenMovement(Vector2 input)
        {
            EnsureTargetCamera();
            if (targetCamera == null) return;

            // Emparentar directamente a la cámara de su propia escena para eliminar latencia y vibraciones
            if (targetCamera.gameObject.scene == gameObject.scene && transform.parent != targetCamera.transform)
            {
                transform.SetParent(targetCamera.transform, false);
            }

            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * targetCamera.aspect;
            float limitX = Mathf.Max(0.5f, halfWidth * (1f - viewportMargin.x));
            float limitY = Mathf.Max(0.5f, halfHeight * (1f - viewportMargin.y));

            // 1. Control Táctil Directo 1:1 e Instantáneo (Móvil / Unity Remote / Ratón)
            if (enableTouchControls && MobileTouchInput.IsTouching)
            {
                Vector2 touchScreenPos = MobileTouchInput.PrimaryTouchPosition;
                Vector3 worldPoint = targetCamera.ScreenToWorldPoint(new Vector3(touchScreenPos.x, touchScreenPos.y, 10f));
                Vector2 camLocalTouch = (Vector2)(worldPoint - targetCamera.transform.position);

                if (!isTouchDragging)
                {
                    isTouchDragging = true;
                    lastTouchCamSpace = camLocalTouch;
                    touchVirtualVelocity = Vector2.zero;
                }
                else
                {
                    Vector2 delta = (camLocalTouch - lastTouchCamSpace) * touchDragMultiplier;
                    lastTouchCamSpace = camLocalTouch;

                    screenOffset += delta;
                    touchVirtualVelocity = delta / Mathf.Max(0.0001f, Time.deltaTime);
                    currentMoveVelocity = touchVirtualVelocity;
                }
            }
            else
            {
                if (isTouchDragging)
                {
                    isTouchDragging = false;
                }

                // 2. Control con Teclado (WASD), Mando o Dispositivos Externos
                if (input.sqrMagnitude > 0.01f)
                {
                    currentMoveVelocity = input * freeMoveSpeed;
                }
                else
                {
                    currentMoveVelocity = Vector2.MoveTowards(currentMoveVelocity, Vector2.zero, freeDeceleration * Time.deltaTime);
                }

                screenOffset += currentMoveVelocity * Time.deltaTime;
            }

            // 3. Confinamiento matemático exacto en la vista de la cámara
            screenOffset.x = Mathf.Clamp(screenOffset.x, -limitX, limitX);
            screenOffset.y = Mathf.Clamp(screenOffset.y, -limitY, limitY);

            // Detener inercia en los bordes para evitar vibración
            if (screenOffset.x <= -limitX && currentMoveVelocity.x < 0f) currentMoveVelocity.x = 0f;
            if (screenOffset.x >= limitX && currentMoveVelocity.x > 0f) currentMoveVelocity.x = 0f;
            if (screenOffset.y <= -limitY && currentMoveVelocity.y < 0f) currentMoveVelocity.y = 0f;
            if (screenOffset.y >= limitY && currentMoveVelocity.y > 0f) currentMoveVelocity.y = 0f;

            // Posicionar con absoluta solidez en coordenadas locales de la cámara (z = 10f proyecta a z = 0f en el mundo)
            transform.localPosition = new Vector3(screenOffset.x, screenOffset.y, 10f);
        }

        private void HandleRotation(Vector2 input)
        {
            float horizontalEffort = input.x;
            if (isTouchDragging && Mathf.Abs(touchVirtualVelocity.x) > 0.1f)
            {
                horizontalEffort = Mathf.Clamp(touchVirtualVelocity.x / freeMoveSpeed, -1f, 1f);
            }

            switch (rotationMode)
            {
                case PlaneRotationMode.FixedUp:
                default:
                {
                    float targetBank = -horizontalEffort * maxBankAngle;
                    currentBankAngle = Mathf.MoveTowards(currentBankAngle, targetBank, Time.deltaTime * bankSpeed);
                    transform.localRotation = Quaternion.Euler(0f, 0f, currentBankAngle);
                    break;
                }
                case PlaneRotationMode.OmnidirectionalWASD:
                {
                    Vector2 dir = isTouchDragging ? touchVirtualVelocity : input;
                    if (dir.sqrMagnitude > 0.04f)
                    {
                        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                        currentFacingAngle = Mathf.MoveTowardsAngle(currentFacingAngle, targetAngle, Time.deltaTime * 360f);
                    }
                    transform.localRotation = Quaternion.Euler(0f, 0f, currentFacingAngle);
                    break;
                }
                case PlaneRotationMode.AlignWithPath:
                {
                    Vector3 forwardDir = GetRoadDirectionAtPosition();
                    float baseAngle = Mathf.Atan2(forwardDir.y, forwardDir.x) * Mathf.Rad2Deg - 90f;
                    float targetBank = -horizontalEffort * maxBankAngle;
                    currentBankAngle = Mathf.MoveTowards(currentBankAngle, targetBank, Time.deltaTime * bankSpeed);
                    transform.localRotation = Quaternion.Euler(0f, 0f, baseAngle + currentBankAngle);
                    break;
                }
            }
        }

        private void HandleAutopilotPathMovement(Vector2 input)
        {
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            float speedMod = 1f;
            if (input.y > 0.1f) speedMod = 1.4f;
            else if (input.y < -0.1f) speedMod = 0.65f;

            currentPathDist += flightSpeed * speedMod * Time.deltaTime;
            currentPathDist = Mathf.Clamp(currentPathDist, 0f, totalPathLength);

            SamplePathAtDistance(currentPathDist, out Vector3 centerPos, out Vector3 tangent, out Vector3 normal);

            currentLateralOffset += input.x * lateralDodgeSpeed * Time.deltaTime;
            currentLateralOffset = Mathf.Clamp(currentLateralOffset, -maxLateralDodge, maxLateralDodge);

            if (Mathf.Approximately(input.x, 0f))
            {
                currentLateralOffset = Mathf.MoveTowards(currentLateralOffset, 0f, Time.deltaTime * 2.0f);
            }

            Vector3 targetPos = centerPos + normal * currentLateralOffset;
            targetPos.z = 0f;
            transform.position = targetPos;

            float roadAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg - 90f;
            float bankAngle = -input.x * maxBankAngle;
            float targetAngle = roadAngle + bankAngle;
            currentFacingAngle = Mathf.MoveTowardsAngle(currentFacingAngle, targetAngle, Time.deltaTime * 360f);
            transform.rotation = Quaternion.Euler(0f, 0f, currentFacingAngle);
        }

        private Vector3 GetRoadDirectionAtPosition()
        {
            var cmTarget = FindAnyObjectByType<CinemachineShmupTarget>();
            if (cmTarget != null && cmTarget.CurrentDirection.sqrMagnitude > 0.01f)
            {
                return cmTarget.CurrentDirection;
            }

            if (flightPath != null && flightPath.Count >= 2)
            {
                SamplePathAtDistance(currentPathDist, out _, out Vector3 tangent, out _);
                return tangent;
            }

            return Vector3.up;
        }

        public void SetFlightPath(List<Vector3> path, Vector3 startPos)
        {
            flightPath = (path != null) ? new List<Vector3>(path) : new List<Vector3>();
            currentPathDist = 0f;
            currentLateralOffset = 0f;
            currentMoveVelocity = Vector2.zero;
            currentBankAngle = 0f;
            currentFacingAngle = 0f;

            ComputePathLengths();

            EnsureTargetCamera();
            if (targetCamera != null && !followFlightPath)
            {
                if (transform.parent != targetCamera.transform)
                {
                    transform.SetParent(targetCamera.transform, false);
                }
                float halfH = targetCamera.orthographicSize;
                screenOffset = new Vector2(0f, -halfH * 0.42f);
                transform.localPosition = new Vector3(screenOffset.x, screenOffset.y, 10f);
                transform.localRotation = Quaternion.identity;
                hasInitializedOffset = true;
            }
            else
            {
                if (transform.parent != null) transform.SetParent(null);
                transform.position = startPos;
                transform.rotation = Quaternion.identity;
                hasInitializedOffset = false;
            }
        }

        public void TeleportToPosition(Vector3 newPos)
        {
            EnsureTargetCamera();
            if (targetCamera != null && !followFlightPath)
            {
                if (transform.parent != targetCamera.transform)
                {
                    transform.SetParent(targetCamera.transform, false);
                }
                Vector3 camPos = targetCamera.transform.position;
                screenOffset = new Vector2(newPos.x - camPos.x, newPos.y - camPos.y);
                transform.localPosition = new Vector3(screenOffset.x, screenOffset.y, 10f);
            }
            else
            {
                if (transform.parent != null) transform.SetParent(null);
                transform.position = new Vector3(newPos.x, newPos.y, 0f);
            }
            currentMoveVelocity = Vector2.zero;
            currentBankAngle = 0f;
        }

        private void ComputePathLengths()
        {
            if (flightPath == null || flightPath.Count < 2)
            {
                segmentLengths = new float[0];
                totalPathLength = 0f;
                return;
            }

            segmentLengths = new float[flightPath.Count - 1];
            totalPathLength = 0f;
            for (int i = 0; i < flightPath.Count - 1; i++)
            {
                segmentLengths[i] = Vector3.Distance(flightPath[i], flightPath[i + 1]);
                totalPathLength += segmentLengths[i];
            }
        }

        public void SamplePathAtDistance(float dist, out Vector3 pos, out Vector3 tangent, out Vector3 normal)
        {
            if (flightPath == null || flightPath.Count == 0)
            {
                pos = transform.position;
                tangent = Vector3.up;
                normal = Vector3.left;
                return;
            }

            if (flightPath.Count == 1)
            {
                pos = flightPath[0];
                tangent = Vector3.up;
                normal = Vector3.left;
                return;
            }

            if (segmentLengths == null || segmentLengths.Length != flightPath.Count - 1)
            {
                ComputePathLengths();
            }

            float accumulated = 0f;
            for (int i = 0; i < segmentLengths.Length; i++)
            {
                float segLen = segmentLengths[i];
                if (segLen < 0.0001f) continue;

                if (dist <= accumulated + segLen || i == segmentLengths.Length - 1)
                {
                    float t = Mathf.Clamp01((dist - accumulated) / segLen);
                    pos = Vector3.Lerp(flightPath[i], flightPath[i + 1], t);
                    tangent = (flightPath[i + 1] - flightPath[i]).normalized;
                    normal = new Vector3(-tangent.y, tangent.x, 0f);
                    return;
                }
                accumulated += segLen;
            }

            int last = flightPath.Count - 1;
            pos = flightPath[last];
            tangent = (flightPath[last] - flightPath[last - 1]).normalized;
            normal = new Vector3(-tangent.y, tangent.x, 0f);
        }

        private Vector2 ReadMovementInput()
        {
            if (customInputProvider != null)
            {
                return Vector2.ClampMagnitude(customInputProvider.ReadMovement(), 1f);
            }

            if (isTouchDragging)
            {
                return Vector2.ClampMagnitude(touchVirtualVelocity / Mathf.Max(1f, freeMoveSpeed), 1f);
            }

            Vector2 move = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard[Key.W].isPressed || keyboard[Key.UpArrow].isPressed) move.y += 1f;
                if (keyboard[Key.S].isPressed || keyboard[Key.DownArrow].isPressed) move.y -= 1f;
                if (keyboard[Key.A].isPressed || keyboard[Key.LeftArrow].isPressed) move.x -= 1f;
                if (keyboard[Key.D].isPressed || keyboard[Key.RightArrow].isPressed) move.x += 1f;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f) move += stick;
                if (gamepad.dpad.up.isPressed) move.y += 1f;
                if (gamepad.dpad.down.isPressed) move.y -= 1f;
                if (gamepad.dpad.left.isPressed) move.x -= 1f;
                if (gamepad.dpad.right.isPressed) move.x += 1f;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                move.x += Input.GetAxisRaw("Horizontal");
                move.y += Input.GetAxisRaw("Vertical");
            }
            catch { }
#endif

            return Vector2.ClampMagnitude(move, 1f);
        }

        private bool ReadFireInput()
        {
            if (customInputProvider != null)
            {
                return customInputProvider.ReadFire();
            }

            // Disparo táctil 100% prioritario: mientras cualquier dedo toque la pantalla (Unity Remote o Móvil), dispara ráfagas
            if (enableTouchControls && autoFireOnTouch && MobileTouchInput.IsTouching)
            {
                return true;
            }

#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard[Key.Space].isPressed || keyboard[Key.J].isPressed || keyboard[Key.Enter].isPressed)
                    return true;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.isPressed)
                    return true;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.buttonSouth.isPressed || gamepad.rightTrigger.isPressed)
                    return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J))
                    return true;
            }
            catch { }
#endif

            return false;
        }

        private void HandleShooting()
        {
            if (!canShoot) return;

            if (ReadFireInput())
            {
                if (Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + fireRate;
                    FireBullet();
                }
            }
        }

        private void FireBullet()
        {
            Vector3 spawnPos = transform.position + transform.up * 0.9f;

            if (bulletPrefab != null)
            {
                GameObject b = Instantiate(bulletPrefab, spawnPos, transform.rotation);
                var rb = b.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = (Vector2)transform.up * bulletSpeed;
            }
            else
            {
                GameObject b = new GameObject("Shmup_Bullet");
                b.transform.position = spawnPos;
                b.transform.rotation = transform.rotation;
                var sr = b.AddComponent<SpriteRenderer>();
                sr.sortingOrder = (spriteRenderer != null) ? spriteRenderer.sortingOrder + 1 : 100;
                sr.color = new Color(1f, 0.95f, 0.25f, 1f);

                Texture2D tex = new Texture2D(4, 14, TextureFormat.RGBA32, false);
                for (int x = 0; x < 4; x++)
                    for (int y = 0; y < 14; y++)
                        tex.SetPixel(x, y, Color.white);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 14), new Vector2(0.5f, 0.5f), 16f);

                var bulletScript = b.AddComponent<ShmupBullet>();
                bulletScript.speed = bulletSpeed;
                bulletScript.direction = (Vector2)transform.up;
                bulletScript.lifetime = 2.5f;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (flightPath == null || flightPath.Count < 2) return;
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
            for (int i = 0; i < flightPath.Count - 1; i++)
            {
                Gizmos.DrawLine(flightPath[i], flightPath[i + 1]);
            }
        }
    }
}
