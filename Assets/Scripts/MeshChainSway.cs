using UnityEngine;
using System.Collections;

/// <summary>
/// Mesh'e mouse ile tıklandığında zincir efekti gibi sallanma animasyonu sağlar.
/// Spring/elastic benzeri bir fizik sistemi kullanarak doğal bir sallanma efekti yaratır.
/// </summary>
public class MeshChainSway : MonoBehaviour
{
    [Header("Sallanma Ayarları")]
    [Tooltip("Sallanmanın maksimum açısı (derece)")]
    [Range(5f, 90f)]
    public float maxSwayAngle = 30f;
    
    [Tooltip("Sallanma hızı (yüksek değer = daha hızlı)")]
    [Range(0.5f, 10f)]
    public float swaySpeed = 3f;
    
    [Tooltip("Sallanmanın sönümlenme hızı (yüksek değer = daha hızlı durur)")]
    [Range(0.1f, 5f)]
    public float damping = 1.5f;
    
    [Tooltip("Sallanmanın süresi (saniye)")]
    [Range(0.5f, 5f)]
    public float swayDuration = 2f;
    
    [Header("Sallanma Yönü")]
    [Tooltip("Sallanma ekseni (hangi eksende sallanacak)")]
    public SwayAxis swayAxis = SwayAxis.X;
    
    [Tooltip("Rastgele sallanma yönü (her tıklamada farklı yön)")]
    public bool randomDirection = true;
    
    [Header("Tıklama Ayarları")]
    [Tooltip("Tıklama için gerekli Collider kontrolü")]
    public bool requireCollider = true;
    
    [Tooltip("Tıklama mesafesi (kamera mesafesi kontrolü)")]
    [Range(1f, 50f)]
    public float maxClickDistance = 20f;
    
    // Sallanma durumu
    private Vector3 originalRotation;
    private float currentSwayVelocity = 0f;
    private float currentSwayAngle = 0f;
    private bool isSwaying = false;
    private Coroutine swayCoroutine;
    private float swayDirection = 1f; // 1 veya -1 (sağa/sola)
    
    // Spring fizik parametreleri
    private float springForce = 0f;
    private float targetAngle = 0f;
    
    public enum SwayAxis
    {
        X,
        Y,
        Z
    }
    
    void Start()
    {
        // Orijinal rotasyonu kaydet
        originalRotation = transform.localEulerAngles;
        
        // Collider kontrolü
        if (requireCollider && GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"MeshChainSway: {gameObject.name} üzerinde Collider bulunamadı! OnMouseDown çalışmayabilir.");
        }
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
        }
        
        // Başlangıç değerlerini ayarla
        currentSwayVelocity = maxSwayAngle * swaySpeed * swayDirection; // İlk hız
        targetAngle = 0f; // Hedef açı (dengede)
        springForce = 0f;
        
        isSwaying = true;
        swayCoroutine = StartCoroutine(SwayAnimation());
    }
    
    /// <summary>
    /// Sallanma animasyonu coroutine'i
    /// </summary>
    private IEnumerator SwayAnimation()
    {
        float elapsedTime = 0f;
        float initialVelocity = currentSwayVelocity;
        
        while (elapsedTime < swayDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Spring fizik hesaplaması
            // F = -kx (Hooke yasası benzeri)
            float springConstant = swaySpeed * swaySpeed; // Spring sabiti
            float displacement = currentSwayAngle - targetAngle; // Yer değiştirme
            springForce = -springConstant * displacement; // Geri çağırıcı kuvvet
            
            // Sönümlenme (damping)
            float dampingForce = -damping * currentSwayVelocity;
            
            // Toplam kuvvet
            float totalForce = springForce + dampingForce;
            
            // Hızı güncelle (F = ma, a = F/m, m = 1 varsayıyoruz)
            currentSwayVelocity += totalForce * Time.deltaTime;
            
            // Açıyı güncelle
            currentSwayAngle += currentSwayVelocity * Time.deltaTime;
            
            // Sallanma açısını sınırla
            currentSwayAngle = Mathf.Clamp(currentSwayAngle, -maxSwayAngle, maxSwayAngle);
            
            // Transform'u güncelle
            Vector3 newRotation = originalRotation;
            switch (swayAxis)
            {
                case SwayAxis.X:
                    newRotation.x += currentSwayAngle;
                    break;
                case SwayAxis.Y:
                    newRotation.y += currentSwayAngle;
                    break;
                case SwayAxis.Z:
                    newRotation.z += currentSwayAngle;
                    break;
            }
            transform.localEulerAngles = newRotation;
            
            // Eğer sallanma çok küçükse ve hız da düşükse, durdur
            if (Mathf.Abs(currentSwayAngle) < 0.1f && Mathf.Abs(currentSwayVelocity) < 0.5f)
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
        
        // Yumuşak bir şekilde orijinal pozisyona dön
        StartCoroutine(ReturnToOriginalPosition());
    }
    
    /// <summary>
    /// Orijinal pozisyona yumuşak dönüş
    /// </summary>
    private IEnumerator ReturnToOriginalPosition()
    {
        float returnSpeed = 5f; // Dönüş hızı
        
        while (Mathf.Abs(currentSwayAngle) > 0.01f || Mathf.Abs(currentSwayVelocity) > 0.01f)
        {
            // Spring fizik ile orijinal pozisyona dön
            float springConstant = returnSpeed * returnSpeed;
            float displacement = currentSwayAngle - 0f; // Hedef: 0
            springForce = -springConstant * displacement;
            float dampingForce = -damping * currentSwayVelocity;
            float totalForce = springForce + dampingForce;
            
            currentSwayVelocity += totalForce * Time.deltaTime;
            currentSwayAngle += currentSwayVelocity * Time.deltaTime;
            
            // Transform'u güncelle
            Vector3 newRotation = originalRotation;
            switch (swayAxis)
            {
                case SwayAxis.X:
                    newRotation.x += currentSwayAngle;
                    break;
                case SwayAxis.Y:
                    newRotation.y += currentSwayAngle;
                    break;
                case SwayAxis.Z:
                    newRotation.z += currentSwayAngle;
                    break;
            }
            transform.localEulerAngles = newRotation;
            
            yield return null;
        }
        
        // Tam olarak orijinal pozisyona ayarla
        transform.localEulerAngles = originalRotation;
        currentSwayAngle = 0f;
        currentSwayVelocity = 0f;
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
        StopSway();
    }
}

