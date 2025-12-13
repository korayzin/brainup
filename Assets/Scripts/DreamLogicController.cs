using UnityEngine;

/// <summary>
/// Rüya mantığı algoritması: Input değerlerinden latent state'leri hesaplayıp material property'lerini günceller.
/// </summary>
public class DreamLogicController : MonoBehaviour
{
    [Header("Material Referansları")]
    [Tooltip("Beyin büzüşme materiali")]
    public Material brainMat;
    
    [Tooltip("Rüya efekti materiali")]
    public Material dreamMat;

    [Header("Input Değerleri (0..1)")]
    [Tooltip("EMF (Elektromanyetik alan) değeri")]
    [Range(0f, 1f)]
    public float emf = 0f;
    
    [Tooltip("Caffeine Smell (Kafein kokusu) değeri")]
    [Range(0f, 1f)]
    public float caffeineSmell = 0f;
    
    [Tooltip("Lavender Smell (Lavanta kokusu) değeri")]
    [Range(0f, 1f)]
    public float lavenderSmell = 0f;
    
    [Tooltip("Warm Air (Sıcak hava) değeri")]
    [Range(0f, 1f)]
    public float warmAir = 0f;
    
    [Tooltip("Melatonin (Derin uyku hormonu) değeri - Yeni buton")]
    [Range(0f, 1f)]
    public float melatonin = 0f;
    
    [Header("Smooth Geçiş Ayarları")]
    [Tooltip("Smooth geçiş hızı (saniye) - Düşük değer = daha hızlı. Buton basıldığında otomatik olarak 0.15 katına düşer")]
    [Range(0.1f, 2.0f)]
    public float smoothTime = 0.3f;
    
    [Header("Oyun Durumu")]
    [Tooltip("Kazanma için shrink değeri (beyin büyüklüğü - 0.190 veya daha küçük)")]
    [Range(0f, 1f)]
    public float winShrinkThreshold = 0.190f; // Beyin yeterince büyük (shrink <= 0.190)
    
    [Tooltip("Beyin max değilse blur/glitch minimum değeri (rüya asla %100 net olmaz)")]
    [Range(0f, 1f)]
    public float minBlurWhenBrainNotMax = 0.15f; // Beyin max değilse minimum blur
    
    [Tooltip("Tüm butonların referansları (otomatik bulunur)")]
    public ButtonManager[] allButtons;
    
    [Header("Kazanma UI Feedback")]
    [Tooltip("Kazanma durumunda gösterilecek UI Text (Unity UI Text)")]
    public UnityEngine.UI.Text winText;
    
    [Tooltip("Kazanma durumunda gösterilecek TMP Text (TextMeshPro)")]
    public TMPro.TextMeshProUGUI winTextTMP;
    
    [Tooltip("Kazanma mesajı (Inspector'dan özelleştirilebilir)")]
    public string winMessage = "VICTORY! Brain Restored!";
    
    [Tooltip("Kazanma durumunda gösterilecek süre (saniye)")]
    public float winDisplayDuration = 5f;
    
    [Header("Win/Lose Screen Manager")]
    [Tooltip("Win/Lose Screen Manager (Tools > Create Win/Lose Screen ile oluşturulabilir)")]
    public WinLoseScreenManager winLoseScreenManager;

    [Header("Latent Brain States (0..1)")]
    [Tooltip("Arousal (Uyarılma) - Otomatik hesaplanır")]
    [Range(0f, 1f)]
    public float Arousal = 0f;
    
    [Tooltip("Relaxation (Rahatlama) - Otomatik hesaplanır")]
    [Range(0f, 1f)]
    public float Relaxation = 0f;
    
    [Tooltip("ThermalStress (Termal Stres) - Otomatik hesaplanır")]
    [Range(0f, 1f)]
    public float ThermalStress = 0f;
    
    [Tooltip("Fragmentation (Parçalanma) - Otomatik hesaplanır")]
    [Range(0f, 1f)]
    public float Fragmentation = 0f;
    
    [Header("Derived States (0..1)")]
    [Tooltip("Clarity (Netlik) - Otomatik hesaplanır")]
    [Range(0f, 1f)]
    public float Clarity = 0f;
    
    [Tooltip("Vividness (Canlılık) - Otomatik hesaplanır")]
    [Range(0f, 1f)]
    public float Vividness = 0f;
    
    [Tooltip("Chaos (Kaos) - Otomatik hesaplanır")]
    [Range(0f, 1f)]
    public float Chaos = 0f;
    
    // Smooth geçişler için velocity değişkenleri
    private float smoothArousalVel = 0f;
    private float smoothRelaxationVel = 0f;
    private float smoothThermalStressVel = 0f;
    private float smoothFragmentationVel = 0f;
    private float smoothClarityVel = 0f;
    private float smoothVividnessVel = 0f;
    private float smoothChaosVel = 0f;
    private float smoothShrinkAmountVel = 0f;
    
    // Mevcut shrink değeri (smooth geçiş için)
    private float currentShrinkAmount = 0.900f; // Başlangıç değeri: 0.900 (küçük beyin - görsel olarak küçük başlar)
    
    // Material'dan okunan başlangıç değerleri
    private float initialShrinkAmount = 0.900f; // Başlangıç değeri: 0.900 (küçük beyin - görsel olarak küçük başlar)
    private float initialShrinkScale = 0.0256f; // Material'dan okunacak (_ShrinkScale)
    private float initialMinObjectScale = 0.6f; // Material'dan okunacak (_MinObjectScale)
    
    // Mevcut blur ve glitch değerleri (kazanma kontrolü için)
    private float currentBlurAmount = 0.8f;
    private float currentGlitchAmount = 0.5f;
    
    // Oyun durumu
    private bool gameEnded = false;
    private bool gameWon = false;
    private ButtonManager.ButtonType lastPressedButton; // Son basılan buton
    private bool hasAnyButtonBeenPressed = false; // Hiç buton basıldı mı? (ilk basış kontrolü için)

    // Material property ID'leri (performans için)
    // DREAM MATERIAL PROPERTIES
    private int blurAmountID;
    private int maxBlurRadiusID;
    private int blurSamplesID;
    private int noiseTilingDreamID;
    private int noiseSpeedID;
    private int uvDistortionID;
    private int waveAmplitudeID;
    private int waveFrequencyID;
    private int waveSpeedID;
    private int glitchAmountID;
    private int glitchSpeedID;
    private int glitchBlockSizeID;
    private int rgbShiftID;
    private int rimIntensityDreamID;
    private int rimPowerDreamID;
    private int rimColorDreamID;
    
    // BRAIN MATERIAL PROPERTIES
    private int shrinkProgressID;
    private int maxShrinkDepthID;
    private int noiseTilingBrainID;
    private int minMeshScaleID;
    private int rimIntensityBrainID;
    private int rimPowerBrainID;
    private int rimColorBrainID;
    
    // MaterialPropertyBlock (performans için)
    private MaterialPropertyBlock brainPropertyBlock;
    private MaterialPropertyBlock dreamPropertyBlock;
    
    // Renderer referansları
    private Renderer brainRenderer;
    private Renderer dreamRenderer;
    
    // Debug için: Son kontrol zamanı (her frame log yazmamak için)
    private float lastCheckTime = 0f;
    private int lastUsedButtonCount = -1;

    void Start()
    {
        // winShrinkThreshold değerini garanti et (Inspector'da yanlış ayarlanmış olabilir)
        if (winShrinkThreshold <= 0f || winShrinkThreshold > 1f)
        {
            winShrinkThreshold = 0.190f; // Varsayılan değer
            Debug.LogWarning($"DreamLogicController: winShrinkThreshold geçersiz değer, 0.190'a ayarlandı.");
        }
        
        InitializeMaterials();
        InitializePropertyIDs();
        
        // Material'dan başlangıç değerlerini oku
        ReadInitialValuesFromMaterial();
        
        // Tüm butonları bul (eğer atanmamışsa)
        if (allButtons == null || allButtons.Length == 0)
        {
            allButtons = FindObjectsOfType<ButtonManager>();
        }
        
        // WinLoseScreenManager'ı otomatik bul (eğer atanmamışsa)
        if (winLoseScreenManager == null)
        {
            winLoseScreenManager = FindObjectOfType<WinLoseScreenManager>();
        }
        
        // Oyunu başlat
        StartNewGame();
    }
    
    /// <summary>
    /// Material'dan başlangıç değerlerini oku
    /// NOT: ShrinkAmount başlangıç değeri 0.900 olarak sabitlenmiştir (oyun mekaniği için)
    /// </summary>
    private void ReadInitialValuesFromMaterial()
    {
        if (brainMat != null)
        {
            // ShrinkAmount'u oku (ama başlangıç değeri 0.900 olarak sabitlenmiştir)
            if (brainMat.HasProperty(shrinkProgressID))
            {
                // Material'dan oku ama başlangıç değeri 0.900 olarak sabit (oyun mekaniği için)
                // initialShrinkAmount = brainMat.GetFloat(shrinkProgressID); // Material değerini kullanma
                initialShrinkAmount = 0.900f; // Sabit başlangıç değeri (küçük beyin - görsel olarak küçük başlar)
                currentShrinkAmount = initialShrinkAmount;
                
                // Material'daki değeri de 0.900'e ayarla (görsel tutarlılık için)
                brainMat.SetFloat(shrinkProgressID, 0.900f);
            }
            
            // ShrinkScale'i oku
            if (brainMat.HasProperty(maxShrinkDepthID))
            {
                initialShrinkScale = brainMat.GetFloat(maxShrinkDepthID);
            }
            
            // MinObjectScale'i oku
            if (brainMat.HasProperty(minMeshScaleID))
            {
                initialMinObjectScale = brainMat.GetFloat(minMeshScaleID);
            }
            
            Debug.Log($"Material değerleri okundu: ShrinkAmount={initialShrinkAmount:F3} (başlangıç - küçük beyin), ShrinkScale={initialShrinkScale:F4}, MinObjectScale={initialMinObjectScale:F2}");
            Debug.Log($"Shrink Mantığı: YÜKSEK değer (0.900) = KÜÇÜK beyin, DÜŞÜK değer (0.350 altı) = BÜYÜK beyin");
        }
    }
    
    /// <summary>
    /// Yeni oyunu başlat (restart için)
    /// </summary>
    public void StartNewGame()
    {
        gameEnded = false;
        gameWon = false;
        
        // Win/Lose ekranlarını gizle
        if (winLoseScreenManager != null)
        {
            winLoseScreenManager.HideAllScreens();
        }
        hasAnyButtonBeenPressed = false; // İlk basış kontrolünü sıfırla
        
        // Tüm butonları sıfırla
        if (allButtons != null)
        {
            foreach (ButtonManager button in allButtons)
            {
                if (button != null)
                {
                    button.ResetButton();
                }
            }
        }
        
        // Başlangıç değerlerini material'dan okunan değerlere ayarla
        currentShrinkAmount = initialShrinkAmount; // Material'daki başlangıç değeri
        
        // Tüm input değerlerini sıfırla
        caffeineSmell = 0f;
        emf = 0f;
        lavenderSmell = 0f;
        warmAir = 0f;
        melatonin = 0f;
        
        // Tüm state'leri başlangıç değerlerine direkt ayarla (animasyon olmasın)
        Arousal = 0f;
        Relaxation = 0f;
        ThermalStress = 0f;
        Fragmentation = 0f;
        Clarity = 0.2f; // Başlangıçta düşük (blur yüksek)
        Vividness = 0.2f;
        Chaos = 0.5f; // Başlangıçta orta seviye
        
        // Smooth geçiş velocity'lerini sıfırla
        smoothArousalVel = 0f;
        smoothRelaxationVel = 0f;
        smoothThermalStressVel = 0f;
        smoothFragmentationVel = 0f;
        smoothClarityVel = 0f;
        smoothVividnessVel = 0f;
        smoothChaosVel = 0f;
        smoothShrinkAmountVel = 0f;
        
        // Material property'lerini material'daki başlangıç değerlerine ayarla
        if (brainRenderer != null && brainPropertyBlock != null && brainMat != null)
        {
            // Material'daki başlangıç shrink değerini kullan (0.900 - küçük beyin)
            if (brainMat.HasProperty(shrinkProgressID))
                brainPropertyBlock.SetFloat(shrinkProgressID, initialShrinkAmount); // 0.900
            
            // Material'daki başlangıç shrink scale ve min mesh scale değerlerini kullan
            if (brainMat.HasProperty(maxShrinkDepthID))
                brainPropertyBlock.SetFloat(maxShrinkDepthID, initialShrinkScale);
            if (brainMat.HasProperty(minMeshScaleID))
                brainPropertyBlock.SetFloat(minMeshScaleID, initialMinObjectScale);
            
            brainRenderer.SetPropertyBlock(brainPropertyBlock);
        }
        
        // Dream material'ı da başlangıç değerlerine ayarla
        if (dreamRenderer != null && dreamPropertyBlock != null && dreamMat != null)
        {
            // Başlangıçta blur yüksek (Clarity düşük)
            if (dreamMat.HasProperty(blurAmountID))
                dreamPropertyBlock.SetFloat(blurAmountID, 0.8f); // Yüksek blur
            
            // Başlangıç glitch
            if (dreamMat.HasProperty(glitchAmountID))
                dreamPropertyBlock.SetFloat(glitchAmountID, 0.5f); // Orta seviye glitch
            
            dreamRenderer.SetPropertyBlock(dreamPropertyBlock);
        }
        
        Debug.Log("Yeni oyun başlatıldı!");
    }

    void Update()
    {
        // Eğer oyun bittiyse güncelleme yapma
        if (gameEnded)
        {
            return;
        }
        
        // Input'lardan latent state'leri hesapla
        CalculateLatentStates();
        
        // Derived state'leri hesapla (Clarity, Vividness, Chaos)
        CalculateDerivedStates();
        
        // Material property'lerini yeni state'lere göre güncelle
        UpdateMaterialProperties();
        
        // Tüm butonlar bitti mi kontrol et
        CheckGameEnd();
    }
    
    /// <summary>
    /// Buton basıldığında çağrılır
    /// </summary>
    public void OnButtonPressed(ButtonManager.ButtonType buttonType)
    {
        if (gameEnded)
            return;
        
        // İlk basış işaretini ayarla
        hasAnyButtonBeenPressed = true;
        
        // Son basılan butonu kaydet
        lastPressedButton = buttonType;
        
        Debug.Log($"Buton basıldı: {buttonType}");
        
        // Buton basıldıktan sonra oyun bitiş kontrolü yap (buton durumu değişmiş olabilir)
        CheckGameEnd();
    }
    
    /// <summary>
    /// Son basılan butonu döndür
    /// </summary>
    public ButtonManager.ButtonType GetLastPressedButton()
    {
        return lastPressedButton;
    }
    
    /// <summary>
    /// Hiç buton basıldı mı? (ilk basış kontrolü için)
    /// </summary>
    public bool HasAnyButtonBeenPressed()
    {
        return hasAnyButtonBeenPressed;
    }
    
    /// <summary>
    /// Tüm butonlar bitti mi kontrol et
    /// </summary>
    private void CheckGameEnd()
    {
        if (gameEnded)
            return;
        
        // Butonları bul (eğer null ise veya boşsa)
        if (allButtons == null || allButtons.Length == 0)
        {
            allButtons = FindObjectsOfType<ButtonManager>();
            Debug.Log($"CheckGameEnd: Butonlar bulundu. Toplam buton sayısı: {allButtons?.Length ?? 0}");
        }
        
        // Tüm OYUN butonları kullanıldı mı? (5 buton: Kafein, Radyasyon, Lavanta, Isı, Melatonin)
        // NOT: Restart butonu sayılmaz (RestartButton scripti kullanır, ButtonManager değil)
        // NOT: Her buton tipinden sadece BİR TANE sayılır (eğer aynı tipte birden fazla buton varsa)
        bool allButtonsUsed = true;
        int usedButtonCount = 0;
        int totalButtonCount = 0;
        
        // Oyun butonları: Sadece bu 5 buton tipi sayılır
        ButtonManager.ButtonType[] gameButtonTypes = {
            ButtonManager.ButtonType.Caffeine,
            ButtonManager.ButtonType.Radiation,
            ButtonManager.ButtonType.Lavender,
            ButtonManager.ButtonType.Heat,
            ButtonManager.ButtonType.Melatonin
        };
        
        // Her buton tipinden sadece birini saymak için (eğer aynı tipte birden fazla buton varsa)
        // Her buton tipi için en iyi butonu seç (kullanılan olanı tercih et, yoksa ilk bulunanı)
        ButtonManager[] foundGameButtons = new ButtonManager[gameButtonTypes.Length];
        
        if (allButtons != null && allButtons.Length > 0)
        {
            foreach (ButtonManager button in allButtons)
            {
                if (button != null)
                {
                    // Sadece oyun butonlarını say (Restart butonu hariç)
                    int buttonTypeIndex = System.Array.IndexOf(gameButtonTypes, button.buttonType);
                    
                    if (buttonTypeIndex >= 0)
                    {
                        // Bu buton tipinden daha önce bir buton bulunmadıysa
                        if (foundGameButtons[buttonTypeIndex] == null)
                        {
                            foundGameButtons[buttonTypeIndex] = button;
                        }
                        else
                        {
                            // Eğer bu buton kullanıldıysa ve önceki bulunan buton kullanılmadıysa, bu butonu tercih et
                            if (button.IsButtonUsed() && !foundGameButtons[buttonTypeIndex].IsButtonUsed())
                            {
                                foundGameButtons[buttonTypeIndex] = button;
                            }
                        }
                    }
                }
            }
            
            // Şimdi bulunan oyun butonlarını kontrol et
            for (int i = 0; i < gameButtonTypes.Length; i++)
            {
                if (foundGameButtons[i] != null)
                {
                    totalButtonCount++;
                    bool isUsed = foundGameButtons[i].IsButtonUsed();
                    int remaining = foundGameButtons[i].GetRemainingPressCount();
                    
                    if (isUsed)
                    {
                        usedButtonCount++;
                    }
                    else
                    {
                        allButtonsUsed = false;
                    }
                }
            }
        }
        else
        {
            // Butonlar bulunamadı
            Debug.LogWarning("CheckGameEnd: Butonlar bulunamadı! allButtons null veya boş.");
            return;
        }
        
        // Tüm OYUN butonları kullanıldı mı kontrol et (5 buton: Kafein, Radyasyon, Lavanta, Isı, Melatonin)
        if (allButtonsUsed && totalButtonCount == 5) // Tam 5 oyun butonu olmalı
        {
            // Tüm oyun butonları kullanıldı, oyun bitti - kazanma/kaybetme kontrolü yap
            Debug.Log($"=== TÜM OYUN BUTONLARI KULLANILDI ===");
            Debug.Log($"Kullanılan buton sayısı: {usedButtonCount}/{totalButtonCount}");
            
            // Her oyun butonunun durumunu göster (sadece bulunan butonlar)
            for (int i = 0; i < gameButtonTypes.Length; i++)
            {
                if (foundGameButtons[i] != null)
                {
                    Debug.Log($"  - {foundGameButtons[i].buttonType}: Kullanıldı={foundGameButtons[i].IsButtonUsed()}, Kalan Hak={foundGameButtons[i].GetRemainingPressCount()}");
                }
            }
            
            EndGame();
        }
        else if (totalButtonCount < 5)
        {
            // Yeterli oyun butonu yok - debug için bir kere log yaz
            if (Time.time - lastCheckTime > 5f) // 5 saniyede bir log yaz
            {
                Debug.LogWarning($"CheckGameEnd: Yeterli oyun butonu bulunamadı! Bulunan oyun butonu sayısı: {totalButtonCount} (Beklenen: 5)");
                lastCheckTime = Time.time;
            }
        }
        else if (!allButtonsUsed)
        {
            // Bazı oyun butonları henüz kullanılmadı - sadece durum değiştiğinde log yaz
            if (usedButtonCount != lastUsedButtonCount)
            {
                Debug.Log($"CheckGameEnd: Henüz tüm oyun butonları kullanılmadı. Kullanılan: {usedButtonCount}/{totalButtonCount}");
                for (int i = 0; i < gameButtonTypes.Length; i++)
                {
                    if (foundGameButtons[i] != null && !foundGameButtons[i].IsButtonUsed())
                    {
                        Debug.Log($"  - {foundGameButtons[i].buttonType}: Kullanılmadı, Kalan Hak={foundGameButtons[i].GetRemainingPressCount()}");
                    }
                }
                lastUsedButtonCount = usedButtonCount;
            }
        }
    }
    
    /// <summary>
    /// Oyunu bitir ve kazanma/kaybetme kontrolü yap
    /// </summary>
    private void EndGame()
    {
        gameEnded = true;
        
        // winShrinkThreshold değerini garanti et (güvenlik kontrolü - Inspector'da yanlış ayarlanmış olabilir)
        const float WIN_THRESHOLD = 0.190f; // Sabit değer - her zaman 0.190
        if (winShrinkThreshold <= 0f || winShrinkThreshold > 1f)
        {
            winShrinkThreshold = WIN_THRESHOLD;
            Debug.LogWarning($"EndGame: winShrinkThreshold geçersiz ({winShrinkThreshold}), {WIN_THRESHOLD}'e ayarlandı.");
        }
        
        // Kazanma kontrolü için sabit threshold kullan (Inspector değerinden bağımsız)
        float actualThreshold = WIN_THRESHOLD;
        
        // DEBUG: Beyin büyüklüğü ve rüya netliği bilgilerini göster
        Debug.Log($"=== OYUN BİTTİ ===");
        Debug.Log($"Beyin Büyüklüğü (Shrink): {currentShrinkAmount:F3}");
        Debug.Log($"Kazanma Threshold: {actualThreshold:F3} (Shrink <= {actualThreshold:F3} ise KAZANMA)");
        Debug.Log($"Rüya Netliği (Blur): {currentBlurAmount:F3}");
        Debug.Log($"Rüya Kalitesi (Glitch): {currentGlitchAmount:F3}");
        
        // Kazanma koşulu: Shrink değeri 0.190'dan küçük veya eşit olmalı
        bool brainIsMax = currentShrinkAmount <= actualThreshold;
        
        // Debug: Karşılaştırma detayları
        Debug.Log($"=== KAZANMA KONTROLÜ ===");
        Debug.Log($"Shrink Değeri: {currentShrinkAmount:F3}");
        Debug.Log($"Threshold: {actualThreshold:F3}");
        Debug.Log($"Karşılaştırma: {currentShrinkAmount:F3} <= {actualThreshold:F3} = {brainIsMax}");
        
        // Kazanma: Shrink <= 0.190
        bool hasWon = brainIsMax;
        
        if (hasWon)
        {
            // KAZANILDI! Shrink değeri 0.190'dan küçük veya eşit
            gameWon = true;
            Debug.Log($"✅✅✅ OYUN KAZANILDI! ✅✅✅");
            Debug.Log($"Beyin yeterince büyük: Shrink={currentShrinkAmount:F3} <= Threshold={actualThreshold:F3}");
            Debug.Log($"Rüya Netliği: Blur={currentBlurAmount:F3}, Glitch={currentGlitchAmount:F3}");
            OnGameWon();
        }
        else
        {
            // KAYBEDİLDİ! Shrink değeri 0.190'dan büyük
            gameWon = false;
            Debug.Log($"❌❌❌ OYUN KAYBEDİLDİ! ❌❌❌");
            Debug.Log($"Beyin yeterince büyük değil: Shrink={currentShrinkAmount:F3} > Threshold={actualThreshold:F3}");
            Debug.Log($"Rüya Netliği: Blur={currentBlurAmount:F3}, Glitch={currentGlitchAmount:F3}");
            OnGameLost();
        }
    }
    
    /// <summary>
    /// Oyun kazanıldığında çağrılır
    /// </summary>
    private void OnGameWon()
    {
        Debug.Log("TEBRİKLER! Beyin büyütüldü ve rüya netleştirildi!");
        
        // Win/Lose Screen Manager varsa win ekranını göster
        if (winLoseScreenManager != null)
        {
            winLoseScreenManager.ShowWinScreen();
        }
        
        // Eski UI Feedback göster (geriye dönük uyumluluk için)
        ShowWinFeedback();
        
        // Kazanma durumunda özel bir şey yapılabilir (ses efekti, animasyon vb.)
        // Örnek: Time.timeScale = 0.5f; // Slow motion efekti
        
        // NOT: Restart butonu WinLoseScreenManager'da yönetiliyor, burada otomatik restart yapmıyoruz
    }
    
    /// <summary>
    /// Kazanma UI Feedback göster
    /// </summary>
    private void ShowWinFeedback()
    {
        // Unity UI Text kullanılıyorsa
        if (winText != null)
        {
            winText.text = winMessage;
            winText.gameObject.SetActive(true);
            
            // Belirli süre sonra gizle
            StartCoroutine(HideWinFeedbackAfterDelay(winDisplayDuration, false));
        }
        // TextMeshPro kullanılıyorsa
        else if (winTextTMP != null)
        {
            winTextTMP.text = winMessage;
            winTextTMP.gameObject.SetActive(true);
            
            // Belirli süre sonra gizle
            StartCoroutine(HideWinFeedbackAfterDelay(winDisplayDuration, true));
        }
    }
    
    /// <summary>
    /// Kazanma feedback'ini belirli bir süre sonra gizle
    /// </summary>
    private System.Collections.IEnumerator HideWinFeedbackAfterDelay(float delay, bool isTMP)
    {
        yield return new WaitForSeconds(delay);
        if (isTMP)
        {
            if (winTextTMP != null)
            {
                winTextTMP.gameObject.SetActive(false);
            }
        }
        else
        {
            if (winText != null)
            {
                winText.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Oyun kaybedildiğinde çağrılır
    /// </summary>
    private void OnGameLost()
    {
        Debug.Log("OYUN BİTTİ! Tekrar deneyin...");
        
        // Win/Lose Screen Manager varsa lose ekranını göster
        if (winLoseScreenManager != null)
        {
            winLoseScreenManager.ShowLoseScreen();
        }
        
        // NOT: Restart butonu WinLoseScreenManager'da yönetiliyor, burada otomatik restart yapmıyoruz
    }

    /// <summary>
    /// Material referanslarını ve property ID'lerini başlat
    /// </summary>
    private void InitializeMaterials()
    {
        // Renderer'ları bul
        if (brainMat != null)
        {
            Renderer[] allRenderers = FindObjectsOfType<Renderer>();
            foreach (Renderer renderer in allRenderers)
            {
                if (renderer.sharedMaterial == brainMat || renderer.material == brainMat)
                {
                    brainRenderer = renderer;
                    break;
                }
            }
        }
        
        if (dreamMat != null)
        {
            Renderer[] allRenderers = FindObjectsOfType<Renderer>();
            foreach (Renderer renderer in allRenderers)
            {
                if (renderer.sharedMaterial == dreamMat || renderer.material == dreamMat)
                {
                    dreamRenderer = renderer;
                    break;
                }
            }
        }
        
        // MaterialPropertyBlock'ları oluştur
        brainPropertyBlock = new MaterialPropertyBlock();
        dreamPropertyBlock = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Shader property ID'lerini cache'le (performans için)
    /// </summary>
    private void InitializePropertyIDs()
    {
        // DREAM MATERIAL PROPERTIES (Shader property isimlerine göre)
        blurAmountID = Shader.PropertyToID("_Blur"); // Shader'da _Blur
        maxBlurRadiusID = Shader.PropertyToID("_BlurRadius"); // Shader'da _BlurRadius
        blurSamplesID = Shader.PropertyToID("_Samples"); // Shader'da _Samples
        noiseTilingDreamID = Shader.PropertyToID("_NoiseScale"); // Shader'da _NoiseScale
        noiseSpeedID = Shader.PropertyToID("_NoiseSpeed"); // Shader'da _NoiseSpeed
        uvDistortionID = Shader.PropertyToID("_Distort"); // Shader'da _Distort
        waveAmplitudeID = Shader.PropertyToID("_WaveAmp"); // Shader'da _WaveAmp
        waveFrequencyID = Shader.PropertyToID("_WaveFreq"); // Shader'da _WaveFreq
        waveSpeedID = Shader.PropertyToID("_WaveSpeed"); // Shader'da _WaveSpeed
        glitchAmountID = Shader.PropertyToID("_Glitch"); // Shader'da _Glitch
        glitchSpeedID = Shader.PropertyToID("_GlitchSpeed"); // Shader'da _GlitchSpeed
        glitchBlockSizeID = Shader.PropertyToID("_GlitchBlockSize"); // Shader'da _GlitchBlockSize
        rgbShiftID = Shader.PropertyToID("_RGBShift"); // Shader'da _RGBShift
        rimIntensityDreamID = Shader.PropertyToID("_RimIntensity"); // Shader'da _RimIntensity
        rimPowerDreamID = Shader.PropertyToID("_RimPower"); // Shader'da _RimPower
        rimColorDreamID = Shader.PropertyToID("_RimColor"); // Shader'da _RimColor
        
        // BRAIN MATERIAL PROPERTIES
        shrinkProgressID = Shader.PropertyToID("_ShrinkAmount"); // Shader'da _ShrinkAmount
        maxShrinkDepthID = Shader.PropertyToID("_ShrinkScale"); // Shader'da _ShrinkScale
        noiseTilingBrainID = Shader.PropertyToID("_NoiseTiling");
        minMeshScaleID = Shader.PropertyToID("_MinObjectScale"); // Shader'da _MinObjectScale
        rimIntensityBrainID = Shader.PropertyToID("_RimIntensity");
        rimPowerBrainID = Shader.PropertyToID("_FresnelPower"); // Shader'da _FresnelPower
        rimColorBrainID = Shader.PropertyToID("_SciFiColor"); // Shader'da _SciFiColor
    }

    /// <summary>
    /// Input'lardan latent state'leri hesapla (Arousal, Relaxation, ThermalStress, Fragmentation)
    /// </summary>
    private void CalculateLatentStates()
    {
        // Başlangıç değerleri
        float targetArousal = 0f;
        float targetRelaxation = 0f;
        float targetThermalStress = 0f;
        float targetFragmentation = 0f;
        
        // KAFEIN kuralları:
        // - Beyni büyütür AMA glitch artırır (anksiyetik etki - trade-off)
        // - Netliği biraz artırır ama tek başına yeterli değil
        targetArousal += 0.80f * caffeineSmell; // Yüksek uyarılma (beyin aktivitesi artar)
        targetFragmentation -= 0.15f * caffeineSmell; // Parçalanma azalır (beyin sağlığı artar - ama daha az)
        targetRelaxation -= 0.30f * caffeineSmell; // Rahatlama azalır (uyarıcı etki - glitch artırır)
        targetThermalStress += 0.10f * caffeineSmell; // Hafif stres (anksiyetik etki)
        
        // RADYASYON (EMF) kuralları:
        // - Beyni aşırı derecede küçültür, netliği bozar
        targetFragmentation += 0.85f * emf; // Çok yüksek parçalanma (beyin sağlığını aşırı bozar)
        targetArousal += 0.15f * emf; // Hafif uyarılma
        targetThermalStress += 0.40f * emf; // Yüksek hücresel stres
        targetRelaxation -= 0.50f * emf; // Rahatlama çok azalır
        
        // LAVANTA kuralları:
        // - Beyni büyütür, netliği biraz artırır
        targetRelaxation += 0.70f * lavenderSmell; // Yüksek rahatlama
        targetFragmentation -= 0.40f * lavenderSmell; // Parçalanma azalır (beyin sağlığı artar)
        targetArousal -= 0.15f * lavenderSmell; // Uyarılma azalır
        targetThermalStress -= 0.20f * lavenderSmell; // Stres azalır
        
        // ISI (Warm Air) kuralları:
        // - Beyni küçültür (olumsuz etki)
        // - Rüya görünümü bozulur (olumsuz etki)
        // - Yüksek ısı uyku kalitesini bozar, beyin sağlığını olumsuz etkiler
        targetThermalStress += 0.60f * warmAir; // Yüksek termal stres (olumsuz)
        targetArousal -= 0.10f * warmAir; // Uyarılma azalır (uyku kalitesi bozulur)
        targetRelaxation -= 0.40f * warmAir; // Rahatlama azalır (uyku kalitesi bozulur - olumsuz)
        targetFragmentation += 0.50f * warmAir; // Parçalanma artar (beyin sağlığı bozulur - olumsuz)
        
        // MELATONIN kuralları:
        // - Beyni büyütür (iyileştirici etki)
        // - Rüya kalitesini iyileştirir (netlik ve tutarlılık artar)
        // - Derin uyku sağlar, beyin sağlığını artırır
        // - Kombinasyonlarda daha da güçlenir
        targetRelaxation += 0.50f * melatonin; // Yüksek rahatlama (derin uyku)
        targetFragmentation -= 0.35f * melatonin; // Parçalanma azalır (beyin sağlığı artar)
        targetArousal -= 0.10f * melatonin; // Uyarılma hafif azalır (derin uyku - ama beyin büyümesini engellemez)
        targetThermalStress -= 0.25f * melatonin; // Stres azaltma (iyileştirici)
        
        // Clamp to 0..1
        targetArousal = Mathf.Clamp01(targetArousal);
        targetRelaxation = Mathf.Clamp01(targetRelaxation);
        targetThermalStress = Mathf.Clamp01(targetThermalStress);
        targetFragmentation = Mathf.Clamp01(targetFragmentation);
        
        // Smooth geçişler (Mathf.SmoothDamp)
        // Eğer input değerleri değiştiyse daha hızlı geçiş yap (buton basıldığında hemen görünsün)
        bool hasActiveInput = (caffeineSmell > 0.1f || emf > 0.1f || lavenderSmell > 0.1f || warmAir > 0.1f || melatonin > 0.1f);
        float currentSmoothTime = hasActiveInput ? smoothTime * 0.5f : smoothTime; // Buton basıldığında 2x daha hızlı
        
        Arousal = Mathf.SmoothDamp(Arousal, targetArousal, ref smoothArousalVel, currentSmoothTime);
        Relaxation = Mathf.SmoothDamp(Relaxation, targetRelaxation, ref smoothRelaxationVel, currentSmoothTime);
        ThermalStress = Mathf.SmoothDamp(ThermalStress, targetThermalStress, ref smoothThermalStressVel, currentSmoothTime);
        Fragmentation = Mathf.SmoothDamp(Fragmentation, targetFragmentation, ref smoothFragmentationVel, currentSmoothTime);
    }
    
    /// <summary>
    /// Derived state'leri hesapla (Clarity, Vividness, Chaos)
    /// </summary>
    private void CalculateDerivedStates()
    {
        // Clarity (Rüya Netliği): 
        // - Kafein biraz artırır (ama glitch artırır - trade-off)
        // - Lavanta biraz artırır (netlik biraz artar)
        // - Melatonin artırır (derin uyku netliği iyileştirir)
        // - Radyasyon azaltır (netliği bozar)
        // - Isı azaltır (yüksek ısı uyku kalitesini bozar, netliği düşürür - olumsuz)
        float targetClarity = 0.30f * Arousal + 0.35f * Relaxation + 0.30f * melatonin - 0.80f * Fragmentation - 0.20f * ThermalStress - 0.30f * warmAir + 0.15f; // Melatonin netliği artırır
        targetClarity = Mathf.Clamp01(targetClarity);
        
        // Vividness (Canlılık):
        // - Kafein artırır (uyarılma)
        // - Lavanta hafif artırır (rahatlama)
        // - Radyasyon azaltır (stres)
        float targetVividness = 0.30f * Relaxation + 0.50f * Arousal - 0.25f * ThermalStress - 0.20f * Fragmentation + 0.25f;
        targetVividness = Mathf.Clamp01(targetVividness);
        
        // Chaos (Glitch/Anksiyete):
        // - Kafein artırır (anksiyetik - trade-off: beyin büyütür ama glitch artırır)
        // - Radyasyon artırır (yüksek parçalanma)
        // - Lavanta azaltır (rahatlama)
        // - Melatonin azaltır (derin uyku glitch'i azaltır)
        // - Isı orta seviyede
        float targetChaos = 0.60f * Fragmentation + 0.40f * ThermalStress + 0.60f * Arousal - 0.50f * Relaxation - 0.50f * melatonin; // Melatonin glitch'i daha fazla azaltır
        targetChaos = Mathf.Clamp01(targetChaos);
        
        // Smooth geçişler
        // Input değerleri değiştiyse smooth time'ı azalt (daha hızlı tepki)
        bool hasActiveInput = (caffeineSmell > 0.1f || emf > 0.1f || lavenderSmell > 0.1f || warmAir > 0.1f || melatonin > 0.1f);
        float currentSmoothTime = hasActiveInput ? smoothTime * 0.5f : smoothTime;
        
        Clarity = Mathf.SmoothDamp(Clarity, targetClarity, ref smoothClarityVel, currentSmoothTime);
        Vividness = Mathf.SmoothDamp(Vividness, targetVividness, ref smoothVividnessVel, currentSmoothTime);
        Chaos = Mathf.SmoothDamp(Chaos, targetChaos, ref smoothChaosVel, currentSmoothTime);
    }
    
    /// <summary>
    /// Aktif buton sayısını ve kombinasyon bonusunu hesapla
    /// </summary>
    private void CalculateCombinationBonus(out int activeButtonCount, out float combinationBonus)
    {
        activeButtonCount = 0;
        if (caffeineSmell > 0.5f) activeButtonCount++;
        if (emf > 0.5f) activeButtonCount++;
        if (lavenderSmell > 0.5f) activeButtonCount++;
        if (warmAir > 0.5f) activeButtonCount++;
        if (melatonin > 0.5f) activeButtonCount++;
        
        // Kombinasyon bonusu: İkili kombinasyonlar güçlü sinerji yaratır
        combinationBonus = 0f;
        
            // İkili kombinasyonlar (güçlü sinerji)
            if (activeButtonCount >= 2)
            {
                // Kafein + Lavanta: Optimal sinerji (Flow State) - Kafein'in glitch etkisini Lavanta dengeler
                if (caffeineSmell > 0.5f && lavenderSmell > 0.5f)
                    combinationBonus += 0.35f; // İyi sinerji (güçlendirildi)
                
                // Kafein + Lavanta + Melatonin: MÜKEMMEL SİNERJİ (Optimal Çözüm)
                // Bu kombinasyon tüm hedeflere ulaştırır: Shrink 0.350'e kadar, Blur 0, Glitch 0
                if (caffeineSmell > 0.5f && lavenderSmell > 0.5f && melatonin > 0.5f)
                    combinationBonus += 0.50f; // Güçlü sinerji (güçlendirildi)
                
                // Melatonin + Lavanta: Derin uyku + rahatlama (iyi kombinasyon)
                if (melatonin > 0.5f && lavenderSmell > 0.5f)
                    combinationBonus += 0.30f; // Orta seviye sinerji (güçlendirildi)
                
                // Melatonin + Kafein: İlginç sinerji (uyarı + derin uyku dengesi)
                // Kafein'in glitch etkisini Melatonin dengeler
                if (melatonin > 0.5f && caffeineSmell > 0.5f)
                    combinationBonus += 0.30f; // İyi sinerji (güçlendirildi)
            
            // Kafein + Isı: Zararlı kombinasyon (yüksek ısı + uyarılma = stres)
            if (caffeineSmell > 0.5f && warmAir > 0.5f)
                combinationBonus -= 0.15f; // Negatif bonus (olumsuz)
            
            // Lavanta + Isı: Lavanta ısının zararını biraz azaltır ama yine de olumsuz
            if (lavenderSmell > 0.5f && warmAir > 0.5f)
                combinationBonus -= 0.05f; // Hafif negatif (lavanta koruyucu ama yeterli değil)
            
            // Kafein + Radyasyon: ÇOK ZARARLI (cezalandırıcı kombinasyon)
            // Bu kombinasyon oyunu kaybettirir - yanlış strateji
            if (caffeineSmell > 0.5f && emf > 0.5f)
                combinationBonus -= 0.20f; // NEGATİF BONUS (cezalandırıcı)
            
            // Lavanta + Radyasyon: Lavanta radyasyonun zararını azaltır (koruyucu etki)
            if (lavenderSmell > 0.5f && emf > 0.5f)
                combinationBonus += 0.35f; // Lavanta koruyucu etki (artırıldı)
            
            // Melatonin + Radyasyon: Melatonin radyasyonun zararını azaltır
            if (melatonin > 0.5f && emf > 0.5f)
                combinationBonus += 0.30f; // Melatonin koruyucu etki (artırıldı)
            
            // Kafein + Lavanta + Melatonin + Radyasyon: Güçlü sinerji (tüm butonlar kullanıldığında)
            // Bu kombinasyon tüm butonlara basmak zorunda kalındığında kazanmayı sağlar
            if (caffeineSmell > 0.5f && lavenderSmell > 0.5f && melatonin > 0.5f && emf > 0.5f)
                combinationBonus += 0.60f; // Ekstra sinerji (tüm butonlar birlikte - güçlendirildi)
            
            // Kafein + Lavanta + Melatonin + Radyasyon + Isı: TÜM BUTONLAR (Maksimum Sinerji)
            // Bu, tüm butonlara 3'er kere basıldığında kazanmayı sağlayan optimal kombinasyon
            // Isı olumsuz ama diğer butonların gücü ısının zararını dengeler
            if (caffeineSmell > 0.5f && lavenderSmell > 0.5f && melatonin > 0.5f && emf > 0.5f && warmAir > 0.5f)
                combinationBonus += 0.50f; // Maksimum sinerji (tüm butonlar birlikte - güçlendirildi)
        }
        
        // Üçlü kombinasyonlar (güçlü sinerji)
        if (activeButtonCount >= 3)
        {
            combinationBonus += 0.25f; // Ekstra bonus (güçlendirildi)
        }
        
        // Dörtlü kombinasyon (maksimum sinerji)
        if (activeButtonCount >= 4)
        {
            combinationBonus += 0.30f; // Ekstra bonus (güçlendirildi)
        }
        
        // Beşli kombinasyon (tüm butonlar - maksimum sinerji)
        if (activeButtonCount >= 5)
        {
            combinationBonus += 0.35f; // Ekstra bonus (tüm butonlar birlikte)
        }
        
        // Clamp combination bonus (negatif olabilir - cezalandırıcı kombinasyonlar için)
        combinationBonus = Mathf.Clamp(combinationBonus, -0.30f, 1.0f);
    }
    
    /// <summary>
    /// Material property'lerini yeni state'lere göre güncelle
    /// </summary>
    private void UpdateMaterialProperties()
    {
        // Kombinasyon bonusunu hesapla
        int activeButtonCount;
        float combinationBonus;
        CalculateCombinationBonus(out activeButtonCount, out combinationBonus);
        
        // BRAIN MATERIAL MAPPING
        if (brainRenderer != null && brainPropertyBlock != null && brainMat != null)
        {
            // ShrinkAmount: Başlangıç 1.0 (maksimum küçük), kombinasyonlarla 0'a yaklaşır
            // StructuralIntegrity hesaplama:
            // - Lavanta: Relaxation artırır, Fragmentation azaltır → StructuralIntegrity artar (beyin büyür)
            // - Kafein: Arousal artırır ama Fragmentation düşük → StructuralIntegrity artar (beyin büyür)
            // - Radyasyon: Fragmentation yüksek → StructuralIntegrity azalır (beyin küçülür)
            // - Isı: Orta seviye etki → StructuralIntegrity orta seviyede
            // 
            // Arousal (kafein) beyin aktivitesini artırır, bu da beyin sağlığı için pozitif
            // Relaxation (lavanta) beyin sağlığını artırır
            // Fragmentation (radyasyon) beyin sağlığını bozar
            // ThermalStress (ısı) orta seviyede etki eder
            // StructuralIntegrity hesaplama: Beyin sağlığı ve büyüme potansiyeli
            // Melatonin tek başına NEREDEYSE HİÇ ETKİSİ YOK, sadece kombinasyonlarda güçlenir
            
            // Radyasyon'un zararını azaltmak için: Eğer Lavanta veya Melatonin varsa, Radyasyon'un etkisi azalır
            // Bu, tüm butonlara basmak zorunda kalındığında Radyasyon'un zararını dengelemek için
            float radiationDamage = 0.60f * Fragmentation; // Radyasyon'un zararı
            float protectionFactor = 0f;
            if (lavenderSmell > 0.3f) protectionFactor += 0.50f; // Lavanta güçlü koruyucu
            if (melatonin > 0.3f) protectionFactor += 0.40f; // Melatonin koruyucu
            if (caffeineSmell > 0.3f && lavenderSmell > 0.3f) protectionFactor += 0.30f; // Kafein + Lavanta kombinasyonu koruyucu
            protectionFactor = Mathf.Clamp01(protectionFactor);
            radiationDamage *= (1f - protectionFactor); // Koruyucu faktör radyasyon zararını azaltır
            
            // StructuralIntegrity hesaplama: Beyin sağlığı ve büyüme potansiyeli
            // Shrink değeri 0.900'dan 0.350'e düşmek için StructuralIntegrity ≈ 0.61 olmalı
            // Bu yüzden butonların etkilerini güçlendirmeliyiz
            float baseStructuralIntegrity = Mathf.Clamp01(
                0.40f * Relaxation +      // Lavanta: Beyin sağlığını artırır (güçlendirildi)
                0.35f * Arousal +        // Kafein: Aktivite artışı beyin büyümesine yardımcı (güçlendirildi)
                0.20f * Clarity +        // Netlik beyin sağlığını gösterir (güçlendirildi)
                0.35f * melatonin +      // Melatonin: Beyin sağlığını artırır (güçlendirildi)
                radiationDamage -        // Radyasyon: Yüksek parçalanma (ama koruyucu faktörlerle azaltılmış)
                0.40f * ThermalStress -  // Isı: Yüksek termal stres (olumsuz etki)
                0.25f * warmAir          // Isı: Direkt olumsuz etki (beyin sağlığını bozar)
            );
            
            // KOMBİNASYON BONUSU: Kombinasyonlar StructuralIntegrity'yi güçlendirir
            // Kombinasyon bonusunu artırdık (daha güçlü sinerji)
            float structuralIntegrity = Mathf.Clamp01(baseStructuralIntegrity + combinationBonus * 0.70f);
            structuralIntegrity = Mathf.Clamp01(structuralIntegrity);
            
            // Hedef shrink değeri: StructuralIntegrity arttıkça başlangıç değerinden 0'a git
            // Material'daki başlangıç değerinden (initialShrinkAmount) başla
            // StructuralIntegrity arttıkça 0'a git
            float targetShrinkAmount = Mathf.Lerp(initialShrinkAmount, 0.0f, structuralIntegrity);
            targetShrinkAmount = Mathf.Clamp01(targetShrinkAmount);
            
            // Smooth geçiş - Input değerleri değiştiyse çok daha hızlı tepki ver
            bool hasActiveInput = (caffeineSmell > 0.1f || emf > 0.1f || lavenderSmell > 0.1f || warmAir > 0.1f || melatonin > 0.1f);
            // Buton basıldığında çok daha hızlı tepki ver (0.1 saniye veya daha hızlı)
            float currentSmoothTime = hasActiveInput ? Mathf.Max(0.05f, smoothTime * 0.15f) : smoothTime;
            
            currentShrinkAmount = Mathf.SmoothDamp(currentShrinkAmount, targetShrinkAmount, ref smoothShrinkAmountVel, currentSmoothTime);
            currentShrinkAmount = Mathf.Clamp01(currentShrinkAmount);
            
            if (brainMat.HasProperty(shrinkProgressID))
                brainPropertyBlock.SetFloat(shrinkProgressID, currentShrinkAmount);
            
            // ShrinkScale: Shrink amount azaldıkça (beyin büyüdükçe) shrink scale de azalır
            // Material'daki initialShrinkScale (0.0256) başlangıç değeri
            // Shrink amount 0.0'a gittiğinde shrink scale de 0.0'a yakın olmalı (beyin büyüdü, derinlik yok)
            // Shrink amount initialShrinkAmount'tan 1.0'a gittiğinde shrink scale artmalı (beyin küçüldü, derinlik arttı)
            float minDepth = 0.01f; // Minimum derinlik (beyin büyüdüğünde)
            float maxDepth = Mathf.Max(initialShrinkScale * 2f, 0.08f); // Maximum derinlik (beyin küçüldüğünde, ama çok agresif olmasın)
            // Shrink amount'a göre interpolasyon: 0.0 = min depth, initialShrinkAmount = initialShrinkScale, 1.0 = max depth
            float shrinkScale;
            if (currentShrinkAmount <= initialShrinkAmount)
            {
                // Shrink amount başlangıç değerinden küçük veya eşit (beyin büyüyor)
                shrinkScale = Mathf.Lerp(minDepth, initialShrinkScale, currentShrinkAmount / initialShrinkAmount);
            }
            else
            {
                // Shrink amount başlangıç değerinden büyük (beyin küçülüyor)
                float progress = Mathf.InverseLerp(initialShrinkAmount, 1.0f, currentShrinkAmount);
                shrinkScale = Mathf.Lerp(initialShrinkScale, maxDepth, progress);
            }
            shrinkScale = Mathf.Clamp(shrinkScale, minDepth, maxDepth);
            if (brainMat.HasProperty(maxShrinkDepthID))
                brainPropertyBlock.SetFloat(maxShrinkDepthID, shrinkScale);
            
            // MinObjectScale: Shrink amount arttıkça (beyin küçüldükçe) mesh scale azalır
            // Shrink değeri YÜKSEK = beyin KÜÇÜK (shader mantığı: lerp(1.0, _MinObjectScale, _ShrinkAmount))
            // Shrink amount 0.0 olduğunda mesh scale 1.0 olmalı (tam büyük)
            // Shrink amount 1.0 olduğunda mesh scale minimum olmalı (küçük beyin)
            // Başlangıç shrink = 0.900 olduğunda beyin küçük görünmeli (görsel olarak küçük başlar)
            // Kazanma shrink = 0.350 olduğunda beyin büyük görünmeli
            // Shader mantığı: lerp(1.0, _MinObjectScale, _ShrinkAmount)
            // Yani shrink=0.0 → scale=1.0 (büyük), shrink=1.0 → scale=_MinObjectScale (küçük)
            // Başlangıç shrink=0.900 olduğunda beyin küçük görünmeli (scale ≈ 0.55)
            // Kazanma shrink=0.350 olduğunda beyin büyük görünmeli (scale ≈ 0.81)
            // Bu yüzden MinObjectScale'i daha düşük yapmalıyız (0.4-0.45 arası)
            float minScaleLimit = Mathf.Min(initialMinObjectScale, 0.45f); // Maximum 0.45 (daha belirgin küçük beyin için)
            // Shrink değeri arttıkça scale azalır (shader mantığına uygun)
            float minMeshScale = Mathf.Lerp(1.0f, minScaleLimit, currentShrinkAmount);
            minMeshScale = Mathf.Clamp(minMeshScale, minScaleLimit, 1.0f);
            if (brainMat.HasProperty(minMeshScaleID))
                brainPropertyBlock.SetFloat(minMeshScaleID, minMeshScale);
            
            // Neural Activity: Beyin aktivitesini gösteren yeni parametre (kombinasyonlarla artar)
            // Kafein aktiviteyi artırır, Lavanta denge sağlar, kombinasyonlar sinerji yaratır
            float neuralActivity = Mathf.Clamp01(
                0.50f * Arousal +                    // Kafein: Yüksek aktivite
                0.30f * Relaxation +                // Lavanta: Dengeli aktivite
                0.20f * (1f - Fragmentation) +       // Radyasyon olmadığında aktivite artar
                combinationBonus * 0.40f            // Kombinasyonlar sinerji yaratır
            );
            
            // Recovery Rate: İyileşme hızını gösteren yeni parametre
            // Kombinasyonlar (özellikle Lavanta + Kafein) iyileşmeyi hızlandırır
            float recoveryRate = Mathf.Clamp01(
                0.40f * Relaxation +                // Lavanta: İyileşme
                0.25f * (1f - Fragmentation) +      // Radyasyon olmadığında iyileşme
                0.20f * Clarity +                   // Netlik iyileşme göstergesi
                combinationBonus * 0.50f             // Kombinasyonlar iyileşmeyi hızlandırır
            );
            
            // NoiseTiling: Kombinasyonlarla daha dinamik
            // Yüksek aktivite ve kombinasyonlar noise pattern'ini değiştirir
            float baseNoiseTiling = Mathf.Lerp(1.0f, 6.0f, Chaos);
            float neuralNoiseBoost = neuralActivity * 2.0f; // Neural activity noise'u artırır
            float noiseTiling = Mathf.Clamp(baseNoiseTiling + neuralNoiseBoost, 1.0f, 12.0f);
            if (brainMat.HasProperty(noiseTilingBrainID))
                brainPropertyBlock.SetFloat(noiseTilingBrainID, noiseTiling);
            
            // RimIntensity: Kombinasyonlarla çok daha güçlü
            // Neural Activity ve Recovery Rate rim'i güçlendirir
            // Stres durumunda (Fragmentation/ThermalStress) intensity azalır (kırmızı yerine bu feedback)
            float baseRimIntensityFactor = Mathf.Clamp01(0.6f * Arousal + 0.4f * Relaxation);
            float neuralRimBoost = neuralActivity * 0.6f; // Neural activity rim'i parlatır
            float recoveryRimBoost = recoveryRate * 0.4f;  // Recovery rate rim'i iyileştirir
            float stressRimReduction = (Fragmentation + ThermalStress) * 0.5f; // Stres rim'i zayıflatır (kırmızı yerine bu feedback)
            float rimIntensityFactor = Mathf.Clamp01(baseRimIntensityFactor + neuralRimBoost + recoveryRimBoost - stressRimReduction);
            float rimIntensity = Mathf.Lerp(1.0f, 15.0f, rimIntensityFactor); // Maksimum değer artırıldı
            if (brainMat.HasProperty(rimIntensityBrainID))
                brainPropertyBlock.SetFloat(rimIntensityBrainID, rimIntensity);
            
            // RimPower: Neural Activity ve stres durumuna göre dinamik
            // Yüksek aktivite rim'i daha yumuşak yapar (daha geniş glow)
            // Stres durumunda (Fragmentation/ThermalStress) rim daha dar olur (kırmızı yerine bu feedback)
            float baseRimPower = Mathf.Lerp(8.0f, 2.5f, Chaos);
            float neuralPowerModifier = neuralActivity * -2.0f; // Aktivite arttıkça power azalır (daha geniş)
            float stressPowerBoost = (Fragmentation + ThermalStress) * 3.0f; // Stres rim'i daraltır (kırmızı yerine bu feedback)
            float rimPower = Mathf.Clamp(baseRimPower + neuralPowerModifier + stressPowerBoost, 1.5f, 10.0f);
            if (brainMat.HasProperty(rimPowerBrainID))
                brainPropertyBlock.SetFloat(rimPowerBrainID, rimPower);
            
            // RimColor: SABİT RENK (değişmez, material'daki değer kullanılır)
            // Rim color artık değişmiyor, material'daki başlangıç değeri kullanılıyor
            // Material'daki _SciFiColor değeri sabit kalacak
            
            // Material property block'ı her frame uygula (değişikliklerin görünmesi için)
            // ÖNEMLİ: SetPropertyBlock her frame çağrılmalı ki değişiklikler görünsün
            if (brainRenderer != null)
            {
                brainRenderer.SetPropertyBlock(brainPropertyBlock);
            }
        }
        
        // DREAM MATERIAL MAPPING
        if (dreamRenderer != null && dreamPropertyBlock != null && dreamMat != null)
        {
            // ÖNEMLİ: Beyin büyüklüğü faktörü - blur ve glitch hesaplamalarında kullanılacak
            // Beyin shrink değeri: 0 = max büyük, 1 = min küçük
            // Beyin max değilse (shrink > winShrinkThreshold) blur/glitch minimum değerde kalır
            float brainSizeFactor = Mathf.InverseLerp(winShrinkThreshold, 1.0f, currentShrinkAmount); // 0 = max beyin, 1 = min beyin
            
            // Dream Coherence: Rüya tutarlılığını gösteren parametre (kombinasyonlarla artar)
            // Melatonin derin uyku sağlar ve rüya tutarlılığını artırır
            float dreamCoherence = Mathf.Clamp01(
                0.40f * Clarity +                  // Netlik tutarlılık sağlar
                0.25f * Relaxation +               // Rahatlama tutarlılık artırır
                0.15f * (1f - Chaos) +             // Kaos azaldıkça tutarlılık artar
                0.30f * melatonin +                // Melatonin coherence artışı (derin uyku tutarlılık sağlar)
                combinationBonus * 0.50f           // Kombinasyonlar sinerji yaratır (asıl güç burada)
            );
            
            // BlurAmount: Dream Coherence ile kombinasyon bonusu
            // ÖNEMLİ: Beyin max büyüklüğe ulaşmadan rüya asla %100 net olmaz
            // Beyin büyüklüğü (shrink) blur'ı etkiler
            float baseBlurAmount = Mathf.Lerp(0.0f, 1.0f, 1f - Clarity);
            float coherenceBlurReduction = dreamCoherence * 0.50f; // Coherence blur'ı azaltır
            float combinationBlurReduction = combinationBonus * 0.40f; // Kombinasyonlar blur'ı azaltır
            float melatoninBlurReduction = melatonin * 0.25f; // Melatonin blur'ı azaltır (derin uyku netliği artırır)
            float calculatedBlur = Mathf.Clamp01(baseBlurAmount - coherenceBlurReduction - combinationBlurReduction - melatoninBlurReduction);
            
            // Beyin büyüklüğü faktörü: Beyin max değilse (shrink yüksekse) blur minimum değerde kalır
            // brainSizeFactor yukarıda tanımlandı (0 = max beyin, 1 = min beyin)
            float minBlurFromBrainSize = Mathf.Lerp(0f, minBlurWhenBrainNotMax, brainSizeFactor); // Beyin küçükse blur yüksek
            float blurAmount = Mathf.Max(calculatedBlur, minBlurFromBrainSize); // Blur minimum değerden küçük olamaz
            blurAmount = Mathf.Clamp01(blurAmount);
            currentBlurAmount = blurAmount; // Kazanma kontrolü için sakla
            if (dreamMat.HasProperty(blurAmountID))
                dreamPropertyBlock.SetFloat(blurAmountID, blurAmount);
            
            // MaxBlurRadius = lerp(minRadius, superMaxRadius, 1 - C)
            float minRadius = 0.05f;
            float superMaxRadius = 0.40f;
            float maxBlurRadius = Mathf.Lerp(minRadius, superMaxRadius, 1f - Clarity);
            if (dreamMat.HasProperty(maxBlurRadiusID))
                dreamPropertyBlock.SetFloat(maxBlurRadiusID, maxBlurRadius);
            
            // BlurSamples sabit kalır (performans için)
            // if (dreamMat.HasProperty(blurSamplesID))
            //     dreamPropertyBlock.SetInt(blurSamplesID, 8); // Sabit değer
            
            // UVDistortion = lerp(0.002, 0.03, K)
            float uvDistortion = Mathf.Lerp(0.002f, 0.03f, Chaos);
            if (dreamMat.HasProperty(uvDistortionID))
                dreamPropertyBlock.SetFloat(uvDistortionID, uvDistortion);
            
            // NoiseTiling = lerp(0.1, 0.8, clamp01(0.7*K + 0.3*(1-C)))
            float noiseTilingFactor = Mathf.Clamp01(0.7f * Chaos + 0.3f * (1f - Clarity));
            float noiseTiling = Mathf.Lerp(0.1f, 0.8f, noiseTilingFactor);
            if (dreamMat.HasProperty(noiseTilingDreamID))
                dreamPropertyBlock.SetFloat(noiseTilingDreamID, noiseTiling);
            
            // NoiseSpeed = lerpVec2(slow, fast, K)
            Vector2 slowSpeed = new Vector2(0.01f, 0.01f);
            Vector2 fastSpeed = new Vector2(0.1f, 0.1f);
            Vector2 noiseSpeed = Vector2.Lerp(slowSpeed, fastSpeed, Chaos);
            if (dreamMat.HasProperty(noiseSpeedID))
                dreamPropertyBlock.SetVector(noiseSpeedID, noiseSpeed);
            
            // WaveAmplitude: Dream Coherence ile kombinasyon bonusu
            // Kombinasyonlar wave'leri daha canlı yapar
            float baseWaveAmplitude = Mathf.Lerp(0.0f, 0.06f, Vividness);
            float coherenceWaveBoost = dreamCoherence * 0.02f; // Coherence wave'leri güçlendirir
            float combinationWaveBoost = combinationBonus * 0.015f; // Kombinasyonlar wave'leri güçlendirir
            float waveAmplitude = Mathf.Clamp(baseWaveAmplitude + coherenceWaveBoost + combinationWaveBoost, 0.0f, 0.10f);
            if (dreamMat.HasProperty(waveAmplitudeID))
                dreamPropertyBlock.SetFloat(waveAmplitudeID, waveAmplitude);
            
            // WaveFrequency: Dream Coherence ile dinamik
            float baseWaveFrequency = Mathf.Lerp(0.8f, 2.5f, Vividness);
            float coherenceFreqBoost = dreamCoherence * 0.4f; // Coherence frequency'yi artırır
            float waveFrequency = Mathf.Clamp(baseWaveFrequency + coherenceFreqBoost, 0.8f, 3.5f);
            if (dreamMat.HasProperty(waveFrequencyID))
                dreamPropertyBlock.SetFloat(waveFrequencyID, waveFrequency);
            
            // WaveSpeed: Dream Coherence ile dinamik
            float baseWaveSpeed = Mathf.Lerp(0.2f, 1.5f, Vividness);
            float coherenceSpeedBoost = dreamCoherence * 0.3f; // Coherence speed'i artırır
            float waveSpeed = Mathf.Clamp(baseWaveSpeed + coherenceSpeedBoost, 0.2f, 2.0f);
            if (dreamMat.HasProperty(waveSpeedID))
                dreamPropertyBlock.SetFloat(waveSpeedID, waveSpeed);
            
            // GlitchAmount: Dream Coherence ile kombinasyon bonusu
            // ÖNEMLİ: Beyin max büyüklüğe ulaşmadan rüya asla %100 net olmaz
            // Beyin büyüklüğü (shrink) glitch'i de etkiler
            float baseGlitchAmount = Mathf.Lerp(0.0f, 1.0f, Chaos);
            float coherenceGlitchReduction = dreamCoherence * 0.45f; // Coherence glitch'i azaltır
            float combinationGlitchReduction = combinationBonus * 0.35f; // Kombinasyonlar glitch'i azaltır
            float melatoninGlitchReduction = melatonin * 0.10f; // Melatonin çok minimal glitch azaltma (tek başına neredeyse yok)
            // Kafein glitch artırır (anksiyetik etki - trade-off)
            // Kafein + Melatonin kombinasyonunda Melatonin kafein'in glitch etkisini dengeler
            float caffeineGlitchBoost = (caffeineSmell > 0.5f && melatonin < 0.3f) ? 0.25f * caffeineSmell : 0f; // Kafein tek başına glitch artırır
            if (caffeineSmell > 0.5f && melatonin > 0.5f)
                caffeineGlitchBoost = 0f; // Melatonin kafein'in glitch etkisini dengeler (kombinasyon bonusu)
            float calculatedGlitch = Mathf.Clamp01(baseGlitchAmount - coherenceGlitchReduction - combinationGlitchReduction - melatoninGlitchReduction + caffeineGlitchBoost);
            
            // Beyin büyüklüğü faktörü: Beyin max değilse (shrink yüksekse) glitch minimum değerde kalır
            float minGlitchFromBrainSize = Mathf.Lerp(0f, minBlurWhenBrainNotMax, brainSizeFactor); // Beyin küçükse glitch yüksek
            float glitchAmount = Mathf.Max(calculatedGlitch, minGlitchFromBrainSize); // Glitch minimum değerden küçük olamaz
            glitchAmount = Mathf.Clamp01(glitchAmount);
            currentGlitchAmount = glitchAmount; // Kazanma kontrolü için sakla
            if (dreamMat.HasProperty(glitchAmountID))
                dreamPropertyBlock.SetFloat(glitchAmountID, glitchAmount);
            
            // GlitchSpeed = lerp(0.0, 2.0, clamp01(K + 0.3*A))
            float glitchSpeedFactor = Mathf.Clamp01(Chaos + 0.3f * Arousal);
            float glitchSpeed = Mathf.Lerp(0.0f, 30.0f, glitchSpeedFactor); // Shader'da range 0-30
            if (dreamMat.HasProperty(glitchSpeedID))
                dreamPropertyBlock.SetFloat(glitchSpeedID, glitchSpeed);
            
            // GlitchBlockSize = lerp(200, 40, K)
            float glitchBlockSize = Mathf.Lerp(120f, 4f, Chaos); // Shader'da range 4-120, tersine
            if (dreamMat.HasProperty(glitchBlockSizeID))
                dreamPropertyBlock.SetFloat(glitchBlockSizeID, glitchBlockSize);
            
            // RGBShift: Dream Coherence ile kombinasyon bonusu
            // Kombinasyonlar RGB shift'i azaltır (daha tutarlı renkler)
            float baseRgbShiftFactor = Mathf.Clamp01(0.7f * Chaos + 0.2f * Arousal);
            float coherenceRgbReduction = dreamCoherence * 0.3f; // Coherence RGB shift'i azaltır
            float combinationRgbReduction = combinationBonus * 0.25f; // Kombinasyonlar RGB shift'i azaltır
            float rgbShiftFactor = Mathf.Clamp01(baseRgbShiftFactor - coherenceRgbReduction - combinationRgbReduction);
            float rgbShift = Mathf.Lerp(0.0f, 0.02f, rgbShiftFactor);
            if (dreamMat.HasProperty(rgbShiftID))
                dreamPropertyBlock.SetFloat(rgbShiftID, rgbShift);
            
            // RimIntensity = lerp(0.8, 3.0, R)
            float rimIntensity = Mathf.Lerp(0.8f, 3.0f, Relaxation);
            if (dreamMat.HasProperty(rimIntensityDreamID))
                dreamPropertyBlock.SetFloat(rimIntensityDreamID, rimIntensity);
            
            // RimPower = lerp(6.0, 2.5, R)
            float rimPower = Mathf.Lerp(6.0f, 2.5f, Relaxation);
            if (dreamMat.HasProperty(rimPowerDreamID))
                dreamPropertyBlock.SetFloat(rimPowerDreamID, rimPower);
            
            // RimColor = lerp(softCoolColor, warmColor, R)
            Color softCoolColor = new Color(0.75f, 0.9f, 1.0f, 1.0f); // Soft cyan (shader default)
            Color warmColor = new Color(1.0f, 0.8f, 0.6f, 1.0f); // Warm (relaxation)
            Color rimColor = Color.Lerp(softCoolColor, warmColor, Relaxation);
            if (dreamMat.HasProperty(rimColorDreamID))
                dreamPropertyBlock.SetColor(rimColorDreamID, rimColor);
            
            dreamRenderer.SetPropertyBlock(dreamPropertyBlock);
        }
    }
}
