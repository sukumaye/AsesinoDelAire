using System.Collections.Generic;
using UnityEngine;

namespace WorldGen.Shmup
{
    /// <summary>
    /// Objeto guía invisible para Cinemachine en modo Shoot 'em Up.
    /// Avanza de forma pausada y elegante a lo largo de la carretera procedimental en 2D (horizontal, vertical y curvas).
    /// Tu CinemachineCamera sigue a este punto, ofreciendo un tour panorámico de las islas y ciudades mientras el jugador combate libremente dentro de la pantalla.
    /// </summary>
    [SelectionBase]
    public class CinemachineShmupTarget : MonoBehaviour
    {
        [Header("🚀 Motor de Desplazamiento")]
        [Tooltip("Si está activo, el objetivo guía avanza suavemente por la carretera a ritmo constante.")]
        public bool isScrolling = true;

        [Tooltip("Velocidad de avance pausada y elegante (unidades/segundo).")]
        [Range(0.5f, 5.0f)]
        public float scrollSpeed = 1.6f;

        [Header("🕊️ Suavizado Cinematográfico")]
        [Tooltip("Amortiguación suave para que la cámara nunca pegue tirones en las curvas.")]
        [Range(0.1f, 1.5f)]
        public float smoothTime = 0.55f;

        [Header("🛣️ Puntos de la Carretera")]
        public List<Vector3> roadPoints = new List<Vector3>();

        /// <summary>
        /// Dirección normalizada (tangente) hacia donde avanza la carretera en este instante.
        /// </summary>
        public Vector3 CurrentDirection { get; private set; } = Vector3.up;

        private float currentDistance = 0f;
        private float totalPathLength = 0f;
        private float[] segmentLengths;
        private Vector3 currentVelocity = Vector3.zero;

        private void Awake()
        {
            ComputePathLengths();
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (!isScrolling) return;
            if (roadPoints == null || roadPoints.Count < 2) return;

            // 1. Avanzar a lo largo de la longitud real de la carretera
            currentDistance += scrollSpeed * Time.deltaTime;
            currentDistance = Mathf.Clamp(currentDistance, 0f, totalPathLength);

            // 2. Muestrear posición exacta en la polilínea 2D
            Vector3 targetPos = EvaluatePointAtDistance(currentDistance, out Vector3 tangent);
            if (tangent.sqrMagnitude > 0.001f) CurrentDirection = tangent;

            // 3. Posicionar directamente en la polilínea continua (Cinemachine se encarga del suavizado cinematográfico)
            transform.position = targetPos;
        }

        public void SetupAtPathStart(Vector3 startPoint, List<Vector3> points)
        {
            roadPoints = (points != null) ? new List<Vector3>(points) : new List<Vector3>();
            currentDistance = 0f;
            currentVelocity = Vector3.zero;

            ComputePathLengths();

            if (roadPoints.Count > 0)
            {
                transform.position = roadPoints[0];
            }
            else
            {
                transform.position = startPoint;
            }
            isScrolling = true;
        }

        private void ComputePathLengths()
        {
            if (roadPoints == null || roadPoints.Count < 2)
            {
                segmentLengths = new float[0];
                totalPathLength = 0f;
                return;
            }

            segmentLengths = new float[roadPoints.Count - 1];
            totalPathLength = 0f;
            for (int i = 0; i < roadPoints.Count - 1; i++)
            {
                segmentLengths[i] = Vector3.Distance(roadPoints[i], roadPoints[i + 1]);
                totalPathLength += segmentLengths[i];
            }
        }

        public Vector3 EvaluatePointAtDistance(float dist)
        {
            return EvaluatePointAtDistance(dist, out _);
        }

        public Vector3 EvaluatePointAtDistance(float dist, out Vector3 tangent)
        {
            tangent = Vector3.up;
            if (roadPoints == null || roadPoints.Count == 0) return transform.position;
            if (roadPoints.Count == 1) return roadPoints[0];

            if (segmentLengths == null || segmentLengths.Length != roadPoints.Count - 1)
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
                    tangent = (roadPoints[i + 1] - roadPoints[i]).normalized;
                    return Vector3.Lerp(roadPoints[i], roadPoints[i + 1], t);
                }
                accumulated += segLen;
            }

            int last = roadPoints.Count - 1;
            tangent = (roadPoints[last] - roadPoints[last - 1]).normalized;
            return roadPoints[last];
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 0.9f, 1f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, 0.8f);
            Gizmos.DrawLine(transform.position + Vector3.left * 1.2f, transform.position + Vector3.right * 1.2f);
            Gizmos.DrawLine(transform.position + Vector3.down * 1.2f, transform.position + Vector3.up * 1.2f);
        }
    }
}
