using System.Collections.Generic;
using UnityEngine;
using WorldGen.Core;

namespace WorldGen.Generators
{
    public class IslandData
    {
        public int id;
        public List<Vector2Int> tiles = new List<Vector2Int>();
        public Vector2 center;
        public RectInt bounds;
        public int Area => tiles.Count;
    }

    public static class TerrainGenerator
    {
        // 1. Método principal directo e intuitivo
        public static CellType[,] Generate(int width, int height, float waterLevel, int seed, TerrainStyle style = TerrainStyle.Archipelago, bool largeIslands = true, int archipelagoIslandCount = 3)
        {
            CellType[,] grid = new CellType[width, height];
            Random.InitState(seed);

            switch (style)
            {
                case TerrainStyle.Archipelago:
                    GenerateArchipelago(grid, width, height, waterLevel, seed, largeIslands, archipelagoIslandCount);
                    break;
                case TerrainStyle.Continent:
                    GenerateContinent(grid, width, height, waterLevel, seed);
                    break;
                case TerrainStyle.CoastalIslands:
                    GenerateCoastalIslands(grid, width, height, waterLevel, seed);
                    break;
                case TerrainStyle.LakesAndRivers:
                    GenerateLakesAndRivers(grid, width, height, waterLevel, seed);
                    break;
                case TerrainStyle.FourContinents:
                    GenerateFourContinents(grid, width, height, waterLevel, seed);
                    break;
                default:
                    GenerateArchipelago(grid, width, height, waterLevel, seed, largeIslands, archipelagoIslandCount);
                    break;
            }

            SmoothWaterAndEnforceAccidents(grid, width, height, style);
            return grid;
        }

        // Sobrecargas retrocompatibles para no romper código existente
        public static CellType[,] Generate(int width, int height, float scale, float waterThreshold, int seed, TerrainStyle style, int islandSections, float islandSize, int octaves)
        {
            return Generate(width, height, waterThreshold, seed, style);
        }

        public static CellType[,] Generate(int width, int height, float scale, float waterThreshold, int seed)
        {
            return Generate(width, height, waterThreshold, seed, TerrainStyle.Archipelago);
        }

        #region 1. Archipiélago (Islas Grandes, Bahías, Cabos y Estrechos)
        private static void GenerateArchipelago(CellType[,] grid, int width, int height, float waterLevel, int seed, bool largeIslands = true, int targetIslandCount = 3)
        {
            List<IslandSeed> islands = new List<IslandSeed>();

            float margin = Mathf.Min(width, height) * 0.10f;
            float usableW = width - 2f * margin;
            float usableH = height - 2f * margin;

            if (largeIslands)
            {
                // Modo Archipiélago Mayor: Islas amplias y habitables con radios generosos
                int count = Mathf.Clamp(targetIslandCount, 2, 4);

                // Coordenadas relativas bien distribuidas para que no se superpongan en un solo pegote
                float[][] relativePositions;
                if (count == 2)
                {
                    relativePositions = new float[][] {
                        new float[] { 0.35f, 0.45f },
                        new float[] { 0.65f, 0.55f }
                    };
                }
                else if (count == 3)
                {
                    relativePositions = new float[][] {
                        new float[] { 0.30f, 0.65f }, // Noroeste
                        new float[] { 0.70f, 0.60f }, // Noreste
                        new float[] { 0.50f, 0.30f }  // Sur central
                    };
                }
                else // 4 islas
                {
                    relativePositions = new float[][] {
                        new float[] { 0.30f, 0.70f },
                        new float[] { 0.70f, 0.70f },
                        new float[] { 0.30f, 0.30f },
                        new float[] { 0.70f, 0.30f }
                    };
                }

                // Radio base calibrado: islas grandes pero con estrechos marítimos bien definidos
                float baseRadius = Mathf.Min(usableW, usableH) * (count <= 2 ? 0.30f : 0.24f);

                for (int i = 0; i < count; i++)
                {
                    float jitterX = Random.Range(-0.04f, 0.04f);
                    float jitterY = Random.Range(-0.04f, 0.04f);
                    float cx = margin + Mathf.Clamp01(relativePositions[i][0] + jitterX) * usableW;
                    float cy = margin + Mathf.Clamp01(relativePositions[i][1] + jitterY) * usableH;

                    // Jerarquía de Islas: Isla Mayor (i=0) espaciosa, secundarias con siluetas orgánicas
                    float radiusMultiplier = (i == 0) ? 1.25f : Random.Range(0.92f, 1.10f);
                    float radius = baseRadius * radiusMultiplier;
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float aspect = (i == 1) ? Random.Range(1.22f, 1.45f) : Random.Range(0.88f, 1.20f);
                    float strength = (i == 0) ? 1.28f : 1.18f; // Fuerza elevada en el núcleo de tierra

                    islands.Add(new IslandSeed(cx, cy, radius, angle, aspect, strength));
                }
            }
            else
            {
                // Modo Clásico con islotes satélite
                int majorIslandCount = Random.Range(2, 4);
                int mediumIslandCount = Random.Range(2, 4);
                int satelliteCount = Random.Range(3, 7);

                for (int i = 0; i < majorIslandCount; i++)
                {
                    float cx = margin + Random.Range(0.15f, 0.85f) * usableW;
                    float cy = margin + Random.Range(0.15f, 0.85f) * usableH;
                    float radius = Mathf.Min(usableW, usableH) * Random.Range(0.25f, 0.38f);
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float aspect = Random.Range(0.7f, 1.4f);
                    islands.Add(new IslandSeed(cx, cy, radius, angle, aspect, 1.0f));
                }

                for (int i = 0; i < mediumIslandCount; i++)
                {
                    float cx = margin + Random.Range(0.1f, 0.9f) * usableW;
                    float cy = margin + Random.Range(0.1f, 0.9f) * usableH;
                    float radius = Mathf.Min(usableW, usableH) * Random.Range(0.14f, 0.22f);
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float aspect = Random.Range(0.8f, 1.3f);
                    islands.Add(new IslandSeed(cx, cy, radius, angle, aspect, 0.9f));
                }

                for (int i = 0; i < satelliteCount; i++)
                {
                    float cx = margin + Random.Range(0.08f, 0.92f) * usableW;
                    float cy = margin + Random.Range(0.08f, 0.92f) * usableH;
                    float radius = Mathf.Min(usableW, usableH) * Random.Range(0.06f, 0.11f);
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    islands.Add(new IslandSeed(cx, cy, radius, angle, 1.0f, 0.75f));
                }
            }

            // Mapeo con Domain Warping multi-octava para esculpir bahías, penínsulas y estrechos
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float w1X = Mathf.Sin(y * 0.07f + seed * 0.17f) * 3.4f + Mathf.Cos(x * 0.05f + seed * 0.31f) * 2.2f;
                    float w1Y = Mathf.Cos(x * 0.07f + seed * 0.29f) * 3.4f + Mathf.Sin(y * 0.05f + seed * 0.43f) * 2.2f;
                    float w2X = Mathf.Sin((x + y) * 0.16f + seed * 0.63f) * 1.4f;
                    float w2Y = Mathf.Cos((x - y) * 0.16f + seed * 0.81f) * 1.4f;

                    float warpX = x + w1X + w2X;
                    float warpY = y + w1Y + w2Y;

                    float firstInfluence = 0f;
                    float secondInfluence = 0f;
                    for (int i = 0; i < islands.Count; i++)
                    {
                        var isl = islands[i];
                        float dx = warpX - isl.cx;
                        float dy = warpY - isl.cy;

                        float rotX = dx * Mathf.Cos(isl.angle) - dy * Mathf.Sin(isl.angle);
                        float rotY = (dx * Mathf.Sin(isl.angle) + dy * Mathf.Cos(isl.angle)) / isl.aspect;
                        float dist = Mathf.Sqrt(rotX * rotX + rotY * rotY);

                        if (dist < isl.radius)
                        {
                            float t = dist / isl.radius;
                            float infl = Mathf.Cos(t * Mathf.PI * 0.5f) * isl.strength;
                            if (infl > firstInfluence)
                            {
                                secondInfluence = firstInfluence;
                                firstInfluence = infl;
                            }
                            else if (infl > secondInfluence)
                            {
                                secondInfluence = infl;
                            }
                        }
                    }

                    // Penalización de estrecho: cuando dos islas compiten, tallar un canal navegable
                    float straitPenalty = 0f;
                    if (largeIslands && secondInfluence > 0.22f)
                    {
                        straitPenalty = (secondInfluence - 0.22f) * 0.85f;
                    }

                    float fractal = FractalNoise(x, y, 0.06f, 3, seed);
                    float elevation = largeIslands 
                        ? ((firstInfluence - straitPenalty) * 0.74f) + (fractal * 0.26f) 
                        : (firstInfluence * 0.68f) + (fractal * 0.38f);

                    // Amortiguación perimetral (Border Falloff) para garantizar mar abierto en todas las orillas del mapa (mundo rodeado de océano)
                    float borderDistX = Mathf.Min(x, width - 1 - x);
                    float borderDistY = Mathf.Min(y, height - 1 - y);
                    float minBorderDist = Mathf.Min(borderDistX, borderDistY);
                    float topDist = height - 1 - y;

                    // Franja perimetral de mar abierto continuo (garantizada de al menos 8 casillas al norte)
                    if (topDist < 8 || minBorderDist < 4)
                    {
                        grid[x, y] = CellType.Water;
                        continue;
                    }

                    // Caída suave hacia la orilla para que las islas terminen de forma natural antes del borde
                    float falloffRange = Mathf.Max(6f, Mathf.Min(width, height) * 0.12f);
                    float topFalloff = Mathf.Clamp01((topDist - 4f) / Mathf.Max(12f, height * 0.12f));
                    float sideFalloff = Mathf.Clamp01(minBorderDist / falloffRange);
                    float borderFalloff = Mathf.Min(topFalloff, sideFalloff);
                    elevation *= (borderFalloff * borderFalloff);

                    // Laguna interior opcional en la isla principal solo en modo clásico
                    if (!largeIslands && firstInfluence > 0.82f && fractal < 0.22f)
                    {
                        grid[x, y] = CellType.Water;
                    }
                    else
                    {
                        grid[x, y] = (elevation < waterLevel) ? CellType.Water : CellType.Grass;
                    }
                }
            }
        }

        #region Detección y Análisis de Islas
        public static List<IslandData> DetectIslands(CellType[,] grid, int width, int height, int minTileArea = 16)
        {
            List<IslandData> islands = new List<IslandData>();
            bool[,] visited = new bool[width, height];
            int nextId = 0;

            Vector2Int[] dirs = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] == CellType.Water || visited[x, y]) continue;

                    // Inundación (Flood fill) para abarcar toda la masa de tierra contigua
                    IslandData island = new IslandData();
                    island.id = nextId++;
                    Queue<Vector2Int> queue = new Queue<Vector2Int>();
                    queue.Enqueue(new Vector2Int(x, y));
                    visited[x, y] = true;

                    int minX = x, maxX = x, minY = y, maxY = y;
                    float sumX = 0, sumY = 0;

                    while (queue.Count > 0)
                    {
                        Vector2Int curr = queue.Dequeue();
                        island.tiles.Add(curr);
                        sumX += curr.x;
                        sumY += curr.y;

                        if (curr.x < minX) minX = curr.x;
                        if (curr.x > maxX) maxX = curr.x;
                        if (curr.y < minY) minY = curr.y;
                        if (curr.y > maxY) maxY = curr.y;

                        for (int d = 0; d < 4; d++)
                        {
                            int nx = curr.x + dirs[d].x;
                            int ny = curr.y + dirs[d].y;
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                if (!visited[nx, ny] && grid[nx, ny] != CellType.Water)
                                {
                                    visited[nx, ny] = true;
                                    queue.Enqueue(new Vector2Int(nx, ny));
                                }
                            }
                        }
                    }

                    island.bounds = new RectInt(minX, minY, (maxX - minX + 1), (maxY - minY + 1));
                    island.center = new Vector2(sumX / island.tiles.Count, sumY / island.tiles.Count);

                    if (island.Area >= minTileArea)
                    {
                        islands.Add(island);
                    }
                    else
                    {
                        // Limpiar micro-manchas o restos menores a minTileArea para evitar islas inútiles
                        foreach (var t in island.tiles)
                        {
                            grid[t.x, t.y] = CellType.Water;
                        }
                    }
                }
            }

            // Ordenar las islas de mayor superficie a menor superficie
            islands.Sort((a, b) => b.Area.CompareTo(a.Area));
            return islands;
        }
        #endregion
        #endregion

        #region 2. Continente (Golfos, Penínsulas, Cabos y Lagunas)
        private static void GenerateContinent(CellType[,] grid, int width, int height, float waterLevel, int seed)
        {
            // Un continente cubre 65-75% de la tierra con golfos profundos y penínsulas
            float centerX = width * 0.5f;
            float centerY = height * 0.5f;
            float maxDim = Mathf.Max(width, height);

            // Centros de golfos / bahías marinas que muerden la costa
            int gulfCount = Random.Range(2, 5);
            List<Vector2> gulfCenters = new List<Vector2>();
            List<float> gulfRadii = new List<float>();

            for (int i = 0; i < gulfCount; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float distFromCenter = Random.Range(0.35f, 0.50f) * maxDim;
                gulfCenters.Add(new Vector2(centerX + Mathf.Cos(ang) * distFromCenter, centerY + Mathf.Sin(ang) * distFromCenter));
                gulfRadii.Add(Random.Range(0.18f, 0.28f) * maxDim);
            }

            // Centros de lagunas interiores
            int lakeCount = Random.Range(1, 3);
            List<Vector2> lakeCenters = new List<Vector2>();
            List<float> lakeRadii = new List<float>();

            for (int i = 0; i < lakeCount; i++)
            {
                lakeCenters.Add(new Vector2(centerX + Random.Range(-0.25f, 0.25f) * width, centerY + Random.Range(-0.25f, 0.25f) * height));
                lakeRadii.Add(Random.Range(0.08f, 0.14f) * maxDim);
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Deformación de dominio para penínsulas y cabos
                    float warpX = x + Mathf.Sin(y * 0.10f + seed * 0.2f) * 5.0f;
                    float warpY = y + Mathf.Cos(x * 0.10f + seed * 0.4f) * 5.0f;

                    // Distancia normalizada al centro del continente
                    float distToCenter = Vector2.Distance(new Vector2(warpX, warpY), new Vector2(centerX, centerY)) / (maxDim * 0.55f);
                    float baseContinent = Mathf.Clamp01(1.0f - distToCenter);

                    // Descontar influencia de los golfos marinos
                    float gulfInfluence = 0f;
                    for (int g = 0; g < gulfCenters.Count; g++)
                    {
                        float d = Vector2.Distance(new Vector2(warpX, warpY), gulfCenters[g]);
                        if (d < gulfRadii[g])
                        {
                            float t = 1.0f - (d / gulfRadii[g]);
                            if (t > gulfInfluence) gulfInfluence = t;
                        }
                    }

                    // Lagunas interiores
                    float lakeInfluence = 0f;
                    for (int l = 0; l < lakeCenters.Count; l++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), lakeCenters[l]);
                        if (d < lakeRadii[l])
                        {
                            float t = 1.0f - (d / lakeRadii[l]);
                            if (t > lakeInfluence) lakeInfluence = t;
                        }
                    }

                    float fractal = FractalNoise(x, y, 0.05f, 3, seed);
                    float elevation = (baseContinent * 0.85f) - (gulfInfluence * 0.65f) + (fractal * 0.35f);

                    // Amortiguación perimetral para mapa rodeado de océano exterior (franja de mar arriba)
                    float borderDistX = Mathf.Min(x, width - 1 - x);
                    float borderDistY = Mathf.Min(y, height - 1 - y);
                    float minBorderDist = Mathf.Min(borderDistX, borderDistY);
                    float topDist = height - 1 - y;

                    if (topDist < 8 || minBorderDist < 4)
                    {
                        grid[x, y] = CellType.Water;
                        continue;
                    }

                    float topFalloff = Mathf.Clamp01((topDist - 4f) / Mathf.Max(12f, height * 0.12f));
                    float sideFalloff = Mathf.Clamp01(minBorderDist / Mathf.Max(8f, width * 0.08f));
                    float borderFalloff = Mathf.Min(topFalloff, sideFalloff);
                    elevation *= (borderFalloff * borderFalloff);

                    if (lakeInfluence > 0.5f)
                    {
                        grid[x, y] = CellType.Water;
                    }
                    else
                    {
                        grid[x, y] = (elevation < waterLevel) ? CellType.Water : CellType.Grass;
                    }
                }
            }
        }
        #endregion

        #region 3. Costa Continental e Islas Satélite
        private static void GenerateCoastalIslands(CellType[,] grid, int width, int height, float waterLevel, int seed)
        {
            // Elige un lado del mapa para ser el continente (0=Oeste, 1=Este, 2=Sur, 3=Norte)
            int coastSide = Random.Range(0, 4);

            // Generar 3-5 islas satélite en el lado del mar
            int satelliteCount = Random.Range(3, 6);
            List<IslandSeed> offshoreIslands = new List<IslandSeed>();

            for (int i = 0; i < satelliteCount; i++)
            {
                float cx, cy;
                if (coastSide == 0) // Continente al oeste, mar al este
                {
                    cx = Random.Range(0.55f, 0.85f) * width;
                    cy = Random.Range(0.15f, 0.78f) * height;
                }
                else if (coastSide == 1) // Continente al este, mar al oeste
                {
                    cx = Random.Range(0.15f, 0.45f) * width;
                    cy = Random.Range(0.15f, 0.78f) * height;
                }
                else if (coastSide == 2) // Continente al sur, mar al norte
                {
                    cx = Random.Range(0.15f, 0.85f) * width;
                    cy = Random.Range(0.50f, 0.78f) * height;
                }
                else // Continente al norte, mar al sur
                {
                    cx = Random.Range(0.15f, 0.85f) * width;
                    cy = Random.Range(0.15f, 0.45f) * height;
                }

                float r = Mathf.Min(width, height) * Random.Range(0.09f, 0.18f);
                offshoreIslands.Add(new IslandSeed(cx, cy, r, Random.Range(0f, Mathf.PI), Random.Range(0.8f, 1.3f), 0.9f));
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Gradiente del continente principal
                    float coastalGradient = 0f;
                    if (coastSide == 0) coastalGradient = 1.0f - ((float)x / width);
                    else if (coastSide == 1) coastalGradient = (float)x / width;
                    else if (coastSide == 2) coastalGradient = 1.0f - ((float)y / height);
                    else coastalGradient = (float)y / height;

                    // Deformación de costa
                    float warp = Mathf.Sin(x * 0.1f + seed) * 0.12f + Mathf.Cos(y * 0.1f + seed * 0.5f) * 0.12f;
                    coastalGradient += warp;

                    // Influencia de islas satélite
                    float islandInfluence = 0f;
                    for (int i = 0; i < offshoreIslands.Count; i++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(offshoreIslands[i].cx, offshoreIslands[i].cy));
                        if (d < offshoreIslands[i].radius)
                        {
                            float t = Mathf.Cos((d / offshoreIslands[i].radius) * Mathf.PI * 0.5f) * offshoreIslands[i].strength;
                            if (t > islandInfluence) islandInfluence = t;
                        }
                    }

                    float fractal = FractalNoise(x, y, 0.06f, 3, seed);
                    float elevation = (coastalGradient * 0.70f) + (islandInfluence * 0.85f) + (fractal * 0.28f);

                    // Amortiguación perimetral para mapa rodeado de océano exterior (franja de mar arriba garantizada)
                    float borderDistX = Mathf.Min(x, width - 1 - x);
                    float borderDistY = Mathf.Min(y, height - 1 - y);
                    float minBorderDist = Mathf.Min(borderDistX, borderDistY);
                    float topDist = height - 1 - y;

                    if (topDist < 8 || minBorderDist < 4)
                    {
                        grid[x, y] = CellType.Water;
                        continue;
                    }

                    float topFalloff = Mathf.Clamp01((topDist - 4f) / Mathf.Max(12f, height * 0.12f));
                    float sideFalloff = Mathf.Clamp01(minBorderDist / Mathf.Max(8f, width * 0.08f));
                    float borderFalloff = Mathf.Min(topFalloff, sideFalloff);
                    elevation *= (borderFalloff * borderFalloff);

                    grid[x, y] = (elevation < waterLevel) ? CellType.Water : CellType.Grass;
                }
            }
        }
        #endregion

        #region 4. Lagos y Ríos Interiores
        private static void GenerateLakesAndRivers(CellType[,] grid, int width, int height, float waterLevel, int seed)
        {
            // Tierra firme continental con una red de 3 a 6 lagos orgánicos y meandros
            int lakeCount = Random.Range(3, 7);
            List<Vector2> lakes = new List<Vector2>();
            List<float> lakeRadii = new List<float>();

            for (int i = 0; i < lakeCount; i++)
            {
                lakes.Add(new Vector2(Random.Range(0.15f, 0.85f) * width, Random.Range(0.15f, 0.85f) * height));
                lakeRadii.Add(Mathf.Min(width, height) * Random.Range(0.10f, 0.20f));
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float warpX = x + Mathf.Sin(y * 0.12f + seed * 0.3f) * 4.0f;
                    float warpY = y + Mathf.Cos(x * 0.12f + seed * 0.7f) * 4.0f;

                    float lakeMask = 0f;
                    for (int i = 0; i < lakes.Count; i++)
                    {
                        float d = Vector2.Distance(new Vector2(warpX, warpY), lakes[i]);
                        if (d < lakeRadii[i])
                        {
                            float t = 1.0f - (d / lakeRadii[i]);
                            if (t > lakeMask) lakeMask = t;
                        }
                    }

                    float fractal = FractalNoise(x, y, 0.05f, 3, seed);
                    float waterDepression = (lakeMask * 0.75f) + ((1.0f - fractal) * 0.25f);

                    grid[x, y] = (waterDepression > (1.0f - waterLevel * 0.8f)) ? CellType.Water : CellType.Grass;
                }
            }
        }
        #endregion

        #region 5. Cuatro Continentes / Cuatro Secciones (Chunk Map Macro-Zones)
        private static void GenerateFourContinents(CellType[,] grid, int width, int height, float waterLevel, int seed)
        {
            float midX = width * 0.5f;
            float midY = height * 0.5f;
            float straitWidth = Mathf.Max(6f, Mathf.Min(width, height) * 0.12f);
            float straitHalfW = straitWidth * 0.5f;

            // 4 Semillas continentales bien calibradas en cada cuadrante (NW, NE, SW, SE)
            List<IslandSeed> continentSeeds = new List<IslandSeed>
            {
                // Cuadrante 0: Noroeste (NorthWest)
                new IslandSeed(midX * 0.50f, midY + midY * 0.50f, Mathf.Min(midX, midY) * 0.65f, 0.3f, 1.05f, 1.25f),
                // Cuadrante 1: Noreste (NorthEast)
                new IslandSeed(midX + midX * 0.50f, midY + midY * 0.50f, Mathf.Min(midX, midY) * 0.62f, 0.9f, 1.25f, 1.20f),
                // Cuadrante 2: Suroeste (SouthWest)
                new IslandSeed(midX * 0.50f, midY * 0.50f, Mathf.Min(midX, midY) * 0.62f, -0.4f, 0.88f, 1.22f),
                // Cuadrante 3: Sureste (SouthEast)
                new IslandSeed(midX + midX * 0.50f, midY * 0.50f, Mathf.Min(midX, midY) * 0.66f, 0.4f, 1.15f, 1.24f)
            };

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Falloff perimetral: Franja de mar abierto continuo garantizada en todos los bordes
                    float borderDistX = Mathf.Min(x, width - 1 - x);
                    float borderDistY = Mathf.Min(y, height - 1 - y);
                    float borderDist = Mathf.Min(borderDistX, borderDistY);
                    float topDist = height - 1 - y;

                    if (topDist < 8 || borderDist < 4)
                    {
                        grid[x, y] = CellType.Water;
                        continue;
                    }

                    // Deformación de dominio orgánica para bahías, cabos y siluetas costeras vivas
                    float wX = Mathf.Sin(y * 0.08f + seed * 0.23f) * 3.8f + Mathf.Cos(x * 0.06f + seed * 0.41f) * 2.2f;
                    float wY = Mathf.Cos(x * 0.08f + seed * 0.37f) * 3.8f + Mathf.Sin(y * 0.06f + seed * 0.19f) * 2.2f;
                    float warpX = x + wX;
                    float warpY = y + wY;

                    // Estrechos oceánicos marítimos entre los 4 continentes (canal en cruz con ondulación natural)
                    float straitV_X = midX + Mathf.Sin(y * 0.09f + seed * 0.31f) * 3.2f;
                    float straitH_Y = midY + Mathf.Cos(x * 0.09f + seed * 0.43f) * 3.2f;

                    float distV = Mathf.Abs(warpX - straitV_X);
                    float distH = Mathf.Abs(warpY - straitH_Y);

                    // Núcleo del canal: agua garantizada
                    if (distV < (straitHalfW * 0.60f) || distH < (straitHalfW * 0.60f))
                    {
                        grid[x, y] = CellType.Water;
                        continue;
                    }

                    // Penalización de proximidad a los estrechos
                    float straitPenalty = 0f;
                    if (distV < (straitHalfW * 1.5f))
                    {
                        float t = 1.0f - (distV / (straitHalfW * 1.5f));
                        straitPenalty = Mathf.Max(straitPenalty, t * 1.2f);
                    }
                    if (distH < (straitHalfW * 1.5f))
                    {
                        float t = 1.0f - (distH / (straitHalfW * 1.5f));
                        straitPenalty = Mathf.Max(straitPenalty, t * 1.2f);
                    }

                    // Influencia continental máxima
                    float maxInfl = 0f;
                    for (int i = 0; i < 4; i++)
                    {
                        var c = continentSeeds[i];
                        float dx = warpX - c.cx;
                        float dy = warpY - c.cy;
                        float rotX = dx * Mathf.Cos(c.angle) - dy * Mathf.Sin(c.angle);
                        float rotY = (dx * Mathf.Sin(c.angle) + dy * Mathf.Cos(c.angle)) / c.aspect;
                        float dist = Mathf.Sqrt(rotX * rotX + rotY * rotY);
                        if (dist < c.radius)
                        {
                            float t = dist / c.radius;
                            float infl = Mathf.Cos(t * Mathf.PI * 0.5f) * c.strength;
                            if (infl > maxInfl) maxInfl = infl;
                        }
                    }

                    float fractal = FractalNoise(x, y, 0.06f, 3, seed);
                    float elevation = (maxInfl * 0.72f) + (fractal * 0.28f) - (straitPenalty * 0.75f);

                    // Caída suave hacia la orilla del mapa con franja norte generosa
                    float topFalloff = Mathf.Clamp01((topDist - 4f) / Mathf.Max(12f, height * 0.12f));
                    float sideFalloff = Mathf.Clamp01(borderDist / 8f);
                    float falloff = Mathf.Min(topFalloff, sideFalloff);
                    elevation *= (falloff * falloff);

                    grid[x, y] = (elevation < waterLevel) ? CellType.Water : CellType.Grass;
                }
            }
        }
        #endregion

        #region Pulido Topológico y Garantía Autotile 3x3 + 2x2
        private static void SmoothWaterAndEnforceAccidents(CellType[,] grid, int width, int height, TerrainStyle style)
        {
            // Para TODOS los estilos de mundo exterior (Archipiélago, Costas, Continente, Cuatro Continentes):
            bool isOpenOcean = (style == TerrainStyle.Archipelago || style == TerrainStyle.CoastalIslands || style == TerrainStyle.FourContinents || style == TerrainStyle.Continent);

            int topMargin = Mathf.Max(8, Mathf.RoundToInt(height * 0.06f));    // Al menos 8 a 10 casillas de franja de mar arriba garantizadas
            int sideMargin = Mathf.Max(4, Mathf.RoundToInt(width * 0.03f));    // Al menos 4 casillas a izquierda y derecha
            int bottomMargin = Mathf.Max(4, Mathf.RoundToInt(height * 0.03f)); // Al menos 4 casillas abajo

            // 1. Margen de seguridad perimetral de mar abierto continuo
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (isOpenOcean)
                    {
                        if (y >= height - topMargin || y < bottomMargin || x < sideMargin || x >= width - sideMargin)
                        {
                            grid[x, y] = CellType.Water;
                        }
                    }
                    else
                    {
                        // En lagos y ríos (estilo puramente interior):
                        if (x <= 1 || x >= width - 2 || y <= 1 || y >= height - 2)
                        {
                            grid[x, y] = CellType.Grass;
                        }
                    }
                }
            }

            // 2. Eliminar esquinas diagonales pinzadas (Pinch points)
            for (int x = 1; x < width - 2; x++)
            {
                for (int y = 1; y < height - 2; y++)
                {
                    if (grid[x, y] == CellType.Water && grid[x + 1, y + 1] == CellType.Water &&
                        grid[x + 1, y] == CellType.Grass && grid[x, y + 1] == CellType.Grass)
                    {
                        grid[x + 1, y] = CellType.Water;
                    }
                    else if (grid[x + 1, y] == CellType.Water && grid[x, y + 1] == CellType.Water &&
                             grid[x, y] == CellType.Grass && grid[x + 1, y + 1] == CellType.Grass)
                    {
                        grid[x, y] = CellType.Water;
                    }
                }
            }

            // 3. Regla de Oro del Autotile 3x3 y 2x2:
            // - Ningún canal de agua debe ser de 1 casilla (ensanchar a 2 casillas o podar a pasto)
            // - Ninguna península debe ser de 1 casilla (ensanchar a 2 casillas o abrir a agua)
            // - Rellenar agujeros aislados 1x1
            for (int pass = 0; pass < 3; pass++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        bool isWater = (grid[x, y] == CellType.Water);
                        bool n = (grid[x, y + 1] == CellType.Water);
                        bool s = (grid[x, y - 1] == CellType.Water);
                        bool w = (grid[x - 1, y] == CellType.Water);
                        bool e = (grid[x + 1, y] == CellType.Water);

                        if (isWater)
                        {
                            int grassCount = 0;
                            if (!n) grassCount++;
                            if (!s) grassCount++;
                            if (!w) grassCount++;
                            if (!e) grassCount++;

                            // Espiga de agua de 1 casilla rodeada de pasto -> podar a pasto
                            if (grassCount >= 3)
                            {
                                grid[x, y] = CellType.Grass;
                            }
                            // Canal de agua de 1 casilla encajonado entre pastos opuestos -> podar a pasto
                            else if (!w && !e)
                            {
                                grid[x, y] = CellType.Grass;
                            }
                            else if (!n && !s)
                            {
                                grid[x, y] = CellType.Grass;
                            }
                        }
                        else
                        {
                            int waterCount = 0;
                            if (n) waterCount++;
                            if (s) waterCount++;
                            if (w) waterCount++;
                            if (e) waterCount++;

                            // Hueco de pasto de 1x1 dentro de un lago -> rellenar a agua
                            if (waterCount == 4)
                            {
                                grid[x, y] = CellType.Water;
                            }
                            // Península de pasto de 1 casilla en medio del mar -> abrir a agua
                            else if (n && s)
                            {
                                grid[x, y] = CellType.Water;
                            }
                            else if (w && e)
                            {
                                grid[x, y] = CellType.Water;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Utilidades Internas
        private static float FractalNoise(float x, float y, float frequency, int octaves, int seed)
        {
            float total = 0f;
            float freq = frequency;
            float amp = 1f;
            float maxAmp = 0f;

            float offX = (seed * 17.13f) % 1000f;
            float offY = (seed * 29.47f) % 1000f;

            for (int i = 0; i < octaves; i++)
            {
                float sx = (x + offX) * freq;
                float sy = (y + offY) * freq;
                total += Mathf.PerlinNoise(sx, sy) * amp;
                maxAmp += amp;
                amp *= 0.5f;
                freq *= 2.0f;
            }

            return (maxAmp > 0f) ? (total / maxAmp) : 0.5f;
        }

        private struct IslandSeed
        {
            public float cx;
            public float cy;
            public float radius;
            public float angle;
            public float aspect;
            public float strength;

            public IslandSeed(float cx, float cy, float radius, float angle, float aspect, float strength)
            {
                this.cx = cx;
                this.cy = cy;
                this.radius = radius;
                this.angle = angle;
                this.aspect = aspect;
                this.strength = strength;
            }
        }
        #endregion
    }
}


