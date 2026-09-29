using UnityEngine;
using UnityEngine.Tilemaps;
using WorldGen.Core;
using WorldGen.Data;

namespace WorldGen.Generators
{
    public static class StructurePlacer
    {
        public static bool TryPlaceStructureInZone(StructureData data, CellType[,] grid, int width, int height, int minY, int maxY, out Vector2Int placedOrigin, int maxAttempts = 300)
        {
            placedOrigin = Vector2Int.zero;
            int clampedMinY = Mathf.Clamp(minY, 2, Mathf.Max(2, height - data.size.y - 2));
            int clampedMaxY = Mathf.Clamp(maxY, clampedMinY, Mathf.Max(clampedMinY, height - data.size.y - 2));

            // Intentar primero dentro de la franja vertical deseada
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int rx = Random.Range(2, Mathf.Max(3, width - data.size.x - 2));
                int ry = Random.Range(clampedMinY, clampedMaxY + 1);

                if (CanPlaceAt(data, rx, ry, grid, width, height))
                {
                    placedOrigin = new Vector2Int(rx, ry);
                    MarkStructureCells(data, rx, ry, grid);
                    return true;
                }
            }

            // Fallback a cualquier parte del mapa si la franja no tiene pasto suficiente
            return TryPlaceStructure(data, grid, width, height, out placedOrigin, maxAttempts);
        }

        public static bool TryPlaceStructureInIsland(StructureData data, CellType[,] grid, int width, int height, IslandData island, out Vector2Int placedOrigin, int maxAttempts = 200)
        {
            placedOrigin = Vector2Int.zero;
            if (island == null || island.tiles == null || island.tiles.Count == 0)
                return TryPlaceStructure(data, grid, width, height, out placedOrigin, maxAttempts);

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var seedTile = island.tiles[Random.Range(0, island.tiles.Count)];
                int rx = seedTile.x - Random.Range(0, data.size.x);
                int ry = seedTile.y - Random.Range(0, data.size.y);

                if (rx >= 1 && rx + data.size.x < width - 1 && ry >= 1 && ry + data.size.y < height - 1)
                {
                    if (CanPlaceAt(data, rx, ry, grid, width, height))
                    {
                        placedOrigin = new Vector2Int(rx, ry);
                        MarkStructureCells(data, rx, ry, grid);
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryPlaceStructureInQuadrant(StructureData data, CellType[,] grid, int width, int height, int quadrantIndex, out Vector2Int placedOrigin, int maxAttempts = 300)
        {
            placedOrigin = Vector2Int.zero;
            int midX = width / 2;
            int midY = height / 2;

            int minX, maxX, minY, maxY;
            switch (quadrantIndex)
            {
                case 0: // Noroeste (NW)
                    minX = 2;
                    maxX = Mathf.Max(2, midX - data.size.x - 2);
                    minY = midY + 2;
                    maxY = Mathf.Max(minY, height - data.size.y - 2);
                    break;
                case 1: // Noreste (NE)
                    minX = midX + 2;
                    maxX = Mathf.Max(minX, width - data.size.x - 2);
                    minY = midY + 2;
                    maxY = Mathf.Max(minY, height - data.size.y - 2);
                    break;
                case 2: // Suroeste (SW)
                    minX = 2;
                    maxX = Mathf.Max(2, midX - data.size.x - 2);
                    minY = 2;
                    maxY = Mathf.Max(2, midY - data.size.y - 2);
                    break;
                case 3: // Sureste (SE)
                    minX = midX + 2;
                    maxX = Mathf.Max(minX, width - data.size.x - 2);
                    minY = 2;
                    maxY = Mathf.Max(2, midY - data.size.y - 2);
                    break;
                default:
                    return TryPlaceStructure(data, grid, width, height, out placedOrigin, maxAttempts);
            }

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int rx = Random.Range(minX, maxX + 1);
                int ry = Random.Range(minY, maxY + 1);

                if (CanPlaceAt(data, rx, ry, grid, width, height))
                {
                    placedOrigin = new Vector2Int(rx, ry);
                    MarkStructureCells(data, rx, ry, grid);
                    return true;
                }
            }

            return false;
        }

        public static bool TryPlaceStructure(StructureData data, CellType[,] grid, int width, int height, out Vector2Int placedOrigin, int maxAttempts = 300)
        {
            placedOrigin = Vector2Int.zero;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int rx = Random.Range(2, width - data.size.x - 2);
                int ry = Random.Range(2, height - data.size.y - 2);

                if (CanPlaceAt(data, rx, ry, grid, width, height))
                {
                    placedOrigin = new Vector2Int(rx, ry);
                    MarkStructureCells(data, rx, ry, grid);
                    return true;
                }
            }
            return false;
        }

        private static void MarkStructureCells(StructureData data, int rx, int ry, CellType[,] grid)
        {
            for (int x = 0; x < data.size.x; x++)
            {
                for (int y = 0; y < data.size.y; y++)
                {
                    TileBase tile = data.GetTile(x, y);
                    if (tile == null)
                        continue;

                    // Si es la casilla de la puerta configurada por el usuario, dejarla transitable
                    if (data.hasDoor && x == data.doorOffset.x && y == data.doorOffset.y)
                        continue;

                    // Si es camino, prado o suelo, transitable
                    string tName = tile.name.ToLower();
                    if (tName.Contains("camino") || tName.Contains("prado") || tName.Contains("path") || tName.Contains("road"))
                        continue;

                    grid[rx + x, ry + y] = CellType.Structure;
                }
            }
        }

        private static bool CanPlaceAt(StructureData data, int originX, int originY, CellType[,] grid, int width, int height)
        {
            for (int x = 0; x < data.size.x; x++)
            {
                for (int y = 0; y < data.size.y; y++)
                {
                    int wx = originX + x;
                    int wy = originY + y;
                    if (grid[wx, wy] != CellType.Grass)
                        return false;
                }
            }

            // Si es un elemento natural sin puerta (Bosque, Meseta, etc.), no requiere comprobación de acceso
            if (!data.hasDoor)
                return true;

            Vector2Int door = new Vector2Int(originX + data.doorOffset.x, originY + data.doorOffset.y);
            if (door.x < 0 || door.x >= width || door.y < 0 || door.y >= height)
                return false;

            // Comprobar la casilla justo delante de la puerta según su posición en el borde
            int frontX = door.x;
            int frontY = door.y;
            if (data.doorOffset.y == 0) frontY--;
            else if (data.doorOffset.y == data.size.y - 1) frontY++;
            else if (data.doorOffset.x == 0) frontX--;
            else if (data.doorOffset.x == data.size.x - 1) frontX++;

            // Si la casilla de acceso exterior está fuera del mapa o es agua, no es viable
            if (frontX < 0 || frontX >= width || frontY < 0 || frontY >= height || grid[frontX, frontY] == CellType.Water)
                return false;

            return true;
        }
    }
}
