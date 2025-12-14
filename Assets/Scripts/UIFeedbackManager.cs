using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class UIFeedbackManager : MonoBehaviour
{
    [Header("UI Text Feedback Settings")]
    [Tooltip("TextMeshProUGUI prefab for UI feedback text (Canvas üzerinde çalışır)")]
    public GameObject feedbackTextPrefab;
    
    [Tooltip("Canvas referansı (otomatik bulunur veya manuel atanabilir)")]
    public Canvas targetCanvas;
    
    [Header("Animation Settings")]
    [Tooltip("Yukarı doğru hareket hızı")]
    public float moveSpeed = 200f;
    
    [Tooltip("Yukarı doğru hareket mesafesi (pixel)")]
    public float moveDistance = 100f;
    
    [Tooltip("Görünür kalma süresi (saniye)")]
    public float displayDuration = 1.5f;
    
    [Tooltip("Fade out süresi (saniye)")]
    public float fadeDuration = 0.5f;
    
    [Header("Scale Animation")]
    [Tooltip("Başlangıç scale değeri (pop-up efekti için)")]
    public float startScale = 0.3f;
    
    [Tooltip("Maksimum scale değeri")]
    public float maxScale = 1.2f;
    
    [Tooltip("Normal scale değeri")]
    public float normalScale = 1f;
    
    [Tooltip("Scale animasyon süresi (saniye)")]
    public float scaleDuration = 0.3f;
    
    [Header("Text Settings")]
    [Tooltip("Font boyutu")]
    public float fontSize = 48f;
    
    [Tooltip("Font style")]
    public FontStyles fontStyle = FontStyles.Bold;
    
    [Header("Text Colors")]
    [Tooltip("Her buton tipi için renk ayarları")]
    public Color caffeineColor = new Color(1f, 0.8f, 0.2f); // Yellow/Orange
    public Color radiationColor = new Color(0.2f, 1f, 0.2f); // Green
    public Color lavenderColor = new Color(0.8f, 0.2f, 1f); // Purple
    public Color melatoninColor = new Color(0.2f, 0.6f, 1f); // Blue
    public Color heatColor = new Color(1f, 0.3f, 0.2f); // Red/Orange
    
    [Header("World to Screen Conversion")]
    [Tooltip("Eğer buton 3D dünyada ise, bu offset ile ekran pozisyonuna çevrilir")]
    public Vector3 worldToScreenOffset = Vector3.zero;
    
    [Header("Beyin Mesh Referansı")]
    [Tooltip("Beyin mesh Transform (otomatik bulunur, feedback beynin yanından çıkar)")]
    public Transform brainMeshTransform;
    
    [Tooltip("Feedback'in beynin yanından çıkması için offset (world space) - DEPRECATED: Artık her buton tipi için ayrı offset kullanılıyor")]
    public Vector3 brainOffset = new Vector3(0.5f, 0f, 0f); // Beynin sağından
    
    [Tooltip("Beyin mesh'ini kullan (buton pozisyonu yerine)")]
    public bool useBrainPosition = true;
    
    [Header("Buton Tipi Bazlı Offset'ler")]
    [Tooltip("Her buton tipi için beynin etrafından çıkış pozisyonu (world space)")]
    public Vector3 caffeineOffset = new Vector3(0.5f, 0.3f, 0f); // Sağ üst
    public Vector3 radiationOffset = new Vector3(0f, 0.5f, 0f); // Üst
    public Vector3 lavenderOffset = new Vector3(-0.5f, 0.3f, 0f); // Sol üst
    public Vector3 melatoninOffset = new Vector3(-0.5f, -0.3f, 0f); // Sol alt
    public Vector3 heatOffset = new Vector3(0.5f, -0.3f, 0f); // Sağ alt
    
    private Camera mainCamera;
    private RectTransform canvasRect;
    
    void Start()
    {
        // Find main camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }
        
        // Find canvas if not assigned
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogWarning("UIFeedbackManager: Canvas bulunamadı! UI feedback çalışmayabilir.");
            }
        }
        
        if (targetCanvas != null)
        {
            canvasRect = targetCanvas.GetComponent<RectTransform>();
        }
        
        // Beyin mesh'ini bul (eğer atanmamışsa)
        if (brainMeshTransform == null && useBrainPosition)
        {
            // DreamLogicController'dan brainRenderer'ı al
            DreamLogicController dreamLogic = FindObjectOfType<DreamLogicController>();
            if (dreamLogic != null)
            {
                var brainRendererField = typeof(DreamLogicController).GetField("brainRenderer", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (brainRendererField != null)
                {
                    Renderer brainRenderer = brainRendererField.GetValue(dreamLogic) as Renderer;
                    if (brainRenderer != null)
                    {
                        brainMeshTransform = brainRenderer.transform;
                    }
                }
            }
            
            // Eğer hala bulunamadıysa, sahnede "Brain" isimli objeyi ara
            if (brainMeshTransform == null)
            {
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
        }
        
        // If no prefab assigned, create a default one
        if (feedbackTextPrefab == null)
        {
            CreateDefaultPrefab();
        }
    }
    
    /// <summary>
    /// Shows a UI feedback text for the given button type at world position
    /// </summary>
    public void ShowFeedback(ButtonManager.ButtonType buttonType, Vector3 worldPosition)
    {
        // Eğer beyin pozisyonu kullanılacaksa, buton pozisyonu yerine beyin pozisyonunu kullan
        Vector3 feedbackWorldPosition = worldPosition;
        
        if (useBrainPosition && brainMeshTransform != null)
        {
            // Buton tipine göre uygun offset'i al
            Vector3 buttonOffset = GetBrainOffsetForButtonType(buttonType);
            
            // Beyin mesh'inin pozisyonunu al ve offset uygula
            Vector3 brainPos = brainMeshTransform.position;
            
            // Beyin mesh'inin bounds'ını al (daha doğru pozisyon için)
            Renderer brainRenderer = brainMeshTransform.GetComponent<Renderer>();
            if (brainRenderer != null)
            {
                Bounds bounds = brainRenderer.bounds;
                // Buton tipine göre beynin farklı noktalarından çık
                feedbackWorldPosition = bounds.center + brainMeshTransform.TransformDirection(buttonOffset);
            }
            else
            {
                // Renderer yoksa transform pozisyonunu kullan
                feedbackWorldPosition = brainPos + brainMeshTransform.TransformDirection(buttonOffset);
            }
        }
        
        // Convert world position to screen position
        Vector2 screenPosition = WorldToScreenPosition(feedbackWorldPosition);
        ShowFeedback(buttonType, screenPosition);
    }
    
    /// <summary>
    /// Gets the brain offset for a specific button type
    /// </summary>
    private Vector3 GetBrainOffsetForButtonType(ButtonManager.ButtonType buttonType)
    {
        switch (buttonType)
        {
            case ButtonManager.ButtonType.Caffeine:
                return caffeineOffset;
            case ButtonManager.ButtonType.Radiation:
                return radiationOffset;
            case ButtonManager.ButtonType.Lavender:
                return lavenderOffset;
            case ButtonManager.ButtonType.Melatonin:
                return melatoninOffset;
            case ButtonManager.ButtonType.Heat:
                return heatOffset;
            default:
                return brainOffset; // Fallback to default
        }
    }
    
    /// <summary>
    /// Shows a UI feedback text for the given button type at screen position
    /// </summary>
    public void ShowFeedback(ButtonManager.ButtonType buttonType, Vector2 screenPosition)
    {
        if (targetCanvas == null || canvasRect == null)
        {
            Debug.LogWarning("UIFeedbackManager: Canvas bulunamadı! Feedback gösterilemiyor.");
            return;
        }
        
        string message = GetFeedbackMessage(buttonType);
        Color textColor = GetFeedbackColor(buttonType);
        
        // Create feedback text
        GameObject feedbackObj = Instantiate(feedbackTextPrefab, targetCanvas.transform);
        
        // Get RectTransform
        RectTransform rectTransform = feedbackObj.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = feedbackObj.AddComponent<RectTransform>();
        }
        
        // Set initial position (screen space to canvas space)
        Vector2 canvasPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, 
            screenPosition, 
            targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : mainCamera, 
            out canvasPosition
        );
        rectTransform.anchoredPosition = canvasPosition;
        
        // Get or add TextMeshProUGUI component
        TextMeshProUGUI textMesh = feedbackObj.GetComponent<TextMeshProUGUI>();
        if (textMesh == null)
        {
            textMesh = feedbackObj.AddComponent<TextMeshProUGUI>();
            // Set default font if available
            if (TMP_Settings.defaultFontAsset != null)
            {
                textMesh.font = TMP_Settings.defaultFontAsset;
            }
        }
        
        // Configure text
        textMesh.text = message;
        textMesh.fontSize = fontSize;
        textMesh.color = textColor;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontStyle = fontStyle;
        
        // Text'in tek satırda kalması için ayarlar
        textMesh.enableWordWrapping = false; // Kelime kaydırmayı kapat
        textMesh.overflowMode = TextOverflowModes.Overflow; // Taşmayı overflow moduna al
        textMesh.enableAutoSizing = false; // Otomatik boyutlandırmayı kapat
        
        // Set RectTransform properties - genişliği artır ki text yan yana yazılsın
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(500f, 100f); // Genişliği artırdık (300'den 500'e)
        
        // Start animation coroutine
        StartCoroutine(AnimateFeedback(feedbackObj, rectTransform, textMesh, canvasPosition));
    }
    
    /// <summary>
    /// Converts world position to screen position
    /// </summary>
    private Vector2 WorldToScreenPosition(Vector3 worldPosition)
    {
        if (mainCamera != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition + worldToScreenOffset);
            return new Vector2(screenPos.x, screenPos.y);
        }
        return Vector2.zero;
    }
    
    /// <summary>
    /// Animates the feedback text with beautiful effects: scale pop, move up, fade out
    /// </summary>
    private IEnumerator AnimateFeedback(GameObject feedbackObj, RectTransform rectTransform, TextMeshProUGUI textMesh, Vector2 startPosition)
    {
        if (textMesh == null || rectTransform == null)
        {
            Destroy(feedbackObj);
            yield break;
        }
        
        // Phase 1: Scale Pop Animation (0 to maxScale, then to normalScale)
        float elapsedTime = 0f;
        Vector2 targetPosition = startPosition + Vector2.up * moveDistance;
        Color startColor = textMesh.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        // Scale pop: 0 -> maxScale -> normalScale
        while (elapsedTime < scaleDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / scaleDuration;
            
            // Ease out animation
            t = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease out
            
            if (t < 0.5f)
            {
                // 0 to maxScale
                float scaleT = t * 2f;
                float currentScale = Mathf.Lerp(startScale, maxScale, scaleT);
                rectTransform.localScale = Vector3.one * currentScale;
            }
            else
            {
                // maxScale to normalScale
                float scaleT = (t - 0.5f) * 2f;
                float currentScale = Mathf.Lerp(maxScale, normalScale, scaleT);
                rectTransform.localScale = Vector3.one * currentScale;
            }
            
            yield return null;
        }
        
        rectTransform.localScale = Vector3.one * normalScale;
        
        // Phase 2: Move Up and Display (normalScale, moving up, staying visible)
        elapsedTime = 0f;
        while (elapsedTime < displayDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / displayDuration;
            
            // Smooth movement upward
            float moveT = 1f - Mathf.Pow(1f - t, 2f); // Quadratic ease out
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, moveT);
            
            // Slight scale bounce effect (optional - subtle)
            float bounceScale = normalScale + Mathf.Sin(elapsedTime * 5f) * 0.05f;
            rectTransform.localScale = Vector3.one * bounceScale;
            
            yield return null;
        }
        
        // Phase 3: Fade Out (moving up more, fading out)
        elapsedTime = 0f;
        Vector2 fadeStartPosition = rectTransform.anchoredPosition;
        Vector2 fadeEndPosition = fadeStartPosition + Vector2.up * (moveDistance * 0.5f);
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            
            // Continue moving upward
            float moveT = 1f - Mathf.Pow(1f - t, 2f);
            rectTransform.anchoredPosition = Vector2.Lerp(fadeStartPosition, fadeEndPosition, moveT);
            
            // Fade out
            textMesh.color = Color.Lerp(startColor, endColor, t);
            
            // Slight scale down while fading
            float fadeScale = Mathf.Lerp(normalScale, normalScale * 0.8f, t);
            rectTransform.localScale = Vector3.one * fadeScale;
            
            yield return null;
        }
        
        // Destroy the feedback object
        Destroy(feedbackObj);
    }
    
    /// <summary>
    /// Gets the feedback message for a button type
    /// </summary>
    private string GetFeedbackMessage(ButtonManager.ButtonType buttonType)
    {
        switch (buttonType)
        {
            case ButtonManager.ButtonType.Caffeine:
                return "Caffeine UP!";
            case ButtonManager.ButtonType.Radiation:
                return "Radiation UP!";
            case ButtonManager.ButtonType.Lavender:
                return "Lavender UP!";
            case ButtonManager.ButtonType.Melatonin:
                return "Melatonin UP!";
            case ButtonManager.ButtonType.Heat:
                return "Heat UP!";
            default:
                return "UP!";
        }
    }
    
    /// <summary>
    /// Gets the feedback color for a button type
    /// </summary>
    private Color GetFeedbackColor(ButtonManager.ButtonType buttonType)
    {
        switch (buttonType)
        {
            case ButtonManager.ButtonType.Caffeine:
                return caffeineColor;
            case ButtonManager.ButtonType.Radiation:
                return radiationColor;
            case ButtonManager.ButtonType.Lavender:
                return lavenderColor;
            case ButtonManager.ButtonType.Melatonin:
                return melatoninColor;
            case ButtonManager.ButtonType.Heat:
                return heatColor;
            default:
                return Color.white;
        }
    }
    
    /// <summary>
    /// Creates a default TextMeshProUGUI prefab if none is assigned
    /// </summary>
    private void CreateDefaultPrefab()
    {
        if (targetCanvas == null)
        {
            Debug.LogWarning("UIFeedbackManager: Canvas bulunamadı! Default prefab oluşturulamıyor.");
            return;
        }
        
        GameObject defaultPrefab = new GameObject("DefaultFeedbackText");
        defaultPrefab.transform.SetParent(targetCanvas.transform);
        
        RectTransform rectTransform = defaultPrefab.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(500f, 100f); // Genişliği artırdık
        
        TextMeshProUGUI textMesh = defaultPrefab.AddComponent<TextMeshProUGUI>();
        textMesh.text = "UP!";
        textMesh.fontSize = fontSize;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontStyle = fontStyle;
        
        // Text'in tek satırda kalması için ayarlar
        textMesh.enableWordWrapping = false; // Kelime kaydırmayı kapat
        textMesh.overflowMode = TextOverflowModes.Overflow; // Taşmayı overflow moduna al
        textMesh.enableAutoSizing = false; // Otomatik boyutlandırmayı kapat
        
        // Make it a prefab-like object (you'll need to assign a real prefab in the inspector)
        feedbackTextPrefab = defaultPrefab;
    }
}
