using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum PanelDirection
{
    Yukari,
    Asagi,
    Sagdan,
    Soldan
}

public class InfoPanelToggle : MonoBehaviour
{
    [Header("Panel Ayarları")]
    [Tooltip("Açılacak panel objesi (RectTransform)")]
    public RectTransform panel;
    
    [Header("Açılma Yönü")]
    [Tooltip("Panel hangi yöne açılacak?")]
    public PanelDirection openDirection = PanelDirection.Yukari;
    
    [Tooltip("Panel açıkken ne kadar kayacak (pixel)")]
    public float openOffset = 500f;
    
    [Tooltip("Animasyon süresi (saniye)")]
    public float animationDuration = 0.3f;
    
    [Header("Animasyon Eğrisi")]
    [Tooltip("Animasyon eğrisi (varsayılan: smooth)")]
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private Vector2 closedPosition;
    private Vector2 openPosition;
    private bool isOpen = false;
    private bool isAnimating = false;
    
    void Start()
    {
        // Panel referansı yoksa, bu objenin RectTransform'unu kullan
        if (panel == null)
        {
            panel = GetComponent<RectTransform>();
        }
        
        // Kapalı pozisyonu kaydet
        closedPosition = panel.anchoredPosition;
        
        // Açık pozisyonu hesapla (seçilen yöne göre)
        openPosition = CalculateOpenPosition(closedPosition);
        
        // Başlangıçta kapalı olsun
        panel.anchoredPosition = closedPosition;
    }
    
    // Seçilen yöne göre açık pozisyonu hesapla
    private Vector2 CalculateOpenPosition(Vector2 basePosition)
    {
        switch (openDirection)
        {
            case PanelDirection.Yukari:
                return basePosition + new Vector2(0, openOffset);
            case PanelDirection.Asagi:
                return basePosition + new Vector2(0, -openOffset);
            case PanelDirection.Sagdan:
                return basePosition + new Vector2(openOffset, 0);
            case PanelDirection.Soldan:
                return basePosition + new Vector2(-openOffset, 0);
            default:
                return basePosition + new Vector2(0, openOffset);
        }
    }
    
    // Panel üzerine tıklandığında çağrılacak metod
    public void TogglePanel()
    {
        if (isAnimating) return; // Animasyon devam ediyorsa yeni animasyon başlatma
        
        isOpen = !isOpen;
        StartCoroutine(AnimatePanel());
    }
    
    // Panel animasyonu
    private IEnumerator AnimatePanel()
    {
        isAnimating = true;
        
        Vector2 startPos = panel.anchoredPosition;
        Vector2 targetPos = isOpen ? openPosition : closedPosition;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            
            // Eğri kullanarak smooth animasyon
            float curveValue = animationCurve.Evaluate(t);
            
            panel.anchoredPosition = Vector2.Lerp(startPos, targetPos, curveValue);
            
            yield return null;
        }
        
        // Son pozisyonu garanti et
        panel.anchoredPosition = targetPos;
        isAnimating = false;
    }
    
    // Dışarıdan açmak için
    public void OpenPanel()
    {
        if (!isOpen && !isAnimating)
        {
            isOpen = true;
            StartCoroutine(AnimatePanel());
        }
    }
    
    // Dışarıdan kapatmak için
    public void ClosePanel()
    {
        if (isOpen && !isAnimating)
        {
            isOpen = false;
            StartCoroutine(AnimatePanel());
        }
    }
}
