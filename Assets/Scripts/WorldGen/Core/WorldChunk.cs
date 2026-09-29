using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace WorldGen.Core
{
    [System.Serializable]
    public struct ChunkTileRecord
    {
        public Vector3Int position;
        public TileBase tile;
    }

    /// <summary>
    /// Representa un Chunk del mapa en el sistema de Chunk Map.
    /// Almacena en caché los tiles de Suelo, Caminos y Estructuras.
    /// Permite que los chunks aparezcan (Load) y desaparezcan (Unload) dinámicamente
    /// según la posición del jugador/cámara o la sección continental activa.
    /// </summary>
    [System.Serializable]
    public class WorldChunk
    {
        public Vector2Int chunkCoord;       // Coordenadas en la cuadrícula de chunks (ej: [0,0], [1,0]...)
        public RectInt worldBounds;         // Límites del chunk en coordenadas de tiles del mundo
        public int continentId;             // 0: Noroeste, 1: Noreste, 2: Suroeste, 3: Sureste
        public ContinentZone continentZone;
        public string continentName;
        public bool isLoaded;

        // Registros de tiles cacheados
        public List<ChunkTileRecord> groundTiles = new List<ChunkTileRecord>();
        public List<ChunkTileRecord> pathTiles = new List<ChunkTileRecord>();
        public List<ChunkTileRecord> structureTiles = new List<ChunkTileRecord>();

        // Arrays pre-optimizados para llamadas nativas SetTiles de Unity (0 garbage, ultrarrápido)
        private Vector3Int[] groundPositions;
        private TileBase[] groundTileBases;
        private TileBase[] groundNulls;

        private Vector3Int[] pathPositions;
        private TileBase[] pathTileBases;
        private TileBase[] pathNulls;

        private Vector3Int[] structPositions;
        private TileBase[] structTileBases;
        private TileBase[] structNulls;

        public WorldChunk(Vector2Int coord, RectInt bounds, int continent, string name)
        {
            chunkCoord = coord;
            worldBounds = bounds;
            continentId = continent;
            continentZone = (ContinentZone)Mathf.Clamp(continent, 0, 3);
            continentName = name;
            isLoaded = false;
        }

        public void Clear()
        {
            groundTiles.Clear();
            pathTiles.Clear();
            structureTiles.Clear();
            groundPositions = null;
            pathPositions = null;
            structPositions = null;
            isLoaded = false;
        }

        public void RecordGround(Vector3Int pos, TileBase tile)
        {
            if (tile != null)
            {
                groundTiles.Add(new ChunkTileRecord { position = pos, tile = tile });
            }
        }

        public void RecordPath(Vector3Int pos, TileBase tile)
        {
            if (tile != null)
            {
                pathTiles.Add(new ChunkTileRecord { position = pos, tile = tile });
            }
        }

        public void RecordStructure(Vector3Int pos, TileBase tile)
        {
            if (tile != null)
            {
                structureTiles.Add(new ChunkTileRecord { position = pos, tile = tile });
            }
        }

        /// <summary>
        /// Prepara los arrays para ejecución nativa en bloque de Tilemap.SetTiles
        /// </summary>
        public void PrepareCache()
        {
            // Ground
            int gCount = groundTiles.Count;
            groundPositions = new Vector3Int[gCount];
            groundTileBases = new TileBase[gCount];
            groundNulls = new TileBase[gCount];
            for (int i = 0; i < gCount; i++)
            {
                groundPositions[i] = groundTiles[i].position;
                groundTileBases[i] = groundTiles[i].tile;
                groundNulls[i] = null;
            }

            // Path
            int pCount = pathTiles.Count;
            pathPositions = new Vector3Int[pCount];
            pathTileBases = new TileBase[pCount];
            pathNulls = new TileBase[pCount];
            for (int i = 0; i < pCount; i++)
            {
                pathPositions[i] = pathTiles[i].position;
                pathTileBases[i] = pathTiles[i].tile;
                pathNulls[i] = null;
            }

            // Structure
            int sCount = structureTiles.Count;
            structPositions = new Vector3Int[sCount];
            structTileBases = new TileBase[sCount];
            structNulls = new TileBase[sCount];
            for (int i = 0; i < sCount; i++)
            {
                structPositions[i] = structureTiles[i].position;
                structTileBases[i] = structureTiles[i].tile;
                structNulls[i] = null;
            }
        }

        /// <summary>
        /// Hace que el chunk APAREZCA en la pantalla estampando sus tiles en los Tilemaps correspondientes.
        /// </summary>
        public void Load(Tilemap groundMap, Tilemap pathMap, Tilemap structureMap)
        {
            if (isLoaded) return;
            if (groundPositions == null) PrepareCache();

            if (groundMap != null && groundPositions != null && groundPositions.Length > 0)
            {
                groundMap.SetTiles(groundPositions, groundTileBases);
            }

            if (pathMap != null && pathPositions != null && pathPositions.Length > 0)
            {
                pathMap.SetTiles(pathPositions, pathTileBases);
            }

            if (structureMap != null && structPositions != null && structPositions.Length > 0)
            {
                structureMap.SetTiles(structPositions, structTileBases);
            }

            isLoaded = true;
        }

        /// <summary>
        /// Hace que el chunk DESAPAREZCA de la pantalla retirando sus tiles de los Tilemaps sin perder la caché.
        /// </summary>
        public void Unload(Tilemap groundMap, Tilemap pathMap, Tilemap structureMap)
        {
            if (!isLoaded) return;
            if (groundPositions == null) PrepareCache();

            if (groundMap != null && groundPositions != null && groundPositions.Length > 0)
            {
                groundMap.SetTiles(groundPositions, groundNulls);
            }

            if (pathMap != null && pathPositions != null && pathPositions.Length > 0)
            {
                pathMap.SetTiles(pathPositions, pathNulls);
            }

            if (structureMap != null && structPositions != null && structPositions.Length > 0)
            {
                structureMap.SetTiles(structPositions, structNulls);
            }

            isLoaded = false;
        }

        public bool ContainsTile(int x, int y)
        {
            return worldBounds.Contains(new Vector2Int(x, y));
        }

        public Vector2 WorldCenter => new Vector2(worldBounds.x + worldBounds.width * 0.5f, worldBounds.y + worldBounds.height * 0.5f);
    }
}
