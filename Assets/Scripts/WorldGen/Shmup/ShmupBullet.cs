using UnityEngine;

namespace WorldGen.Shmup
{
    /// <summary>
    /// Proyectil balístico cinético de alta velocidad para el modo arcade Shoot 'em Up.
    /// Se desplaza en línea recta según el vector de dirección del cañón,
    /// colisiona con naves y defensas enemigas y se autodestruye al agotar su tiempo de vida.
    /// </summary>
    public class ShmupBullet : MonoBehaviour
    {
        [Header("Balística")]
        [Tooltip("Velocidad lineal de desplazamiento del proyectil en unidades por segundo.")]
        public float speed = 22f;

        [Tooltip("Vector de dirección normalizado en el que viaja el proyectil (por defecto hacia arriba).")]
        public Vector2 direction = Vector2.up;

        [Header("Ciclo de Vida")]
        [Tooltip("Tiempo de persistencia en segundos antes de que el proyectil sea reciclado/destruido.")]
        public float lifetime = 2.5f;

        [Tooltip("Daño infligido al colisionar con blindaje enemigo.")]
        public float damage = 10f;

        private void Start()
        {
            // Programar autodestrucción segura para evitar fugas de memoria por proyectiles perdidos fuera de pantalla
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            // Movimiento balístico continuo independiente de la tasa de cuadros (framerate)
            transform.position += (Vector3)(direction * (speed * Time.deltaTime));
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Detección de impacto contra defensas o jefes mecánicos
            if (collision.CompareTag("Enemy") || collision.CompareTag("Boss"))
            {
                // Emitir sonido procedural de impacto balístico
                WorldGen.Flow.ProceduralAudio.PlayBlinkHeavy();
                Destroy(gameObject);
            }
        }
    }
}
