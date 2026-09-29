using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace WorldGen.Data
{
    public enum RoadSocketType
    {
        None,
        Dirt,
        Stone
    }

    public struct RoadTileSocket
    {
        public RoadSocketType north;
        public RoadSocketType south;
        public RoadSocketType west;
        public RoadSocketType east;

        public RoadTileSocket(RoadSocketType n, RoadSocketType s, RoadSocketType w, RoadSocketType e)
        {
            north = n;
            south = s;
            west = w;
            east = e;
        }
    }

    [Serializable]
    public class WfcTileEntry
    {
        public int id;
        public string spriteName;
        public Sprite sprite;
        public TileBase tile;
        public Rect rectInAtlas;

        // Categoría o Módulo al que pertenece
        public string category = "Prado";

        // Rol específico dentro del autotile (Outer_TL, Top, Center, Inner_TL, etc.)
        public string role = "";

        // Listas de IDs de tiles permitidos en cada dirección
        public List<int> allowedNorth = new List<int>();
        public List<int> allowedSouth = new List<int>();
        public List<int> allowedEast  = new List<int>();
        public List<int> allowedWest  = new List<int>();

        public float weight = 1f;
    }


    [CreateAssetMenu(fileName = "WfcTilesetDatabase", menuName = "WorldGen/WFC Visual Database")]
    public class WfcVisualDatabase : ScriptableObject
    {
        public Texture2D atlasTexture;
        public List<WfcTileEntry> tiles = new List<WfcTileEntry>();

        // Lista de módulos / categorías predefinidas
        public List<string> categories = new List<string>
        {
            "Prado",
            "Camino",
            "Camino_Piedra",
            "Agua_Clara",
            "Agua_Turbia",
            "Bosque",
            "Cultivo",
            "Flores",
            "Monticulo",
            "Fuerte",
            "Iglesia",
            "Torre",
            "Cueva",
            "Meseta",
            "Decoracion",
            "Puente"
        };

        public void EnsureCategories()
        {
            if (categories == null) categories = new List<string>();
            string[] defaultCats = { "Prado", "Camino", "Camino_Piedra", "Agua_Clara", "Agua_Turbia", "Bosque", "Cultivo", "Flores", "Monticulo", "Fuerte", "Iglesia", "Torre", "Cueva", "Meseta", "Decoracion", "Puente" };
            foreach (var d in defaultCats)
            {
                if (!categories.Contains(d)) categories.Add(d);
            }
            if (tiles != null)
            {
                foreach (var t in tiles)
                {
                    if (!string.IsNullOrEmpty(t.category) && !categories.Contains(t.category))
                    {
                        categories.Add(t.category);
                    }
                }
            }
        }

        public WfcTileEntry GetTileById(int id)
        {
            return tiles.FirstOrDefault(t => t.id == id);
        }

        public List<WfcTileEntry> GetTilesByCategory(string cat)
        {
            if (string.IsNullOrEmpty(cat)) return new List<WfcTileEntry>();
            string cleanCat = cat.Trim();
            return tiles.Where(t => t.category != null && string.Equals(t.category.Trim(), cleanCat, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public TileBase GetRandomTileByCategory(string cat)
        {
            var list = GetTilesByCategory(cat);
            if (list == null || list.Count == 0) return null;
            return list[UnityEngine.Random.Range(0, list.Count)].tile;
        }

        public TileBase GetPathTile(bool up, bool down, bool left, bool right, string pathCategory = "Camino_Piedra")
        {
            // 1. Intentar por rol explícito de carretera (15 roles exactos del Asistente)
            TileBase tile = GetRoadTileByRole(up, down, left, right, pathCategory);
            if (tile != null) return tile;

            // 2. Intentar con vecinos configurados
            tile = GetAutotile(up, down, left, right, pathCategory);
            if (tile != null) return tile;

            // 3. Fallback inteligente entre Camino_Piedra y Camino
            string fallbackCat = null;
            if (pathCategory == "Camino") fallbackCat = "Camino_Piedra";
            else if (pathCategory == "Camino_Piedra") fallbackCat = "Camino";
            else if (pathCategory == "Puente") fallbackCat = "Camino_Piedra";

            if (!string.IsNullOrEmpty(fallbackCat))
            {
                tile = GetRoadTileByRole(up, down, left, right, fallbackCat);
                if (tile != null) return tile;

                tile = GetAutotile(up, down, left, right, fallbackCat);
                if (tile != null) return tile;
            }

            // Si aún no hay coincidencia, tomar cualquier tile disponible de la categoría
            var catTiles = GetTilesByCategory(pathCategory);
            if (catTiles != null && catTiles.Count > 0 && catTiles[0].tile != null)
            {
                return catTiles[0].tile;
            }

            if (!string.IsNullOrEmpty(fallbackCat))
            {
                var fbTiles = GetTilesByCategory(fallbackCat);
                if (fbTiles != null && fbTiles.Count > 0 && fbTiles[0].tile != null)
                {
                    return fbTiles[0].tile;
                }
            }

            return null;
        }

        public TileBase GetRoadTileByRole(bool up, bool down, bool left, bool right, string category)
        {
            var catTiles = GetTilesByCategory(category);
            if (catTiles == null || catTiles.Count == 0) return null;

            string targetRole = "";
            string fallbackRole = "";

            int count = (up ? 1 : 0) + (down ? 1 : 0) + (left ? 1 : 0) + (right ? 1 : 0);

            if (count == 4)
            {
                targetRole = "Road_Cross";
            }
            else if (count == 3)
            {
                if (!up) targetRole = "Road_T_Up";
                else if (!down) targetRole = "Road_T_Down";
                else if (!left) targetRole = "Road_T_Left";
                else if (!right) targetRole = "Road_T_Right";
            }
            else if (count == 2)
            {
                if (up && down) targetRole = "Road_Straight_V";
                else if (left && right) targetRole = "Road_Straight_H";
                else if (down && right) targetRole = "Road_Corner_TL";
                else if (down && left) targetRole = "Road_Corner_TR";
                else if (up && right) targetRole = "Road_Corner_BL";
                else if (up && left) targetRole = "Road_Corner_BR";
            }
            else if (count == 1)
            {
                bool isStone = category.ToLower().Contains("piedra");
                if (down)
                {
                    targetRole = isStone ? "Road_Trans_N" : "Road_Cap_N";
                    fallbackRole = isStone ? "Road_Cap_N" : "Road_Trans_N";
                }
                else if (up)
                {
                    targetRole = isStone ? "Road_Trans_S" : "Road_Cap_S";
                    fallbackRole = isStone ? "Road_Cap_S" : "Road_Trans_S";
                }
                else if (right)
                {
                    targetRole = isStone ? "Road_Trans_W" : "Road_Cap_W";
                    fallbackRole = isStone ? "Road_Cap_W" : "Road_Trans_W";
                }
                else if (left)
                {
                    targetRole = isStone ? "Road_Trans_E" : "Road_Cap_E";
                    fallbackRole = isStone ? "Road_Cap_E" : "Road_Trans_E";
                }
            }
            else
            {
                targetRole = "Road_Straight_H";
                fallbackRole = "Road_Straight_V";
            }

            var match = catTiles.FirstOrDefault(t => t.role == targetRole && t.tile != null);
            if (match != null) return match.tile;

            if (!string.IsNullOrEmpty(fallbackRole))
            {
                var fbMatch = catTiles.FirstOrDefault(t => t.role == fallbackRole && t.tile != null);
                if (fbMatch != null) return fbMatch.tile;
            }

            return null;
        }

        public TileBase GetAutotile(bool up, bool down, bool left, bool right, string category)
        {
            var catTiles = GetTilesByCategory(category);
            if (catTiles == null || catTiles.Count == 0) return null;

            foreach (var t in catTiles)
            {
                bool connectsUp    = t.allowedNorth.Any(id => GetTileById(id)?.category == category);
                bool connectsDown  = t.allowedSouth.Any(id => GetTileById(id)?.category == category);
                bool connectsLeft  = t.allowedWest.Any(id => GetTileById(id)?.category == category);
                bool connectsRight = t.allowedEast.Any(id => GetTileById(id)?.category == category);

                if (connectsUp == up && connectsDown == down && connectsLeft == left && connectsRight == right)
                {
                    return t.tile;
                }
            }

            return catTiles[0].tile;
        }

        public TileBase GetWaterAutotile(bool N, bool S, bool W, bool E, bool NW, bool NE, bool SW, bool SE, string category = "Agua_Clara")

        {
            var catTiles = GetTilesByCategory(category);
            if (catTiles == null || catTiles.Count == 0) return null;

            string targetRole = "Center";

            // 1. Esquinas exteriores (Outer Corners)

            if (!N && !W) targetRole = "Outer_TL";
            else if (!N && !E) targetRole = "Outer_TR";
            else if (!S && !W) targetRole = "Outer_BL";
            else if (!S && !E) targetRole = "Outer_BR";

            // 2. Orillas rectas (Shores)
            else if (!N) targetRole = "Top";
            else if (!S) targetRole = "Bottom";
            else if (!W) targetRole = "Left";
            else if (!E) targetRole = "Right";

            // 3. Esquinas interiores (Concave Corners) o Centro de fondo
            else
            {
                if (!NW) targetRole = "Inner_TL";
                else if (!NE) targetRole = "Inner_TR";
                else if (!SW) targetRole = "Inner_BL";
                else if (!SE) targetRole = "Inner_BR";
                else targetRole = "Center";
            }

            // Buscar primero por role explícito configurado
            var match = catTiles.FirstOrDefault(t => t.role == targetRole);
            if (match != null && match.tile != null)
            {
                return match.tile;
            }

            // Fallback al centro o primer tile
            var centerTile = catTiles.FirstOrDefault(t => t.role == "Center");
            if (centerTile != null && centerTile.tile != null) return centerTile.tile;
            return catTiles[0].tile;

        }



        public void AddNeighbor(int fromId, int toId, Vector2Int dir, bool reciprocal = true)
        {
            var from = GetTileById(fromId);
            var to = GetTileById(toId);
            if (from == null || to == null) return;

            if (dir == Vector2Int.up && !from.allowedNorth.Contains(toId))
            {
                from.allowedNorth.Add(toId);
                if (reciprocal && !to.allowedSouth.Contains(fromId)) to.allowedSouth.Add(fromId);
            }
            else if (dir == Vector2Int.down && !from.allowedSouth.Contains(toId))
            {
                from.allowedSouth.Add(toId);
                if (reciprocal && !to.allowedNorth.Contains(fromId)) to.allowedNorth.Add(fromId);
            }
            else if (dir == Vector2Int.right && !from.allowedEast.Contains(toId))
            {
                from.allowedEast.Add(toId);
                if (reciprocal && !to.allowedWest.Contains(fromId)) to.allowedWest.Add(fromId);
            }
            else if (dir == Vector2Int.left && !from.allowedWest.Contains(toId))
            {
                from.allowedWest.Add(toId);
                if (reciprocal && !to.allowedEast.Contains(fromId)) to.allowedEast.Add(fromId);
            }
        }

        public void RemoveNeighbor(int fromId, int toId, Vector2Int dir, bool reciprocal = true)
        {
            var from = GetTileById(fromId);
            var to = GetTileById(toId);
            if (from == null) return;

            if (dir == Vector2Int.up)
            {
                from.allowedNorth.Remove(toId);
                if (reciprocal && to != null) to.allowedSouth.Remove(fromId);
            }
            else if (dir == Vector2Int.down)
            {
                from.allowedSouth.Remove(toId);
                if (reciprocal && to != null) to.allowedNorth.Remove(fromId);
            }
            else if (dir == Vector2Int.right)
            {
                from.allowedEast.Remove(toId);
                if (reciprocal && to != null) to.allowedWest.Remove(fromId);
            }
            else if (dir == Vector2Int.left)
            {
                from.allowedWest.Remove(toId);
                if (reciprocal && to != null) to.allowedEast.Remove(fromId);
            }
        }
    }
}
