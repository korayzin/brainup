using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class TMPTextBender : MonoBehaviour
{
    [Header("Bend Settings")]
    public AnimationCurve bendCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.5f, 1f),
        new Keyframe(1f, 0f)
    );

    public float amplitude = 25f;
    public float yOffset = 0f;
    public float zAmplitude = 0f;

    [Header("Sweet Letter Animation")]
    [Tooltip("Harflerin ne kadar oynadığı")]
    public float wiggleAmount = 3f;

    [Tooltip("Animasyon hızı")]
    public float wiggleSpeed = 1.2f;

    [Tooltip("Z ekseninde çok hafif derinlik")]
    public float wiggleZ = 0.5f;

    public bool liveUpdate = true;

    TMP_Text tmp;
    bool hasTextChanged;

    void OnEnable()
    {
        tmp = GetComponent<TMP_Text>();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        ForceUpdate();
    }

    void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    void OnTextChanged(Object obj)
    {
        if (obj == tmp) hasTextChanged = true;
    }

    void Update()
    {
        if (!tmp) return;

        if (!liveUpdate && !hasTextChanged) return;

        hasTextChanged = false;
        Bend();
    }

    public void ForceUpdate()
    {
        if (!tmp) tmp = GetComponent<TMP_Text>();
        hasTextChanged = true;
        Bend();
    }

    void Bend()
    {
        tmp.ForceMeshUpdate();
        var textInfo = tmp.textInfo;
        if (textInfo == null || textInfo.characterCount == 0) return;

        var bounds = tmp.bounds;
        float minX = bounds.min.x;
        float maxX = bounds.max.x;
        float width = Mathf.Max(0.0001f, maxX - minX);

        float time = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;
            Vector3[] verts = textInfo.meshInfo[matIndex].vertices;

            Vector3 bl = verts[vertIndex + 0];
            Vector3 tr = verts[vertIndex + 2];
            Vector3 mid = (bl + tr) * 0.5f;

            // Normalize X for bend
            float x01 = Mathf.Clamp01((mid.x - minX) / width);

            // ---- BEND ----
            float bendY = bendCurve.Evaluate(x01) * amplitude + yOffset;
            float bendZ = bendCurve.Evaluate(x01) * zAmplitude;

            // ---- SWEET WIGGLE ----
            float phase = i * 0.35f;
            float wiggle =
                Mathf.Sin(time * wiggleSpeed + phase) *
                Mathf.PerlinNoise(i * 0.3f, time * 0.5f);

            float wiggleY = wiggle * wiggleAmount;
            float wiggleDepth = Mathf.Sin(time * wiggleSpeed * 0.7f + phase) * wiggleZ;

            Vector3 totalOffset = new Vector3(
                0f,
                bendY + wiggleY,
                bendZ + wiggleDepth
            );

            // Pivot
            Vector3 offset = mid;
            for (int v = 0; v < 4; v++)
                verts[vertIndex + v] -= offset;

            for (int v = 0; v < 4; v++)
                verts[vertIndex + v] += offset + totalOffset;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            tmp.UpdateGeometry(meshInfo.mesh, i);
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += () =>
            {
                if (this) ForceUpdate();
            };
        }
    }
#endif
}
