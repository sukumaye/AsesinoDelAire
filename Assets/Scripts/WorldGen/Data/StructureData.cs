using UnityEngine;
using UnityEngine.Tilemaps;

namespace WorldGen.Data
{
    [CreateAssetMenu(fileName = "NuevaEstructura", menuName = "WorldGen/Structure Data")]
    public class StructureData : ScriptableObject
    {
        public string structureName = "Estructura";
        public bool hasDoor = true;
        public Vector2Int size = new Vector2Int(4, 4);
        public Vector2Int doorOffset = new Vector2Int(2, 0);
        public TileBase[] tiles;

        public TileBase GetTile(int x, int y)
        {
            if (tiles == null || tiles.Length == 0) return null;
            if (x < 0 || x >= size.x || y < 0 || y >= size.y) return null;
            int index = y * size.x + x;
            if (index >= 0 && index < tiles.Length)
                return tiles[index];
            return null;
        }

        public void SetTile(int x, int y, TileBase tile)
        {
            if (x < 0 || x >= size.x || y < 0 || y >= size.y) return;
            int targetLength = size.x * size.y;
            if (tiles == null || tiles.Length != targetLength)
            {
                Resize(size);
            }
            int index = y * size.x + x;
            if (index >= 0 && index < tiles.Length)
            {
                tiles[index] = tile;
            }
        }

        public void Resize(Vector2Int newSize)
        {
            newSize.x = Mathf.Max(1, newSize.x);
            newSize.y = Mathf.Max(1, newSize.y);

            TileBase[] newTiles = new TileBase[newSize.x * newSize.y];

            if (tiles != null && size.x > 0 && size.y > 0)
            {
                for (int y = 0; y < Mathf.Min(size.y, newSize.y); y++)
                {
                    for (int x = 0; x < Mathf.Min(size.x, newSize.x); x++)
                    {
                        int oldIdx = y * size.x + x;
                        int newIdx = y * newSize.x + x;
                        if (oldIdx < tiles.Length)
                        {
                            newTiles[newIdx] = tiles[oldIdx];
                        }
                    }
                }
            }

            size = newSize;
            tiles = newTiles;

            // Asegurar que doorOffset queda dentro de los límites
            doorOffset.x = Mathf.Clamp(doorOffset.x, 0, size.x - 1);
            doorOffset.y = Mathf.Clamp(doorOffset.y, 0, size.y - 1);
        }
    }
}

