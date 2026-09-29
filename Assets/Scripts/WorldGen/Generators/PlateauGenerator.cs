using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using WorldGen.Core;
using WorldGen.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WorldGen.Generators
{
    /// <summary>
    /// Generador procedural de mesetas como auténticos accidentes geográficos dinámicos.
    /// Crea mesetas de dimensiones y formas adaptables al terreno de la isla,
    /// con su rampa orientada y modelada con la hendidura curva circular del cráter (#88, #89)
    /// y el pasillo de subida (#133, #134, #135).
    /// </summary>
    public static class PlateauGenerator
    {
        public static bool TryGenerateDynamicPlateau(
            CellType[,] grid,
            int width,
            int height,
            IslandData targetIsland,
            WfcVisualDatabase database,
            Tilemap structureMap,
            out Vector2Int rampDoorPos,
            int minW = 7,
            int maxW = 15,
            int minH = 5,
            int maxH = 9)
        {
            rampDoorPos = Vector2Int.zero;

            // Determinar dimensiones dinámicas proporcionadas al espacio de la isla (nunca ocupar más del 52%)
            int maxAllowedW = 10;
            int maxAllowedH = 7;
            if (targetIsland != null)
            {
                maxAllowedW = Mathf.Clamp(Mathf.FloorToInt(targetIsland.bounds.width * 0.52f), 6, 9);
                maxAllowedH = Mathf.Clamp(Mathf.FloorToInt(targetIsland.bounds.height * 0.52f), 5, 7);
            }

            int targetW = Random.Range(Mathf.Min(minW, maxAllowedW), maxAllowedW + 1);
            int targetH = Random.Range(Mathf.Min(minH, maxAllowedH), maxAllowedH + 1);

            // Buscar una posición viable con margen sobre pasto
            int attempts = 150;
            Vector2Int bestOrigin = Vector2Int.zero;
            bool foundSpot = false;

            for (int att = 0; att < attempts; att++)
            {
                int rx, ry;
                if (targetIsland != null && targetIsland.tiles.Count > 0)
                {
                    Vector2Int seedTile = targetIsland.tiles[Random.Range(0, targetIsland.tiles.Count)];
                    rx = seedTile.x - targetW / 2;
                    ry = seedTile.y - targetH / 2;
                }
                else
                {
                    rx = Random.Range(3, width - targetW - 3);
                    ry = Random.Range(3, height - targetH - 3);
                }

                if (CanPlacePlateau(rx, ry, targetW, targetH, grid, width, height))
                {
                    bestOrigin = new Vector2Int(rx, ry);
                    foundSpot = true;
                    break;
                }
            }

            if (!foundSpot)
            {
                // Intentar con un tamaño más compacto si la isla era ajustada
                targetW = Mathf.Max(6, minW - 1);
                targetH = Mathf.Max(5, minH - 1);

                for (int att = 0; att < attempts; att++)
                {
                    int rx, ry;
                    if (targetIsland != null && targetIsland.tiles.Count > 0)
                    {
                        Vector2Int seedTile = targetIsland.tiles[Random.Range(0, targetIsland.tiles.Count)];
                        rx = seedTile.x - targetW / 2;
                        ry = seedTile.y - targetH / 2;
                    }
                    else
                    {
                        rx = Random.Range(3, width - targetW - 3);
                        ry = Random.Range(3, height - targetH - 3);
                    }

                    if (CanPlacePlateau(rx, ry, targetW, targetH, grid, width, height))
                    {
                        bestOrigin = new Vector2Int(rx, ry);
                        foundSpot = true;
                        break;
                    }
                }
            }

            if (!foundSpot) return false;

            int ox = bestOrigin.x;
            int oy = bestOrigin.y;

            // Cargar tiles necesarios
            TileBase t53 = GetTile(database, 53, "Meseta_53");
            TileBase t54 = GetTile(database, 54, "Meseta_54");
            TileBase t55 = GetTile(database, 55, "Meseta_55");
            TileBase t65 = GetTile(database, 65, "Meseta_65");
            TileBase t66 = GetTile(database, 66, "Meseta_66");
            TileBase t76 = GetTile(database, 76, "Meseta_76");
            TileBase t77 = GetTile(database, 77, "Meseta_77");
            TileBase t78 = GetTile(database, 78, "Meseta_78");

            // Columna de la puerta de acceso en el frente sur
            int doorCol = Mathf.Clamp(targetW / 2, 1, targetW - 2);

            // Construir el perímetro rocoso y la entrada limpia
            for (int y = 0; y < targetH; y++)
            {
                for (int x = 0; x < targetW; x++)
                {
                    int wx = ox + x;
                    int wy = oy + y;
                    Vector3Int pos = new Vector3Int(wx, wy, 0);

                    // 1. Cornisa Superior (Norte)
                    if (y == targetH - 1)
                    {
                        TileBase tile = (x == 0) ? t53 : (x == targetW - 1) ? t55 : t54;
                        if (structureMap != null && tile != null) structureMap.SetTile(pos, tile);
                        grid[wx, wy] = CellType.Structure;
                    }
                    // 2. Muro Lateral Oeste
                    else if (x == 0)
                    {
                        TileBase tile = (y == 0) ? t76 : t65;
                        if (structureMap != null && tile != null) structureMap.SetTile(pos, tile);
                        grid[wx, wy] = CellType.Structure;
                    }
                    // 3. Muro Lateral Este
                    else if (x == targetW - 1)
                    {
                        TileBase tile = (y == 0) ? t78 : t66;
                        if (structureMap != null && tile != null) structureMap.SetTile(pos, tile);
                        grid[wx, wy] = CellType.Structure;
                    }
                    // 4. Frente Inferior y Puerta de Acceso (y = 0)
                    else if (y == 0)
                    {
                        if (x == doorCol)
                        {
                            // Abertura libre de la puerta: transitable para el jugador y la carretera A*
                            grid[wx, wy] = CellType.Grass;
                        }
                        else
                        {
                            if (structureMap != null && t77 != null) structureMap.SetTile(pos, t77);
                            grid[wx, wy] = CellType.Structure;
                        }
                    }
                    // 5. Interior de la meseta
                    else
                    {
                        grid[wx, wy] = CellType.Grass;
                    }
                }
            }

            // Puerta / Waypoint exterior justo frente a la entrada
            rampDoorPos = new Vector2Int(ox + doorCol, oy - 1);

            // Opcional: Si la meseta es amplia, generar un elemento interior temático (cultivos o árboles)
            PopulatePlateauInterior(ox, oy, targetW, targetH, doorCol, grid, database, structureMap);

            Debug.Log($"<color=cyan><b>[PlateauGenerator]</b> Meseta dinámica generada con éxito ({targetW}x{targetH}) en ({ox}, {oy}) con puerta en ({ox + doorCol}, {oy}).</color>");
            return true;
        }

        private static bool CanPlacePlateau(int ox, int oy, int w, int h, CellType[,] grid, int mapW, int mapH)
        {
            // Margen de seguridad de al menos 1 celda de pasto alrededor para no tocar agua
            if (ox < 2 || ox + w >= mapW - 2 || oy < 2 || oy + h >= mapH - 2) return false;

            for (int x = -1; x <= w; x++)
            {
                for (int y = -1; y <= h; y++)
                {
                    int wx = ox + x;
                    int wy = oy + y;
                    if (grid[wx, wy] != CellType.Grass) return false;
                }
            }
            return true;
        }

        private static void PopulatePlateauInterior(int ox, int oy, int w, int h, int rampCol, CellType[,] grid, WfcVisualDatabase database, Tilemap structureMap)
        {
            if (structureMap == null) return;

            int interiorW = w - 2;
            int interiorH = h - 2;

            var roadTile = GetTile(database, -1, "caminos_134_tile") ?? GetTile(database, -1, "caminos_20_tile") ?? GetTile(database, -1, "Camino_Piedra_10");

            // 1. Trazar sendero de entrada desde la rampa hacia el centro de la meseta
            int pathTargetY = oy + Mathf.Clamp(interiorH / 2 + 1, 1, h - 2);
            for (int y = oy + 1; y <= pathTargetY; y++)
            {
                if (roadTile != null)
                {
                    structureMap.SetTile(new Vector3Int(ox + rampCol, y, 0), roadTile);
                }
            }

            // 2. Si hay espacio suficiente (al menos 4x4 interior), poblar con una casa o cultivos
            if (interiorW >= 4 && interiorH >= 4)
            {
                bool placeHouse = (Random.value < 0.55f);

                if (placeHouse)
                {
                    // Asentamiento: Cabaña 3x3
                    var hRoofL = GetTile(database, -1, "casitas_182");
                    var hRoofM = GetTile(database, -1, "casitas_183");
                    var hRoofR = GetTile(database, -1, "casitas_184");
                    var hWallL = GetTile(database, -1, "casitas_193");
                    var hWallM = GetTile(database, -1, "casitas_194");
                    var hWallR = GetTile(database, -1, "casitas_195");
                    var hBaseL = GetTile(database, -1, "casitas_199");
                    var hDoor  = GetTile(database, -1, "casitas_200");
                    var hBaseR = GetTile(database, -1, "casitas_201");

                    int houseX = Mathf.Clamp(ox + w / 2 - 1, ox + 1, ox + w - 4);
                    int houseY = oy + h - 4;

                    if (hRoofL != null && hBaseL != null && houseY > oy + 1)
                    {
                        structureMap.SetTile(new Vector3Int(houseX,     houseY,     0), hBaseL);
                        structureMap.SetTile(new Vector3Int(houseX + 1, houseY,     0), hDoor);
                        structureMap.SetTile(new Vector3Int(houseX + 2, houseY,     0), hBaseR);

                        structureMap.SetTile(new Vector3Int(houseX,     houseY + 1, 0), hWallL);
                        structureMap.SetTile(new Vector3Int(houseX + 1, houseY + 1, 0), hWallM);
                        structureMap.SetTile(new Vector3Int(houseX + 2, houseY + 1, 0), hWallR);

                        structureMap.SetTile(new Vector3Int(houseX,     houseY + 2, 0), hRoofL);
                        structureMap.SetTile(new Vector3Int(houseX + 1, houseY + 2, 0), hRoofM);
                        structureMap.SetTile(new Vector3Int(houseX + 2, houseY + 2, 0), hRoofR);

                        // Marcar huella de la casa como Structure para colisiones
                        for (int cx = 0; cx < 3; cx++)
                        {
                            for (int cy = 0; cy < 3; cy++)
                            {
                                int gx = houseX + cx;
                                int gy = houseY + cy;
                                if (gx >= 0 && gx < grid.GetLength(0) && gy >= 0 && gy < grid.GetLength(1))
                                {
                                    if (cx != 1 || cy != 0) // No bloquear la puerta de la casa
                                    {
                                        grid[gx, gy] = CellType.Structure;
                                    }
                                }
                            }
                        }

                        // Conectar sendero con la puerta de la cabaña
                        int hDoorX = houseX + 1;
                        if (roadTile != null)
                        {
                            for (int y = pathTargetY; y < houseY; y++)
                            {
                                structureMap.SetTile(new Vector3Int(ox + rampCol, y, 0), roadTile);
                            }
                            int minX = Mathf.Min(ox + rampCol, hDoorX);
                            int maxX = Mathf.Max(ox + rampCol, hDoorX);
                            for (int x = minX; x <= maxX; x++)
                            {
                                structureMap.SetTile(new Vector3Int(x, houseY - 1, 0), roadTile);
                            }
                        }
                    }
                }
                else
                {
                    // Huerto de Cultivos 3x3
                    int cropX = (rampCol > w / 2) ? ox + 1 : ox + w - 4;
                    int cropY = oy + h - 4;

                    var c144 = GetTile(database, 144, "Cultivo_144");
                    var c145 = GetTile(database, 145, "Cultivo_145");
                    var c146 = GetTile(database, 146, "Cultivo_146");
                    var c156 = GetTile(database, 156, "Cultivo_156");
                    var c157 = GetTile(database, 157, "Cultivo_157");
                    var c158 = GetTile(database, 158, "Cultivo_158");
                    var c168 = GetTile(database, 168, "Cultivo_168");
                    var c169 = GetTile(database, 169, "Cultivo_169");
                    var c170 = GetTile(database, 170, "Cultivo_170");

                    if (c144 != null && c157 != null && cropX >= ox + 1 && cropX + 2 < ox + w - 1 && cropY > oy)
                    {
                        structureMap.SetTile(new Vector3Int(cropX,     cropY + 2, 0), c144);
                        structureMap.SetTile(new Vector3Int(cropX + 1, cropY + 2, 0), c145);
                        structureMap.SetTile(new Vector3Int(cropX + 2, cropY + 2, 0), c146);

                        structureMap.SetTile(new Vector3Int(cropX,     cropY + 1, 0), c156);
                        structureMap.SetTile(new Vector3Int(cropX + 1, cropY + 1, 0), c157);
                        structureMap.SetTile(new Vector3Int(cropX + 2, cropY + 1, 0), c158);

                        structureMap.SetTile(new Vector3Int(cropX,     cropY,     0), c168);
                        structureMap.SetTile(new Vector3Int(cropX + 1, cropY,     0), c169);
                        structureMap.SetTile(new Vector3Int(cropX + 2, cropY,     0), c170);
                    }
                }
            }
        }

        private static TileBase GetTile(WfcVisualDatabase database, int id, string fallbackName)
        {
            if (database != null)
            {
                var entry = database.GetTileById(id);
                if (entry != null && entry.tile != null) return entry.tile;
            }

#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets($"{fallbackName} t:TileBase");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<TileBase>(path);
            }
#endif
            return null;
        }
    }
}
