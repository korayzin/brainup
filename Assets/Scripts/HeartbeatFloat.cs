using UnityEngine;

public class HeartbeatFloat : MonoBehaviour
{
    [Header("Heartbeat (Scale Pulse)")]
    [Tooltip("Dakikadaki atım sayısı (BPM). 60 = saniyede 1 atım.")]
    public float bpm = 72f;

    [Tooltip("Atımın büyüklüğü. 0.08 = %8 büyüme gibi düşün.")]
    [Range(0f, 0.5f)] public float pulseAmount = 0.08f;

    [Tooltip("Atımın keskinliği. Büyükse daha 'dum-dum' gibi olur.")]
    [Range(1f, 12f)] public float pulseSharpness = 6f;

    [Header("Float (Hover)")]
    public float floatAmplitude = 0.02f;   // metre
    public float floatFrequency = 0.6f;    // Hz (saniyede kaç kez)

    [Header("Optional")]
    public bool useUnscaledTime = false;   // pause menüsü vs. etkilenmesin

    Vector3 _baseScale;
    Vector3 _basePos;

    void Awake()
    {
        _baseScale = transform.localScale;
        _basePos   = transform.localPosition;
    }

    void OnEnable()
    {
        // Enable/disable sonrası zıplamasın diye yeniden yakala
        _baseScale = transform.localScale;
        _basePos   = transform.localPosition;
    }

    void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;

        // --- Heartbeat ---
        // 0..1 arası döngü (BPM -> Hz)
        float beatHz = bpm / 60f;
        float phase = (t * beatHz) % 1f;

        // "dum-dum" hissi için: sin'i keskinleştirip kısa bir vurgu yapıyoruz
        // 0..1 -> sin -> 0..1, sonra pow ile keskinleştir
        float s = Mathf.Sin(phase * Mathf.PI);     // 0..1..0
        s = Mathf.Pow(s, pulseSharpness);          // daha kısa/sert vurgu

        float scaleMul = 1f + (s * pulseAmount);
        transform.localScale = _baseScale * scaleMul;

        // --- Float ---
        float floatY = Mathf.Sin(t * Mathf.PI * 2f * floatFrequency) * floatAmplitude;
        transform.localPosition = _basePos + new Vector3(0f, floatY, 0f);
    }
}
