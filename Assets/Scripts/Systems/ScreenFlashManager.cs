using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFlashManager : MonoBehaviour
{
    private static ScreenFlashManager instance;

    public static ScreenFlashManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject tmp = new GameObject(typeof(ScreenFlashManager).Name);
                instance = tmp.AddComponent<ScreenFlashManager>();
            }
            return instance;
        }
    }

    [Header("Flash Settings")]
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 0.6f);
    [SerializeField] private float fadeInTime = 0.08f;
    [SerializeField] private float holdTime = 0.05f;
    [SerializeField] private float fadeOutTime = 0.2f;
    [Header("Fail Feedback")]
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeIntensity = 0.25f;
    [SerializeField] private float slowScale = 0.6f;
    [SerializeField] private float slowDuration = 0.2f;

    private Image overlayImage;
    private Coroutine flashRoutine;
    private Coroutine shakeRoutine;
    private Coroutine slowRoutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        EnsureOverlay();
    }

    public void PlayFailFlash()
    {
        EnsureOverlay();
        if (overlayImage == null)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    public void PlayFailFeedback()
    {
        PlayFailFlash();
        StartShake();
        StartSlow();
    }

    private void EnsureOverlay()
    {
        if (overlayImage != null)
        {
            return;
        }

        Canvas targetCanvas = FindObjectOfType<Canvas>();
        if (targetCanvas == null)
        {
            GameObject canvasObject = new GameObject("ScreenFlashCanvas");
            targetCanvas = canvasObject.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 999;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject overlayObject = new GameObject("ScreenFlashOverlay");
        overlayObject.transform.SetParent(targetCanvas.transform, false);
        overlayImage = overlayObject.AddComponent<Image>();
        overlayImage.raycastTarget = false;

        RectTransform rect = overlayImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        SetOverlayAlpha(0f);
    }

    private IEnumerator FlashRoutine()
    {
        SetOverlayAlpha(0f);

        float elapsed = 0f;
        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            float t = fadeInTime <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeInTime);
            SetOverlayAlpha(Mathf.Lerp(0f, flashColor.a, t));
            yield return null;
        }

        if (holdTime > 0f)
        {
            yield return new WaitForSeconds(holdTime);
        }

        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float t = fadeOutTime <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeOutTime);
            SetOverlayAlpha(Mathf.Lerp(flashColor.a, 0f, t));
            yield return null;
        }

        SetOverlayAlpha(0f);
        flashRoutine = null;
    }

    private void StartShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindObjectOfType<Camera>();
        }

        if (targetCamera == null)
        {
            shakeRoutine = null;
            yield break;
        }

        Transform cameraTransform = targetCamera.transform;
        Vector3 originalLocalPos = cameraTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            cameraTransform.localPosition = originalLocalPos + Random.insideUnitSphere * shakeIntensity;
            yield return null;
        }

        cameraTransform.localPosition = originalLocalPos;
        shakeRoutine = null;
    }

    private void StartSlow()
    {
        if (slowRoutine != null)
        {
            StopCoroutine(slowRoutine);
        }
        slowRoutine = StartCoroutine(SlowRoutine());
    }

    private IEnumerator SlowRoutine()
    {
        float originalScale = Time.timeScale;
        float originalFixedDelta = Time.fixedDeltaTime;
        float targetScale = Mathf.Clamp(slowScale, 0.05f, 1f);

        Time.timeScale = targetScale;
        Time.fixedDeltaTime = originalFixedDelta * targetScale;

        float elapsed = 0f;
        while (elapsed < slowDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = originalScale;
        Time.fixedDeltaTime = originalFixedDelta;
        slowRoutine = null;
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (overlayImage == null)
        {
            return;
        }

        Color color = flashColor;
        color.a = Mathf.Clamp01(alpha);
        overlayImage.color = color;
    }
}
