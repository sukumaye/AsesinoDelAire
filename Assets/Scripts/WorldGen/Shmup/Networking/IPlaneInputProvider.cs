using UnityEngine;

namespace WorldGen.Shmup
{
    /// <summary>
    /// Interfaz desacoplada para proveer controles al avión.
    /// Permite conectar indistintamente:
    /// - Controles Locales (Teclado, Mando, Pantalla Táctil / Unity Remote)
    /// - Controles Remotos por Bluetooth / Wi-Fi / Red (Multijugador)
    /// - IA o Bots de prueba
    /// </summary>
    public interface IPlaneInputProvider
    {
        /// <summary>
        /// Retorna el vector de movimiento normalizado (-1 a 1 en cada eje).
        /// </summary>
        Vector2 ReadMovement();

        /// <summary>
        /// Retorna si el botón o acción de disparo está activo.
        /// </summary>
        bool ReadFire();
    }
}
