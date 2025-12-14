using UnityEngine;
using System.Collections;

public class FloatingDisappear : MonoBehaviour
{
    [Header("Timing")]
    public float activeDuration = 5f;
    public float disappearDuration = 0.6f;

    [Header("Floating")]
    public float floatAmplitude = 0.15f;
    public float floatSpeed = 2f;

    [Header("Idle Animation")]
    public float pulseScale = 0.05f;
    public float pulseSpeed = 3f;
    public float rotateSpeed = 20f;

    Vector3 startPos;
    Vector3 startScale;
    float timer;

    void Start()
    {
        startPos = transform.position;
        startScale = transform.localScale;
        StartCoroutine(LifeCycle());
    }

    void Update()
    {
        // Floating
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = startPos + Vector3.up * yOffset;

        // Pulse scale
        float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
        transform.localScale = startScale * (1f + scaleOffset);

        // Soft rotation
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.World);
    }

    IEnumerator LifeCycle()
    {
        yield return new WaitForSeconds(activeDuration);

        // Stop Update animation influence
        enabled = false;

        Vector3 currentScale = transform.localScale;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / disappearDuration;
            transform.localScale = Vector3.Lerp(currentScale, Vector3.zero, Mathf.SmoothStep(0,1,t));
            yield return null;
        }

        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
        // Destroy(gameObject); // istersek tamamen yok eder
    }
}
