using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverScale : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Scale Settings")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float animationSpeed = 10f;

    private Vector3 defaultScale;
    private Vector3 targetScale;

    void Awake()
    {
        defaultScale = transform.localScale;
        targetScale = defaultScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * animationSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = defaultScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = defaultScale;
    }
}
