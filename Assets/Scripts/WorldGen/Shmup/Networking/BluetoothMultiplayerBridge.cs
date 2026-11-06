using System;
using UnityEngine;

namespace WorldGen.Shmup
{
    /// <summary>
    /// Estructura de paquete de datos para la sincronización multijugador por Bluetooth.
    /// Ligera y optimizada para bajo consumo de ancho de banda en Bluetooth SPP / RFCOMM.
    /// </summary>
    [System.Serializable]
    public struct PlaneNetworkPacket
    {
        public int sequenceNumber;
        public float moveX;
        public float moveY;
        public bool isFiring;
        public float posX;
        public float posY;
        public float facingAngle;
        public long timestamp;
    }

    /// <summary>
    /// Puente y receptor de conexión multijugador para Bluetooth / Red.
    /// Conecta los paquetes recibidos por Bluetooth con el avión remoto (Jugador 2),
    /// o empaqueta el input del Jugador 1 para enviarlo al otro móvil.
    /// </summary>
    [DisallowMultipleComponent]
    public class BluetoothMultiplayerBridge : MonoBehaviour, IPlaneInputProvider
    {
        [Header("📡 Estado de Conexión Bluetooth")]
        [Tooltip("Indica si este cliente está conectado como Servidor (Host) o Cliente.")]
        public bool isHost = true;

        [Tooltip("Indica si la conexión Bluetooth está activa.")]
        public bool isConnected = false;

        [Header("🛩️ Avión a Controlar")]
        [Tooltip("Referencia al avión remoto asignado a este receptor.")]
        public ShmupPlaneController targetPlane;

        // Últimos datos recibidos por Bluetooth
        private Vector2 remoteMovement = Vector2.zero;
        private bool remoteFire = false;
        private Vector2 remoteTargetPosition = Vector2.zero;

        // Eventos para cuando se conecte o reciba paquetes en el futuro
        public event Action<PlaneNetworkPacket> OnPacketReceived;
        public event Action OnBluetoothConnected;
        public event Action OnBluetoothDisconnected;

        private void Awake()
        {
            if (targetPlane != null)
            {
                targetPlane.SetInputProvider(this);
            }
        }

        #region IPlaneInputProvider Implementation
        public Vector2 ReadMovement()
        {
            return remoteMovement;
        }

        public bool ReadFire()
        {
            return remoteFire;
        }
        #endregion

        /// <summary>
        /// Método para inyectar paquetes recibidos desde el adaptador Bluetooth de Android en el futuro.
        /// </summary>
        public void FeedNetworkPacket(PlaneNetworkPacket packet)
        {
            remoteMovement = new Vector2(packet.moveX, packet.moveY);
            remoteFire = packet.isFiring;
            remoteTargetPosition = new Vector2(packet.posX, packet.posY);

            OnPacketReceived?.Invoke(packet);
        }

        /// <summary>
        /// Genera el paquete del avión local listo para serializar y enviar por el socket Bluetooth.
        /// </summary>
        public PlaneNetworkPacket GenerateLocalPacket(ShmupPlaneController localPlane, int seq)
        {
            Vector3 localPos = localPlane.transform.localPosition;
            return new PlaneNetworkPacket
            {
                sequenceNumber = seq,
                moveX = localPlane.CurrentMoveInput.x,
                moveY = localPlane.CurrentMoveInput.y,
                isFiring = localPlane.IsFiring,
                posX = localPos.x,
                posY = localPos.y,
                facingAngle = localPlane.transform.localEulerAngles.z,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        /// <summary>
        /// Simular conexión para pruebas en editor antes de enlazar el hardware Bluetooth.
        /// </summary>
        public void SetConnected(bool connected)
        {
            isConnected = connected;
            if (connected) OnBluetoothConnected?.Invoke();
            else OnBluetoothDisconnected?.Invoke();
        }
    }
}
