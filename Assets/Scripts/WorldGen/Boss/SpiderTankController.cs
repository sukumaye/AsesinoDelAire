using System;
using UnityEngine;
using WorldGen.Core;
using WorldGen.Shmup;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WorldGen.Boss
{
    public enum BossAttackPattern
    {
        [Tooltip("Patrulla completa: alterna de derecha a izquierda, de arriba a abajo y de esquina en esquina.")]
        FullCycle,
        [Tooltip("Movimiento continuo de derecha a izquierda en el eje horizontal.")]
        RightToLeft,
        [Tooltip("Movimiento continuo de arriba a abajo en el eje vertical.")]
        UpDown,
        [Tooltip("Arremetidas diagonales de esquina a esquina en la pantalla.")]
        CornerToCorner,
        [Tooltip("Quieto en su posición con animación activa de patas y torreta.")]
        IdleAnchor
    }

    /// <summary>
    /// Controlador para el Tanque Boss:
    /// - 4 Patas mecánicas:
    ///   * 2 Patas delanteras con 1 hueso cada una (bone_3, bone_4): se mueven exclusivamente PALANTE Y PATRÁS (eje Y).
    ///   * 2 Patas traseras con 2 huesos cada una (izq: bone_1 + bone_6, der: bone_2 + bone_7): se mueven de IZQUIERDA A DERECHA en el mismo eje (eje X).
    /// - Cabeza / torreta central móvil (bone_5).
    /// - Orientación estricta en ángulo recto (0°, alineado en los ejes sin inclinaciones raras).
    /// - Posicionable un poco afuera de la pantalla al final del camino.
    /// - Patrón de ataque en pantalla: de derecha a izquierda, de arriba a abajo y de esquina en esquina.
    /// </summary>
    [ExecuteAlways]
    [SelectionBase]
    [DisallowMultipleComponent]
    [AddComponentMenu("AK47/Tanque Boss")]
    public class SpiderTankController : MonoBehaviour
    {
        [Header("🎮 Control General")]
        [Tooltip("Activa o desactiva la animación y el movimiento del tanque.")]
        public bool isAnimating = true;

        [Tooltip("Si está activo, actualiza la animación y movimiento incluso en modo edición en la escena.")]
        public bool animateInEditMode = true;

        [Header("📐 Orientación en Ángulo Recto (0° Fijo en Eje)")]
        [Tooltip("Forzado a ángulo recto: rotación fija en 0°, alineado a los ejes sin inclinaciones raras.")]
        public bool keepStrictRightAngle = true;

        [Tooltip("Rotación Z base (estrictamente 0°).")]
        public float baseRotationZ = 0f;

        [Header("🗺️ Posición y Anclaje (Afuera al Final del Camino)")]
        [Tooltip("Punto central de anclaje de la arena o final del camino.")]
        public Vector3 anchorPosition;

        [Tooltip("Desplazamiento para situarlo un poco afuera de la pantalla hacia arriba (en Y).")]
        public float outsideOffsetY = 4.5f;

        [Tooltip("Desplazamiento fino adicional.")]
        public Vector3 roadEndOffset = new Vector3(0f, 0f, 0f);

        [Header("⚔️ Patrón de Ataque en Pantalla")]
        public BossAttackPattern attackPattern = BossAttackPattern.FullCycle;

        [Tooltip("Velocidad de traslación del tanque en pantalla.")]
        [Range(1f, 15f)]
        public float moveSpeed = 4.5f;

        [Tooltip("Distancia horizontal del ataque (amplitud de derecha a izquierda).")]
        [Range(2f, 18f)]
        public float horizontalRange = 6.0f;

        [Tooltip("Distancia vertical del ataque (amplitud de arriba a abajo).")]
        [Range(2f, 18f)]
        public float verticalRange = 5.0f;

        [Tooltip("Tiempo de pausa en cada esquina o extremo antes de cambiar de fase (segundos).")]
        [Range(0f, 3f)]
        public float pauseAtWaypoint = 0.5f;

        [Header("⚙️ Animación Mecánica de Patas (4 Patas)")]
        [Tooltip("Velocidad del movimiento cíclico de las patas.")]
        [Range(0.5f, 12f)]
        public float legAnimSpeed = 3.5f;

        [Tooltip("Amplitud de movimiento PALANTE Y PATRÁS de las patas delanteras (eje Y).")]
        [Range(0.1f, 3.0f)]
        public float frontLegsStrokeY = 0.7f;

        [Tooltip("Amplitud de movimiento DE IZQUIERDA A DERECHA de las patas traseras (eje X).")]
        [Range(0.1f, 3.0f)]
        public float rearLegsStrokeX = 0.8f;

        [Header("🤖 Cabeza / Torreta Central")]
        public bool enableHeadAnimation = true;

        [Tooltip("Ángulo de giro o escaneo de la cabeza.")]
        [Range(0f, 35f)]
        public float headScanAngle = 18f;

        [Tooltip("Velocidad de escaneo de la cabeza.")]
        [Range(0.2f, 6f)]
        public float headScanSpeed = 1.5f;

        [Tooltip("Retroceso o desplazamiento en Y de la cabeza.")]
        [Range(0f, 0.5f)]
        public float headBobAmount = 0.12f;

        [Tooltip("Orientar sutilmente la torreta hacia el jugador.")]
        public bool trackPlayerPlane = true;

        [Header("🦴 Anatomía de Huesos")]
        [Tooltip("Cabeza central (bone_5).")]
        public Transform boneHead;

        [Header("🦴 Patas Delanteras (1 Hueso cada una - Eje Y)")]
        [Tooltip("Pata delantera izquierda (bone_3).")]
        public Transform frontLeftLeg;
        [Tooltip("Pata delantera derecha (bone_4).")]
        public Transform frontRightLeg;

        [Header("🦴 Patas Traseras (2 Huesos cada una - Eje X)")]
        [Tooltip("Pata trasera izquierda - Muñón / Superior (bone_1).")]
        public Transform rearUpperLeft;
        [Tooltip("Pata trasera izquierda - Pata Inferior (bone_6).")]
        public Transform rearLowerLeft;

        [Tooltip("Pata trasera derecha - Muñón / Superior (bone_2).")]
        public Transform rearUpperRight;
        [Tooltip("Pata trasera derecha - Pata Inferior (bone_7).")]
        public Transform rearLowerRight;

        [Tooltip("Chasis / Cuerpo central.")]
        public Transform bodyChassis;

        // Posturas de reposo registradas
        [Serializable]
        public struct RestPose
        {
            public Vector3 pos;
            public Quaternion rot;
        }

        [HideInInspector] public RestPose restHead;
        [HideInInspector] public RestPose restFrontL;       // bone_3
        [HideInInspector] public RestPose restFrontR;       // bone_4
        [HideInInspector] public RestPose restRearUpperL;   // bone_1
        [HideInInspector] public RestPose restRearLowerL;   // bone_6
        [HideInInspector] public RestPose restRearUpperR;   // bone_2
        [HideInInspector] public RestPose restRearLowerR;   // bone_7
        [HideInInspector] public bool hasRestPoses = false;

        // Variables de estado del patrón de ataque
        private int currentAttackStep = 0;
        private float stepTimer = 0f;
        private float currentHeadAimAngle = 0f;
        private Transform playerTransform;

        private void OnValidate()
        {
            if (keepStrictRightAngle)
            {
                baseRotationZ = 0f;
            }
        }

        private void OnEnable()
        {
            EnsureBonesDetected();
            EnforceRightAngle();
            if (anchorPosition == Vector3.zero)
            {
                anchorPosition = transform.position;
            }
        }

        private void Start()
        {
            EnsureBonesDetected();
            EnforceRightAngle();
            FindPlayerPlane();
        }

        /// <summary>
        /// Mantiene al tanque estrictamente en ángulo recto (0° en el eje, sin alabeos ni inclinaciones).
        /// </summary>
        public void EnforceRightAngle()
        {
            baseRotationZ = 0f;
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        public void EnforceFacingDown()
        {
            EnforceRightAngle();
        }

        public void SetAnchorPosition(Vector3 newAnchor)
        {
            anchorPosition = newAnchor;
        }

        /// <summary>
        /// Detecta automáticamente los huesos y partes en la jerarquía:
        /// 2 patas delanteras con 1 hueso (bone_3, bone_4),
        /// 2 patas traseras con 2 huesos (bone_1 + bone_6, bone_2 + bone_7),
        /// cabeza (bone_5).
        /// </summary>
        [ContextMenu("🔍 Auto-detectar Huesos y Partes")]
        public void EnsureBonesDetected()
        {
            // 1. Cabeza (bone_5)
            if (boneHead == null) boneHead = FindRecursive(transform, "bone_5", "cabeza", "head", "turret");

            // 2. Patas Delanteras (1 hueso cada una: bone_3 y bone_4)
            if (frontLeftLeg == null) frontLeftLeg = FindRecursive(transform, "bone_3", "pata izquierdo", "pata izquierda");
            if (frontRightLeg == null) frontRightLeg = FindRecursive(transform, "bone_4", "pata derecho", "pata derecha");

            // 3. Patas Traseras (2 huesos cada una: muñones bone_1/bone_2 + inferiores bone_6/bone_7)
            if (rearUpperLeft == null) rearUpperLeft = FindRecursive(transform, "bone_1", "muyon izquierdo", "muñon izquierdo");
            if (rearLowerLeft == null) rearLowerLeft = FindRecursive(transform, "bone_6", "pata inferior izquierda");

            if (rearUpperRight == null) rearUpperRight = FindRecursive(transform, "bone_2", "muyon derecho", "muñon derecho");
            if (rearLowerRight == null) rearLowerRight = FindRecursive(transform, "bone_7", "pata inferior derecha");

            // 4. Chasis
            if (bodyChassis == null) bodyChassis = FindRecursive(transform, "Tanque", "chassis", "body");

            SaveRestPoses(false);
        }

        private Transform FindRecursive(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (var n in names)
            {
                if (string.Equals(root.name, n, StringComparison.OrdinalIgnoreCase)) return root;
            }
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindRecursive(root.GetChild(i), names);
                if (found != null) return found;
            }
            return null;
        }

        [ContextMenu("💾 Guardar Posturas de Reposo")]
        public void SaveRestPoses(bool force = true)
        {
            if (hasRestPoses && !force) return;

            if (boneHead != null) restHead = new RestPose { pos = boneHead.localPosition, rot = boneHead.localRotation };
            if (frontLeftLeg != null) restFrontL = new RestPose { pos = frontLeftLeg.localPosition, rot = frontLeftLeg.localRotation };
            if (frontRightLeg != null) restFrontR = new RestPose { pos = frontRightLeg.localPosition, rot = frontRightLeg.localRotation };

            if (rearUpperLeft != null) restRearUpperL = new RestPose { pos = rearUpperLeft.localPosition, rot = rearUpperLeft.localRotation };
            if (rearLowerLeft != null) restRearLowerL = new RestPose { pos = rearLowerLeft.localPosition, rot = rearLowerLeft.localRotation };

            if (rearUpperRight != null) restRearUpperR = new RestPose { pos = rearUpperRight.localPosition, rot = rearUpperRight.localRotation };
            if (rearLowerRight != null) restRearLowerR = new RestPose { pos = rearLowerRight.localPosition, rot = rearLowerRight.localRotation };

            hasRestPoses = true;
        }

        [ContextMenu("🔄 Resetear Postura Original")]
        public void ResetRestPoses()
        {
            if (!hasRestPoses) return;

            if (boneHead != null) { boneHead.localPosition = restHead.pos; boneHead.localRotation = restHead.rot; }
            if (frontLeftLeg != null) { frontLeftLeg.localPosition = restFrontL.pos; frontLeftLeg.localRotation = restFrontL.rot; }
            if (frontRightLeg != null) { frontRightLeg.localPosition = restFrontR.pos; frontRightLeg.localRotation = restFrontR.rot; }

            if (rearUpperLeft != null) { rearUpperLeft.localPosition = restRearUpperL.pos; rearUpperLeft.localRotation = restRearUpperL.rot; }
            if (rearLowerLeft != null) { rearLowerLeft.localPosition = restRearLowerL.pos; rearLowerLeft.localRotation = restRearLowerL.rot; }

            if (rearUpperRight != null) { rearUpperRight.localPosition = restRearUpperR.pos; rearUpperRight.localRotation = restRearUpperR.rot; }
            if (rearLowerRight != null) { rearLowerRight.localPosition = restRearLowerR.pos; rearLowerRight.localRotation = restRearLowerR.rot; }

            if (anchorPosition != Vector3.zero)
            {
                transform.position = anchorPosition;
            }
            EnforceRightAngle();
        }

        // LateUpdate garantiza que la animación mecánica se vea siempre, incluso si hay un Animator
        private void LateUpdate()
        {
            if (!isAnimating) return;

#if UNITY_EDITOR
            if (!Application.isPlaying && !animateInEditMode) return;
#endif

            float time = GetCurrentTime();
            float deltaTime = GetDeltaTime();

            // 1. Movimiento del tanque en pantalla (Patrón de combate en ángulo recto 0°)
            UpdateAttackMovement(time, deltaTime);

            // 2. Patas mecánicas:
            // - Delanteras: palante y patrás en eje Y
            // - Traseras: izquierda a derecha en el mismo eje X
            UpdateLegsMechanicalAnimation(time);

            // 3. Cabeza / torreta central
            UpdateHeadAnimation(time, deltaTime);
        }

        private float GetCurrentTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return (float)EditorApplication.timeSinceStartup;
#endif
            return Time.time;
        }

        private float GetDeltaTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return 0.016f;
#endif
            return Time.deltaTime;
        }

        /// <summary>
        /// Mueve al tanque según el patrón de ataque en ángulo recto (0° estricto, sin rotaciones raras).
        /// </summary>
        private void UpdateAttackMovement(float time, float deltaTime)
        {
            if (anchorPosition == Vector3.zero) anchorPosition = transform.position;

            Vector3 targetPos = anchorPosition;

            switch (attackPattern)
            {
                case BossAttackPattern.RightToLeft:
                {
                    // Movimiento continuo de derecha a izquierda en el eje horizontal X
                    float x = Mathf.Sin(time * moveSpeed * 0.5f) * horizontalRange;
                    targetPos = anchorPosition + new Vector3(x, 0f, 0f);
                    transform.position = targetPos;
                    break;
                }

                case BossAttackPattern.UpDown:
                {
                    // Movimiento continuo de arriba a abajo en el eje vertical Y
                    float y = Mathf.Sin(time * moveSpeed * 0.5f) * verticalRange;
                    targetPos = anchorPosition + new Vector3(0f, y, 0f);
                    transform.position = targetPos;
                    break;
                }

                case BossAttackPattern.CornerToCorner:
                {
                    // Arremetidas de esquina a esquina en la pantalla
                    Vector3[] corners = new Vector3[]
                    {
                        anchorPosition + new Vector3(-horizontalRange, verticalRange, 0f),  // Esquina Sup-Izq
                        anchorPosition + new Vector3(horizontalRange, -verticalRange, 0f),  // Esquina Inf-Der
                        anchorPosition + new Vector3(horizontalRange, verticalRange, 0f),   // Esquina Sup-Der
                        anchorPosition + new Vector3(-horizontalRange, -verticalRange, 0f)  // Esquina Inf-Izq
                    };

                    stepTimer += deltaTime;
                    Vector3 currentDest = corners[currentAttackStep % corners.Length];
                    transform.position = Vector3.MoveTowards(transform.position, currentDest, moveSpeed * deltaTime);

                    if (Vector3.Distance(transform.position, currentDest) < 0.2f && stepTimer > pauseAtWaypoint)
                    {
                        stepTimer = 0f;
                        currentAttackStep++;
                    }
                    break;
                }

                case BossAttackPattern.FullCycle:
                {
                    // Ciclo completo:
                    // 1) Derecha a izquierda (eje X)
                    // 2) Arriba a abajo (eje Y)
                    // 3) De esquina a esquina en la pantalla
                    Vector3[] waypoints = new Vector3[]
                    {
                        anchorPosition + new Vector3(horizontalRange, 0f, 0f),               // 0: Derecha
                        anchorPosition + new Vector3(-horizontalRange, 0f, 0f),              // 1: Izquierda
                        anchorPosition + new Vector3(0f, 0f, 0f),                           // 2: Centro
                        anchorPosition + new Vector3(0f, -verticalRange, 0f),               // 3: Abajo
                        anchorPosition + new Vector3(0f, verticalRange * 0.5f, 0f),         // 4: Arriba
                        anchorPosition + new Vector3(-horizontalRange, verticalRange, 0f),  // 5: Esquina Sup-Izq
                        anchorPosition + new Vector3(horizontalRange, -verticalRange, 0f),  // 6: Esquina Inf-Der
                        anchorPosition + new Vector3(horizontalRange, verticalRange, 0f),   // 7: Esquina Sup-Der
                        anchorPosition + new Vector3(-horizontalRange, -verticalRange, 0f), // 8: Esquina Inf-Izq
                    };

                    stepTimer += deltaTime;
                    Vector3 dest = waypoints[currentAttackStep % waypoints.Length];
                    transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * deltaTime);

                    if (Vector3.Distance(transform.position, dest) < 0.2f && stepTimer > pauseAtWaypoint)
                    {
                        stepTimer = 0f;
                        currentAttackStep++;
                    }
                    break;
                }

                case BossAttackPattern.IdleAnchor:
                default:
                    transform.position = anchorPosition;
                    break;
            }

            // RIGUROSAMENTE EN ÁNGULO RECTO (0° en el eje, sin inclinaciones raras)
            if (keepStrictRightAngle)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }

        /// <summary>
        /// Animación mecánica limpia en los ejes:
        /// - 2 Patas delanteras (1 hueso cada una): movimiento lineal PALANTE Y PATRÁS en el eje Y.
        /// - 2 Patas traseras (2 huesos cada una): movimiento lineal DE IZQUIERDA A DERECHA en el eje X.
        /// Sin rotaciones angulares raras tipo araña loca.
        /// </summary>
        private void UpdateLegsMechanicalAnimation(float time)
        {
            if (!hasRestPoses) SaveRestPoses(false);

            float t = time * legAnimSpeed;

            // 1. PATAS DELANTERAS (2 patas de 1 hueso cada una) -> PALANTE Y PATRÁS (Eje Y)
            float strokeFrontL = Mathf.Sin(t) * frontLegsStrokeY;
            float strokeFrontR = Mathf.Sin(t + Mathf.PI) * frontLegsStrokeY;

            if (frontLeftLeg != null)
            {
                frontLeftLeg.localPosition = restFrontL.pos + new Vector3(0f, strokeFrontL, 0f);
                frontLeftLeg.localRotation = restFrontL.rot;
            }
            if (frontRightLeg != null)
            {
                frontRightLeg.localPosition = restFrontR.pos + new Vector3(0f, strokeFrontR, 0f);
                frontRightLeg.localRotation = restFrontR.rot;
            }

            // 2. PATAS TRASERAS (2 patas de 2 huesos cada una) -> DE IZQUIERDA A DERECHA (Eje X)
            float strokeRearL = Mathf.Sin(t) * rearLegsStrokeX;
            float strokeRearR = Mathf.Sin(t + Mathf.PI) * rearLegsStrokeX;

            // Pata trasera izquierda (bone_1 y bone_6 se desplazan en el mismo eje X)
            if (rearUpperLeft != null)
            {
                rearUpperLeft.localPosition = restRearUpperL.pos + new Vector3(strokeRearL, 0f, 0f);
                rearUpperLeft.localRotation = restRearUpperL.rot;
            }
            if (rearLowerLeft != null)
            {
                rearLowerLeft.localPosition = restRearLowerL.pos + new Vector3(strokeRearL, 0f, 0f);
                rearLowerLeft.localRotation = restRearLowerL.rot;
            }

            // Pata trasera derecha (bone_2 y bone_7 se desplazan en el mismo eje X)
            if (rearUpperRight != null)
            {
                rearUpperRight.localPosition = restRearUpperR.pos + new Vector3(strokeRearR, 0f, 0f);
                rearUpperRight.localRotation = restRearUpperR.rot;
            }
            if (rearLowerRight != null)
            {
                rearLowerRight.localPosition = restRearLowerR.pos + new Vector3(strokeRearR, 0f, 0f);
                rearLowerRight.localRotation = restRearLowerR.rot;
            }
        }

        /// <summary>
        /// Anima sutilmente la cabeza / torreta.
        /// </summary>
        private void UpdateHeadAnimation(float time, float deltaTime)
        {
            if (!enableHeadAnimation || boneHead == null) return;

            // Escaneo lateral sutil
            float scan = Mathf.Sin(time * headScanSpeed) * headScanAngle;

            // Respiración / retroceso vertical
            float bob = Mathf.Sin(time * headScanSpeed * 2f) * headBobAmount;
            boneHead.localPosition = restHead.pos + new Vector3(0f, bob, 0f);

            float targetAngle = scan;
            if (trackPlayerPlane)
            {
                if (playerTransform == null) FindPlayerPlane();

                if (playerTransform != null)
                {
                    Vector3 dir = playerTransform.position - boneHead.position;
                    Vector3 localDir = transform.InverseTransformDirection(dir);
                    float aim = Mathf.Atan2(localDir.x, localDir.y) * Mathf.Rad2Deg;
                    aim = Mathf.Clamp(aim, -30f, 30f);
                    targetAngle = Mathf.Lerp(scan * 0.3f, aim, 0.7f);
                }
            }

            currentHeadAimAngle = Mathf.Lerp(currentHeadAimAngle, targetAngle, deltaTime * 6f);
            boneHead.localRotation = restHead.rot * Quaternion.Euler(0f, 0f, currentHeadAimAngle);
        }

        private void FindPlayerPlane()
        {
            var plane = FindAnyObjectByType<ShmupPlaneController>();
            if (plane != null) playerTransform = plane.transform;
            else
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) playerTransform = p.transform;
            }
        }

        /// <summary>
        /// Posiciona al tanque UN POCO AFUERA de la pantalla al final del camino,
        /// alineado en ángulo recto a 0°.
        /// </summary>
        [ContextMenu("🎯 Posicionar Afuera al Final del Camino (0°)")]
        public void PositionOutsideAtRoadEnd()
        {
            MapGenerator mapGen = FindAnyObjectByType<MapGenerator>();
            if (mapGen == null)
            {
                Debug.LogWarning("[TanqueBoss] No se encontró MapGenerator en la escena.");
                return;
            }

            Vector3 roadEnd = mapGen.GetRoadEndPosition();
            // Situado un poco afuera de la pantalla hacia arriba (Y + outsideOffsetY)
            Vector3 finalPos = roadEnd + new Vector3(0f, outsideOffsetY, 0f) + roadEndOffset;

            SetAnchorPosition(finalPos);
            transform.position = finalPos;

            EnforceRightAngle();

            mapGen.endLevelCharacterInstance = this.transform;
            mapGen.endLevelCharacterRotationZ = 0f;

            EnsureBonesDetected();
            SaveRestPoses(true);

            var sg = GetComponent<UnityEngine.Rendering.SortingGroup>();
            if (sg == null) sg = gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
            sg.sortingOrder = 10;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                EditorUtility.SetDirty(mapGen);
                EditorUtility.SetDirty(gameObject);
            }
#endif

            Debug.Log($"<color=green><b>[TanqueBoss]</b> ¡Tanque '{name}' posicionado UN POCO AFUERA en ({finalPos.x:F1}, {finalPos.y:F1}) y alineado en el eje a 0°!</color>");
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = anchorPosition != Vector3.zero ? anchorPosition : transform.position;

            // Límites de combate de derecha a izquierda y de arriba a abajo
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
            Gizmos.DrawWireCube(center, new Vector3(horizontalRange * 2f, verticalRange * 2f, 0.1f));

            // Esquinas del ataque
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(center + new Vector3(-horizontalRange, verticalRange, 0f), 0.3f);
            Gizmos.DrawSphere(center + new Vector3(horizontalRange, -verticalRange, 0f), 0.3f);
            Gizmos.DrawSphere(center + new Vector3(horizontalRange, verticalRange, 0f), 0.3f);
            Gizmos.DrawSphere(center + new Vector3(-horizontalRange, -verticalRange, 0f), 0.3f);

            // Dirección alineada en eje
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, Vector3.up * 2.5f);
        }
    }
}
