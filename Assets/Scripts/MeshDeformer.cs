using UnityEngine;

/// <summary>
/// Mesh deformasyon scripti - Çarpışmalarda yumuşak cisim (soft body) etkisi oluşturur
/// 
/// ÖNEMLİ: Bu scriptin çalışması için:
/// 1. Bu GameObject'te MeshFilter ve MeshCollider olmalı
/// 2. Çarpışan nesnede Rigidbody olmalı (veya bu nesnede)
/// 3. MeshCollider'ın "Convex" özelliği KAPALI olmalı (eğer concave mesh kullanıyorsanız)
/// 4. Collider'lar "Is Trigger" KAPALI olmalı
/// 5. Rigidbody'ler "Is Kinematic" KAPALI olmalı (fizik çarpışması için)
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class MeshDeformer : MonoBehaviour
{
    [Header("Deformasyon Ayarları")]
    [Tooltip("Deformasyonun etkili olacağı yarıçap (temas noktasından itibaren)")]
    [SerializeField] private float deformRadius = 0.5f;
    
    [Tooltip("Deformasyon kuvveti - Ne kadar içeri göçeceği")]
    [SerializeField] private float deformForce = 0.1f;
    
    [Tooltip("Orijinal pozisyona geri dönüş hızı (0-1 arası, yüksek değer = daha hızlı)")]
    [SerializeField] private float restoreSpeed = 0.1f;

    [Header("Debug Ayarları")]
    [Tooltip("Çarpışma olaylarını konsola yazdır")]
    [SerializeField] private bool debugMode = true;

    // Mesh bileşenleri
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Rigidbody rb;
    private Mesh originalMesh;
    private Mesh deformedMesh;
    
    // Orijinal vertex pozisyonları (dünya koordinatlarında)
    private Vector3[] originalVertices;
    
    // Mevcut vertex pozisyonları (deformasyon sonrası)
    private Vector3[] currentVertices;
    
    // Vertex'lerin orijinal pozisyonlarına dönüp dönmediğini kontrol etmek için
    private bool isDeformed = false;

    /// <summary>
    /// Start fonksiyonu - Mesh'in orijinal vertex pozisyonlarını hafızaya alır
    /// </summary>
    void Start()
    {
        // MeshFilter ve MeshCollider bileşenlerini al
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        rb = GetComponent<Rigidbody>();
        
        // MeshFilter kontrolü
        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError($"[MeshDeformer] {gameObject.name}: MeshFilter veya Mesh bulunamadı!", this);
            enabled = false;
            return;
        }
        
        // MeshCollider kontrolü
        if (meshCollider == null)
        {
            Debug.LogError($"[MeshDeformer] {gameObject.name}: MeshCollider bulunamadı! Otomatik ekleniyor...", this);
            meshCollider = gameObject.AddComponent<MeshCollider>();
        }
        
        // Collider ayarlarını kontrol et
        if (meshCollider.isTrigger)
        {
            Debug.LogWarning($"[MeshDeformer] {gameObject.name}: MeshCollider 'Is Trigger' açık! OnCollisionEnter çalışmayacak. Kapatılıyor...", this);
            meshCollider.isTrigger = false;
        }
        
        // Rigidbody kontrolü - Çarpışma algılaması için gerekli olabilir
        if (rb == null)
        {
            if (debugMode)
                Debug.LogWarning($"[MeshDeformer] {gameObject.name}: Rigidbody yok. Çarpışan nesnede Rigidbody olmalı!", this);
        }
        else
        {
            // Rigidbody varsa kinematic olmamalı (fizik çarpışması için)
            if (rb.isKinematic && debugMode)
            {
                Debug.LogWarning($"[MeshDeformer] {gameObject.name}: Rigidbody 'Is Kinematic' açık. Fizik çarpışmaları algılanmayabilir!", this);
            }
        }
        
        // Orijinal mesh'i klonla (orijinal mesh'i bozmamak için)
        originalMesh = meshFilter.mesh;
        deformedMesh = Instantiate(originalMesh);
        meshFilter.mesh = deformedMesh;
        
        // Orijinal vertex pozisyonlarını dünya koordinatlarında sakla
        originalVertices = new Vector3[originalMesh.vertexCount];
        currentVertices = new Vector3[originalMesh.vertexCount];
        
        // Transform matrisini kullanarak local vertex pozisyonlarını dünya koordinatlarına çevir
        for (int i = 0; i < originalMesh.vertexCount; i++)
        {
            Vector3 localPos = originalMesh.vertices[i];
            // Dünya pozisyonuna çevir (scale ve rotation'ı da hesaba katar)
            originalVertices[i] = transform.TransformPoint(localPos);
            currentVertices[i] = originalVertices[i];
        }
        
        // MeshCollider ayarları
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = deformedMesh;
            // Convex kapalı olmalı (concave mesh'ler için)
            // Not: Convex açık olursa bazı çarpışmalar algılanmayabilir
            if (debugMode && meshCollider.convex)
            {
                Debug.LogWarning($"[MeshDeformer] {gameObject.name}: MeshCollider 'Convex' açık. Bazı çarpışmalar algılanmayabilir!", this);
            }
        }
        
        if (debugMode)
            Debug.Log($"[MeshDeformer] {gameObject.name}: Başarıyla başlatıldı. Vertex sayısı: {originalMesh.vertexCount}", this);
    }

    /// <summary>
    /// Çarpışma anında çağrılır - Deformasyon işlemini başlatır
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        if (debugMode)
            Debug.Log($"[MeshDeformer] {gameObject.name}: OnCollisionEnter - {collision.gameObject.name} ile çarpışma!", this);
        
        // Çarpışma bilgilerini al
        if (collision.contacts.Length == 0)
        {
            if (debugMode)
                Debug.LogWarning($"[MeshDeformer] {gameObject.name}: Çarpışmada temas noktası bulunamadı!", this);
            return;
        }
        
        ContactPoint contact = collision.contacts[0];
        Vector3 contactPoint = contact.point; // Temas noktası (dünya koordinatlarında)
        Vector3 relativeVelocity = collision.relativeVelocity; // Çarpışma hızı
        
        // Çarpışma yönünü hesapla (temas noktasından mesh merkezine doğru)
        Vector3 collisionDirection = (transform.position - contactPoint).normalized;
        
        // Çarpışma şiddetini hesapla (hızın büyüklüğü)
        float impactMagnitude = relativeVelocity.magnitude;
        
        if (debugMode)
            Debug.Log($"[MeshDeformer] {gameObject.name}: Çarpışma şiddeti: {impactMagnitude}, Yön: {collisionDirection}", this);
        
        // Deformasyonu uygula
        DeformMesh(contactPoint, collisionDirection, impactMagnitude);
    }

    /// <summary>
    /// Çarpışma devam ederken çağrılır - Sürekli temas için
    /// </summary>
    void OnCollisionStay(Collision collision)
    {
        // Sürekli temas durumunda da deformasyon uygula (daha hafif)
        if (collision.contacts.Length == 0)
            return;
        
        ContactPoint contact = collision.contacts[0];
        Vector3 contactPoint = contact.point;
        Vector3 relativeVelocity = collision.relativeVelocity;
        Vector3 collisionDirection = (transform.position - contactPoint).normalized;
        float impactMagnitude = relativeVelocity.magnitude * 0.1f; // Sürekli temas için daha hafif
        
        DeformMesh(contactPoint, collisionDirection, impactMagnitude);
    }

    /// <summary>
    /// Mesh'i deforme eder - Temas noktasına yakın vertex'leri çarpışma yönünde iter
    /// </summary>
    /// <param name="contactPoint">Temas noktası (dünya koordinatlarında)</param>
    /// <param name="direction">Deformasyon yönü</param>
    /// <param name="impactMagnitude">Çarpışma şiddeti</param>
    void DeformMesh(Vector3 contactPoint, Vector3 direction, float impactMagnitude)
    {
        if (deformedMesh == null || deformedMesh.vertices == null)
            return;
        
        // Mesh'in vertex'lerini al
        Vector3[] vertices = deformedMesh.vertices;
        int deformedVertexCount = 0;
        
        // Her vertex için kontrol et
        for (int i = 0; i < vertices.Length; i++)
        {
            // Vertex'in dünya pozisyonunu hesapla
            Vector3 worldVertexPos = transform.TransformPoint(vertices[i]);
            
            // Temas noktasına olan mesafeyi hesapla
            float distance = Vector3.Distance(worldVertexPos, contactPoint);
            
            // Eğer vertex deformasyon yarıçapı içindeyse
            if (distance < deformRadius)
            {
                // Mesafeye göre deformasyon kuvvetini hesapla (yakın vertex'ler daha fazla etkilenir)
                float distanceFactor = 1f - (distance / deformRadius); // 0 (uzak) - 1 (yakın) arası
                
                // Deformasyon miktarını hesapla (şiddet, kuvvet ve mesafe faktörüne göre)
                // Time.fixedDeltaTime yerine sabit bir değer kullan (daha tutarlı sonuç için)
                float deformationAmount = deformForce * impactMagnitude * distanceFactor * 0.01f;
                
                // Vertex'i çarpışma yönünde içeriye doğru it
                Vector3 deformation = direction * deformationAmount;
                
                // Dünya koordinatlarında yeni pozisyonu hesapla
                Vector3 newWorldPos = worldVertexPos + deformation;
                
                // Local koordinatlara geri çevir ve vertex pozisyonunu güncelle
                vertices[i] = transform.InverseTransformPoint(newWorldPos);
                
                // Mevcut vertex pozisyonunu güncelle (restore için)
                currentVertices[i] = newWorldPos;
                
                isDeformed = true;
                deformedVertexCount++;
            }
        }
        
        // Eğer hiç vertex deforme olmadıysa çık
        if (deformedVertexCount == 0)
            return;
        
        if (debugMode && deformedVertexCount > 0)
            Debug.Log($"[MeshDeformer] {gameObject.name}: {deformedVertexCount} vertex deforme edildi", this);
        
        // Mesh'i güncelle
        deformedMesh.vertices = vertices;
        deformedMesh.RecalculateNormals(); // Işıklandırma için normal'leri yeniden hesapla
        deformedMesh.RecalculateBounds(); // Bounds'ı da güncelle
        
        // MeshCollider'ı güncelle (fizik çarpışmaları için)
        if (meshCollider != null)
        {
            // MeshCollider'ı geçici olarak kapat, güncelle, sonra tekrar aç
            bool wasEnabled = meshCollider.enabled;
            meshCollider.enabled = false;
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = deformedMesh;
            meshCollider.enabled = wasEnabled;
        }
    }

    /// <summary>
    /// Update fonksiyonu - Vertex'leri yavaşça orijinal pozisyonlarına geri döndürür (Spring effect)
    /// </summary>
    void Update()
    {
        // Eğer deformasyon yoksa işlem yapma
        if (!isDeformed)
            return;
        
        // Mesh'in vertex'lerini al
        Vector3[] vertices = deformedMesh.vertices;
        bool stillDeformed = false;
        
        // Her vertex için kontrol et
        for (int i = 0; i < vertices.Length; i++)
        {
            // Vertex'in mevcut dünya pozisyonunu hesapla
            Vector3 currentWorldPos = transform.TransformPoint(vertices[i]);
            
            // Orijinal pozisyona olan mesafeyi kontrol et
            float distanceToOriginal = Vector3.Distance(currentWorldPos, originalVertices[i]);
            
            // Eğer hala orijinal pozisyondan uzaksa
            if (distanceToOriginal > 0.001f) // Küçük bir eşik değeri (sonsuz döngüyü önlemek için)
            {
                // Lerp kullanarak orijinal pozisyona doğru yavaşça hareket et
                Vector3 newWorldPos = Vector3.Lerp(currentWorldPos, originalVertices[i], restoreSpeed * Time.deltaTime);
                
                // Local koordinatlara geri çevir ve vertex pozisyonunu güncelle
                vertices[i] = transform.InverseTransformPoint(newWorldPos);
                
                // Mevcut vertex pozisyonunu güncelle
                currentVertices[i] = newWorldPos;
                
                stillDeformed = true;
            }
            else
            {
                // Orijinal pozisyona çok yakınsa, direkt orijinal pozisyona ayarla
                vertices[i] = transform.InverseTransformPoint(originalVertices[i]);
                currentVertices[i] = originalVertices[i];
            }
        }
        
        // Eğer hala deformasyon varsa mesh'i güncelle
        if (stillDeformed)
        {
            deformedMesh.vertices = vertices;
            deformedMesh.RecalculateNormals(); // Işıklandırma için normal'leri yeniden hesapla
            deformedMesh.RecalculateBounds(); // Bounds'ı da güncelle
            
            // MeshCollider'ı güncelle
            if (meshCollider != null)
            {
                // MeshCollider'ı geçici olarak kapat, güncelle, sonra tekrar aç
                bool wasEnabled = meshCollider.enabled;
                meshCollider.enabled = false;
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = deformedMesh;
                meshCollider.enabled = wasEnabled;
            }
        }
        else
        {
            // Tüm vertex'ler orijinal pozisyonlarına döndü
            isDeformed = false;
        }
    }

    /// <summary>
    /// Editor'da değerler değiştiğinde çağrılır - Validasyon için
    /// </summary>
    void OnValidate()
    {
        // Değerlerin mantıklı aralıklarda olduğundan emin ol
        deformRadius = Mathf.Max(0.1f, deformRadius);
        deformForce = Mathf.Max(0.01f, deformForce);
        restoreSpeed = Mathf.Clamp(restoreSpeed, 0.01f, 1f);
    }
}
