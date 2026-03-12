using UnityEngine;

public class BuildingMaterialSwitcher : MonoBehaviour
{
    public Material Mat_Normal;
    public Material Mat_Ghost;
    public Transform Player;
    public float fadeDuration = 0.4f;
    public float targetGhostAlpha = 0.4f;
    public float checkDistance = 50f;

    private Renderer[] renderers;
    private bool isOccluded = false;
    private bool lastIsOccluded = false;
    private bool isFading = false;
    private float fadeTimer = 0f;

    private Color ghostOriginColor;

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        if (Mat_Ghost.HasProperty("_Color"))
        {
            ghostOriginColor = Mat_Ghost.color;
        }
    }

    void Update()
    {
        if (Player == null) return;

        float distance = Vector3.Distance(transform.position, Player.position);
        if (distance > checkDistance)
        {
            if (isOccluded)
            {
                SwitchToNormalMaterial();
                isOccluded = false;
                lastIsOccluded = false;
            }
            return;
        }

        Camera cam = Camera.main;
        Vector3 viewDir = Player.position - cam.transform.position;
        float dist = viewDir.magnitude;
        Ray ray = new Ray(cam.transform.position, viewDir.normalized);
        bool occluded = false;

        foreach (var hit in Physics.RaycastAll(ray, dist))
        {
            if (hit.transform == this.transform)
            {
                occluded = true;
                break;
            }
        }

        isOccluded = occluded;

        if (isOccluded != lastIsOccluded)
        {
            fadeTimer = 0f;
            isFading = true;
            if (isOccluded)
            {
                SwitchToGhostMaterial();
            }
        }

        if (isFading)
        {
            fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fadeTimer / fadeDuration);

            if (isOccluded)
            {
                foreach (var r in renderers)
                {
                    if (r.material.HasProperty("_Color"))
                    {
                        Color c = r.material.color;
                        r.material.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1, targetGhostAlpha, t));
                    }
                }
                if (t >= 1f) isFading = false;
            }
            else
            {
                foreach (var r in renderers)
                {
                    if (r.material.HasProperty("_Color"))
                    {
                        Color c = r.material.color;
                        r.material.color = new Color(c.r, c.g, c.b, Mathf.Lerp(targetGhostAlpha, 1, t));
                    }
                }
                if (t >= 1f)
                {
                    SwitchToNormalMaterial();
                    isFading = false;
                }
            }
        }

        lastIsOccluded = isOccluded;
    }

    void SwitchToGhostMaterial()
    {
        foreach (var r in renderers)
        {
            r.material = Mat_Ghost;
            if (Mat_Ghost.HasProperty("_Color"))
            {
                r.material.color = new Color(ghostOriginColor.r, ghostOriginColor.g, ghostOriginColor.b, 1f);
            }
        }
    }

    void SwitchToNormalMaterial()
    {
        foreach (var r in renderers)
        {
            r.material = Mat_Normal;
        }
    }
}