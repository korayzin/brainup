using UnityEngine;
using System.Collections;

/// <summary>
/// Mesh'e mouse ile tıklandığında zincir efekti gibi sallanma animasyonu sağlar.
/// Gerçekçi fizik simülasyonu ile doğal bir sallanma efekti yaratır.
/// </summary>
public class MeshChainSway : MonoBehaviour
{
    [Header("Sallanma Ayarları")]
    [Tooltip("Sallanmanın maksimum açısı (derece)")]
    [Range(5f, 90f)]
    public float maxSwayAngle = 30f;
    
    [Tooltip("Sallanma frekansı (yüksek değer = daha hızlı titreşim)")]
    [Range(0.5f, 10f)]
    public float swayFrequency = 2.5f;
    
    [Tooltip("Sallanmanın sönümlenme katsayısı (0.01 = çok yavaş, 0.1 = hızlı)")]
    [Range(0.01f, 0.2f)]
    public float dampingRatio = 0.05f;
    
    [Tooltip("Sallanmanın süresi (saniye)")]
    [Range(1f, 8f)]
    public float swayDuration = 3.5f;
    
    [Header("Fizik Parametreleri")]
    [Tooltip("Kütle (yüksek = daha ağır, daha yavaş hareket)")]
    [Range(0.1f, 5f)]
    public float mass = 1f;
    
    [Tooltip("Yerçekimi etkisi (0 = yok, 1 = tam)")]
    [Range(0f, 1f)]
    public float gravityInfluence = 0.3f;
    
    [Tooltip("Hava direnci (yüksek = daha hızlı durur)")]
    [Range(0f, 0.1f)]
    public float airResistance = 0.02f;
    
    [Header("Sallanma Yönü")]
    [Tooltip("Sallanma ekseni (hangi eksende sallanacak)")]
    public SwayAxis swayAxis = SwayAxis.X;
    
    [Tooltip("Rastgele sallanma yönü (her tıklamada farklı yön)")]
    public bool randomDirection = true;
    
    [Tooltip("Rastgele açı varyasyonu (her sallanma biraz farklı)")]
    [Range(0f, 0.5f)]
    public float angleVariation = 0.2f;
    
    [Header("Tıklama Ayarları")]
    [Tooltip("Tıklama için gerekli Collider kontrolü")]
    public bool requireCollider = true;
    
    [Tooltip("Tıklama mesafesi (kamera mesafesi kontrolü)")]
    [Range(1f, 50f)]
    public float maxClickDistance = 20f;
    
    [Header("Buton Entegrasyonu")]
    [Tooltip("ButtonManager referansı (otomatik bulunur, buton kullanıldıysa animasyon çalışmaz)")]
    public ButtonManager buttonManager;
    
    [Header("Gelişmiş Ayarlar")]
    [Tooltip("Momentum korunumu (yüksek = daha uzun sallanma)")]
    [Range(0.5f, 1f)]
    public float momentumPreservation = 0.85f;
    
    [Tooltip("Mikro titreşimler (çok küçük rastgele hareketler)")]
    public bool enableMicroVibrations = true;
    
    [Tooltip("Mikro titreşim gücü")]
    [Range(0f, 2f)]
    public float microVibrationStrength = 0.5f;
    
    [Tooltip("Smooth geçiş hızı (yüksek = daha hızlı tepki)")]
    [Range(0.01f, 1f)]
    public float smoothDamping = 0.15f;
    
    [Tooltip("Sallanma yumuşaklığı (yüksek = daha yumuşak)")]
    [Range(0.1f, 2f)]
    public float smoothnessFactor = 1.2f;
    
    [Header("Zincir/Pivot Ayarları")]
    [Tooltip("Pivot noktası (0 = üst, 0.5 = orta, 1 = alt)")]
    [Range(0f, 1f)]
    public float pivotPoint = 0f; // 0 = tepeden sallanır (zincir gibi)
    
    [Tooltip("İki iplikten asılı gibi davran (X ve Z ekseninde sallanma)")]
    public bool twoPointSuspension = true;
    
    [Tooltip("İkinci sallanma ekseni açısı (derece)")]
    [Range(0f, 90f)]
    public float maxSecondarySwayAngle = 15f;
    
    // Sallanma durumu
    private Vector3 originalRotation;
    private Vector3 originalPosition;
    private float currentSwayVelocity = 0f;
    private float currentSwayAngle = 0f;
    private float currentSwayAcceleration = 0f;
    private bool isSwaying = false;
    private Coroutine swayCoroutine;
    private float swayDirection = 1f; // 1 veya -1 (sağa/sola)
    
    // İki noktalı sallanma için
    private float secondarySwayVelocity = 0f;
    private float secondarySwayAngle = 0f; // Runtime'da kullanılan değer
    private float secondarySwayDirection = 1f;
    
    // Smooth animasyon için
    private float smoothCurrentAngle = 0f;
    private float smoothCurrentAngleVelocity = 0f;
    private float smoothSecondaryAngle = 0f;
    private float smoothSecondaryAngleVelocity = 0f;
    
    // Pivot noktası hesaplama
    private Vector3 pivotOffset = Vector3.zero;
    private Bounds meshBounds;
    private bool boundsCalculated = false;
    
    // Gerçekçi fizik parametreleri
    private float naturalFrequency = 0f; // Doğal frekans (rad/s)
    private float dampedFrequency = 0f; // Sönümlü frekans
    private float initialImpulse = 0f; // İlk itme gücü
    private float timeSinceStart = 0f;
    
    // Mikro titreşimler için
    private float microVibrationTime = 0f;
    private float microVibrationOffset = 0f;
    
    public enum SwayAxis
    {
        X,
        Y,
        Z
    }
    
    void Start()
    {
        // Orijinal rotasyonu ve pozisyonu kaydet
        originalRotation = transform.localEulerAngles;
        originalPosition = transform.localPosition;
        
        // Mesh bounds'ını hesapla (pivot noktası için)
        CalculateMeshBounds();
        
        // Pivot offset'ini hesapla (mesh'in tepesinden sallanması için)
        CalculatePivotOffset();
        
        // Doğal frekansı hesapla (ω = 2πf)
        naturalFrequency = 2f * Mathf.PI * swayFrequency;
        
        // Sönümlü frekansı hesapla (ωd = ω√(1-ζ²))
        float zeta = dampingRatio; // Sönümlenme oranı
        dampedFrequency = naturalFrequency * Mathf.Sqrt(Mathf.Max(0f, 1f - zeta * zeta));
        
        // ButtonManager'ı bul (eğer atanmamışsa)
        if (buttonManager == null)
        {
            // Önce aynı GameObject'te ara
            buttonManager = GetComponent<ButtonManager>();
            
            // Bulunamazsa parent'ta ara
            if (buttonManager == null)
            {
                buttonManager = GetComponentInParent<ButtonManager>();
            }
            
            // Bulunamazsa child'larda ara
            if (buttonManager == null)
            {
                buttonManager = GetComponentInChildren<ButtonManager>();
            }
        }
        
        // Collider kontrolü
        if (requireCollider && GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"MeshChainSway: {gameObject.name} üzerinde Collider bulunamadı! OnMouseDown çalışmayabilir.");
        }
    }
    
    /// <summary>
    /// Mesh bounds'ını hesapla (pivot noktası için)
    /// </summary>
    private void CalculateMeshBounds()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Renderer renderer = GetComponent<Renderer>();
        
        if (renderer != null)
        {
            meshBounds = renderer.bounds;
            boundsCalculated = true;
        }
        else if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            meshBounds = meshFilter.sharedMesh.bounds;
            // Local bounds'ı world space'e çevir
            meshBounds.center = transform.TransformPoint(meshBounds.center);
            meshBounds.size = transform.TransformVector(meshBounds.size);
            boundsCalculated = true;
        }
        else
        {
            // Fallback: Collider bounds kullan
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                meshBounds = col.bounds;
                boundsCalculated = true;
            }
        }
    }
    
    /// <summary>
    /// Pivot offset'ini hesapla (mesh'in tepesinden sallanması için)
    /// </summary>
    private void CalculatePivotOffset()
    {
        if (!boundsCalculated)
        {
            CalculateMeshBounds();
        }
        
        // Mesh'in üst noktasını bul (Y ekseninde en yüksek)
        float topY = meshBounds.max.y;
        float bottomY = meshBounds.min.y;
        float height = topY - bottomY;
        
        // Pivot noktası: pivotPoint = 0 ise üstten, 1 ise alttan
        float pivotY = Mathf.Lerp(topY, bottomY, pivotPoint);
        
        // Pivot offset: mesh'in merkezinden pivot noktasına olan mesafe
        Vector3 meshCenter = meshBounds.center;
        pivotOffset = new Vector3(0f, pivotY - meshCenter.y, 0f);
        
        // Local space'e çevir
        pivotOffset = transform.InverseTransformVector(pivotOffset);
    }
    
    /// <summary>
    /// Mouse ile tıklama algılama (OnMouseDown için Collider gerekli)
    /// </summary>
    void OnMouseDown()
    {
        // Mesafe kontrolü
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
            if (distance > maxClickDistance)
            {
                return; // Çok uzaktan tıklama algılanmasın
            }
        }
        
        // ButtonManager kontrolü: Eğer buton kullanıldıysa animasyon çalışmasın
        if (buttonManager != null)
        {
            // Butonun kullanıldığını veya basma hakkının bittiğini kontrol et
            if (buttonManager.IsButtonUsed() || buttonManager.GetRemainingPressCount() <= 0)
            {
                return; // Buton kullanıldı, animasyon çalışmasın
            }
        }
        
        // Sallanmayı başlat
        StartSway();
    }
    
    /// <summary>
    /// Sallanmayı başlat
    /// </summary>
    public void StartSway()
    {
        // Eğer zaten sallanıyorsa, mevcut sallanmayı durdur ve yeni başlat
        if (isSwaying && swayCoroutine != null)
        {
            StopCoroutine(swayCoroutine);
        }
        
        // Rastgele yön seç (eğer aktifse)
        if (randomDirection)
        {
            swayDirection = Random.Range(0, 2) == 0 ? 1f : -1f;
            secondarySwayDirection = Random.Range(0, 2) == 0 ? 1f : -1f;
        }
        
        // Rastgele açı varyasyonu ekle
        float angleMultiplier = 1f + Random.Range(-angleVariation, angleVariation);
        float targetMaxAngle = maxSwayAngle * angleMultiplier;
        float targetSecondaryAngle = maxSecondarySwayAngle * angleMultiplier;
        
        // Gerçekçi başlangıç değerleri
        // İlk itme gücü: daha gerçekçi bir başlangıç hızı
        initialImpulse = targetMaxAngle * naturalFrequency * 0.8f * swayDirection;
        currentSwayVelocity = initialImpulse;
        currentSwayAngle = 0f;
        currentSwayAcceleration = 0f;
        timeSinceStart = 0f;
        
        // Smooth değerleri sıfırla
        smoothCurrentAngle = 0f;
        smoothCurrentAngleVelocity = 0f;
        smoothSecondaryAngle = 0f;
        smoothSecondaryAngleVelocity = 0f;
        
        // İkinci eksen için (iki iplikten asılı gibi)
        if (twoPointSuspension)
        {
            secondarySwayVelocity = targetSecondaryAngle * naturalFrequency * 0.6f * secondarySwayDirection;
            secondarySwayAngle = 0f;
        }
        
        // Mikro titreşim için rastgele offset
        microVibrationTime = Random.Range(0f, 100f);
        microVibrationOffset = Random.Range(-1f, 1f);
        
        isSwaying = true;
        swayCoroutine = StartCoroutine(SwayAnimation());
    }
    
    /// <summary>
    /// Sallanma animasyonu coroutine'i - Gerçekçi fizik simülasyonu
    /// </summary>
    private IEnumerator SwayAnimation()
    {
        float elapsedTime = 0f;
        float zeta = dampingRatio; // Sönümlenme oranı
        
        while (elapsedTime < swayDuration)
        {
            elapsedTime += Time.deltaTime;
            timeSinceStart = elapsedTime;
            
            // Gerçekçi sönümlü harmonik osilatör denklemi
            // θ(t) = A * e^(-ζωt) * sin(ωd*t + φ)
            // A = başlangıç genliği, ζ = sönümlenme oranı, ω = doğal frekans, ωd = sönümlü frekans
            
            // Eksponansiyel sönümlenme faktörü
            float decayFactor = Mathf.Exp(-zeta * naturalFrequency * elapsedTime);
            
            // Faz açısı (başlangıç hızına göre)
            float phase = Mathf.Atan2(initialImpulse, naturalFrequency * maxSwayAngle);
            
            // Sönümlü harmonik hareket
            float dampedOscillation = Mathf.Sin(dampedFrequency * elapsedTime + phase);
            
            // Genlik (başlangıç genliği * sönümlenme)
            float amplitude = maxSwayAngle * decayFactor;
            
            // Ana sallanma açısı
            currentSwayAngle = amplitude * dampedOscillation * swayDirection;
            
            // Hızı hesapla (türev)
            float velocityTerm1 = -zeta * naturalFrequency * amplitude * dampedOscillation;
            float velocityTerm2 = dampedFrequency * amplitude * Mathf.Cos(dampedFrequency * elapsedTime + phase);
            currentSwayVelocity = (velocityTerm1 + velocityTerm2) * swayDirection;
            
            // İvme hesapla (ikinci türev - daha gerçekçi hareket için)
            float accelerationTerm1 = (zeta * zeta * naturalFrequency * naturalFrequency - dampedFrequency * dampedFrequency) * amplitude * dampedOscillation;
            float accelerationTerm2 = -2f * zeta * naturalFrequency * dampedFrequency * amplitude * Mathf.Cos(dampedFrequency * elapsedTime + phase);
            currentSwayAcceleration = (accelerationTerm1 + accelerationTerm2) * swayDirection;
            
            // Yerçekimi etkisi (aşağı doğru hafif eğilme)
            float gravityEffect = -gravityInfluence * Mathf.Sin(currentSwayAngle * Mathf.Deg2Rad) * 2f;
            currentSwayAngle += gravityEffect * Time.deltaTime;
            
            // Hava direnci (hızın karesiyle orantılı)
            float airResistanceForce = -airResistance * currentSwayVelocity * Mathf.Abs(currentSwayVelocity);
            currentSwayVelocity += airResistanceForce * Time.deltaTime / mass;
            
            // Mikro titreşimler (çok küçük rastgele hareketler - gerçekçilik için)
            float microVibration = 0f;
            if (enableMicroVibrations)
            {
                microVibrationTime += Time.deltaTime * 15f; // Hızlı titreşim
                microVibration = Mathf.Sin(microVibrationTime + microVibrationOffset) * 
                                 Mathf.Sin(microVibrationTime * 1.7f + microVibrationOffset * 2f) * 
                                 microVibrationStrength * decayFactor; // Sönümlenme ile azalır
            }
            
            // Toplam açı (ana sallanma + mikro titreşim)
            float targetAngle = currentSwayAngle + microVibration;
            
            // İki noktalı sallanma (iki iplikten asılı levha gibi)
            float targetSecondaryAngle = 0f;
            if (twoPointSuspension)
            {
                // İkinci eksen için de aynı fizik simülasyonu
                float secondaryDecay = Mathf.Exp(-zeta * naturalFrequency * elapsedTime * 0.9f);
                float secondaryOscillation = Mathf.Sin(dampedFrequency * elapsedTime * 0.9f + phase * 1.2f);
                float secondaryAmplitude = maxSecondarySwayAngle * secondaryDecay;
                secondarySwayAngle = secondaryAmplitude * secondaryOscillation * secondarySwayDirection;
                targetSecondaryAngle = secondarySwayAngle;
                
                // İkinci eksen hızı
                float secondaryVel1 = -zeta * naturalFrequency * secondaryAmplitude * secondaryOscillation;
                float secondaryVel2 = dampedFrequency * secondaryAmplitude * Mathf.Cos(dampedFrequency * elapsedTime * 0.9f + phase * 1.2f);
                secondarySwayVelocity = (secondaryVel1 + secondaryVel2) * secondarySwayDirection;
            }
            
            // Smooth interpolation ile yumuşak geçiş
            float smoothTime = smoothDamping * smoothnessFactor;
            smoothCurrentAngle = Mathf.SmoothDamp(smoothCurrentAngle, targetAngle, ref smoothCurrentAngleVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
            
            if (twoPointSuspension)
            {
                smoothSecondaryAngle = Mathf.SmoothDamp(smoothSecondaryAngle, targetSecondaryAngle, ref smoothSecondaryAngleVelocity, smoothTime * 1.1f, Mathf.Infinity, Time.deltaTime);
            }
            
            // Pivot noktasından rotasyon uygula (zincir gibi tepeden sallanma)
            ApplyRotationFromPivot(smoothCurrentAngle);
            
            // Eğer sallanma çok küçükse ve hız da düşükse, durdur (enerji tükendi)
            float energy = 0.5f * mass * currentSwayVelocity * currentSwayVelocity + 
                          0.5f * naturalFrequency * naturalFrequency * currentSwayAngle * currentSwayAngle;
            if (energy < 0.01f && elapsedTime > 0.5f) // Minimum süre geçtiyse
            {
                break; // Sallanma durdu
            }
            
            yield return null;
        }
        
        // Sallanmayı sonlandır ve orijinal pozisyona dön
        EndSway();
    }
    
    /// <summary>
    /// Sallanmayı sonlandır ve orijinal pozisyona dön
    /// </summary>
    private void EndSway()
    {
        isSwaying = false;
        
        // Obje aktif ve enabled ise coroutine başlat
        // Play mode'dan çıktıktan sonra inactive objelerde coroutine başlatılamaz
        if (gameObject.activeInHierarchy && enabled)
        {
            StartCoroutine(ReturnToOriginalPosition());
        }
        else
        {
            // Obje inactive ise direkt orijinal pozisyona ayarla
            transform.localRotation = Quaternion.Euler(originalRotation);
            transform.localPosition = originalPosition;
            currentSwayAngle = 0f;
            currentSwayVelocity = 0f;
            currentSwayAcceleration = 0f;
            secondarySwayAngle = 0f;
            secondarySwayVelocity = 0f;
            smoothCurrentAngle = 0f;
            smoothCurrentAngleVelocity = 0f;
            smoothSecondaryAngle = 0f;
            smoothSecondaryAngleVelocity = 0f;
        }
    }
    
    /// <summary>
    /// Pivot noktasından rotasyon uygula (zincir gibi tepeden sallanma)
    /// </summary>
    private void ApplyRotationFromPivot(float angle)
    {
        // İki noktalı sallanma için ikinci açıyı da al (smooth değer)
        float secondaryAngle = twoPointSuspension ? smoothSecondaryAngle : 0f;
        
        // Ana rotasyonu hesapla
        Vector3 newRotation = originalRotation;
        Vector3 secondaryRotation = Vector3.zero;
        
        switch (swayAxis)
        {
            case SwayAxis.X:
                newRotation.x += angle;
                if (twoPointSuspension)
                    secondaryRotation.z = secondaryAngle; // Z ekseninde ikinci sallanma
                break;
            case SwayAxis.Y:
                newRotation.y += angle;
                if (twoPointSuspension)
                    secondaryRotation.x = secondaryAngle; // X ekseninde ikinci sallanma
                break;
            case SwayAxis.Z:
                newRotation.z += angle;
                if (twoPointSuspension)
                    secondaryRotation.x = secondaryAngle; // X ekseninde ikinci sallanma
                break;
        }
        
        // İki rotasyonu birleştir
        Quaternion primaryRot = Quaternion.Euler(newRotation);
        Quaternion secondaryRot = Quaternion.Euler(secondaryRotation);
        Quaternion finalRotation = primaryRot * secondaryRot;
        
        // Pivot noktasından rotasyon uygula
        // 1. Pivot noktasına göre pozisyonu ayarla
        Vector3 pivotWorldPos = transform.TransformPoint(pivotOffset);
        
        // 2. Rotasyonu uygula
        transform.localRotation = finalRotation;
        
        // 3. Pivot noktası sabit kalacak şekilde pozisyonu düzelt
        Vector3 newPivotWorldPos = transform.TransformPoint(pivotOffset);
        Vector3 pivotOffsetCorrection = pivotWorldPos - newPivotWorldPos;
        transform.localPosition = originalPosition + transform.InverseTransformVector(pivotOffsetCorrection);
    }
    
    /// <summary>
    /// Orijinal pozisyona yumuşak dönüş - Gerçekçi fizik ile
    /// </summary>
    private IEnumerator ReturnToOriginalPosition()
    {
        float returnDamping = dampingRatio * 1.5f; // Daha hızlı sönümlenme
        float returnFrequency = naturalFrequency * 1.2f; // Biraz daha hızlı dönüş
        
        while (Mathf.Abs(currentSwayAngle) > 0.01f || Mathf.Abs(currentSwayVelocity) > 0.1f || 
               (twoPointSuspension && (Mathf.Abs(secondarySwayAngle) > 0.01f || Mathf.Abs(secondarySwayVelocity) > 0.1f)))
        {
            // Gerçekçi sönümlü osilatör ile orijinal pozisyona dön
            float dt = Time.deltaTime;
            
            // Ana eksen için geri çağırıcı kuvvet
            float springConstant = returnFrequency * returnFrequency;
            float restoringForce = -springConstant * currentSwayAngle;
            float dampingForce = -2f * returnDamping * returnFrequency * currentSwayVelocity;
            float totalForce = (restoringForce + dampingForce) / mass;
            
            currentSwayAcceleration = totalForce;
            currentSwayVelocity += currentSwayAcceleration * dt;
            currentSwayAngle += currentSwayVelocity * dt;
            
            // İkinci eksen için (eğer aktifse)
            if (twoPointSuspension)
            {
                float secondaryRestoringForce = -springConstant * secondarySwayAngle;
                float secondaryDampingForce = -2f * returnDamping * returnFrequency * secondarySwayVelocity;
                float secondaryTotalForce = (secondaryRestoringForce + secondaryDampingForce) / mass;
                secondarySwayVelocity += secondaryTotalForce * dt;
                secondarySwayAngle += secondarySwayVelocity * dt;
            }
            
            // Momentum korunumu (yumuşak durma)
            if (Mathf.Abs(currentSwayAngle) < 0.5f)
            {
                currentSwayVelocity *= momentumPreservation;
            }
            if (twoPointSuspension && Mathf.Abs(secondarySwayAngle) < 0.5f)
            {
                secondarySwayVelocity *= momentumPreservation;
            }
            
            // Smooth interpolation ile yumuşak dönüş
            float smoothTime = smoothDamping * smoothnessFactor;
            smoothCurrentAngle = Mathf.SmoothDamp(smoothCurrentAngle, 0f, ref smoothCurrentAngleVelocity, smoothTime, Mathf.Infinity, dt);
            
            if (twoPointSuspension)
            {
                smoothSecondaryAngle = Mathf.SmoothDamp(smoothSecondaryAngle, 0f, ref smoothSecondaryAngleVelocity, smoothTime * 1.1f, Mathf.Infinity, dt);
            }
            
            // Pivot noktasından rotasyon uygula
            ApplyRotationFromPivot(smoothCurrentAngle);
            
            yield return null;
        }
        
        // Tam olarak orijinal pozisyona ayarla
        transform.localRotation = Quaternion.Euler(originalRotation);
        transform.localPosition = originalPosition;
        currentSwayAngle = 0f;
        currentSwayVelocity = 0f;
        currentSwayAcceleration = 0f;
        secondarySwayAngle = 0f;
        secondarySwayVelocity = 0f;
        smoothCurrentAngle = 0f;
        smoothCurrentAngleVelocity = 0f;
        smoothSecondaryAngle = 0f;
        smoothSecondaryAngleVelocity = 0f;
    }
    
    /// <summary>
    /// Manuel olarak sallanmayı başlat (kod ile çağrılabilir)
    /// </summary>
    public void TriggerSway()
    {
        StartSway();
    }
    
    /// <summary>
    /// Sallanmayı durdur
    /// </summary>
    public void StopSway()
    {
        if (swayCoroutine != null)
        {
            StopCoroutine(swayCoroutine);
        }
        EndSway();
    }
    
    void OnDisable()
    {
        // Script devre dışı bırakıldığında sallanmayı durdur
        // Coroutine'leri durdur (obje inactive olabilir)
        if (swayCoroutine != null)
        {
            StopCoroutine(swayCoroutine);
            swayCoroutine = null;
        }
        
        isSwaying = false;
        
        // Obje hala aktifse EndSway çağır, değilse direkt sıfırla
        if (gameObject.activeInHierarchy)
        {
            EndSway();
        }
        else
        {
            // Obje inactive ise direkt orijinal pozisyona ayarla
            transform.localRotation = Quaternion.Euler(originalRotation);
            transform.localPosition = originalPosition;
            currentSwayAngle = 0f;
            currentSwayVelocity = 0f;
            currentSwayAcceleration = 0f;
            secondarySwayAngle = 0f;
            secondarySwayVelocity = 0f;
            smoothCurrentAngle = 0f;
            smoothCurrentAngleVelocity = 0f;
            smoothSecondaryAngle = 0f;
            smoothSecondaryAngleVelocity = 0f;
        }
    }
}

