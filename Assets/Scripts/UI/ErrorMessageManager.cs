using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ErrorMessageManager : MonoBehaviour
{
    public static ErrorMessageManager Instance { get; private set; }

    [Header("Animation Settings")]
    [SerializeField]
    private float fadeInDuration = 0.3f;

    [SerializeField]
    private float fadeOutDuration = 0.5f;

    [SerializeField]
    private float defaultDisplayDuration = 4f;

    private RectTransform _errorContainerRect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CreateErrorContainer();
    }

    private void CreateErrorContainer()
    {
        if (_errorContainerRect != null)
            return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("ErrorMessageManager: Nenhum Canvas encontrado.");
            return;
        }

        GameObject containerGO = new GameObject("ErrorContainer (Generated)");
        containerGO.transform.SetParent(canvas.transform, false);

        _errorContainerRect = containerGO.AddComponent<RectTransform>();
        _errorContainerRect.anchorMin = new Vector2(1, 1);
        _errorContainerRect.anchorMax = new Vector2(1, 1);
        _errorContainerRect.pivot = new Vector2(1, 1);
        _errorContainerRect.anchoredPosition = new Vector2(-20, -20);
        _errorContainerRect.sizeDelta = new Vector2(350, 0);

        var layoutGroup = containerGO.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 15;
        layoutGroup.childAlignment = TextAnchor.UpperRight;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;

        var csf = containerGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    public void ShowError(string message, float? duration = null)
    {
        if (_errorContainerRect == null)
            CreateErrorContainer();

        if (_errorContainerRect == null)
        {
            Debug.LogError("ErrorMessageManager: O contentor de erros não existe");
            return;
        }
        StartCoroutine(ShowAndFadeErrorCoroutine(message, duration ?? defaultDisplayDuration));
    }

    private IEnumerator ShowAndFadeErrorCoroutine(string message, float displayDuration)
    {
        GameObject instance = new GameObject("ErrorMessage (Generated)");
        instance.transform.SetParent(_errorContainerRect, false);
        instance.transform.SetAsFirstSibling();

        var bgImage = instance.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        var layoutElement = instance.AddComponent<LayoutElement>();
        layoutElement.minHeight = 50;

        GameObject textGO = new GameObject("Error");
        textGO.transform.SetParent(instance.transform, false);

        var textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = message;
        textComponent.color = Color.white;
        textComponent.fontSize = 14;
        textComponent.alignment = TextAlignmentOptions.Center;

        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(15, 10);
        textRect.offsetMax = new Vector2(-15, -10);

        var canvasGroup = instance.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        yield return null;

        float timer = 0f;
        while (timer < fadeInDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(0, 1, timer / fadeInDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(displayDuration);

        timer = 0f;
        while (timer < fadeOutDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(1, 0, timer / fadeOutDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(instance);
    }
}
