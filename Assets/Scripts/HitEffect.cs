using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HitEffect : MonoBehaviour
{
    /// <summary>
    /// エフェクトの継続時間
    /// </summary>
    public float duration;
    
    /// <summary>
    /// 最終的な拡大率
    /// </summary>
    public float targetScale; 

    private float timer = 0.0f;
    private SpriteRenderer objRenderer;
    private Color initialColor;
    private Vector3 initialScale;

    void Start()
    {
        objRenderer = GetComponent<SpriteRenderer>();
        
        initialColor = objRenderer.color; 
        initialScale = transform.localScale;
        initialColor.a = 1.0f;
        objRenderer.color = initialColor;
    }

    void Update()
    {
        timer += Time.deltaTime;
        
        // 0から1への進捗率
        float progress = timer / duration;

        if (progress >= 1.0f)
        {
            Destroy(gameObject);
            return;
        }

        // フェードアウト
        Color newColor = initialColor;
        newColor.a = 1.0f - progress;
        objRenderer.color = newColor;

        // 拡大 (Scaleを 1x -> targetScale
        transform.localScale = Vector3.Lerp(initialScale, initialScale * targetScale, progress);
    }
}