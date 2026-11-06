using System.Collections.Generic;
using UnityEngine;
using WorldGen.Core;

namespace WorldGen.Generators
{
    /// <summary>
    /// Motor de búsqueda de caminos óptimos mediante el algoritmo A* (A-Star) en cuadrícula 2D.
    /// Diseñado para el trazado de carreteras, puentes sobre estrechos acuáticos y cálculo
    /// de la trayectoria troncal de vuelo (spine path) que guía la cámara y el caza en AK-47.
    /// </summary>
    public static class GridAStar
    {
        /// <summary>
        /// Encuentra un camino óptimo entre dos celdas del mapa considerando costes de terreno y puentes.
        /// </summary>
        /// <param name="start">Coordenadas del punto de inicio.</param>
        /// <param name="target">Coordenadas del punto de destino.</param>
        /// <param name="map">Matriz bidimensional de celdas del mundo (CellType).</param>
        /// <param name="width">Ancho de la cuadrícula en tiles.</param>
        /// <param name="height">Alto de la cuadrícula en tiles.</param>
        /// <param name="allowBridges">Permite cruzar agua construyendo puentes si es true.</param>
        /// <param name="bridgeCost">Penalización de coste para celdas de agua (fuerza a buscar estrechos).</param>
        /// <returns>Lista secuencial de celdas desde el inicio hasta el objetivo.</returns>
        public static List<Vector2Int> FindPath(
            Vector2Int start, 
            Vector2Int target, 
            CellType[,] map, 
            int width, 
            int height, 
            bool allowBridges = true, 
            float bridgeCost = 5.0f)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            HashSet<Vector2Int> openSet = new HashSet<Vector2Int> { start };
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
            Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();

            gScore[start] = 0f;
            fScore[start] = Vector2Int.Distance(start, target);

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            while (openSet.Count > 0)
            {
                Vector2Int current = default;
                float lowestF = float.MaxValue;
                foreach (var node in openSet)
                {
                    float f = fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;
                    if (f < lowestF)
                    {
                        lowestF = f;
                        current = node;
                    }
                }

                if (current == target)
                {
                    Vector2Int curr = current;
                    while (cameFrom.ContainsKey(curr))
                    {
                        path.Add(curr);
                        curr = cameFrom[curr];
                    }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }

                openSet.Remove(current);

                foreach (var dir in directions)
                {
                    Vector2Int neighbor = current + dir;

                    if (neighbor.x < 0 || neighbor.x >= width || neighbor.y < 0 || neighbor.y >= height)
                        continue;

                    // Si es agua y no se permiten puentes, ignorar
                    if (map[neighbor.x, neighbor.y] == CellType.Water && !allowBridges)
                        continue;

                    // Estructuras: no atravesar estructuras sólidas excepto inicio y destino
                    if (map[neighbor.x, neighbor.y] == CellType.Structure && neighbor != target && neighbor != start)
                        continue;

                    // Cálculo de coste según el tipo de celda
                    float cost = 1.0f; // Terreno estándar (Grass)
                    if (map[neighbor.x, neighbor.y] == CellType.Path)
                    {
                        cost = 0.4f; // Preferir reutilizar carreteras existentes
                    }
                    else if (map[neighbor.x, neighbor.y] == CellType.Bridge)
                    {
                        cost = 0.5f; // Reutilizar puentes ya construidos
                    }
                    else if (map[neighbor.x, neighbor.y] == CellType.Water)
                    {
                        cost = bridgeCost; // Penalización por cruzar agua -> busca el estrecho más angosto entre islas
                    }

                    float tentativeG = gScore[current] + cost;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Vector2Int.Distance(neighbor, target);
                        openSet.Add(neighbor);
                    }
                }
            }
            return path;
        }

        /// <summary>
        /// Encuentra la ruta troncal más óptima a través de la red de carreteras existente,
        /// eligiendo en cada intersección la bifurcación más directa hacia el destino norte
        /// y penalizando giros bruscos y retrocesos.
        /// </summary>
        /// <param name="start">Punto de inicio en el sur de la calzada.</param>
        /// <param name="target">Punto de llegada en el extremo norte.</param>
        /// <param name="map">Matriz de celdas del mundo.</param>
        /// <param name="width">Ancho del mapa en tiles.</param>
        /// <param name="height">Largo del mapa en tiles.</param>
        /// <returns>Ruta óptima sin zigzag ni retrocesos para navegación de cámara.</returns>
        public static List<Vector2Int> FindOptimalSpinePath(
            Vector2Int start,
            Vector2Int target,
            CellType[,] map,
            int width,
            int height)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            HashSet<Vector2Int> openSet = new HashSet<Vector2Int> { start };
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
            Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();

            gScore[start] = 0f;
            fScore[start] = Vector2Int.Distance(start, target);

            Vector2Int[] directions = { Vector2Int.up, Vector2Int.left, Vector2Int.right, Vector2Int.down };

            while (openSet.Count > 0)
            {
                Vector2Int current = default;
                float lowestF = float.MaxValue;
                foreach (var node in openSet)
                {
                    float f = fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;
                    if (f < lowestF)
                    {
                        lowestF = f;
                        current = node;
                    }
                }

                if (current == target)
                {
                    Vector2Int curr = current;
                    while (cameFrom.ContainsKey(curr))
                    {
                        path.Add(curr);
                        curr = cameFrom[curr];
                    }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }

                openSet.Remove(current);

                Vector2Int prevDir = Vector2Int.zero;
                if (cameFrom.ContainsKey(current))
                {
                    prevDir = current - cameFrom[current];
                }

                foreach (var dir in directions)
                {
                    Vector2Int neighbor = current + dir;

                    if (neighbor.x < 0 || neighbor.x >= width || neighbor.y < 0 || neighbor.y >= height)
                        continue;

                    // Coste base según la celda: premiar carreteras y puentes ya existentes
                    float cost = 1.0f;
                    CellType cell = map[neighbor.x, neighbor.y];

                    if (cell == CellType.Path)
                    {
                        cost = 0.2f; // Prioridad máxima: mantenerse en la calzada
                    }
                    else if (cell == CellType.Bridge)
                    {
                        cost = 0.25f; // Prioridad alta: usar puentes existentes
                    }
                    else
                    {
                        cost = 6.0f; // Penalizar fuertemente salirse de la carretera
                    }

                    // Penalización de giro: en intersecciones premia seguir recto hacia el norte y penaliza quiebros
                    if (prevDir != Vector2Int.zero && dir != prevDir)
                    {
                        cost += 0.8f;
                    }

                    // Penalización de retroceso hacia el sur (evita desvíos a callejones sin salida)
                    if (dir.y < 0)
                    {
                        cost += 12.0f;
                    }

                    float tentativeG = gScore[current] + cost;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Vector2Int.Distance(neighbor, target);
                        openSet.Add(neighbor);
                    }
                }
            }

            return path;
        }
    }
}
