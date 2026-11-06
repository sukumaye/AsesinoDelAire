using UnityEngine;

namespace WorldGen.Flow
{
    /// <summary>
    /// Generador de audio procedural y sintetizador DSP en tiempo real para interfaces retro.
    /// Produce clics táctiles, tonos de telemetría de radio, pitidos de radar y zumbidos de motor
    /// sintetizando ondas senoidales con envolvente decreciente directamente en memoria RAM,
    /// eliminando la necesidad de importar archivos de audio pesados para efectos de interfaz.
    /// </summary>
    public static class ProceduralAudio
    {
        // Instancia singleton persistente del reproductor de audio entre cambios de escena
        private static AudioSource _audioSource;

        /// <summary>
        /// Garantiza la existencia del componente AudioSource en un GameObject inmortal (DontDestroyOnLoad).
        /// </summary>
        private static void EnsureAudioSource()
        {
            if (_audioSource == null)
            {
                var go = new GameObject("[ProceduralAudioSource]");
                GameObject.DontDestroyOnLoad(go);
                _audioSource = go.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }

        /// <summary>
        /// Reproduce un clic mecánico corto (800 Hz, 30 ms) ideal para botones y diales giratorios.
        /// </summary>
        public static void PlayTick()
        {
            EnsureAudioSource();
            AudioClip clip = CreateBlipClip(800, 0.03f);
            _audioSource.PlayOneShot(clip, 0.4f);
        }

        /// <summary>
        /// Reproduce un pitido de confirmación estándar (1350 Hz, 45 ms).
        /// </summary>
        public static void PlayBlink()
        {
            EnsureAudioSource();
            AudioClip clip = CreateBlipClip(1350, 0.045f);
            _audioSource.PlayOneShot(clip, 0.35f);
        }

        /// <summary>
        /// Tono grave de advertencia o selección de peso pesado (260 Hz, 65 ms).
        /// </summary>
        public static void PlayBlinkHeavy()
        {
            EnsureAudioSource();
            AudioClip clip = CreateBlipClip(260, 0.065f);
            _audioSource.PlayOneShot(clip, 0.45f);
        }

        /// <summary>
        /// Tono medio de cambio de selección (920 Hz, 40 ms).
        /// </summary>
        public static void PlayBlinkMid()
        {
            EnsureAudioSource();
            AudioClip clip = CreateBlipClip(920, 0.04f);
            _audioSource.PlayOneShot(clip, 0.4f);
        }

        /// <summary>
        /// Tono agudo de contacto ligero (1500 Hz, 25 ms).
        /// </summary>
        public static void PlayBlinkLight()
        {
            EnsureAudioSource();
            AudioClip clip = CreateBlipClip(1500, 0.025f);
            _audioSource.PlayOneShot(clip, 0.35f);
        }

        /// <summary>
        /// Tono armónico resonante para confirmar despliegues de misiones o compras de equipo (1200 Hz, 80 ms).
        /// </summary>
        public static void PlayConfirm()
        {
            EnsureAudioSource();
            AudioClip clip = CreateBlipClip(1200, 0.08f);
            _audioSource.PlayOneShot(clip, 0.6f);
        }

        /// <summary>
        /// Zumbido grave y continuo que emula el arranque de un motor de aviación a pistón (140 Hz, 250 ms).
        /// </summary>
        public static void PlayEngineHum()
        {
            EnsureAudioSource();
            AudioClip clip = CreateBlipClip(140, 0.25f);
            _audioSource.PlayOneShot(clip, 0.35f);
        }

        /// <summary>
        /// Genera matemáticamente un AudioClip mono a 44.1 kHz mediante síntesis aditiva senoidal
        /// con una envolvente lineal de atenuación (Decay) para evitar chasquidos (clicks/pops) de fase.
        /// </summary>
        /// <param name="frequency">Frecuencia fundamental en Hertz (Hz).</param>
        /// <param name="duration">Duración del sonido en segundos.</param>
        /// <returns>AudioClip generado listo para ser emitido por AudioSource.</returns>
        private static AudioClip CreateBlipClip(int frequency, float duration)
        {
            int sampleRate = 44100;
            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - ((float)i / samplesCount); // Caída lineal de amplitud
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope;
            }

            AudioClip clip = AudioClip.Create("Blip_" + frequency, samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
