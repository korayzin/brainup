using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Beyne basınca kamera Dream objesine doğru karanlıklaşarak fade out efekti ile gider ve oyun sahnesi açılır
/// </summary>
public class BrainToDreamTransition : MonoBehaviour
{
    [Header("Hedef Objeler")]
    [Tooltip("Dream objesi (otomatik bulunur veya manuel atanabilir)")]
    public Transform dreamObject;
    
    [Tooltip("Kamera (otomatik bulunur veya manuel atanabilir)")]
    public Camera mainCamera;
    
    [Header("Animasyon Ayarları")]
    [Tooltip("Kamera hareket süresi (saniye)")]
    [Range(1f, 5f)]
    public float cameraMoveDuration = 2.5f;
    
    [Tooltip("Fade out süresi (saniye)")]
    [Range(0.5f, 3f)]
    public float fadeOutDuration = 1.5f;
    
    [Tooltip("Fade out başlangıç zamanı (kamera hareketinin yüzde kaçında başlasın? 0-1)")]
    [Range(0f, 1f)]
    public float fadeStartTime = 0.6f; // %60'ta fade başlar
    
    [Tooltip("Kamera hareket eğrisi (easing)")]
    public AnimationCurve cameraMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Tooltip("Dream objesine ne kadar yaklaşılsın? (1.0 = yüzeyinde, 1.5 = biraz önünde)")]
    [Range(0.5f, 2.5f)]
    public float approachDistance = 1.2f; // Dream'in yarıçapının 1.2 katı mesafede dur
    
    [Header("Sahne Geçişi")]
    [Tooltip("Geçilecek oyun sahnesi adı (Build Settings'teki sahne adı)")]
    public string targetSceneName = "SampleScene";
    
    [Tooltip("Sahne geçişi için ekstra bekleme süresi (fade out tamamen bittikten sonra)")]
    [Range(0.3f, 2f)]
    public float sceneTransitionDelay = 0.8f; // Smooth geçiş için biraz daha uzun bekleme
    
    [Header("Fade Out UI")]
    [Tooltip("Fade out için UI Image (otomatik oluşturulur veya manuel atanabilir)")]
    public Image fadeImage;
    
    [Tooltip("Fade rengi")]
    public Color fadeColor = Color.black;
    
    // Özel değişkenler
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool isTransitioning = false;
    private GameObject fadeCanvasObject;
    private Canvas fadeCanvas;
    
    void Start()
    {
        // Dream objesini bul (eğer atanmamışsa)
        if (dreamObject == null)
        {
            GameObject dreamObj = GameObject.Find("Dream");
            if (dreamObj == null)
            {
                dreamObj = GameObject.FindGameObjectWithTag("Dream");
            }
            if (dreamObj != null)
            {
                dreamObject = dreamObj.transform;
            }
            else
            {
                Debug.LogWarning("BrainToDreamTransition: Dream objesi bulunamadı! Lütfen Inspector'dan manuel olarak atayın.");
            }
        }
        
        // Kamerayı bul (eğer atanmamışsa)
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }
        
        // Orijinal kamera pozisyonunu kaydet
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
            originalCameraRotation = mainCamera.transform.rotation;
        }
        
        // Fade UI'ı hazırla
        SetupFadeUI();
        
        // Collider kontrolü - OnMouseDown için gerekli
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning("BrainToDreamTransition: OnMouseDown çalışması için bu GameObject'te bir Collider bileşeni olmalı!");
        }
    }
    
    /// <summary>
    /// Fade UI'ı hazırla (otomatik oluştur veya mevcut olanı kullan)
    /// </summary>
    private void SetupFadeUI()
    {
        // Eğer fadeImage atanmamışsa, otomatik oluştur
        if (fadeImage == null)
        {
            // Canvas oluştur
            fadeCanvasObject = new GameObject("FadeCanvas");
            fadeCanvas = fadeCanvasObject.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999; // En üstte olsun
            
            // CanvasScaler ekle
            CanvasScaler scaler = fadeCanvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // GraphicRaycaster ekle
            fadeCanvasObject.AddComponent<GraphicRaycaster>();
            
            // Image oluştur
            GameObject imageObject = new GameObject("FadeImage");
            imageObject.transform.SetParent(fadeCanvasObject.transform, false);
            
            fadeImage = imageObject.AddComponent<Image>();
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // Başlangıçta görünmez
            
            // RectTransform ayarla (tüm ekranı kaplasın)
            RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
    
    /// <summary>
    /// Mouse ile tıklama algılama (OnMouseDown için Collider gerekli)
    /// </summary>
    void OnMouseDown()
    {
        // Eğer zaten geçiş yapılıyorsa, tekrar basılamaz
        if (isTransitioning)
        {
            return;
        }
        
        // Geçişi başlat
        StartTransition();
    }
    
    /// <summary>
    /// Geçiş animasyonunu başlat
    /// </summary>
    public void StartTransition()
    {
        if (isTransitioning)
        {
            return;
        }
        
        // Gerekli kontroller
        if (dreamObject == null)
        {
            Debug.LogError("BrainToDreamTransition: Dream objesi bulunamadı! Geçiş yapılamıyor.");
            return;
        }
        
        if (mainCamera == null)
        {
            Debug.LogError("BrainToDreamTransition: Kamera bulunamadı! Geçiş yapılamıyor.");
            return;
        }
        
        // Geçiş başladı
        isTransitioning = true;
        
        // Coroutine'i başlat
        StartCoroutine(TransitionCoroutine());
    }
    
    /// <summary>
    /// Geçiş animasyonu coroutine'i
    /// </summary>
    private IEnumerator TransitionCoroutine()
    {
        // Hedef pozisyon ve rotasyonu hesapla
        Vector3 dreamCenter = dreamObject.position;
        Vector3 directionToDream = (dreamCenter - mainCamera.transform.position).normalized;
        
        // Dream objesinin boyutunu hesapla (Renderer'dan bounds al)
        float dreamRadius = 0.5f; // Varsayılan yarıçap
        Renderer dreamRenderer = dreamObject.GetComponent<Renderer>();
        if (dreamRenderer != null)
        {
            Bounds dreamBounds = dreamRenderer.bounds;
            // En büyük boyutu yarıçap olarak kullan
            dreamRadius = Mathf.Max(dreamBounds.size.x, dreamBounds.size.y, dreamBounds.size.z) * 0.5f;
        }
        else
        {
            // Renderer yoksa, Collider'dan al
            Collider dreamCollider = dreamObject.GetComponent<Collider>();
            if (dreamCollider != null)
            {
                Bounds dreamBounds = dreamCollider.bounds;
                dreamRadius = Mathf.Max(dreamBounds.size.x, dreamBounds.size.y, dreamBounds.size.z) * 0.5f;
            }
        }
        
        // Kamerayı Dream objesinin YAKININA getir (içine değil, sadece yakınına)
        // approachDistance: 1.0 = yüzeyinde, 1.5 = biraz önünde
        float targetDistance = dreamRadius * approachDistance;
        Vector3 finalPosition = dreamCenter - directionToDream * targetDistance;
        
        // Dream objesinin merkezine bak (içinden geçerken de merkeze bakacak)
        Quaternion targetRotation = Quaternion.LookRotation(directionToDream);
        
        // Orijinal pozisyon ve rotasyonu kaydet
        Vector3 startPosition = mainCamera.transform.position;
        Quaternion startRotation = mainCamera.transform.rotation;
        
        // Kamera hareket animasyonu
        float elapsedTime = 0f;
        float fadeStartTimeActual = cameraMoveDuration * fadeStartTime; // Fade başlangıç zamanı
        float totalFadeTime = fadeStartTimeActual + fadeOutDuration; // Toplam fade süresi
        
        // Fade out'un tamamen bitmesi için toplam süreyi hesapla
        float totalAnimationTime = Mathf.Max(cameraMoveDuration, totalFadeTime);
        
        while (elapsedTime < totalAnimationTime)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / cameraMoveDuration);
            
            // Easing curve uygula
            float easedT = cameraMoveCurve.Evaluate(t);
            
            // Kamerayı hareket ettir (sadece kamera hareket süresi boyunca)
            if (elapsedTime <= cameraMoveDuration)
            {
                mainCamera.transform.position = Vector3.Lerp(startPosition, finalPosition, easedT);
                mainCamera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, easedT);
            }
            else
            {
                // Kamera hareketi bittikten sonra pozisyonu sabit tut
                mainCamera.transform.position = finalPosition;
                mainCamera.transform.rotation = targetRotation;
            }
            
            // Fade out animasyonu
            if (fadeImage != null)
            {
                float fadeAlpha = 0f;
                
                if (elapsedTime >= fadeStartTimeActual)
                {
                    // Fade out başladı
                    float fadeProgress = (elapsedTime - fadeStartTimeActual) / fadeOutDuration;
                    fadeProgress = Mathf.Clamp01(fadeProgress);
                    
                    // Smooth fade out için easing curve kullan
                    fadeAlpha = fadeProgress;
                }
                
                // Fade out'u uygula (tamamen kararsın)
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, fadeAlpha);
            }
            
            yield return null;
        }
        
        // Son pozisyonu garanti et
        mainCamera.transform.position = finalPosition;
        mainCamera.transform.rotation = targetRotation;
        
        // Fade out'u TAMAMEN tamamla (ekran tamamen kararsın)
        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        }
        
        // Smooth sahne geçişi için ekstra bekleme (ekran tamamen karardıktan sonra)
        yield return new WaitForSeconds(sceneTransitionDelay);
        
        // Oyun sahnesine geç
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log($"BrainToDreamTransition: '{targetSceneName}' sahnesine geçiliyor...");
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("BrainToDreamTransition: Hedef sahne adı belirtilmemiş! Geçiş yapılamıyor.");
        }
    }
    
    /// <summary>
    /// Fade UI'ı temizle (sahne geçişinden önce)
    /// </summary>
    void OnDestroy()
    {
        // Fade canvas'ı temizle (eğer otomatik oluşturulduysa)
        if (fadeCanvasObject != null && fadeCanvasObject.name == "FadeCanvas")
        {
            Destroy(fadeCanvasObject);
        }
    }
}

