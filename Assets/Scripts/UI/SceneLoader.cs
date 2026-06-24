using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
/// <summary>
/// SceneLoader - Utilidad centralizada para cargar escenas.
/// 
/// Funciona como un singleton persistente entre escenas.
/// Soporta fade in/out con una pantalla negra opcional.
/// 
/// Uso:
///   SceneLoader.Instance.LoadScene("NombreEscena");
///   SceneLoader.Instance.LoadSceneWithFade("NombreEscena", 0.5f);
///   SceneLoader.Instance.ReloadCurrentScene();
///   SceneLoader.Instance.QuitGame();
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [Header("Configuracion de Fade")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float defaultFadeDuration = 0.5f;

    [Header("Pantalla de Fade (opcional)")]
    [Tooltip("Asigna un Image negro que cubra toda la pantalla. Si es null, se crea automaticamente.")]
    [SerializeField] private CanvasGroup fadeOverlay;

    // --- Singleton ---
    public static SceneLoader Instance { get; private set; }

    private bool isLoading = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Crear fade overlay si no existe
        if (fadeOverlay == null)
        {
            CreateFadeOverlay();
        }

        // Iniciar con fade in (pantalla negra a transparente)
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 1f;
            StartCoroutine(FadeCoroutine(0f, defaultFadeDuration));
        }
    }

    // ============ METODOS PUBLICOS ============

    /// <summary>
    /// Carga una escena por su nombre.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (isLoading) return;

        if (useFade)
        {
            StartCoroutine(LoadSceneWithFadeCoroutine(sceneName, defaultFadeDuration));
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    /// <summary>
    /// Carga una escena con fade personalizado.
    /// </summary>
    public void LoadSceneWithFade(string sceneName, float fadeDuration)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneWithFadeCoroutine(sceneName, fadeDuration));
    }

    /// <summary>
    /// Recarga la escena actual.
    /// </summary>
    public void ReloadCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        LoadScene(currentScene);
    }

    /// <summary>
    /// Carga la siguiente escena en el Build Settings.
    /// </summary>
    public void LoadNextScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            LoadScene(SceneUtility.GetScenePathByBuildIndex(nextIndex));
        }
        else
        {
            Debug.LogWarning("[SceneLoader] No hay siguiente escena en Build Settings.");
        }
    }

    /// <summary>
    /// Sale del juego (funciona en build y en editor).
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[SceneLoader] Saliendo del juego...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ============ CORUTINAS PRIVADAS ============

    private IEnumerator LoadSceneWithFadeCoroutine(string sceneName, float fadeDuration)
    {
        isLoading = true;

        // Fade out (a negro)
        yield return StartCoroutine(FadeCoroutine(1f, fadeDuration));

        // Cargar escena
        SceneManager.LoadScene(sceneName);

        // Esperar un frame para que la escena cargue
        yield return null;

        // Fade in (de negro a transparente)
        yield return StartCoroutine(FadeCoroutine(0f, fadeDuration));

        isLoading = false;
    }

    private IEnumerator FadeCoroutine(float targetAlpha, float duration)
    {
        if (fadeOverlay == null) yield break;

        float startAlpha = fadeOverlay.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Usar unscaled para que funcione durante pausa
            float t = elapsed / duration;
            fadeOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        fadeOverlay.alpha = targetAlpha;
    }

    // ============ SETUP ============

    private void CreateFadeOverlay()
    {
        // Crear un Canvas para el fade overlay
        GameObject fadeCanvasGO = new GameObject("FadeOverlayCanvas");
        fadeCanvasGO.transform.SetParent(transform);

        Canvas fadeCanvas = fadeCanvasGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999; // Encima de todo

        CanvasScaler scaler = fadeCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Crear la imagen negra
        GameObject fadeImageGO = new GameObject("FadeImage");
        fadeImageGO.transform.SetParent(fadeCanvasGO.transform);

        RectTransform rectTransform = fadeImageGO.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image fadeImage = fadeImageGO.AddComponent<Image>();
        fadeImage.color = Color.black;

        fadeOverlay = fadeImageGO.AddComponent<CanvasGroup>();
        fadeOverlay.alpha = 0f;
        fadeOverlay.blocksRaycasts = false;
        fadeOverlay.interactable = false;
    }
}
