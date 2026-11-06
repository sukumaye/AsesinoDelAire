#if !DOTWEEN
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DG.Tweening
{
    public enum Ease
    {
        Linear,
        InSine,
        OutSine,
        InOutSine,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        InQuart,
        OutQuart,
        InOutQuart,
        InQuint,
        OutQuint,
        InOutQuint,
        InExpo,
        OutExpo,
        InOutExpo,
        InBack,
        OutBack,
        InOutBack,
        OutBounce
    }

    public enum RotateMode
    {
        Fast,
        FastBeyond360,
        WorldAxisAdd,
        LocalAxisAdd
    }

    public delegate void TweenCallback();

    public abstract class Tween
    {
        internal object target;
        internal float duration;
        internal bool isIndependentUpdate = true;
        internal bool isKilled = false;
        internal bool isPlaying = false;
        internal TweenCallback onComplete;
        internal TweenCallback onKill;

        public bool IsActive() => !isKilled;
        public bool IsPlaying() => isPlaying && !isKilled;

        public virtual void Kill(bool complete = false)
        {
            if (isKilled) return;
            if (complete)
            {
                ForceComplete();
            }
            isKilled = true;
            isPlaying = false;
            onKill?.Invoke();
            DOTweenRuntimeRunner.Unregister(this);
        }

        internal abstract void StartTween();
        internal abstract bool Step(float dt);
        internal abstract void ForceComplete();
    }

    public class Tweener : Tween
    {
        internal Action onStartCapture;
        internal Action<float> onUpdateNormalized;
        internal Ease easeType = Ease.OutQuad;
        internal AnimationCurve customCurve;
        internal float overshootOrAmplitude = 1.70158f;
        private float _elapsed = 0f;
        private bool _started = false;

        internal override void StartTween()
        {
            if (isKilled) return;
            _elapsed = 0f;
            _started = true;
            isPlaying = true;
            onStartCapture?.Invoke();
            onUpdateNormalized?.Invoke(EvaluateEase(0f));
        }

        internal override bool Step(float dt)
        {
            if (isKilled) return true;
            if (!_started) StartTween();

            if (duration <= 0.0001f)
            {
                onUpdateNormalized?.Invoke(EvaluateEase(1f));
                isPlaying = false;
                onComplete?.Invoke();
                return true;
            }

            _elapsed += dt;
            float t = Mathf.Clamp01(_elapsed / duration);
            float eased = EvaluateEase(t);
            onUpdateNormalized?.Invoke(eased);

            if (_elapsed >= duration)
            {
                isPlaying = false;
                onComplete?.Invoke();
                return true;
            }
            return false;
        }

        internal override void ForceComplete()
        {
            if (isKilled) return;
            if (!_started) onStartCapture?.Invoke();
            onUpdateNormalized?.Invoke(EvaluateEase(1f));
            isPlaying = false;
            onComplete?.Invoke();
        }

        private float EvaluateEase(float t)
        {
            if (customCurve != null && customCurve.length > 0)
            {
                return customCurve.Evaluate(t);
            }
            return DOTweenEaseMath.Evaluate(easeType, t, overshootOrAmplitude);
        }
    }

    public class Sequence : Tween
    {
        private class SequenceStepItem
        {
            public float startTime;
            public float duration;
            public Tween tween;
            public TweenCallback callback;
            public bool started;
            public bool completed;
        }

        private readonly List<SequenceStepItem> _items = new List<SequenceStepItem>();
        private float _totalDuration = 0f;
        private float _lastAppendStartTime = 0f;
        private float _elapsed = 0f;
        private bool _started = false;

        internal Sequence()
        {
            DOTweenRuntimeRunner.Register(this);
        }

        internal Sequence AppendInternal(Tween t)
        {
            if (t == null) return this;
            DOTweenRuntimeRunner.Unregister(t); // Managed by this Sequence
            float start = _totalDuration;
            _lastAppendStartTime = start;
            _items.Add(new SequenceStepItem
            {
                startTime = start,
                duration = Mathf.Max(0f, t.duration),
                tween = t
            });
            _totalDuration = Mathf.Max(_totalDuration, start + Mathf.Max(0f, t.duration));
            duration = _totalDuration;
            return this;
        }

        internal Sequence JoinInternal(Tween t)
        {
            if (t == null) return this;
            DOTweenRuntimeRunner.Unregister(t); // Managed by this Sequence
            float start = _lastAppendStartTime;
            _items.Add(new SequenceStepItem
            {
                startTime = start,
                duration = Mathf.Max(0f, t.duration),
                tween = t
            });
            _totalDuration = Mathf.Max(_totalDuration, start + Mathf.Max(0f, t.duration));
            duration = _totalDuration;
            return this;
        }

        internal Sequence AppendIntervalInternal(float interval)
        {
            float d = Mathf.Max(0f, interval);
            _lastAppendStartTime = _totalDuration;
            _totalDuration += d;
            duration = _totalDuration;
            return this;
        }

        internal Sequence AppendCallbackInternal(TweenCallback cb)
        {
            if (cb == null) return this;
            _items.Add(new SequenceStepItem
            {
                startTime = _totalDuration,
                duration = 0f,
                callback = cb
            });
            return this;
        }

        internal Sequence PrependIntervalInternal(float interval)
        {
            float d = Mathf.Max(0f, interval);
            foreach (var item in _items)
            {
                item.startTime += d;
            }
            _lastAppendStartTime += d;
            _totalDuration += d;
            duration = _totalDuration;
            return this;
        }

        internal override void StartTween()
        {
            if (isKilled) return;
            _elapsed = 0f;
            _started = true;
            isPlaying = true;
            foreach (var item in _items)
            {
                item.started = false;
                item.completed = false;
            }
        }

        internal override bool Step(float dt)
        {
            if (isKilled) return true;
            if (!_started) StartTween();

            float prevElapsed = _elapsed;
            _elapsed += dt;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item.completed) continue;

                if (_elapsed >= item.startTime)
                {
                    if (!item.started)
                    {
                        item.started = true;
                        if (item.callback != null)
                        {
                            item.callback.Invoke();
                            item.completed = true;
                            continue;
                        }
                        if (item.tween != null)
                        {
                            item.tween.StartTween();
                        }
                    }

                    if (item.tween != null)
                    {
                        float stepDt = _elapsed - Mathf.Max(prevElapsed, item.startTime);
                        if (item.tween.Step(Mathf.Max(0f, stepDt)))
                        {
                            item.completed = true;
                        }
                    }
                    else
                    {
                        item.completed = true;
                    }
                }
            }

            bool allDone = _elapsed >= _totalDuration;
            if (allDone)
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    var item = _items[i];
                    if (!item.completed)
                    {
                        if (!item.started && item.callback != null) item.callback.Invoke();
                        item.tween?.ForceComplete();
                        item.completed = true;
                    }
                }
                isPlaying = false;
                onComplete?.Invoke();
                return true;
            }

            return false;
        }

        internal override void ForceComplete()
        {
            if (isKilled) return;
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (!item.completed)
                {
                    if (item.callback != null) item.callback.Invoke();
                    item.tween?.ForceComplete();
                    item.completed = true;
                }
            }
            isPlaying = false;
            onComplete?.Invoke();
        }

        public override void Kill(bool complete = false)
        {
            if (isKilled) return;
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].tween?.Kill(complete);
            }
            base.Kill(complete);
        }
    }

    public static class DOTween
    {
        public static Sequence Sequence()
        {
            DOTweenRuntimeRunner.EnsureInstance();
            return new Sequence();
        }

        public static void Kill(object targetOrId, bool complete = false)
        {
            DOTweenRuntimeRunner.KillTarget(targetOrId, complete);
        }

        public static void KillAll(bool complete = false)
        {
            DOTweenRuntimeRunner.KillAll(complete);
        }
    }

    public static class ShortcutExtensions
    {
        // --- RectTransform Extensions ---
        public static Tweener DOAnchorPos(this RectTransform target, Vector2 endValue, float duration, bool snapping = false)
        {
            DOTweenRuntimeRunner.EnsureInstance();
            Vector2 startValue = target != null ? target.anchoredPosition : Vector2.zero;
            var tweener = new Tweener
            {
                target = target,
                duration = duration,
                onStartCapture = () =>
                {
                    if (target != null) startValue = target.anchoredPosition;
                },
                onUpdateNormalized = (t) =>
                {
                    if (target == null) return;
                    Vector2 val = Vector2.LerpUnclamped(startValue, endValue, t);
                    if (snapping)
                    {
                        val.x = Mathf.Round(val.x);
                        val.y = Mathf.Round(val.y);
                    }
                    target.anchoredPosition = val;
                }
            };
            DOTweenRuntimeRunner.Register(tweener);
            return tweener;
        }

        public static Tweener DOAnchorPosX(this RectTransform target, float endValue, float duration, bool snapping = false)
        {
            DOTweenRuntimeRunner.EnsureInstance();
            float startX = target != null ? target.anchoredPosition.x : 0f;
            var tweener = new Tweener
            {
                target = target,
                duration = duration,
                onStartCapture = () =>
                {
                    if (target != null) startX = target.anchoredPosition.x;
                },
                onUpdateNormalized = (t) =>
                {
                    if (target == null) return;
                    Vector2 pos = target.anchoredPosition;
                    pos.x = Mathf.LerpUnclamped(startX, endValue, t);
                    if (snapping) pos.x = Mathf.Round(pos.x);
                    target.anchoredPosition = pos;
                }
            };
            DOTweenRuntimeRunner.Register(tweener);
            return tweener;
        }

        public static Tweener DOAnchorPosY(this RectTransform target, float endValue, float duration, bool snapping = false)
        {
            DOTweenRuntimeRunner.EnsureInstance();
            float startY = target != null ? target.anchoredPosition.y : 0f;
            var tweener = new Tweener
            {
                target = target,
                duration = duration,
                onStartCapture = () =>
                {
                    if (target != null) startY = target.anchoredPosition.y;
                },
                onUpdateNormalized = (t) =>
                {
                    if (target == null) return;
                    Vector2 pos = target.anchoredPosition;
                    pos.y = Mathf.LerpUnclamped(startY, endValue, t);
                    if (snapping) pos.y = Mathf.Round(pos.y);
                    target.anchoredPosition = pos;
                }
            };
            DOTweenRuntimeRunner.Register(tweener);
            return tweener;
        }

        public static Tweener DOPunchAnchorPos(this RectTransform target, Vector2 punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            DOTweenRuntimeRunner.EnsureInstance();
            Vector2 basePos = target != null ? target.anchoredPosition : Vector2.zero;
            var tweener = new Tweener
            {
                target = target,
                duration = duration,
                easeType = Ease.Linear,
                onStartCapture = () =>
                {
                    if (target != null) basePos = target.anchoredPosition;
                },
                onUpdateNormalized = (t) =>
                {
                    if (target == null) return;
                    if (t >= 1f)
                    {
                        target.anchoredPosition = basePos;
                        return;
                    }
                    float damper = 1f - t;
                    float wave = Mathf.Sin(t * Mathf.PI * Mathf.Max(1, vibrato));
                    target.anchoredPosition = basePos + punch * (wave * damper);
                }
            };
            DOTweenRuntimeRunner.Register(tweener);
            return tweener;
        }

        public static Tweener DOShakeAnchorPos(this RectTransform target, float duration, float strength = 15f, int vibrato = 10, float randomness = 90f, bool snapping = false, bool fadeOut = true)
        {
            DOTweenRuntimeRunner.EnsureInstance();
            Vector2 basePos = target != null ? target.anchoredPosition : Vector2.zero;
            var tweener = new Tweener
            {
                target = target,
                duration = duration,
                easeType = Ease.Linear,
                onStartCapture = () =>
                {
                    if (target != null) basePos = target.anchoredPosition;
                },
                onUpdateNormalized = (t) =>
                {
                    if (target == null) return;
                    if (t >= 1f)
                    {
                        target.anchoredPosition = basePos;
                        return;
                    }
                    float damper = fadeOut ? (1f - t) : 1f;
                    float angle = t * Mathf.PI * 2f * Mathf.Max(1, vibrato);
                    Vector2 offset = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle * 1.3f)) * (strength * damper);
                    if (snapping)
                    {
                        offset.x = Mathf.Round(offset.x);
                        offset.y = Mathf.Round(offset.y);
                    }
                    target.anchoredPosition = basePos + offset;
                }
            };
            DOTweenRuntimeRunner.Register(tweener);
            return tweener;
        }

        // --- Transform Extensions ---
        public static Tweener DOScale(this Transform target, Vector3 endValue, float duration)
        {
            DOTweenRuntimeRunner.EnsureInstance();
            Vector3 startValue = target != null ? target.localScale : Vector3.one;
            var tweener = new Tweener
            {
                target = target,
                duration = duration,
                onStartCapture = () =>
                {
                    if (target != null) startValue = target.localScale;
                },
                onUpdateNormalized = (t) =>
                {
                    if (target == null) return;
                    target.localScale = Vector3.LerpUnclamped(startValue, endValue, t);
                }
            };
            DOTweenRuntimeRunner.Register(tweener);
            return tweener;
        }

        public static Tweener DOScale(this Transform target, float endValue, float duration)
        {
            return target.DOScale(new Vector3(endValue, endValue, endValue), duration);
        }

        public static Tweener DORotate(this Transform target, Vector3 endValue, float duration, RotateMode mode = RotateMode.Fast)
        {
            DOTweenRuntimeRunner.EnsureInstance();
            Vector3 startEuler = target != null ? target.localEulerAngles : Vector3.zero;
            var tweener = new Tweener
            {
                target = target,
                duration = duration,
                onStartCapture = () =>
                {
                    if (target != null) startEuler = target.localEulerAngles;
                },
                onUpdateNormalized = (t) =>
                {
                    if (target == null) return;
                    if (mode == RotateMode.FastBeyond360 || mode == RotateMode.LocalAxisAdd)
                    {
                        Vector3 current = (mode == RotateMode.LocalAxisAdd)
                            ? startEuler + endValue * t
                            : Vector3.LerpUnclamped(startEuler, endValue, t);
                        target.localRotation = Quaternion.Euler(current);
                    }
                    else
                    {
                        float x = Mathf.LerpAngle(startEuler.x, endValue.x, t);
                        float y = Mathf.LerpAngle(startEuler.y, endValue.y, t);
                        float z = Mathf.LerpAngle(startEuler.z, endValue.z, t);
                        target.localRotation = Quaternion.Euler(x, y, z);
                    }
                }
            };
            DOTweenRuntimeRunner.Register(tweener);
            return tweener;
        }

        public static Tweener DOPunchScale(this Transform target, Vector3 punch, float duration, int vibrato = 10, float elasticity = 1f)
        {
            DOTweenRuntimeRunner.EnsureInstance();
            Vector3 baseScale = target != null ? target.localScale : Vector3.one;
            var tweener = new Tweener
            {
                target = target,
                duration = duration,
                easeType = Ease.Linear,
                onStartCapture = () =>
                {
                    if (target != null) baseScale = target.localScale;
                },
                onUpdateNormalized = (t) =>
                {
                    if (target == null) return;
                    if (t >= 1f)
                    {
                        target.localScale = baseScale;
                        return;
                    }
                    float damper = 1f - t;
                    float wave = Mathf.Sin(t * Mathf.PI * Mathf.Max(1, vibrato));
                    target.localScale = baseScale + punch * (wave * damper);
                }
            };
            DOTweenRuntimeRunner.Register(tweener);
            return tweener;
        }

        // --- Graphic & CanvasGroup Fade Extensions ---
        public static Tweener DOFade(this Graphic target, float endValue, float duration)
        {
            DOTweenRuntimeRunner.EnsureInstance();
            float startAlpha = target != null ? target.color.a : 1f;
            var tweener = new Tweener
            {
                target = target,
                duration = duration,
                onStartCapture = () =>
                {
                    if (target != null) startAlpha = target.color.a;
                },
                onUpdateNormalized = (t) =>
                {
                    if (target == null) return;
                    Color c = target.color;
                    c.a = Mathf.LerpUnclamped(startAlpha, endValue, t);
                    target.color = c;
                }
            };
            DOTweenRuntimeRunner.Register(tweener);
            return tweener;
        }

        public static Tweener DOFade(this CanvasGroup target, float endValue, float duration)
        {
            DOTweenRuntimeRunner.EnsureInstance();
            float startAlpha = target != null ? target.alpha : 1f;
            var tweener = new Tweener
            {
                target = target,
                duration = duration,
                onStartCapture = () =>
                {
                    if (target != null) startAlpha = target.alpha;
                },
                onUpdateNormalized = (t) =>
                {
                    if (target == null) return;
                    target.alpha = Mathf.LerpUnclamped(startAlpha, endValue, t);
                }
            };
            DOTweenRuntimeRunner.Register(tweener);
            return tweener;
        }

        // --- Sequence & Tween Settings Extensions ---
        public static Sequence Append(this Sequence s, Tween t) => s != null ? s.AppendInternal(t) : null;
        public static Sequence Join(this Sequence s, Tween t) => s != null ? s.JoinInternal(t) : null;
        public static Sequence AppendInterval(this Sequence s, float interval) => s != null ? s.AppendIntervalInternal(interval) : null;
        public static Sequence AppendCallback(this Sequence s, TweenCallback callback) => s != null ? s.AppendCallbackInternal(callback) : null;
        public static Sequence PrependInterval(this Sequence s, float interval) => s != null ? s.PrependIntervalInternal(interval) : null;

        public static T SetEase<T>(this T t, Ease ease, float overshootOrAmplitude = 1.70158f) where T : Tween
        {
            if (t is Tweener tw)
            {
                tw.easeType = ease;
                tw.overshootOrAmplitude = overshootOrAmplitude;
            }
            return t;
        }

        public static T SetEase<T>(this T t, AnimationCurve animCurve) where T : Tween
        {
            if (t is Tweener tw && animCurve != null && animCurve.length > 0)
            {
                tw.customCurve = animCurve;
            }
            return t;
        }

        public static T SetUpdate<T>(this T t, bool isIndependentUpdate) where T : Tween
        {
            if (t != null) t.isIndependentUpdate = isIndependentUpdate;
            return t;
        }

        public static T SetTarget<T>(this T t, object target) where T : Tween
        {
            if (t != null) t.target = target;
            return t;
        }

        public static T OnComplete<T>(this T t, TweenCallback action) where T : Tween
        {
            if (t != null) t.onComplete = action;
            return t;
        }

        public static T OnKill<T>(this T t, TweenCallback action) where T : Tween
        {
            if (t != null) t.onKill = action;
            return t;
        }
    }

    internal static class DOTweenEaseMath
    {
        public static float Evaluate(Ease ease, float t, float overshoot)
        {
            switch (ease)
            {
                case Ease.Linear:
                    return t;
                case Ease.InSine:
                    return 1f - Mathf.Cos((t * Mathf.PI) * 0.5f);
                case Ease.OutSine:
                    return Mathf.Sin((t * Mathf.PI) * 0.5f);
                case Ease.InOutSine:
                    return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
                case Ease.InQuad:
                    return t * t;
                case Ease.OutQuad:
                    return 1f - (1f - t) * (1f - t);
                case Ease.InOutQuad:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                case Ease.InCubic:
                    return t * t * t;
                case Ease.OutCubic:
                    return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.InOutCubic:
                    return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
                case Ease.InQuart:
                    return t * t * t * t;
                case Ease.OutQuart:
                    return 1f - Mathf.Pow(1f - t, 4f);
                case Ease.InOutQuart:
                    return t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) * 0.5f;
                case Ease.InQuint:
                    return t * t * t * t * t;
                case Ease.OutQuint:
                    return 1f - Mathf.Pow(1f - t, 5f);
                case Ease.InOutQuint:
                    return t < 0.5f ? 16f * t * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 5f) * 0.5f;
                case Ease.InExpo:
                    return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
                case Ease.OutExpo:
                    return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                case Ease.InOutExpo:
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    return t < 0.5f
                        ? Mathf.Pow(2f, 20f * t - 10f) * 0.5f
                        : (2f - Mathf.Pow(2f, -20f * t + 10f)) * 0.5f;
                case Ease.InBack:
                {
                    float c1 = overshoot > 0.01f ? overshoot : 1.70158f;
                    float c3 = c1 + 1f;
                    return c3 * t * t * t - c1 * t * t;
                }
                case Ease.OutBack:
                {
                    float c1 = overshoot > 0.01f ? overshoot : 1.70158f;
                    float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
                case Ease.InOutBack:
                {
                    float c1 = overshoot > 0.01f ? overshoot : 1.70158f;
                    float c2 = c1 * 1.525f;
                    return t < 0.5f
                        ? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) * 0.5f
                        : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) * 0.5f;
                }
                case Ease.OutBounce:
                {
                    const float n1 = 7.5625f;
                    const float d1 = 2.75f;
                    if (t < 1f / d1) return n1 * t * t;
                    if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
                    if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
                    t -= 2.625f / d1;
                    return n1 * t * t + 0.984375f;
                }
                default:
                    return t;
            }
        }
    }

    internal class DOTweenRuntimeRunner : MonoBehaviour
    {
        private static DOTweenRuntimeRunner _instance;
        private static readonly List<Tween> _activeTweens = new List<Tween>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticStateOnPlayMode()
        {
            _activeTweens.Clear();
            _instance = null;
        }

        internal static void EnsureInstance()
        {
            if (_instance != null) return;
            var existing = FindAnyObjectByType<DOTweenRuntimeRunner>();
            if (existing != null)
            {
                _instance = existing;
                return;
            }
            var go = new GameObject("[DOTween_RuntimeRunner]");
            go.hideFlags = HideFlags.HideAndDontSave;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(go);
            }
            _instance = go.AddComponent<DOTweenRuntimeRunner>();
        }

        internal static void Register(Tween t)
        {
            if (t == null || _activeTweens.Contains(t)) return;
            _activeTweens.Add(t);
        }

        internal static void Unregister(Tween t)
        {
            if (t == null) return;
            _activeTweens.Remove(t);
        }

        internal static void KillTarget(object target, bool complete)
        {
            if (target == null) return;
            for (int i = _activeTweens.Count - 1; i >= 0; i--)
            {
                var tw = _activeTweens[i];
                if (tw != null && ReferenceEquals(tw.target, target))
                {
                    tw.Kill(complete);
                }
            }
        }

        internal static void KillAll(bool complete)
        {
            for (int i = _activeTweens.Count - 1; i >= 0; i--)
            {
                var tw = _activeTweens[i];
                tw?.Kill(complete);
            }
            _activeTweens.Clear();
        }

        private void Update()
        {
            if (_activeTweens.Count == 0) return;

            for (int i = _activeTweens.Count - 1; i >= 0; i--)
            {
                if (i >= _activeTweens.Count) continue;
                var tw = _activeTweens[i];
                if (tw == null || tw.isKilled)
                {
                    _activeTweens.RemoveAt(i);
                    continue;
                }

                float dt = tw.isIndependentUpdate ? Time.unscaledDeltaTime : Time.deltaTime;
                if (tw.Step(dt))
                {
                    _activeTweens.Remove(tw);
                }
            }
        }
    }
}
#endif
