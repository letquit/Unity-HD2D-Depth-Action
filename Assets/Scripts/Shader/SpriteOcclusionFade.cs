using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteOcclusionFade : MonoBehaviour
{
    [Header("Material")]
    public Material defaultMaterial;
    public Material fadeMaterial;
    
    [Header("Player")]
    public Transform player;
    public Camera targetCamera;
    
    [Header("Settings")]
    public float fadeDuration = 0.3f;
    public float targetAlpha = 0.4f;
    public float checkDistance = 50f;
    public float detectionWidth = 3f; // 检测宽度（X轴范围）
    
    [Header("ShaderPropertyName")]
    public string fadePropertyName = "_FadeAlpha";
    
    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;
    private bool isOccluded = false;
    private float currentAlpha = 1f;
    private float fadeTimer = 0f;
    private int fadePropertyID;
    private int playerLayerMask;
    private int treeLayerMask;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        fadePropertyID = Shader.PropertyToID(fadePropertyName);
        
        currentAlpha = 1f;
        SetAlpha(1f);
        fadeTimer = 1f;
        
        playerLayerMask = ~(1 << LayerMask.NameToLayer("Player"));
        
        treeLayerMask = 1 << LayerMask.NameToLayer("Tree");
    }

    void LateUpdate()
    {
        float distToCam = Vector3.Distance(transform.position, targetCamera.transform.position);
        if (distToCam > checkDistance)
        {
            SetOccluded(false);
            return;
        }
        
        if (player.position.z <= transform.position.z)
        {
            SetOccluded(false);
            return;
        }
        
        float xDistance = Mathf.Abs(player.position.x - transform.position.x);
        if (xDistance > detectionWidth)
        {
            SetOccluded(false);
            return;
        }
        
        Vector3 direction = (player.position - targetCamera.transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(targetCamera.transform.position, player.position);
        
        RaycastHit2D hit = Physics2D.Raycast(
            targetCamera.transform.position,
            direction,
            distanceToPlayer,
            playerLayerMask
        );
        
        bool shouldBeOccluded = false;
        if (hit.collider != null && hit.collider.gameObject == this.gameObject)
        {
            float distToHit = Vector3.Distance(targetCamera.transform.position, hit.point);
            if (distToHit < distanceToPlayer)
            {
                shouldBeOccluded = true;
            }
        }
        
        SetOccluded(shouldBeOccluded);
    }
    
    void SetOccluded(bool occluded)
    {
        if (isOccluded == occluded) return;
        
        isOccluded = occluded;
        fadeTimer = 0f;
        
        if (isOccluded)
        {
            if (fadeMaterial != null)
                spriteRenderer.material = fadeMaterial;
        }
        else
        {
            if (defaultMaterial != null)
                spriteRenderer.material = defaultMaterial;
        }
    }
    
    void Update()
    {
        if (fadeTimer < 1f)
        {
            fadeTimer += Time.deltaTime / fadeDuration;
            float t = Mathf.Clamp01(fadeTimer);
            
            currentAlpha = isOccluded 
                ? Mathf.Lerp(1f, targetAlpha, t)
                : Mathf.Lerp(targetAlpha, 1f, t);
            
            SetAlpha(currentAlpha);
        }
    }
    
    void SetAlpha(float alpha)
    {
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(fadePropertyID, alpha);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }
}