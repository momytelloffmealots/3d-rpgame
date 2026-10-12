
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Effect")]
    public float hoverScale = 1.1f;
    public float bounceScale = 1.16f;
    public float animationSpeed = 15f;

    [Header("Hover Sound")]
    public AudioSource audioSource;
    public AudioClip hoverSound;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(
            BounceAnimation()
        );

        if (audioSource != null && hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(
            ScaleTo(originalScale)
        );
    }

    private IEnumerator BounceAnimation()
    {
        Vector3 bigScale = originalScale * bounceScale;
        Vector3 targetScale = originalScale * hoverScale;

        yield return ScaleTo(bigScale);
        yield return ScaleTo(targetScale);

        animationCoroutine = null;
    }

    private IEnumerator ScaleTo(Vector3 target)
    {
        while (Vector3.Distance(
            rectTransform.localScale, target) > 0.001f)
        {
            rectTransform.localScale = Vector3.Lerp(
                rectTransform.localScale,
                target,
                Time.unscaledDeltaTime * animationSpeed
            );

            yield return null;
        }

        rectTransform.localScale = target;
        animationCoroutine = null;
    }
}
