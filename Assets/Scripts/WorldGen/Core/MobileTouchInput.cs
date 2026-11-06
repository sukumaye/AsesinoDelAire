using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace WorldGen.Core
{
    /// <summary>
    /// Puente universal de entrada táctil 100% compatible con móviles Android, Unity Remote 5 y ratón de pruebas en Editor.
    /// Unifica New Input System y Legacy Input Manager con buffer de tolerancia a jitter/paquetes caídos por USB.
    /// </summary>
    public static class MobileTouchInput
    {
        private static bool isInitialized = false;

        // Estado interno con buffer de persistencia para evitar que 'Dropped touch events' corte el arrastre
        private static int lastEvaluatedFrame = -1;
        private static float lastRawTouchTime = -100f;
        private static bool isBufferedTouching = false;
        private static bool wasBufferedTouchingLastFrame = false;
        private static Vector2 lastKnownScreenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        private static Vector2 lastFrameScreenPos = Vector2.zero;
        private static Vector2 currentScreenDelta = Vector2.zero;

        // Tolerancia de 90ms para absorber jitter de paquetes USB en Unity Remote sin añadir retraso perceptible
        private const float TOUCH_LOST_TIMEOUT = 0.09f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            isInitialized = false;
            lastEvaluatedFrame = -1;
            lastRawTouchTime = -100f;
            isBufferedTouching = false;
            wasBufferedTouchingLastFrame = false;
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (EnhancedTouchSupport.enabled)
                {
                    EnhancedTouchSupport.Disable();
                }
            }
            catch { }
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeTouchSubsystem()
        {
            if (isInitialized) return;
            isInitialized = true;
            lastEvaluatedFrame = -1;
        }

        private static void EvaluateState()
        {
            if (Time.frameCount == lastEvaluatedFrame) return;
            lastEvaluatedFrame = Time.frameCount;

            wasBufferedTouchingLastFrame = isBufferedTouching;
            bool rawTouching = SampleRawInput(out Vector2 rawPos);

            if (rawTouching)
            {
                lastRawTouchTime = Time.unscaledTime;
                lastKnownScreenPos = rawPos;
                isBufferedTouching = true;
            }
            else
            {
                // Si perdimos el toque por menos de 90ms (habitual en streaming USB), mantenemos el estado
                if (Time.unscaledTime - lastRawTouchTime < TOUCH_LOST_TIMEOUT)
                {
                    isBufferedTouching = true;
                }
                else
                {
                    isBufferedTouching = false;
                }
            }

            if (isBufferedTouching)
            {
                if (!wasBufferedTouchingLastFrame)
                {
                    currentScreenDelta = Vector2.zero;
                    lastFrameScreenPos = lastKnownScreenPos;
                }
                else
                {
                    currentScreenDelta = lastKnownScreenPos - lastFrameScreenPos;
                    lastFrameScreenPos = lastKnownScreenPos;
                }
            }
            else
            {
                currentScreenDelta = Vector2.zero;
                lastFrameScreenPos = lastKnownScreenPos;
            }
        }

        private static bool SampleRawInput(out Vector2 pos)
        {
            pos = lastKnownScreenPos;

            // 1. Detección Legacy / Unity Remote (Prioridad máxima para streaming por cable USB)
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
            try
            {
                if (Input.touchCount > 0)
                {
                    var t = Input.GetTouch(0);
                    pos = t.position;
                    return true;
                }
                if (Input.GetMouseButton(0))
                {
                    pos = (Vector2)Input.mousePosition;
                    return true;
                }
            }
            catch { }
#endif

            // 2. Detección New Input System
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (EnhancedTouchSupport.enabled && Touch.activeTouches.Count > 0)
                {
                    pos = Touch.activeTouches[0].screenPosition;
                    return true;
                }

                var touchscreen = Touchscreen.current;
                if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
                {
                    pos = touchscreen.primaryTouch.position.ReadValue();
                    return true;
                }

                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.isPressed)
                {
                    pos = mouse.position.ReadValue();
                    return true;
                }
            }
            catch { }
#endif

            return false;
        }

        /// <summary>
        /// ¿Hay al menos un dedo o puntero pulsando la pantalla? (Con buffer antiblindaje de paquetes caídos).
        /// </summary>
        public static bool IsTouching
        {
            get
            {
                EvaluateState();
                return isBufferedTouching;
            }
        }

        /// <summary>
        /// Número de toques o dedos activos en la pantalla.
        /// </summary>
        public static int TouchCount
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
                try
                {
                    if (Input.touchCount > 0) return Input.touchCount;
                    if (Input.GetMouseButton(0)) return 1;
                }
                catch { }
#endif

#if ENABLE_INPUT_SYSTEM
                try
                {
                    if (EnhancedTouchSupport.enabled && Touch.activeTouches.Count > 0)
                        return Touch.activeTouches.Count;

                    var touchscreen = Touchscreen.current;
                    if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
                        return 1;

                    var mouse = Mouse.current;
                    if (mouse != null && mouse.leftButton.isPressed)
                        return 1;
                }
                catch { }
#endif
                return IsTouching ? 1 : 0;
            }
        }

        /// <summary>
        /// Posición en píxeles de pantalla (Screen Coordinates, origen abajo-izquierda) del toque principal.
        /// </summary>
        public static Vector2 PrimaryTouchPosition
        {
            get
            {
                EvaluateState();
                return lastKnownScreenPos;
            }
        }

        /// <summary>
        /// Delta en píxeles del movimiento del toque respecto al fotograma anterior.
        /// </summary>
        public static Vector2 PrimaryTouchDelta
        {
            get
            {
                EvaluateState();
                return currentScreenDelta;
            }
        }

        /// <summary>
        /// ¿Acaba de tocar la pantalla este fotograma? (Flanco de subida garantizado).
        /// </summary>
        public static bool TouchBegan
        {
            get
            {
                EvaluateState();
                return isBufferedTouching && !wasBufferedTouchingLastFrame;
            }
        }

        /// <summary>
        /// ¿Acaba de levantar el dedo de la pantalla este fotograma? (Flanco de bajada garantizado).
        /// </summary>
        public static bool TouchEnded
        {
            get
            {
                EvaluateState();
                return !isBufferedTouching && wasBufferedTouchingLastFrame;
            }
        }

        /// <summary>
        /// Proyecta la posición del toque táctil a coordenadas de mundo 2D usando una cámara específica.
        /// </summary>
        public static Vector3 GetWorldTouchPosition(Camera cam, float depth = 10f)
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return Vector3.zero;

            Vector2 screenPos = PrimaryTouchPosition;
            return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        }
    }
}
