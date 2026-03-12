using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

[ExecuteAlways]
public class SpriteShadow : MonoBehaviour
{
    void OnEnable()
    {
        if (TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.receiveShadows = true;
            spriteRenderer.shadowCastingMode = ShadowCastingMode.TwoSided;
        }
        else if (TryGetComponent(out TilemapRenderer tilemapRenderer))
        {
            tilemapRenderer.receiveShadows = true;
            tilemapRenderer.shadowCastingMode = ShadowCastingMode.TwoSided;
        }
    }
}
