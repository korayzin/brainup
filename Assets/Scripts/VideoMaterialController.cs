using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Video dosyasını material'a texture olarak atar.
/// VideoPlayer component'i kullanarak video'yu RenderTexture'a oynatır
/// ve bu RenderTexture'ı belirtilen material property'sine atar.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class VideoMaterialController : MonoBehaviour
{
    [Header("Material Ayarları")]
    [Tooltip("Video'nun atanacağı material (M_Dreamy)")]
    public Material targetMaterial;
    
    [Tooltip("Material'daki hangi texture property'sine video atanacak? (Dream Image için _DreamTex)")]
    public string texturePropertyName = "_DreamTex";
    
    [Header("Video Ayarları")]
    [Tooltip("Oynatılacak video dosyası (VideoClip)")]
    public VideoClip videoClip;
    
    [Tooltip("Video dosyası yolu (URL veya dosya yolu) - VideoClip yerine kullanılabilir")]
    public string videoPath;
    
    [Tooltip("RenderTexture çözünürlüğü")]
    public Vector2Int renderTextureResolution = new Vector2Int(1920, 1080);
    
    [Header("Oynatma Ayarları")]
    [Tooltip("Oyun başladığında otomatik oynat")]
    public bool playOnStart = true;
    
    [Tooltip("Video döngüde oynatılsın mı?")]
    public bool loop = true;
    
    [Tooltip("Video oynatma hızı (1.0 = normal hız)")]
    [Range(0.1f, 2.0f)]
    public float playbackSpeed = 1.0f;
    
    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    
    void Awake()
    {
        // VideoPlayer component'ini al
        videoPlayer = GetComponent<VideoPlayer>();
        
        // VideoPlayer'ın PlayOnAwake'ini kapat (biz kontrol edeceğiz)
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
        }
    }
    
    void Start()
    {
        // RenderTexture oluştur
        CreateRenderTexture();
        
        // VideoPlayer'ı yapılandır
        SetupVideoPlayer();
        
        // Material'a texture'ı ata
        ApplyTextureToMaterial();
    }
    
    void CreateRenderTexture()
    {
        // Eski RenderTexture'ı temizle
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        
        // Yeni RenderTexture oluştur
        renderTexture = new RenderTexture(
            renderTextureResolution.x,
            renderTextureResolution.y,
            0,
            RenderTextureFormat.ARGB32
        );
        
        renderTexture.name = "VideoRenderTexture";
        renderTexture.Create();
        
        Debug.Log($"RenderTexture oluşturuldu: {renderTextureResolution.x}x{renderTextureResolution.y}");
    }
    
    void SetupVideoPlayer()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoMaterialController: VideoPlayer component bulunamadı!");
            return;
        }
        
        if (renderTexture == null)
        {
            Debug.LogError("VideoMaterialController: RenderTexture oluşturulamadı!");
            return;
        }
        
        // RenderTexture'ı ÖNCE VideoPlayer'a ata (video kaynağından önce)
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        Debug.Log("RenderTexture VideoPlayer'a atandı.");
        
        // Video kaynağını ayarla
        if (videoClip != null)
        {
            videoPlayer.clip = videoClip;
            videoPlayer.source = VideoSource.VideoClip;
            Debug.Log($"VideoClip atandı: {videoClip.name}");
        }
        else if (!string.IsNullOrEmpty(videoPath))
        {
            videoPlayer.url = videoPath;
            videoPlayer.source = VideoSource.Url;
            Debug.Log($"Video URL atandı: {videoPath}");
        }
        else
        {
            Debug.LogError("VideoMaterialController: VideoClip veya videoPath belirtilmedi!");
            return;
        }
        
        // Oynatma ayarları
        videoPlayer.isLooping = loop;
        videoPlayer.playbackSpeed = playbackSpeed;
        videoPlayer.playOnAwake = false; // Biz kontrol edeceğiz
        
        // Event'leri temizle ve yeniden bağla
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        
        videoPlayer.errorReceived += OnVideoError;
        
        // Video hazır olduğunda otomatik oynat
        Debug.Log("Video hazırlanıyor...");
        videoPlayer.Prepare();
    }
    
    void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("Video hazır!");
        
        // Material'a texture'ı tekrar ata (video hazır olduktan sonra)
        ApplyTextureToMaterial();
        
        if (playOnStart)
        {
            Debug.Log("Video oynatılıyor...");
            vp.Play();
        }
    }
    
    void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"VideoPlayer hatası: {message}");
    }
    
    void ApplyTextureToMaterial()
    {
        if (targetMaterial == null)
        {
            Debug.LogWarning("VideoMaterialController: Target material belirtilmedi!");
            return;
        }
        
        if (renderTexture == null)
        {
            Debug.LogWarning("VideoMaterialController: RenderTexture oluşturulamadı!");
            return;
        }
        
        // Material'da belirtilen property var mı kontrol et
        if (targetMaterial.HasProperty(texturePropertyName))
        {
            targetMaterial.SetTexture(texturePropertyName, renderTexture);
            Debug.Log($"Video texture'ı '{texturePropertyName}' property'sine atandı.");
        }
        else
        {
            Debug.LogWarning($"VideoMaterialController: Material'da '{texturePropertyName}' property'si bulunamadı!");
        }
    }
    
    void OnDestroy()
    {
        // RenderTexture'ı temizle
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        
        // VideoPlayer event'lerini temizle
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }
    
    void OnApplicationQuit()
    {
        // Uygulama kapanırken temizlik
        OnDestroy();
    }
    
    // Public metodlar - dışarıdan kontrol için
    public void PlayVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }
    }
    
    public void PauseVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Pause();
        }
    }
    
    public void StopVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
    }
    
    public void SetVideoClip(VideoClip clip)
    {
        videoClip = clip;
        if (videoPlayer != null)
        {
            SetupVideoPlayer();
        }
    }
    
    public void SetVideoPath(string path)
    {
        videoPath = path;
        if (videoPlayer != null)
        {
            SetupVideoPlayer();
        }
    }
}
