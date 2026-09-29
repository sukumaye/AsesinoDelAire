using UnityEngine;

namespace WorldGen.Shmup
{
    /// <summary>
    /// Proyectil ligero para el modo Shoot 'em Up.
    /// Avanza en la dirección indicada y se destruye tras un tiempo de vida.
    /// </summary>
    public class ShmupBullet : MonoBehaviour
    {
        [Tooltip("Velocidad de desplazamiento del proyectil.")]
        public float speed = 22f;

        [Tooltip("Dirección del proyectil.")]
        public Vector2 direction = Vector2.up;

        [Tooltip("Tiempo de vida antes de autodestruirse.")]
        public float lifetime = 2.5f;

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.position += (Vector3)(direction * (speed * Time.deltaTime));
        }
    }
}
