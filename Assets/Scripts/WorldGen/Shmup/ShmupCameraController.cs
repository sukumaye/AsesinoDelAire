using System.Collections.Generic;
using UnityEngine;

namespace WorldGen.Shmup
{
    /// <summary>
    /// Controlador de cámara para el modo Shoot 'em Up.
    /// - Sigue el trazado del camino desde el sur (abajo) hacia el norte (arriba) con suavidad cinematográfica.
    /// - Utiliza amortiguación por resorte crítico (SmoothDamp) eliminando cualquier movimiento brusco o salto.
    /// - Anticipa las curvas del camino mediante look-ahead para inclinar la toma con delicadeza.
    /// - Se desplaza a ritmo lento y pausado por defecto para una experiencia de vuelo disfrutable.
    /// - Delimita el campo de acción del jugador dentro de la pantalla.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ShmupCameraController : MonoBehaviour
    {
        [Header("🚀 Motor de Avance (La Cámara Conduce Sola)")]
        [Tooltip("Si está activo, la cámara avanza automáticamente hacia el norte siguiendo las curvas de la carretera.")]
        public bool isScrolling = true;

        [Tooltip("Velocidad a la que la cámara avanza sola hacia arriba a lo largo del camino (unidades/segundo).")]
        [Range(0.5f, 6.0f)]
        public float scrollSpeed = 2.2f;

        [Header("✈️ Avión Opcional (La cámara NO depende del avión)")]
        [Tooltip("OPCIONAL: La cámara NO persigue al avión; avanza sola por el camino. Este campo solo sirve para acelerar un poco si el jugador vuela muy arriba.")]
        public Transform targetPlane;

        [Tooltip("Aceleración extra opcional si el jugador vuela en el 35% superior de la pantalla.")]
        [Range(0.0f, 8.0f)]
        public float playerPushSpeed = 2.5f;

        [Header("🕊️ Suavizado Cinematográfico (SmoothDamp)")]
        [Tooltip("Tiempo de amortiguación para cambios horizontales de posición (cuanto mayor, más suave).")]
        [Range(0.1f, 1.5f)]
        public float smoothTimeX = 0.55f;

        [Tooltip("Tiempo de amortiguación para el avance vertical.")]
        [Range(0.1f, 1.5f)]
        public float smoothTimeY = 0.35f;

        [Tooltip("Distancia en unidades hacia adelante para anticipar las curvas del camino.")]
        [Range(1.0f, 8.0f)]
        public float lookAheadDistance = 3.5f;

        [Tooltip("Offset de la cámara respecto al camino (adelantada en Y para encuadrar al avión abajo).")]
        public Vector2 cameraOffset = new Vector2(0f, 3.0f);

        [Header("🛣️ Puntos de la Trayectoria")]
        [Tooltip("Lista secuencial de coordenadas de la columna vertebral óptima generada por MapGenerator.")]
        public List<Vector3> roadPoints = new List<Vector3>();

        private Camera cam;
        private float currentProgressY;
        private float maxRoadY = 100f;
        private Vector3 currentVelocity = Vector3.zero;
        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            currentProgressY = transform.position.y - cameraOffset.y;
        }

        private void Start()
        {
            if (targetPlane == null)
            {
                var plane = FindAnyObjectByType<ShmupPlaneController>();
                if (plane != null) targetPlane = plane.transform;
            }
        }

        private void Update()
        {
            // Si hay un avión objetivo, seguirlo suavemente en 2D (horizontal, vertical y curvas)
            if (targetPlane != null)
            {
                Vector3 targetPos = new Vector3(targetPlane.position.x + cameraOffset.x, targetPlane.position.y + cameraOffset.y, -10f);
                float curX = Mathf.SmoothDamp(transform.position.x, targetPos.x, ref currentVelocity.x, smoothTimeX);
                float curY = Mathf.SmoothDamp(transform.position.y, targetPos.y, ref currentVelocity.y, smoothTimeY);
                transform.position = new Vector3(curX, curY, -10f);
                return;
            }

            if (!isScrolling) return;

            // 1. Calcular el avance vertical (Y)
            float step = scrollSpeed * Time.deltaTime;

            if (targetPlane != null && cam != null)
            {
                // Si el jugador supera el 65% superior de la pantalla, la cámara acompaña suavemente
                Vector3 viewPos = cam.WorldToViewportPoint(targetPlane.position);
                if (viewPos.y > 0.65f)
                {
                    float pushRatio = Mathf.Clamp01((viewPos.y - 0.65f) / 0.30f);
                    step += pushRatio * playerPushSpeed * Time.deltaTime;
                }
            }

            currentProgressY += step;

            // Limitar al final de la carretera si hay puntos
            if (roadPoints != null && roadPoints.Count > 0)
            {
                currentProgressY = Mathf.Min(currentProgressY, maxRoadY);
            }

            // 2. Muestrear la coordenada X anticipada con look-ahead (curva suave)
            float targetLookAheadY = currentProgressY + lookAheadDistance;
            float targetX = SampleRoadXAtY(targetLookAheadY);

            // 3. Objetivo final de la cámara
            Vector3 targetPosition = new Vector3(targetX + cameraOffset.x, currentProgressY + cameraOffset.y, -10f);

            // 4. Amortiguación crítica desacoplada por ejes (SmoothDamp) para máxima delicadeza
            float newX = Mathf.SmoothDamp(transform.position.x, targetPosition.x, ref currentVelocity.x, smoothTimeX);
            float newY = Mathf.SmoothDamp(transform.position.y, targetPosition.y, ref currentVelocity.y, smoothTimeY);

            transform.position = new Vector3(newX, newY, -10f);
        }

        /// <summary>
        /// Muestrea la coordenada X de la carretera a una altura Y dada, interpolando entre los waypoints más cercanos.
        /// </summary>
        public float SampleRoadXAtY(float targetY)
        {
            if (roadPoints == null || roadPoints.Count == 0) return transform.position.x;

            if (targetY <= roadPoints[0].y) return roadPoints[0].x;
            if (targetY >= roadPoints[roadPoints.Count - 1].y) return roadPoints[roadPoints.Count - 1].x;

            // Buscar el segmento en el que cae targetY
            for (int i = 0; i < roadPoints.Count - 1; i++)
            {
                Vector3 pA = roadPoints[i];
                Vector3 pB = roadPoints[i + 1];

                float minY = Mathf.Min(pA.y, pB.y);
                float maxY = Mathf.Max(pA.y, pB.y);

                if (targetY >= minY && targetY <= maxY)
                {
                    if (Mathf.Approximately(maxY, minY)) return (pA.x + pB.x) * 0.5f;
                    float t = (targetY - pA.y) / (pB.y - pA.y);
                    return Mathf.Lerp(pA.x, pB.x, Mathf.Clamp01(t));
                }
            }

            // Fallback: punto más cercano en Y
            float bestDist = float.MaxValue;
            float bestX = roadPoints[0].x;
            foreach (var pt in roadPoints)
            {
                float d = Mathf.Abs(pt.y - targetY);
                if (d < bestDist)
                {
                    bestDist = d;
                    bestX = pt.x;
                }
            }
            return bestX;
        }

        /// <summary>
        /// Inicializa y coloca la cámara en la posición de inicio del camino (sur) orientada hacia el norte con suavidad.
        /// </summary>
        public void SetupAtPathStart(Vector3 startPoint, List<Vector3> points)
        {
            roadPoints = (points != null) ? new List<Vector3>(points) : new List<Vector3>();

            if (roadPoints.Count > 0)
            {
                maxRoadY = roadPoints[roadPoints.Count - 1].y;
            }
            else
            {
                maxRoadY = startPoint.y + 100f;
            }

            currentProgressY = startPoint.y;
            currentVelocity = Vector3.zero;

            float initialX = (roadPoints.Count > 0) ? roadPoints[0].x : startPoint.x;
            transform.position = new Vector3(initialX + cameraOffset.x, currentProgressY + cameraOffset.y, -10f);

            isInitialized = true;
            isScrolling = true;

            Debug.Log($"<color=cyan><b>[ShmupCameraController]</b> Cámara posicionada con suavidad en inicio del camino: ({initialX:F1}, {currentProgressY:F1}). Trazado: {roadPoints.Count} puntos.</color>");
        }

        private void OnDrawGizmosSelected()
        {
            if (roadPoints == null || roadPoints.Count < 2) return;

            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.85f);
            for (int i = 0; i < roadPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(roadPoints[i], roadPoints[i + 1]);
            }
        }
    }
}
