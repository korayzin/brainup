using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// UI Image elementlerine hover efekti ekler (scale ve renk değişimi)
/// </summary>
[RequireComponent(typeof(Image))]
public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Ayarları")]
    [Tooltip("Hover sırasında ne kadar büyüyecek? (1.1 = %10 büyüme)")]
    [SerializeField] private float hoverScale = 1.1f;
    
    [Tooltip("Scale animasyon hızı")]
    [SerializeField] private float scaleAnimationSpeed = 10f;
    
    [Header("Renk Ayarları")]
    [Tooltip("Hover sırasında renk değişimi aktif mi?")]
    [SerializeField] private bool useColorChange = true;
    
    [Tooltip("Hover sırasında alınacak renk (alpha değişmez)")]
    [SerializeField] private Color hoverColor = new Color(1.2f, 1.2f, 1.2f, 1f);
    
    [Tooltip("Renk animasyon hızı")]
    [SerializeField] private float colorAnimationSpeed = 10f;
    
    private Image image;
    private Vector3 defaultScale;
    private Vector3 targetScale;
    private Color defaultColor;
    private Color targetColor;

    void Awake()
    {
        image = GetComponent<Image>();
        
        if (image == null)
        {
            Debug.LogWarning($"UIHoverEffect: {gameObject.name} üzerinde Image component bulunamadı!");
            enabled = false;
            return;
        }
        
        defaultScale = transform.localScale;
        targetScale = defaultScale;
        defaultColor = image.color;
        targetColor = defaultColor;
    }

    void Update()
    {
        // Scale animasyonu
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * scaleAnimationSpeed
        );
        
        // Renk animasyonu
        if (useColorChange && image != null)
        {
            image.color = Color.Lerp(
                image.color,
                targetColor,
                Time.unscaledDeltaTime * colorAnimationSpeed
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = defaultScale * hoverScale;
        
        if (useColorChange)
        {
            // Alpha'yı koruyarak renk değiştir
            targetColor = new Color(
                hoverColor.r,
                hoverColor.g,
                hoverColor.b,
                defaultColor.a
            );
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = defaultScale;
        targetColor = defaultColor;
    }
}
