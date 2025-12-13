using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class WinLoseScreenManager : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("Win panel GameObject")]
    public GameObject winPanel;
    
    [Tooltip("Lose panel GameObject")]
    public GameObject losePanel;
    
    [Header("Images (Resimleri buraya sürükleyip bırakabilirsiniz)")]
    [Tooltip("Win ekranı için resim (Sprite)")]
    public Image winImage;
    
    [Tooltip("Lose ekranı için resim (Sprite)")]
    public Image loseImage;
    
    [Header("Texts")]
    [Tooltip("Win mesajı")]
    public TextMeshProUGUI winText;
    
    [Tooltip("Lose mesajı")]
    public TextMeshProUGUI loseText;
    
    [Header("Dream Logic Controller")]
    [Tooltip("DreamLogicController referansı (otomatik bulunur)")]
    public DreamLogicController dreamLogicController;
    
    [Header("Slide Animation Settings")]
    [Tooltip("Panel slide animasyon süresi (saniye)")]
    public float slideDuration = 0.8f;
    
    [Tooltip("Panel'in başlangıç pozisyonu (ekranın altından ne kadar aşağıda başlayacak)")]
    public float startOffsetY = 1200f;
    
    [Tooltip("Easing tipi (0=Linear, 1=EaseOut, 2=EaseInOut)")]
    public AnimationCurve slideEasing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Camera Post-Process Settings")]
    [Tooltip("Post-Process Volume (URP için) - Manuel atanabilir veya otomatik bulunur")]
    public Volume postProcessVolume;
    
    [Tooltip("Post-process efekti aktif mi?")]
    public bool enablePostProcess = true;
    
    [Header("Vignette Settings")]
    [Tooltip("Vignette intensity (0-1)")]
    [Range(0f, 1f)]
    public float vignetteIntensity = 0.5f;
    
    [Tooltip("Vignette color (kenar rengi)")]
    public Color vignetteColor = Color.black;
    
    [Tooltip("Vignette center point (0.5, 0.5 = merkez)")]
    public Vector2 vignetteCenter = new Vector2(0.5f, 0.5f);
    
    [Tooltip("Vignette smoothness (0-1, yüksek = daha yumuşak geçiş)")]
    [Range(0f, 1f)]
    public float vignetteSmoothness = 0.2f;
    
    [Tooltip("Vignette rounded (true = yuvarlak, false = kare)")]
    public bool vignetteRounded = true;
    
    [Header("Color Adjustments Settings")]
    [Tooltip("Color Adjustments - Saturation azaltma (-100 = siyah-beyaz)")]
    [Range(-100f, 100f)]
    public float saturationShift = -30f;
    
    [Tooltip("Color Adjustments - Post Exposure (parlaklık, EV cinsinden)")]
    [Range(-5f, 5f)]
    public float brightnessShift = 0f;
    
    [Tooltip("Color Adjustments - Contrast (kontrast)")]
    [Range(-100f, 100f)]
    public float contrastShift = 0f;
    
    [Header("Post-Process Animation")]
    [Tooltip("Post-process animasyon süresi (saniye)")]
    public float postProcessDuration = 0.6f;
    
    [Header("Hover Animation Settings")]
    [Tooltip("Hover animasyonu aktif mi?")]
    public bool enableHoverAnimation = true;
    
    // Optimize edilmiş varsayılan değerler - sağlam animasyon için
    private float hoverScale = 1.15f; // %15 büyüme - daha belirgin
    private float hoverSpeed = 12f; // Daha hızlı ve responsive
    private float hoverRotation = 2.5f; // Hafif rotation
    private float elasticBounce = 0.4f; // Güçlü bounce efekti
    private float pulseSpeed = 1.8f; // Yumuşak pulse
    private float pulseAmount = 0.04f; // Belirgin pulse
    private float glowIntensity = 0.5f; // Güçlü glow
    private float shadowOffset = 15f; // Derin gölge
    private float colorShiftAmount = 0.15f; // Belirgin renk değişimi
    private Color hoverColorTint = new Color(1.1f, 1.1f, 1.0f, 0f); // Hafif sarımsı glow
    
    private RectTransform winPanelRect;
    private RectTransform losePanelRect;
    private Vector2 winPanelStartPos;
    private Vector2 losePanelStartPos;
    private Vector2 winPanelTargetPos;
    private Vector2 losePanelTargetPos;
    private bool isAnimating = false;
    private Coroutine currentAnimation;
    
    // Post-process components
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    
    // Hover animation
    private Vector3 winPanelOriginalScale;
    private Vector3 losePanelOriginalScale;
    private Quaternion winPanelOriginalRotation;
    private Quaternion losePanelOriginalRotation;
    private bool isHoveringWin = false;
    private bool isHoveringLose = false;
    private float winHoverTime = 0f;
    private float loseHoverTime = 0f;
    
    // Panel components for hover effects
    private Image winPanelImage;
    private Image losePanelImage;
    private Shadow winPanelShadow;
    private Shadow losePanelShadow;
    private Color winPanelOriginalColor;
    private Color losePanelOriginalColor;
    
    // Image hover animation (winImage ve loseImage için)
    private RectTransform winImageRect;
    private RectTransform loseImageRect;
    private Vector3 winImageOriginalScale;
    private Vector3 loseImageOriginalScale;
    private Color winImageOriginalColor;
    private Color loseImageOriginalColor;
    private bool isHoveringWinImage = false;
    private bool isHoveringLoseImage = false;
    private float winImageHoverTime = 0f;
    private float loseImageHoverTime = 0f;
    
    private void Start()
    {
        // DreamLogicController'ı otomatik bul
        if (dreamLogicController == null)
        {
            dreamLogicController = FindObjectOfType<DreamLogicController>();
        }
        
        // Post-process Volume'u bul
        if (postProcessVolume == null)
        {
            postProcessVolume = FindObjectOfType<Volume>();
        }
        
        // Post-process componentlerini al
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out vignette);
            postProcessVolume.profile.TryGet(out colorAdjustments);
        }
        
        // Panel RectTransform'lerini al ve pozisyonları kaydet
        SetupPanels();
        
        // Restart butonlarını bul ve event'leri bağla
        SetupRestartButtons();
    }
    
    private void SetupPanels()
    {
        // Win Panel
        if (winPanel != null)
        {
            winPanelRect = winPanel.GetComponent<RectTransform>();
            if (winPanelRect != null)
            {
                winPanelTargetPos = winPanelRect.anchoredPosition;
                winPanelStartPos = winPanelTargetPos - new Vector2(0f, startOffsetY);
                winPanelRect.anchoredPosition = winPanelStartPos;
                winPanelOriginalScale = winPanelRect.localScale;
                winPanelOriginalRotation = winPanelRect.localRotation;
            }
            
            // Panel Image component'ini al
            winPanelImage = winPanel.GetComponent<Image>();
            if (winPanelImage != null)
            {
                winPanelOriginalColor = winPanelImage.color;
            }
            
            // Shadow component ekle veya al
            winPanelShadow = winPanel.GetComponent<Shadow>();
            if (winPanelShadow == null)
            {
                winPanelShadow = winPanel.AddComponent<UnityEngine.UI.Shadow>();
                winPanelShadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
                winPanelShadow.effectDistance = new Vector2(0f, 0f);
            }
            
            winPanel.SetActive(false);
        }
        
        // Lose Panel
        if (losePanel != null)
        {
            losePanelRect = losePanel.GetComponent<RectTransform>();
            if (losePanelRect != null)
            {
                losePanelTargetPos = losePanelRect.anchoredPosition;
                losePanelStartPos = losePanelTargetPos - new Vector2(0f, startOffsetY);
                losePanelRect.anchoredPosition = losePanelStartPos;
                losePanelOriginalScale = losePanelRect.localScale;
                losePanelOriginalRotation = losePanelRect.localRotation;
            }
            
            // Panel Image component'ini al
            losePanelImage = losePanel.GetComponent<Image>();
            if (losePanelImage != null)
            {
                losePanelOriginalColor = losePanelImage.color;
            }
            
            // Shadow component ekle veya al
            losePanelShadow = losePanel.GetComponent<Shadow>();
            if (losePanelShadow == null)
            {
                losePanelShadow = losePanel.AddComponent<UnityEngine.UI.Shadow>();
                losePanelShadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
                losePanelShadow.effectDistance = new Vector2(0f, 0f);
            }
            
            // Lose Image hover setup
            if (loseImage != null)
            {
                loseImageRect = loseImage.GetComponent<RectTransform>();
                if (loseImageRect != null)
                {
                    loseImageOriginalScale = loseImageRect.localScale;
                }
                loseImageOriginalColor = loseImage.color;
            }
            
            losePanel.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Panel hover animasyonu
        if (enableHoverAnimation)
        {
            UpdateHoverAnimation();
        }
        
        // Image hover animasyonu (winImage ve loseImage)
        UpdateImageHoverAnimation();
    }
    
    private void UpdateHoverAnimation()
    {
        // Win Panel hover
        if (winPanel != null && winPanel.activeSelf && winPanelRect != null)
        {
            if (isHoveringWin)
            {
                winHoverTime += Time.deltaTime;
            }
            else
            {
                winHoverTime = Mathf.Max(0f, winHoverTime - Time.deltaTime * hoverSpeed);
            }
            
            UpdatePanelHover(
                winPanelRect, 
                winPanelImage, 
                winPanelShadow,
                winPanelOriginalScale, 
                winPanelOriginalRotation,
                winPanelOriginalColor,
                isHoveringWin, 
                winHoverTime
            );
        }
        
        // Lose Panel hover
        if (losePanel != null && losePanel.activeSelf && losePanelRect != null)
        {
            if (isHoveringLose)
            {
                loseHoverTime += Time.deltaTime;
            }
            else
            {
                loseHoverTime = Mathf.Max(0f, loseHoverTime - Time.deltaTime * hoverSpeed);
            }
            
            UpdatePanelHover(
                losePanelRect, 
                losePanelImage, 
                losePanelShadow,
                losePanelOriginalScale, 
                losePanelOriginalRotation,
                losePanelOriginalColor,
                isHoveringLose, 
                loseHoverTime
            );
        }
    }
    
    private void UpdatePanelHover(
        RectTransform panelRect, 
        Image panelImage, 
        Shadow panelShadow,
        Vector3 originalScale, 
        Quaternion originalRotation,
        Color originalColor,
        bool isHovering, 
        float hoverTime)
    {
        if (panelRect == null) return;
        
        // Elastic bounce hesaplama - daha smooth ve etkileyici
        float elasticFactor = 1f;
        if (isHovering && hoverTime < 0.6f)
        {
            // Elastic bounce efekti - cubic ease out ile daha smooth
            float bounceT = hoverTime / 0.6f;
            bounceT = 1f - Mathf.Pow(1f - bounceT, 3f); // Cubic ease out
            elasticFactor = 1f + elasticBounce * (1f - bounceT) * Mathf.Sin(bounceT * Mathf.PI);
        }
        
        // Pulse efekti - daha yumuşak ve sürekli
        float pulse = 1f;
        if (isHovering && pulseSpeed > 0f)
        {
            // Yumuşak pulse - sinüs dalgası
            pulse = 1f + pulseAmount * Mathf.Sin(hoverTime * pulseSpeed * Mathf.PI * 2f);
        }
        
        // Scale hesaplama - elastic + pulse kombinasyonu
        float targetScale = isHovering ? hoverScale : 1f;
        float finalScale = targetScale * elasticFactor * pulse;
        
        // Rotation hesaplama - hover'da smooth shake
        float targetRotation = isHovering ? hoverRotation : 0f;
        float shakeRotation = 0f;
        if (isHovering)
        {
            // Daha yumuşak shake - multiple sinüs dalgaları
            shakeRotation = Mathf.Sin(hoverTime * 6f) * 0.3f + Mathf.Sin(hoverTime * 12f) * 0.15f;
        }
        float finalRotation = targetRotation + shakeRotation;
        
        // Scale animasyonu - smooth damping
        panelRect.localScale = Vector3.Lerp(
            panelRect.localScale,
            originalScale * finalScale,
            Time.deltaTime * hoverSpeed
        );
        
        // Rotation animasyonu - smooth damping
        panelRect.localRotation = Quaternion.Lerp(
            panelRect.localRotation,
            originalRotation * Quaternion.Euler(0f, 0f, finalRotation),
            Time.deltaTime * hoverSpeed
        );
        
        // Color shift efekti - daha belirgin glow
        if (panelImage != null)
        {
            Color targetColor = originalColor;
            if (isHovering)
            {
                // Color tint ekle - daha belirgin
                targetColor = Color.Lerp(originalColor, originalColor * (Color.white + hoverColorTint), colorShiftAmount);
                
                // Glow efekti - smooth fade in
                float glowAlpha = Mathf.SmoothStep(0f, glowIntensity, Mathf.Clamp01(hoverTime * 3f));
                targetColor.a = Mathf.Min(1f, originalColor.a + glowAlpha * 0.2f);
                
                // Pulse ile glow intensity değişimi
                float glowPulse = 1f + Mathf.Sin(hoverTime * pulseSpeed * Mathf.PI * 2f) * 0.2f;
                targetColor = Color.Lerp(targetColor, targetColor * glowPulse, 0.1f);
            }
            
            panelImage.color = Color.Lerp(panelImage.color, targetColor, Time.deltaTime * hoverSpeed);
        }
        
        // Shadow efekti - daha dramatik
        if (panelShadow != null)
        {
            Vector2 targetShadowOffset = isHovering ? new Vector2(0f, -shadowOffset) : Vector2.zero;
            float targetShadowAlpha = isHovering ? 0.7f : 0.3f;
            
            // Shadow offset animasyonu
            panelShadow.effectDistance = Vector2.Lerp(
                panelShadow.effectDistance,
                targetShadowOffset,
                Time.deltaTime * hoverSpeed
            );
            
            // Shadow alpha animasyonu
            Color shadowColor = panelShadow.effectColor;
            shadowColor.a = Mathf.Lerp(shadowColor.a, targetShadowAlpha, Time.deltaTime * hoverSpeed);
            panelShadow.effectColor = shadowColor;
        }
    }
    
    private void SetupRestartButtons()
    {
        // Win panel'deki restart butonu
        if (winPanel != null)
        {
            Button winRestartButton = winPanel.GetComponentInChildren<Button>();
            if (winRestartButton != null)
            {
                winRestartButton.onClick.RemoveAllListeners();
                winRestartButton.onClick.AddListener(OnRestartButtonClicked);
            }
        }
        
        // Lose panel'deki restart butonu
        if (losePanel != null)
        {
            Button loseRestartButton = losePanel.GetComponentInChildren<Button>();
            if (loseRestartButton != null)
            {
                loseRestartButton.onClick.RemoveAllListeners();
                loseRestartButton.onClick.AddListener(OnRestartButtonClicked);
            }
        }
    }
    
    /// <summary>
    /// Win ekranını göster (slide animasyonu ile)
    /// </summary>
    public void ShowWinScreen()
    {
        if (winPanel == null) return;
        
        // Önceki animasyonu durdur
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        // Lose panel'i gizle
        if (losePanel != null)
        {
            losePanel.SetActive(false);
        }
        
        // Win panel'i göster ve animasyonu başlat
        winPanel.SetActive(true);
        currentAnimation = StartCoroutine(AnimatePanelSlide(winPanelRect, winPanelStartPos, winPanelTargetPos, true));
        
        Debug.Log("WinLoseScreenManager: Win ekranı gösterildi!");
    }
    
    /// <summary>
    /// Lose ekranını göster (slide animasyonu ile)
    /// </summary>
    public void ShowLoseScreen()
    {
        if (losePanel == null) return;
        
        // Önceki animasyonu durdur
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        // Win panel'i gizle
        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }
        
        // Lose panel'i göster ve animasyonu başlat
        losePanel.SetActive(true);
        currentAnimation = StartCoroutine(AnimatePanelSlide(losePanelRect, losePanelStartPos, losePanelTargetPos, false));
        
        Debug.Log("WinLoseScreenManager: Lose ekranı gösterildi!");
    }
    
    /// <summary>
    /// Panel slide animasyonu (aşağıdan yukarıya)
    /// </summary>
    private IEnumerator AnimatePanelSlide(RectTransform panelRect, Vector2 startPos, Vector2 targetPos, bool isWin)
    {
        if (panelRect == null) yield break;
        
        isAnimating = true;
        
        // Post-process efekti başlat
        if (enablePostProcess)
        {
            StartCoroutine(AnimatePostProcess(true));
        }
        
        // Slide animasyonu
        float elapsedTime = 0f;
        while (elapsedTime < slideDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / slideDuration;
            
            // Easing uygula
            float easedT = slideEasing.Evaluate(t);
            
            // Pozisyonu güncelle
            panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, easedT);
            
            yield return null;
        }
        
        // Final pozisyon
        panelRect.anchoredPosition = targetPos;
        
        isAnimating = false;
        currentAnimation = null;
    }
    
    /// <summary>
    /// Post-process animasyonu (vignette ve color adjustments)
    /// </summary>
    private IEnumerator AnimatePostProcess(bool enable)
    {
        if (vignette == null && colorAdjustments == null) yield break;
        
        float elapsedTime = 0f;
        
        // Başlangıç değerleri
        float startVignetteIntensity = vignette != null ? vignette.intensity.value : 0f;
        Color startVignetteColor = vignette != null ? vignette.color.value : Color.black;
        Vector2 startVignetteCenter = vignette != null ? vignette.center.value : new Vector2(0.5f, 0.5f);
        float startVignetteSmoothness = vignette != null ? vignette.smoothness.value : 0.2f;
        bool startVignetteRounded = vignette != null ? vignette.rounded.value : true;
        
        float startSaturation = colorAdjustments != null ? colorAdjustments.saturation.value : 0f;
        float startPostExposure = colorAdjustments != null ? colorAdjustments.postExposure.value : 0f;
        float startContrast = colorAdjustments != null ? colorAdjustments.contrast.value : 0f;
        
        // Hedef değerler
        float targetVignetteIntensity = enable ? vignetteIntensity : 0f;
        Color targetVignetteColor = enable ? vignetteColor : Color.black;
        Vector2 targetVignetteCenter = enable ? vignetteCenter : new Vector2(0.5f, 0.5f);
        float targetVignetteSmoothness = enable ? vignetteSmoothness : 0.2f;
        bool targetVignetteRounded = enable ? vignetteRounded : true;
        
        float targetSaturation = enable ? saturationShift : 0f;
        float targetPostExposure = enable ? brightnessShift : 0f; // brightness yerine postExposure kullan
        float targetContrast = enable ? contrastShift : 0f;
        
        while (elapsedTime < postProcessDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / postProcessDuration;
            
            // Smooth easing
            t = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease out
            
            // Vignette ayarları
            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(startVignetteIntensity, targetVignetteIntensity, t);
                vignette.color.value = Color.Lerp(startVignetteColor, targetVignetteColor, t);
                vignette.center.value = Vector2.Lerp(startVignetteCenter, targetVignetteCenter, t);
                vignette.smoothness.value = Mathf.Lerp(startVignetteSmoothness, targetVignetteSmoothness, t);
                
                // Rounded boolean için geçiş yap (t > 0.5 ise target, değilse start)
                vignette.rounded.value = t > 0.5f ? targetVignetteRounded : startVignetteRounded;
            }
            
            // Color Adjustments ayarları
            if (colorAdjustments != null)
            {
                colorAdjustments.saturation.value = Mathf.Lerp(startSaturation, targetSaturation, t);
                colorAdjustments.postExposure.value = Mathf.Lerp(startPostExposure, targetPostExposure, t);
                colorAdjustments.contrast.value = Mathf.Lerp(startContrast, targetContrast, t);
            }
            
            yield return null;
        }
        
        // Final değerler
        if (vignette != null)
        {
            vignette.intensity.value = targetVignetteIntensity;
            vignette.color.value = targetVignetteColor;
            vignette.center.value = targetVignetteCenter;
            vignette.smoothness.value = targetVignetteSmoothness;
            vignette.rounded.value = targetVignetteRounded;
        }
        
        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = targetSaturation;
            colorAdjustments.postExposure.value = targetPostExposure;
            colorAdjustments.contrast.value = targetContrast;
        }
    }
    
    /// <summary>
    /// Tüm ekranları gizle (post-process efektini de kapat)
    /// </summary>
    public void HideAllScreens()
    {
        // Animasyonu durdur
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        
        // Panelleri gizle
        if (winPanel != null)
        {
            winPanel.SetActive(false);
            isHoveringWin = false;
        }
        
        if (losePanel != null)
        {
            losePanel.SetActive(false);
            isHoveringLose = false;
        }
        
        // Post-process efektini kapat
        if (enablePostProcess)
        {
            StartCoroutine(AnimatePostProcess(false));
        }
    }
    
    /// <summary>
    /// Win panel hover event (UI Event Trigger ile kullanılabilir)
    /// </summary>
    public void OnWinPanelEnter()
    {
        isHoveringWin = true;
    }
    
    /// <summary>
    /// Win panel hover exit event
    /// </summary>
    public void OnWinPanelExit()
    {
        isHoveringWin = false;
    }
    
    /// <summary>
    /// Lose panel hover event (UI Event Trigger ile kullanılabilir)
    /// </summary>
    public void OnLosePanelEnter()
    {
        isHoveringLose = true;
    }
    
    /// <summary>
    /// Lose panel hover exit event
    /// </summary>
    public void OnLosePanelExit()
    {
        isHoveringLose = false;
    }
    
    /// <summary>
    /// Win Image hover event
    /// </summary>
    public void OnWinImageEnter()
    {
        isHoveringWinImage = true;
    }
    
    /// <summary>
    /// Win Image hover exit event
    /// </summary>
    public void OnWinImageExit()
    {
        isHoveringWinImage = false;
    }
    
    /// <summary>
    /// Lose Image hover event
    /// </summary>
    public void OnLoseImageEnter()
    {
        isHoveringLoseImage = true;
    }
    
    /// <summary>
    /// Lose Image hover exit event
    /// </summary>
    public void OnLoseImageExit()
    {
        isHoveringLoseImage = false;
    }
    
    /// <summary>
    /// Image hover animasyonu (winImage ve loseImage için)
    /// </summary>
    private void UpdateImageHoverAnimation()
    {
        // Win Image hover
        if (winImage != null && winImageRect != null && winPanel != null && winPanel.activeSelf)
        {
            if (isHoveringWinImage)
            {
                winImageHoverTime += Time.deltaTime;
            }
            else
            {
                winImageHoverTime = Mathf.Max(0f, winImageHoverTime - Time.deltaTime * 15f);
            }
            
            UpdateImageHover(
                winImageRect,
                winImage,
                winImageOriginalScale,
                winImageOriginalColor,
                isHoveringWinImage,
                winImageHoverTime
            );
        }
        
        // Lose Image hover
        if (loseImage != null && loseImageRect != null && losePanel != null && losePanel.activeSelf)
        {
            if (isHoveringLoseImage)
            {
                loseImageHoverTime += Time.deltaTime;
            }
            else
            {
                loseImageHoverTime = Mathf.Max(0f, loseImageHoverTime - Time.deltaTime * 15f);
            }
            
            UpdateImageHover(
                loseImageRect,
                loseImage,
                loseImageOriginalScale,
                loseImageOriginalColor,
                isHoveringLoseImage,
                loseImageHoverTime
            );
        }
    }
    
    /// <summary>
    /// Tek bir Image için hover animasyonu
    /// </summary>
    private void UpdateImageHover(
        RectTransform imageRect,
        Image image,
        Vector3 originalScale,
        Color originalColor,
        bool isHovering,
        float hoverTime)
    {
        if (imageRect == null || image == null) return;
        
        // Elastic bounce - Image için daha dramatik
        float elasticFactor = 1f;
        if (isHovering && hoverTime < 0.5f)
        {
            float bounceT = hoverTime / 0.5f;
            bounceT = 1f - Mathf.Pow(1f - bounceT, 3f); // Cubic ease out
            elasticFactor = 1f + 0.5f * (1f - bounceT) * Mathf.Sin(bounceT * Mathf.PI);
        }
        
        // Pulse efekti - Image için daha belirgin
        float pulse = 1f;
        if (isHovering)
        {
            pulse = 1f + 0.06f * Mathf.Sin(hoverTime * 2.5f * Mathf.PI * 2f);
        }
        
        // Scale hesaplama - Image için daha büyük scale
        float targetScale = isHovering ? 1.2f : 1f; // %20 büyüme
        float finalScale = targetScale * elasticFactor * pulse;
        
        // Scale animasyonu - smooth ve hızlı
        imageRect.localScale = Vector3.Lerp(
            imageRect.localScale,
            originalScale * finalScale,
            Time.deltaTime * 15f
        );
        
        // Rotation efekti - hafif tilt
        float targetRotation = isHovering ? 2f : 0f;
        float shakeRotation = 0f;
        if (isHovering)
        {
            shakeRotation = Mathf.Sin(hoverTime * 5f) * 0.5f;
        }
        float finalRotation = targetRotation + shakeRotation;
        
        imageRect.localRotation = Quaternion.Lerp(
            imageRect.localRotation,
            Quaternion.Euler(0f, 0f, finalRotation),
            Time.deltaTime * 15f
        );
        
        // Color efekti - parlaklık artışı ve glow
        Color targetColor = originalColor;
        if (isHovering)
        {
            // Parlaklık artışı
            float brightness = 1f + Mathf.SmoothStep(0f, 0.3f, Mathf.Clamp01(hoverTime * 2f));
            targetColor = originalColor * brightness;
            
            // Glow efekti - pulse ile
            float glowPulse = 1f + Mathf.Sin(hoverTime * 2.5f * Mathf.PI * 2f) * 0.15f;
            targetColor = Color.Lerp(targetColor, targetColor * glowPulse, 0.2f);
            
            // Saturation artışı (daha canlı renkler)
            float saturationBoost = Mathf.SmoothStep(0f, 0.2f, Mathf.Clamp01(hoverTime * 2f));
            targetColor = Color.Lerp(targetColor, Color.Lerp(targetColor, Color.white, saturationBoost), 0.1f);
        }
        
        image.color = Color.Lerp(image.color, targetColor, Time.deltaTime * 15f);
    }
    
    /// <summary>
    /// Restart butonuna tıklandığında çağrılır
    /// </summary>
    private void OnRestartButtonClicked()
    {
        Debug.Log("WinLoseScreenManager: Restart butonuna tıklandı!");
        
        // Ekranları gizle
        HideAllScreens();
        
        // DreamLogicController'dan oyunu yeniden başlat
        if (dreamLogicController != null)
        {
            dreamLogicController.StartNewGame();
        }
        else
        {
            Debug.LogWarning("WinLoseScreenManager: DreamLogicController bulunamadı! Oyun yeniden başlatılamıyor.");
        }
    }
}

