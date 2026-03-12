using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapCollider3DOptimized : MonoBehaviour
{
    public Tilemap tilemap;

    private static readonly Vector3 SingleTileOffset = new Vector3(0f, -1f, 0.5f);

    void Start()
    {
        var bounds = tilemap.cellBounds;

        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            int x = bounds.xMin;

            while (x < bounds.xMax)
            {
                if (!tilemap.HasTile(new Vector3Int(x, y, 0)))
                {
                    x++;
                    continue;
                }

                int length = 1;
                while (x + length < bounds.xMax && tilemap.HasTile(new Vector3Int(x + length, y, 0)))
                {
                    length++;
                }

                Vector3 size = new Vector3(length, 1f, 1f);

                Vector3 worldStart = tilemap.CellToWorld(new Vector3Int(x, y, 0)) + tilemap.tileAnchor;

                Vector3 center = worldStart + SingleTileOffset + new Vector3((length - 1) * 0.5f, 0f, 0f);

                GameObject colliderObj = new GameObject($"TileCollider_{x}_{y}_len{length}");
                colliderObj.transform.SetParent(transform, worldPositionStays: true);
                colliderObj.transform.position = center;
                colliderObj.layer = gameObject.layer;

                var box = colliderObj.AddComponent<BoxCollider>();
                box.size = size;

                x += length;
            }
        }
    }
}