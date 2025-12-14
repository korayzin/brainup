using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Dream Clarity (Uyku Netliği) seviyesini realtime olarak UI'da gösterir
/// Dream küresinin yakınında dinamik olarak konumlanır
/// </summary>
public class DreamClarityUIManager : MonoBehaviour
{
    [Header("UI Referansları")]
    [Tooltip("Clarity değerini gösteren text")]
    public TextMeshProUGUI clarityValueText;
    
    [Tooltip("Progress bar fill image (soldan sağa)")]
    public Image progressBarFill;
    
    [Header("DreamLogicController Referansı")]
    [Tooltip("DreamLogicController (otomatik bulunur)")]
    public DreamLogicController dreamLogicController;
    
    [Header("Dream Mesh Referansı")]
    [Tooltip("Dream mesh GameObject (otomatik bulunur)")]
    public Transform dreamMeshTransform;
    
    [Tooltip("Dream mesh'in yanına offset (world space)")]
    public Vector3 worldSpaceOffset = new Vector3(0.5f, 0f, 0f);
    
    [Header("Görsel Ayarlar")]
    [Tooltip("Değer güncelleme hızı (saniye)")]
    [Range(0.01f, 0.5f)]
    public float updateSmoothTime = 0.1f;
    
    [Tooltip("Bulanık rüya rengi (blur yüksek)")]
    public Color blurryDreamColor = new Color(1f, 0.3f, 0.3f, 1f); // Kırmızı
    
    [Tooltip("Net rüya rengi (blur düşük)")]
    public Color clearDreamColor = new Color(0.3f, 1f, 0.3f, 1f); // Yeşil
    
    [Tooltip("Orta seviye rengi")]
    public Color mediumDreamColor = new Color(0.3f, 0.7f, 1f, 1f); // Mavi
    
    [Header("UI Pozisyon Ayarları")]
    [Tooltip("Dinamik pozisyonlama kullan (dream mesh'ini takip et)")]
    public bool useDynamicPositioning = true;
    
    [Tooltip("Manuel pozisyon (dinamik pozisyonlama kapalıysa kullanılır)")]
    public Vector2 manualPosition = new Vector2(-50f, 0f);
    
    [Header("UI Rotasyon Ayarları")]
    [Tooltip("UI rotasyonu (derece)")]
    [Range(-180f, 180f)]
    public float uiRotation = 0f;
    
    [Tooltip("Dinamik rotasyon kullan (dream mesh'inin rotasyonunu takip et)")]
    public bool useDynamicRotation = false;
    
    [Tooltip("Dinamik rotasyon offset (dream rotasyonuna eklenir)")]
    [Range(-180f, 180f)]
    public float dynamicRotationOffset = 0f;
    
    [Tooltip("Ekran kenarına minimum mesafe (pixel)")]
    public float minScreenEdgeDistance = 20f;
    
    // Smooth değerler
    private float smoothClarityValue = 0f;
    private float smoothClarityVelocity = 0f;
    
    // Başlangıç blur değeri
    private float initialBlurAmount = 0.8f;
    private float targetBlurAmount = 0f; // Hedef: 0 (tam net)
    
    // UI ve Camera referansları
    private RectTransform uiRectTransform;
    private Camera mainCamera;
    private Canvas canvas;
    
    void Start()
    {
        // UI RectTransform'u al
        uiRectTransform = GetComponent<RectTransform>();
        
        // Canvas'ı bul
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }
        
        // Main Camera'yı bul
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // DreamLogicController'ı bul (eğer atanmamışsa)
        if (dreamLogicController == null)
        {
            dreamLogicController = FindObjectOfType<DreamLogicController>();
        }
        
        // Dream mesh'ini bul
        if (dreamMeshTransform == null && dreamLogicController != null)
        {
            // DreamLogicController'dan dreamRenderer'ı al
            var dreamRendererField = typeof(DreamLogicController).GetField("dreamRenderer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (dreamRendererField != null)
            {
                Renderer dreamRenderer = dreamRendererField.GetValue(dreamLogicController) as Renderer;
                if (dreamRenderer != null)
                {
                    dreamMeshTransform = dreamRenderer.transform;
                }
            }
            
            // Eğer hala bulunamadıysa, sahnede "Dream" isimli objeyi ara
            if (dreamMeshTransform == null)
            {
                GameObject dreamObj = GameObject.Find("Dream");
                if (dreamObj == null)
                {
                    dreamObj = GameObject.FindGameObjectWithTag("Dream");
                }
                if (dreamObj != null)
                {
                    dreamMeshTransform = dreamObj.transform;
                }
            }
        }
        
        // Başlangıç değerlerini al
        if (dreamLogicController != null)
        {
            // Reflection ile private değişkenlere eriş
            var currentBlurField = typeof(DreamLogicController).GetField("currentBlurAmount", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (currentBlurField != null)
            {
                initialBlurAmount = (float)currentBlurField.GetValue(dreamLogicController);
                smoothClarityValue = initialBlurAmount;
            }
        }
        
        // Başlangıç UI güncellemesi
        UpdateUI(smoothClarityValue);
        
        // Dinamik pozisyonlama başlangıç güncellemesi
        if (useDynamicPositioning)
        {
            UpdateDynamicPosition();
        }
        else
        {
            // Manuel pozisyon kullan
            if (uiRectTransform != null)
            {
                uiRectTransform.anchoredPosition = manualPosition;
            }
        }
        
        // Rotasyon güncellemesi
        UpdateRotation();
    }
    
    void Update()
    {
        if (dreamLogicController == null)
        {
            return;
        }
        
        // Reflection ile currentBlurAmount'u al
        var currentBlurField = typeof(DreamLogicController).GetField("currentBlurAmount", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (currentBlurField != null)
        {
            float currentBlur = (float)currentBlurField.GetValue(dreamLogicController);
            
            // Smooth interpolation
            smoothClarityValue = Mathf.SmoothDamp(smoothClarityValue, currentBlur, 
                ref smoothClarityVelocity, updateSmoothTime);
            
            // UI'ı güncelle
            UpdateUI(smoothClarityValue);
        }
        
        // Dinamik pozisyonlama güncellemesi
        if (useDynamicPositioning)
        {
            UpdateDynamicPosition();
        }
        else
        {
            // Manuel pozisyon kullan
            if (uiRectTransform != null)
            {
                uiRectTransform.anchoredPosition = manualPosition;
            }
        }
        
        // Rotasyon güncellemesi
        UpdateRotation();
    }
    
    /// <summary>
    /// UI'ı dream mesh'inin yakınına dinamik olarak konumlandır
    /// </summary>
    private void UpdateDynamicPosition()
    {
        if (dreamMeshTransform == null || mainCamera == null || canvas == null || uiRectTransform == null)
        {
            return;
        }
        
        // Dream mesh'inin bounds'ını al (daha doğru pozisyon için)
        Renderer dreamRenderer = dreamMeshTransform.GetComponent<Renderer>();
        Vector3 dreamWorldPos = dreamMeshTransform.position;
        Vector3 offsetPos = dreamWorldPos;
        
        if (dreamRenderer != null)
        {
            Bounds bounds = dreamRenderer.bounds;
            // Sağ taraf: bounds'ın sağ üst köşesi
            offsetPos = new Vector3(bounds.max.x, bounds.center.y, bounds.center.z);
        }
        else
        {
            // Renderer yoksa transform pozisyonunu kullan ve offset uygula
            offsetPos = dreamWorldPos + dreamMeshTransform.TransformDirection(worldSpaceOffset);
        }
        
        // World space pozisyonu screen space'e çevir
        Vector3 screenPos = mainCamera.WorldToScreenPoint(offsetPos);
        
        // Ekran dışındaysa görünmez yap
        if (screenPos.z < 0f || screenPos.x < 0f || screenPos.x > Screen.width || 
            screenPos.y < 0f || screenPos.y > Screen.height)
        {
            // Ekran dışında, UI'ı gizle veya ekran kenarına taşı
            uiRectTransform.gameObject.SetActive(false);
            return;
        }
        
        uiRectTransform.gameObject.SetActive(true);
        
        // Canvas RectTransform'unu al
        RectTransform canvasRect = canvas.transform as RectTransform;
        
        // Canvas'ın render mode'una göre pozisyonu ayarla
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // Screen space overlay için direkt screen position kullan
            uiRectTransform.position = new Vector3(screenPos.x, screenPos.y, 0f);
        }
        else
        {
            // Screen space camera veya world space için RectTransformUtility kullan
            Vector2 localPoint;
            Camera canvasCamera = canvas.worldCamera != null ? canvas.worldCamera : mainCamera;
            
            if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, canvasCamera, out localPoint))
            {
                uiRectTransform.anchoredPosition = localPoint;
            }
        }
        
        // Ekran kenarlarına çok yakınsa offset uygula
        if (canvasRect != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Vector2 currentPos = uiRectTransform.anchoredPosition;
            Vector2 canvasSize = canvasRect.sizeDelta;
            
            // Sağ kenar kontrolü
            float uiWidth = uiRectTransform.sizeDelta.x;
            if (currentPos.x > -minScreenEdgeDistance)
            {
                currentPos.x = -minScreenEdgeDistance;
            }
            
            // Sol kenar kontrolü (eğer UI çok genişse)
            if (currentPos.x - uiWidth < -canvasSize.x + minScreenEdgeDistance)
            {
                currentPos.x = -canvasSize.x + uiWidth + minScreenEdgeDistance;
            }
            
            // Üst kenar kontrolü
            float uiHeight = uiRectTransform.sizeDelta.y;
            if (currentPos.y > -minScreenEdgeDistance)
            {
                currentPos.y = -minScreenEdgeDistance;
            }
            
            // Alt kenar kontrolü (eğer UI çok yüksekse)
            if (currentPos.y - uiHeight < -canvasSize.y + minScreenEdgeDistance)
            {
                currentPos.y = -canvasSize.y + uiHeight + minScreenEdgeDistance;
            }
            
            uiRectTransform.anchoredPosition = currentPos;
        }
    }
    
    /// <summary>
    /// UI rotasyonunu güncelle
    /// </summary>
    private void UpdateRotation()
    {
        if (uiRectTransform == null)
        {
            return;
        }
        
        float targetRotation = uiRotation;
        
        // Dinamik rotasyon kullanılıyorsa
        if (useDynamicRotation && dreamMeshTransform != null)
        {
            // Dream mesh'inin rotasyonunu al ve offset ekle
            float dreamRotation = dreamMeshTransform.eulerAngles.y; // Y ekseni rotasyonu
            targetRotation = dreamRotation + dynamicRotationOffset;
        }
        
        // Rotasyonu uygula
        uiRectTransform.localEulerAngles = new Vector3(0f, 0f, targetRotation);
    }
    
    /// <summary>
    /// UI'ı güncelle
    /// </summary>
    private void UpdateUI(float blurValue)
    {
        // Blur değerinden Clarity yüzdesini hesapla
        // Blur yüksek = clarity düşük, blur düşük = clarity yüksek
        // Blur 0.8 (başlangıç) = 0% clarity, Blur 0.0 (hedef) = 100% clarity
        float clarityProgress = Mathf.InverseLerp(initialBlurAmount, targetBlurAmount, blurValue);
        clarityProgress = Mathf.Clamp01(clarityProgress);
        float clarityPercentage = clarityProgress * 100f;
        
        // Renk hesaplama (blur düştükçe yeşile, yükseldikçe kırmızıya)
        float colorT = clarityProgress;
        
        Color currentColor;
        if (colorT < 0.5f)
        {
            // Kırmızıdan maviye
            currentColor = Color.Lerp(blurryDreamColor, mediumDreamColor, colorT * 2f);
        }
        else
        {
            // Maviden yeşile
            currentColor = Color.Lerp(mediumDreamColor, clearDreamColor, (colorT - 0.5f) * 2f);
        }
        
        // Text güncelle
        if (clarityValueText != null)
        {
            clarityValueText.text = $"{clarityPercentage:F0}%";
            clarityValueText.color = currentColor;
        }
        
        // Progress bar güncelle (soldan sağa)
        if (progressBarFill != null)
        {
            // Progress: blur düştükçe (clarity arttıkça) bar soldan sağa dolsun
            float progress = clarityProgress;
            
            progressBarFill.fillAmount = progress;
            progressBarFill.color = currentColor;
        }
    }
}

