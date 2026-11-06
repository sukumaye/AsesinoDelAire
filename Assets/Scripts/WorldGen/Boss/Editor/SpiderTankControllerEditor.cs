using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using WorldGen.Boss;
using WorldGen.Core;

namespace WorldGen.EditorTools
{
    [CustomEditor(typeof(SpiderTankController))]
    public class SpiderTankControllerEditor : UnityEditor.Editor
    {
        private bool showAttack = true;
        private bool showLegs = true;
        private bool showHead = true;
        private bool showBones = false;

        public override void OnInspectorGUI()
        {
            SpiderTankController tank = (SpiderTankController)target;
            serializedObject.Update();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🛡️ Tanque Boss - Control, Ataque y Animación", EditorStyles.boldLabel);

            // ==========================================
            // 1. BOTONES PRINCIPALES DE ACCIÓN RÁPIDA
            // ==========================================
            EditorGUILayout.BeginVertical("box");

            // Botón 1: Posicionar Afuera al Final del Camino
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.35f); // Verde
            if (GUILayout.Button("🚀 POSICIONAR AFUERA AL FINAL DEL CAMINO", GUILayout.Height(38)))
            {
                Undo.RecordObject(tank, "Posicionar Tanque Afuera");
                tank.PositionOutsideAtRoadEnd();
                EditorUtility.SetDirty(tank);
            }

            EditorGUILayout.BeginHorizontal();
            // Botón 2: Forzar Ángulo Recto (0° en Eje)
            GUI.backgroundColor = new Color(0.25f, 0.65f, 1f); // Azul cielo
            if (GUILayout.Button("📐 Alinear en Eje (0°)", GUILayout.Height(28)))
            {
                Undo.RecordObject(tank.transform, "Alinear en Eje (0°)");
                tank.EnforceRightAngle();
                EditorUtility.SetDirty(tank);
            }

            // Botón 3: Toggle Animación
            GUI.backgroundColor = tank.isAnimating ? new Color(1f, 0.75f, 0.2f) : new Color(0.5f, 0.5f, 0.5f);
            string toggleText = tank.isAnimating ? "⏸️ Pausar" : "▶️ Activar";
            if (GUILayout.Button(toggleText, GUILayout.Height(28)))
            {
                Undo.RecordObject(tank, "Toggle Animación");
                tank.isAnimating = !tank.isAnimating;
                EditorUtility.SetDirty(tank);
            }

            // Botón 4: Resetear Postura
            GUI.backgroundColor = new Color(0.9f, 0.35f, 0.35f);
            if (GUILayout.Button("🔄 Reset Postura", GUILayout.Height(28)))
            {
                Undo.RecordObject(tank, "Reset Postura");
                tank.ResetRestPoses();
                EditorUtility.SetDirty(tank);
            }
            EditorGUILayout.EndHorizontal();

            // Botón 5: Hornear AnimationClip
            GUI.backgroundColor = new Color(0.4f, 0.85f, 0.9f);
            if (GUILayout.Button("🎬 Hornear y Guardar AnimationClip (.anim) y Animator", GUILayout.Height(30)))
            {
                BakeSpiderAnimationClip(tank);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // ==========================================
            // 2. ORIENTACIÓN EN ÁNGULO RECTO Y ANCLAJE
            // ==========================================
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📐 Orientación en Eje (0°) y Anclaje Afuera", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("keepStrictRightAngle"), new GUIContent("Ángulo Recto Estricto", "Mantiene Z = 0° alineado a los ejes sin inclinaciones raras."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("outsideOffsetY"), new GUIContent("Offset Afuera de Pantalla (Y)", "Distancia para colocar al tanque justo afuera de la parte superior del camino."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isAnimating"), new GUIContent("Animación Activa"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animateInEditMode"), new GUIContent("Animar en Escena"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("anchorPosition"), new GUIContent("Posición de Anclaje"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // ==========================================
            // 3. PATRÓN DE ATAQUE EN PANTALLA
            // ==========================================
            showAttack = EditorGUILayout.Foldout(showAttack, "⚔️ Patrón de Ataque (Derecha-Izquierda, Arriba-Abajo, Esquina a Esquina)", true);
            if (showAttack)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("attackPattern"), new GUIContent("Modo de Ataque"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("moveSpeed"), new GUIContent("Velocidad de Movimiento"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("horizontalRange"), new GUIContent("Rango Horizontal (Der-Izq)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("verticalRange"), new GUIContent("Rango Vertical (Arr-Aba)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pauseAtWaypoint"), new GUIContent("Pausa en Esquinas / Extremos"));
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(4);

            // ==========================================
            // 4. ANIMACIÓN MECÁNICA DE PATAS
            // ==========================================
            showLegs = EditorGUILayout.Foldout(showLegs, "⚙️ Animación Mecánica (Delanteras: Palante/Patrás | Traseras: Izq/Der)", true);
            if (showLegs)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("legAnimSpeed"), new GUIContent("Velocidad de Patas"));
                
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("2 Patas Delanteras (1 hueso c/u - Palante y Patrás en Y):", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("frontLegsStrokeY"), new GUIContent("Recorrido Vertical (Y)"));

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("2 Patas Traseras (2 huesos c/u - De Izquierda a Derecha en X):", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rearLegsStrokeX"), new GUIContent("Recorrido Lateral (X)"));
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(4);

            // ==========================================
            // 5. CABEZA / TORRETA CENTRAL
            // ==========================================
            showHead = EditorGUILayout.Foldout(showHead, "🤖 Cabeza / Torreta Central", true);
            if (showHead)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableHeadAnimation"), new GUIContent("Habilitar Cabeza"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("headScanAngle"), new GUIContent("Ángulo de Giro"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("headScanSpeed"), new GUIContent("Velocidad de Giro"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("headBobAmount"), new GUIContent("Retroceso en Y"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("trackPlayerPlane"), new GUIContent("Apuntar al Jugador"));
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(4);

            // ==========================================
            // 6. REFERENCIAS DE HUESOS
            // ==========================================
            showBones = EditorGUILayout.Foldout(showBones, "🦴 Anatomía de Huesos (4 Patas + Cabeza)", true);
            if (showBones)
            {
                EditorGUILayout.BeginVertical("box");
                if (GUILayout.Button("🔍 Re-escanear Huesos Automáticamente"))
                {
                    Undo.RecordObject(tank, "Auto Detect Bones");
                    tank.EnsureBonesDetected();
                    tank.SaveRestPoses(true);
                    EditorUtility.SetDirty(tank);
                }

                EditorGUILayout.PropertyField(serializedObject.FindProperty("boneHead"), new GUIContent("Cabeza (bone_5)"));
                
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Patas Delanteras (1 hueso):", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("frontLeftLeg"), new GUIContent("Pata Delantera Izq (bone_3)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("frontRightLeg"), new GUIContent("Pata Delantera Der (bone_4)"));
                
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Patas Traseras (2 huesos):", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rearUpperLeft"), new GUIContent("Trasera Izq Superior (bone_1)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rearLowerLeft"), new GUIContent("Trasera Izq Inferior (bone_6)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rearUpperRight"), new GUIContent("Trasera Der Superior (bone_2)"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rearLowerRight"), new GUIContent("Trasera Der Inferior (bone_7)"));
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bodyChassis"), new GUIContent("Chasis Central"));
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }

        public static void BakeSpiderAnimationClip(SpiderTankController tank)
        {
            tank.EnsureBonesDetected();
            tank.SaveRestPoses(true);
            tank.EnforceRightAngle();

            string dir = "Assets/Art/Animation";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string animPath = $"{dir}/Tanque_Spider_Patrol.anim";
            string controllerPath = $"{dir}/Tanque_Spider_Controller.controller";

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, animPath);
            }
            else
            {
                clip.ClearCurves();
            }

            clip.frameRate = 30;
            float duration = 2.0f;
            int samples = 60;

            string GetRelativePath(Transform bone)
            {
                if (bone == null || bone == tank.transform) return "";
                string path = bone.name;
                Transform curr = bone.parent;
                while (curr != null && curr != tank.transform)
                {
                    path = curr.name + "/" + path;
                    curr = curr.parent;
                }
                return path;
            }

            // 1. Patas Delanteras: PALANTE Y PATRÁS en el eje Y (sin giros de araña loca)
            void BakeFrontLeg(Transform bone, SpiderTankController.RestPose rest, float strokeY, float phase)
            {
                if (bone == null) return;
                string path = GetRelativePath(bone);

                AnimationCurve curveY = new AnimationCurve();

                for (int i = 0; i <= samples; i++)
                {
                    float t = (i / (float)samples) * duration;
                    float cycle = (i / (float)samples) * Mathf.PI * 2f * 2f;
                    float yVal = rest.pos.y + Mathf.Sin(cycle + phase) * strokeY;

                    curveY.AddKey(new Keyframe(t, yVal));
                }

                clip.SetCurve(path, typeof(Transform), "m_LocalPosition.y", curveY);
            }

            BakeFrontLeg(tank.frontLeftLeg, tank.restFrontL, tank.frontLegsStrokeY, 0f);
            BakeFrontLeg(tank.frontRightLeg, tank.restFrontR, tank.frontLegsStrokeY, Mathf.PI);

            // 2. Patas Traseras: DE IZQUIERDA A DERECHA en el eje X (sin giros de araña loca)
            void BakeRearLegBone(Transform bone, SpiderTankController.RestPose rest, float strokeX, float phase)
            {
                if (bone == null) return;
                string path = GetRelativePath(bone);

                AnimationCurve curveX = new AnimationCurve();

                for (int i = 0; i <= samples; i++)
                {
                    float t = (i / (float)samples) * duration;
                    float cycle = (i / (float)samples) * Mathf.PI * 2f * 2f;
                    float xVal = rest.pos.x + Mathf.Sin(cycle + phase) * strokeX;

                    curveX.AddKey(new Keyframe(t, xVal));
                }

                clip.SetCurve(path, typeof(Transform), "m_LocalPosition.x", curveX);
            }

            // Pata trasera izquierda (2 huesos: bone_1 y bone_6 se mueven en el mismo eje X)
            BakeRearLegBone(tank.rearUpperLeft, tank.restRearUpperL, tank.rearLegsStrokeX, 0f);
            BakeRearLegBone(tank.rearLowerLeft, tank.restRearLowerL, tank.rearLegsStrokeX, 0f);

            // Pata trasera derecha (2 huesos: bone_2 y bone_7 se mueven en el mismo eje X)
            BakeRearLegBone(tank.rearUpperRight, tank.restRearUpperR, -tank.rearLegsStrokeX, Mathf.PI);
            BakeRearLegBone(tank.rearLowerRight, tank.restRearLowerR, -tank.rearLegsStrokeX, Mathf.PI);

            // 3. Cabeza / Torreta (bone_5)
            if (tank.boneHead != null)
            {
                string headPath = GetRelativePath(tank.boneHead);
                AnimationCurve headRot = new AnimationCurve();
                AnimationCurve headPos = new AnimationCurve();

                for (int i = 0; i <= samples; i++)
                {
                    float t = (i / (float)samples) * duration;
                    float norm = (i / (float)samples) * Mathf.PI * 2f;
                    float rotZ = tank.restHead.rot.eulerAngles.z + Mathf.Sin(norm) * tank.headScanAngle;
                    float posY = tank.restHead.pos.y + Mathf.Sin(norm * 2f) * tank.headBobAmount;

                    headRot.AddKey(new Keyframe(t, rotZ));
                    headPos.AddKey(new Keyframe(t, posY));
                }

                clip.SetCurve(headPath, typeof(Transform), "localEulerAnglesRaw.z", headRot);
                clip.SetCurve(headPath, typeof(Transform), "m_LocalPosition.y", headPos);
            }

            // Configurar clip en loop
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            EditorUtility.SetDirty(clip);

            // Animator Controller
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var rootStateMachine = controller.layers[0].stateMachine;
                var state = rootStateMachine.AddState("TanqueCombat");
                state.motion = clip;
            }
            else
            {
                var rootStateMachine = controller.layers[0].stateMachine;
                if (rootStateMachine.states.Length > 0)
                {
                    rootStateMachine.states[0].state.motion = clip;
                }
                else
                {
                    var state = rootStateMachine.AddState("TanqueCombat");
                    state.motion = clip;
                }
            }

            EditorUtility.SetDirty(controller);

            var animator = tank.GetComponent<Animator>();
            if (animator == null)
            {
                animator = tank.gameObject.AddComponent<Animator>();
            }
            animator.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(animator);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green><b>[TanqueBoss]</b> ¡AnimationClip y AnimatorController horneados con 4 patas mecánicas en ejes (delanteras: Y, traseras: X) y cabeza!</color>");
        }
    }
}
