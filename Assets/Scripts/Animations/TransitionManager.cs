using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Transition Settings")]
    [Tooltip("O tempo que a transição de fade demora, em segundos.")]
    [SerializeField]
    private float transitionDuration = 0.5f;

    [Tooltip("A referência para o CanvasGroup que controla o fade.")]
    [SerializeField]
    private CanvasGroup transitionCanvasGroup;

    void Awake()
    {
        // Singleton para garantir que só existe um gestor.
        if (Instance == null)
        {
            Instance = this;
            // Torna este objeto e o seu canvas persistentes entre cenas.
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(transitionCanvasGroup.gameObject);
        }
        else
        {
            // Se já existe um, destrói este para não haver duplicados.
            Destroy(gameObject);
        }
    }

    // Este é o único método público que você precisa de chamar de outros scripts.
    public void LoadScene(string sceneName)
    {
        // Garante que não iniciamos uma nova transição se já houver uma a decorrer.
        StopAllCoroutines();
        StartCoroutine(TransitionCoroutine(sceneName));
    }

    private IEnumerator TransitionCoroutine(string sceneName)
    {
        // --- FASE 1: FADE OUT (Ficar preto) ---
        yield return Fade(1f); // Faz o fade para Alpha 1 (opaco)

        // --- FASE 2: CARREGAR A CENA ---
        // Usamos LoadSceneAsync para uma transição suave.
        yield return SceneManager.LoadSceneAsync(sceneName);

        // --- FASE 3: FADE IN (Ficar transparente) ---
        yield return Fade(0f); // Faz o fade para Alpha 0 (transparente)
    }

    /// <summary>
    /// Uma coroutine genérica que faz o fade do CanvasGroup para um valor alvo.
    /// </summary>
    /// <param name="targetAlpha">O valor de Alpha final (0 para transparente, 1 para opaco).</param>
    private IEnumerator Fade(float targetAlpha)
    {
        // Ativa os raycasts para bloquear cliques durante o fade.
        transitionCanvasGroup.blocksRaycasts = true;

        float startAlpha = transitionCanvasGroup.alpha;
        float time = 0;

        while (time < transitionDuration)
        {
            // Interpola linearmente o valor do alpha ao longo do tempo.
            transitionCanvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                time / transitionDuration
            );
            time += Time.deltaTime;
            yield return null; // Espera pelo próximo frame.
        }

        // Garante que o alpha final é exatamente o valor alvo.
        transitionCanvasGroup.alpha = targetAlpha;

        // Desativa os raycasts se o canvas ficou transparente.
        transitionCanvasGroup.blocksRaycasts = (targetAlpha != 0);
    }
}
