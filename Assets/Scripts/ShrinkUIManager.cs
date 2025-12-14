using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Shrink seviyesini realtime olarak UI'da gösterir
/// Beyin mesh'inin sol üstünde dinamik olarak konumlanır
/// </summary>
public class ShrinkUIManager : MonoBehaviour
{
    [Header("UI Referansları")]
    [Tooltip("Shrink değerini gösteren text")]
    public TextMeshProUGUI shrinkValueText;
    
    [Tooltip("Progress bar fill image")]
    public Image progressBarFill;
    
    [Header("DreamLogicController Referansı")]
    [Tooltip("DreamLogicController (otomatik bulunur)")]
    public DreamLogicController dreamLogicController;
    
    [Header("Beyin Mesh Referansı")]
    [Tooltip("Beyin mesh GameObject (otomatik bulunur)")]
    public Transform brainMeshTransform;
    
    [Tooltip("Beyin mesh'in sol üstüne offset (world space)")]
    public Vector3 worldSpaceOffset = new Vector3(-0.5f, 0.5f, 0f);
    
    [Header("UI Pozisyon Ayarları")]
    [Tooltip("Dinamik pozisyonlama kullan (beyin mesh'ini takip et)")]
    public bool useDynamicPositioning = true;
    
    [Tooltip("Manuel pozisyon (dinamik pozisyonlama kapalıysa kullanılır)")]
    public Vector2 manualPosition = new Vector2(50f, -50f);
    
    [Header("UI Rotasyon Ayarları")]
    [Tooltip("UI rotasyonu (derece)")]
    [Range(-180f, 180f)]
    public float uiRotation = 0f;
    
    [Tooltip("Dinamik rotasyon kullan (beyin mesh'inin rotasyonunu takip et)")]
    public bool useDynamicRotation = false;
    
    [Tooltip("Dinamik rotasyon offset (beyin rotasyonuna eklenir)")]
    [Range(-180f, 180f)]
    public float dynamicRotationOffset = 0f;
    
    [Header("Görsel Ayarlar")]
    [Tooltip("Değer güncelleme hızı (saniye)")]
    [Range(0.01f, 0.5f)]
    public float updateSmoothTime = 0.1f;
    
    [Tooltip("Küçük beyin rengi (shrink yüksek)")]
    public Color smallBrainColor = new Color(1f, 0.3f, 0.3f, 1f); // Kırmızı
    
    [Tooltip("Büyük beyin rengi (shrink düşük)")]
    public Color largeBrainColor = new Color(0.3f, 1f, 0.3f, 1f); // Yeşil
    
    [Tooltip("Orta seviye rengi")]
    public Color mediumBrainColor = new Color(1f, 0.7f, 0.3f, 1f); // Turuncu
    
    [Tooltip("Ekran kenarına minimum mesafe (pixel)")]
    public float minScreenEdgeDistance = 20f;
    
    [Header("Gösterim Ayarları")]
    [Tooltip("Shrink değeri yerine Brain Size yüzdesi göster (0-100%)")]
    public bool showBrainSizePercentage = true;
    
    [Tooltip("Progress bar'ı tersine çevir (büyük beyin = dolu bar)")]
    public bool invertProgressBar = true;
    
    // Smooth değerler
    private float smoothShrinkValue = 0.900f;
    private float smoothShrinkVelocity = 0f;
    
    // Başlangıç shrink değeri (kazanma threshold'u)
    private float initialShrinkAmount = 0.900f;
    private float winThreshold = 0.190f;
    
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
        
        // Beyin mesh'ini bul
        if (brainMeshTransform == null)
        {
            // Önce sahnede "Brain" isimli objeyi ara
            GameObject brainObj = GameObject.Find("Brain");
            if (brainObj == null)
            {
                brainObj = GameObject.FindGameObjectWithTag("Brain");
            }
            if (brainObj != null)
            {
                brainMeshTransform = brainObj.transform;
            }
        }
        
        // Başlangıç değerlerini al
        if (dreamLogicController != null)
        {
            // Public method ile değerleri al (reflection yerine - build'de daha güvenilir)
            initialShrinkAmount = dreamLogicController.GetInitialShrinkAmount();
            
            // Win threshold'u al
            winThreshold = dreamLogicController.winShrinkThreshold;
            
            smoothShrinkValue = initialShrinkAmount;
        }
        
        // Başlangıç UI güncellemesi
        UpdateUI(smoothShrinkValue);
        
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
        
        // Public method ile currentShrinkAmount'u al (reflection yerine - build'de daha güvenilir)
        float currentShrink = dreamLogicController.GetCurrentShrinkAmount();
        
        // Smooth interpolation
        smoothShrinkValue = Mathf.SmoothDamp(smoothShrinkValue, currentShrink, 
            ref smoothShrinkVelocity, updateSmoothTime);
        
        // UI'ı güncelle
        UpdateUI(smoothShrinkValue);
        
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
        if (useDynamicRotation && brainMeshTransform != null)
        {
            // Beyin mesh'inin rotasyonunu al ve offset ekle
            float brainRotation = brainMeshTransform.eulerAngles.y; // Y ekseni rotasyonu
            targetRotation = brainRotation + dynamicRotationOffset;
        }
        
        // Rotasyonu uygula
        uiRectTransform.localEulerAngles = new Vector3(0f, 0f, targetRotation);
    }
    
    /// <summary>
    /// UI'ı beyin mesh'inin sol üstüne dinamik olarak konumlandır
    /// </summary>
    private void UpdateDynamicPosition()
    {
        if (brainMeshTransform == null || mainCamera == null || canvas == null || uiRectTransform == null)
        {
            return;
        }
        
        // Beyin mesh'inin bounds'ını al (daha doğru pozisyon için)
        Renderer brainRenderer = brainMeshTransform.GetComponent<Renderer>();
        Vector3 brainWorldPos = brainMeshTransform.position;
        Vector3 offsetPos = brainWorldPos;
        
        if (brainRenderer != null)
        {
            Bounds bounds = brainRenderer.bounds;
            // Sol üst nokta: bounds'ın sol üst köşesi
            offsetPos = new Vector3(bounds.min.x, bounds.max.y, bounds.center.z);
        }
        else
        {
            // Renderer yoksa transform pozisyonunu kullan ve offset uygula
            offsetPos = brainWorldPos + brainMeshTransform.TransformDirection(worldSpaceOffset);
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
            
            // Sol kenar kontrolü
            if (currentPos.x < minScreenEdgeDistance)
            {
                currentPos.x = minScreenEdgeDistance;
            }
            
            // Üst kenar kontrolü (y ekseni Unity UI'da aşağı doğru)
            if (currentPos.y > -minScreenEdgeDistance)
            {
                currentPos.y = -minScreenEdgeDistance;
            }
            
            // Sağ kenar kontrolü (eğer UI çok genişse)
            float uiWidth = uiRectTransform.sizeDelta.x;
            if (currentPos.x + uiWidth > canvasSize.x - minScreenEdgeDistance)
            {
                currentPos.x = canvasSize.x - uiWidth - minScreenEdgeDistance;
            }
            
            // Alt kenar kontrolü (eğer UI çok yüksekse)
            float uiHeight = uiRectTransform.sizeDelta.y;
            if (currentPos.y - uiHeight < -canvasSize.y + minScreenEdgeDistance)
            {
                currentPos.y = -canvasSize.y + uiHeight + minScreenEdgeDistance;
            }
            
            uiRectTransform.anchoredPosition = currentPos;
        }
    }
    
    /// <summary>
    /// UI'ı güncelle
    /// </summary>
    private void UpdateUI(float shrinkValue)
    {
        // Shrink değerinden Brain Size yüzdesini hesapla
        // Shrink yüksek = beyin küçük, shrink düşük = beyin büyük
        // Shrink 0.900 (başlangıç) = 0% büyüklük, Shrink 0.190 (kazanma) = 100% büyüklük
        float brainSizeProgress = Mathf.InverseLerp(initialShrinkAmount, winThreshold, shrinkValue);
        brainSizeProgress = Mathf.Clamp01(brainSizeProgress);
        float brainSizePercentage = brainSizeProgress * 100f;
        
        // Renk hesaplama (shrink düştükçe yeşile, yükseldikçe kırmızıya)
        float colorT = brainSizeProgress;
        
        Color currentColor;
        if (colorT < 0.5f)
        {
            // Kırmızıdan turuncuya
            currentColor = Color.Lerp(smallBrainColor, mediumBrainColor, colorT * 2f);
        }
        else
        {
            // Turuncudan yeşile
            currentColor = Color.Lerp(mediumBrainColor, largeBrainColor, (colorT - 0.5f) * 2f);
        }
        
        // Text güncelle
        if (shrinkValueText != null)
        {
            if (showBrainSizePercentage)
            {
                // Brain Size yüzdesi göster (daha anlaşılır)
                shrinkValueText.text = $"{brainSizePercentage:F1}%";
            }
            else
            {
                // Shrink değerini göster
                shrinkValueText.text = shrinkValue.ToString("F3");
            }
            
            shrinkValueText.color = currentColor;
        }
        
        // Progress bar güncelle
        if (progressBarFill != null)
        {
            // Progress: beyin büyüdükçe bar dolsun
            float progress = brainSizeProgress;
            
            // Tersine çevirilmiş progress bar (büyük beyin = dolu bar)
            if (invertProgressBar)
            {
                progress = brainSizeProgress; // Zaten doğru yönde
            }
            else
            {
                progress = 1f - brainSizeProgress; // Tersine çevir
            }
            
            progressBarFill.fillAmount = progress;
            progressBarFill.color = currentColor;
        }
    }
}

