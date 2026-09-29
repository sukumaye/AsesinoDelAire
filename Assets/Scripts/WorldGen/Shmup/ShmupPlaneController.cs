using System.Collections.Generic;
using UnityEngine;
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

        // Estado interno en pantalla (offset respecto al centro de la cámara)
        [SerializeField]
        private Vector2 screenOffset = new Vector2(0f, -5f);
        private Vector2 currentMoveVelocity = Vector2.zero;
        private float currentBankAngle = 0f;
        private float currentFacingAngle = 0f;
        private bool hasInitializedOffset = false;

        // Estado interno del autopiloto
        private float currentPathDist = 0f;
        private float currentLateralOffset = 0f;
        private float[] segmentLengths;
        private float totalPathLength = 0f;

        private float nextFireTime = 0f;
        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            EnsureTargetCamera();
        }

        private void Start()
        {
            EnsureTargetCamera();
            ComputePathLengths();
            if (!hasInitializedOffset)
            {
                InitializeScreenOffset();
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
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) targetCamera = FindAnyObjectByType<Camera>();
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
                    if (transform.parent != targetCamera.transform)
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

            // Emparentar directamente a la cámara para eliminar 100% cualquier latencia de frame o vibración
            if (transform.parent != targetCamera.transform)
            {
                transform.SetParent(targetCamera.transform, false);
            }

            // 1. Manejo de velocidad limpio, directo y reactivo (cero patinaje ni rebotes)
            if (input.sqrMagnitude > 0.01f)
            {
                currentMoveVelocity = input * freeMoveSpeed;
            }
            else
            {
                currentMoveVelocity = Vector2.MoveTowards(currentMoveVelocity, Vector2.zero, freeDeceleration * Time.deltaTime);
            }

            // 2. Aplicar movimiento al offset relativo de la pantalla
            screenOffset += currentMoveVelocity * Time.deltaTime;

            // 3. Confinamiento matemático exacto en la vista de la cámara
            float halfHeight = targetCamera.orthographicSize;
            float halfWidth = halfHeight * targetCamera.aspect;

            float limitX = Mathf.Max(0.5f, halfWidth * (1f - viewportMargin.x));
            float limitY = Mathf.Max(0.5f, halfHeight * (1f - viewportMargin.y));

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
            switch (rotationMode)
            {
                case PlaneRotationMode.FixedUp:
                default:
                {
                    float targetBank = -input.x * maxBankAngle;
                    currentBankAngle = Mathf.MoveTowards(currentBankAngle, targetBank, Time.deltaTime * bankSpeed);
                    transform.localRotation = Quaternion.Euler(0f, 0f, currentBankAngle);
                    break;
                }
                case PlaneRotationMode.OmnidirectionalWASD:
                {
                    if (input.sqrMagnitude > 0.04f)
                    {
                        float targetAngle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg - 90f;
                        currentFacingAngle = Mathf.MoveTowardsAngle(currentFacingAngle, targetAngle, Time.deltaTime * 360f);
                    }
                    transform.localRotation = Quaternion.Euler(0f, 0f, currentFacingAngle);
                    break;
                }
                case PlaneRotationMode.AlignWithPath:
                {
                    Vector3 forwardDir = GetRoadDirectionAtPosition();
                    float baseAngle = Mathf.Atan2(forwardDir.y, forwardDir.x) * Mathf.Rad2Deg - 90f;
                    float targetBank = -input.x * maxBankAngle;
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
#elif ENABLE_LEGACY_INPUT_MANAGER
            move.x = Input.GetAxisRaw("Horizontal");
            move.y = Input.GetAxisRaw("Vertical");
#endif

            return Vector2.ClampMagnitude(move, 1f);
        }

        private bool ReadFireInput()
        {
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
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J);
#else
            return false;
#endif
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
