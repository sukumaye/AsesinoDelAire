namespace WorldGen.Core
{
    public enum CellType
    {
        Empty,
        Water,
        Grass,
        Path,
        Structure,
        Bridge
    }

    public enum TerrainStyle
    {
        Archipelago = 0,          // Archipiélago (Islas de varios tamaños, bahías, cabos, estrechos navegables e islotes)
        Continent = 1,            // Continente (Gran masa terrestre con golfos marítimos, penínsulas y lagunas)
        CoastalIslands = 2,       // Costa e Islas (Continente con archipiélago exterior)
        LakesAndRivers = 3,       // Lagos y Ríos (Tierra firme con lagunas interiores y meandros)
        FourContinents = 4,       // 4 Continentes / 4 Secciones (Mundo dividido en 4 cuadrantes continentales separados por estrechos)
        ContinentWithLakes = 1,   // Alias retrocompatible
        MixedIslandsAndLakes = 2  // Alias retrocompatible
    }

    public enum ChunkStreamingMode
    {
        StreamingRadius = 0,    // Carga dinámica por radio de chunks alrededor del jugador / cámara
        ContinentSection = 1,   // Carga por sección de continente (Muestra 1 de las 4 secciones/continentes a la vez)
        SingleScreenChunk = 2,  // Carga únicamente el chunk/pantalla actual donde está el jugador
        AllVisible = 3          // Modo clásico: todos los chunks visibles a la vez (debug)
    }

    public enum ContinentZone
    {
        NorthWest = 0,  // Continente 1: Noroeste (Superior Izquierda)
        NorthEast = 1,  // Continente 2: Noreste (Superior Derecha)
        SouthWest = 2,  // Continente 3: Suroeste (Inferior Izquierda)
        SouthEast = 3   // Continente 4: Sureste (Inferior Derecha)
    }

    public enum SocketType
    {
        Grass,              // Pasto limpio
        Path_Dirt,          // Camino de tierra
        Path_Stone,         // Camino de adoquines/piedra
        Water_Lake,         // Agua profunda
        Water_Shore,        // Orilla de agua
        Cliff_Rock,         // Borde de roca/acantilado
        Internal_Structure  // Costura interna de estructuras (cráter, pozos, etc.)
    }
}
