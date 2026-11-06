using UnityEngine;

namespace WorldGen.Core
{
    /// <summary>
    /// Puente estático de memoria volátil entre el Menú Principal (Terminal Radar CRT de Sixela) 
    /// y la escena de combate real (gameplay / MapGenerator).
    /// Empaqueta la semilla matemática, el estilo topográfico, el parte de inteligencia bélica
    /// y las coordenadas de despliegue para que el generador procedural reconstruya con fidelidad matemática
    /// el mapa exacto que el jugador previsualizó en los diales de su radio.
    /// </summary>
    public static class MissionDeploymentData
    {
        /// <summary>Bandera que indica si hay un despliegue configurado a la espera de ser procesado por gameplay.</summary>
        public static bool hasCustomDeployment = false;

        // Metadatos narrativos y teológicos del teatro de operaciones
        public static string planetName = "Sixela";
        public static string formationName = "Archipiélago Central";
        public static string factionName = "Herejes del Océano Abisal";
        public static string biomeName = "Océanos Tempestuosos y Archipiélagos Fractales";
        public static string weatherEvent = "Tifón Cat. 5 y Oleaje de 40m";
        public static string situationReport = "Guerra teológica activa sin deidad suprema. Combate aéreo sobre la anomalía insular recursiva.";

        // Parámetros matemáticos de generación procedural del terreno
        public static int selectedSeed = 407949;
        public static TerrainStyle selectedTerrainStyle = TerrainStyle.Archipelago;
        public static int selectedWidth = 100;
        public static int selectedHeight = 300;
        public static Vector2Int selectedChunkSize = new Vector2Int(100, 100);

        // Datos tácticos seleccionados mediante los diales de radio
        public static string selectedSectorName = "ARCHIPIÉLAGO CENTRAL";
        public static float selectedFrequency = 95.0f;
        public static int selectedQuarter = 0;
        public static string selectedObjective = "VANGUARDIA AÉREA";

        /// <summary>
        /// Asigna y bloquea los parámetros de una nueva misión para su transferencia a la escena de combate.
        /// </summary>
        public static void SetDeployment(
            int seed, 
            TerrainStyle style, 
            int w, 
            int h, 
            Vector2Int chunkSize, 
            string sector, 
            float freq, 
            int quarter, 
            string obj,
            string pPlanet = "Sixela",
            string pFormation = "Archipiélago",
            string pFaction = "Herejes del Océano Abisal",
            string pBiome = "Océanos Tempestuosos",
            string pWeather = "Tifón Cat. 5",
            string pReport = "Misión activa.")
        {
            hasCustomDeployment = true;
            selectedSeed = seed;
            selectedTerrainStyle = style;
            selectedWidth = w;
            selectedHeight = h;
            selectedChunkSize = chunkSize;
            selectedSectorName = sector;
            selectedFrequency = freq;
            selectedQuarter = quarter;
            selectedObjective = obj;

            planetName = pPlanet;
            formationName = pFormation;
            factionName = pFaction;
            biomeName = pBiome;
            weatherEvent = pWeather;
            situationReport = pReport;
        }

        /// <summary>
        /// Restablece los datos de despliegue tras completar la carga en el nivel jugable.
        /// </summary>
        public static void Reset()
        {
            hasCustomDeployment = false;
        }
    }
}
