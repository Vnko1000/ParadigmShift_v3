using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// PauseMenu - Sistema completo de menu de pausa.
/// 
/// Caracteristicas:
/// - Pausa el juego (Time.timeScale = 0)
/// - Panel con fondo semitransparente
/// - Botones: Reanudar, Opciones (placeholder), Menu Principal, Salir
/// - Animacion de fade in/out
/// - Navegacion con teclado y gamepad
/// 
/// Uso: Colocar en el HUD_Canvas o en un Canvas separado.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Referencias de UI")]
    [SerializeField] private CanvasGroup pauseCanvasGroup;
    [SerializeField] private RectTransform pausePanel;

    [Header("Botones")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Configuracion")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.15f;
    [SerializeField] private float panelScaleInDuration = 0.25f;

    [Header("Audio (opcional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pauseOpenSound;
    [SerializeField] private AudioClip pauseCloseSound;
    [SerializeField] private AudioClip buttonClickSound;

    // --- Estado ---
    private bool isPaused = false;
    private bool isAnimating = false;
    private Vector3 panelDefaultScale;

    // --- Eventos ---
    public delegate void PauseEvent();
    public static event PauseEvent OnGamePaused;
    public static event PauseEvent OnGameResumed;

    void Start()
    {
        ValidateReferences();
        SetupButtons();
        HideInstant();

        if (pausePanel != null)
            panelDefaultScale = pausePanel.localScale;
    }

    void Update()
    {
        // Detectar tecla de pausa
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
    }

    // ============ METODOS PUBLICOS ============

    /// <summary>
    /// Alterna entre pausado y reanudado.
    /// </summary>
    public void TogglePause()
    {
        if (isAnimating) return;

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    /// <summary>
    /// Pausa el juego y muestra el menu.
    /// </summary>
    public void PauseGame()
    {
        if (isPaused || isAnimating) return;

        isPaused = true;
        Time.timeScale = 0f; // Pausar el tiempo del juego

        ShowPauseMenu();
        PlaySound(pauseOpenSound);

        OnGamePaused?.Invoke();

        Debug.Log("[PauseMenu] Juego pausado.");
    }

    /// <summary>
    /// Reanuda el juego y oculta el menu.
    /// </summary>
    public void ResumeGame()
    {
        if (!isPaused || isAnimating) return;

        isPaused = false;
        Time.timeScale = 1f; // Restaurar el tiempo

        HidePauseMenu();
        PlaySound(pauseCloseSound);

        OnGameResumed?.Invoke();

        Debug.Log("[PauseMenu] Juego reanudado.");
    }

    /// <summary>
    /// Abre el menu de opciones (placeholder para futuro).
    /// </summary>
    public void OpenOptions()
    {
        PlayButtonSound();
        Debug.Log("[PauseMenu] Opciones - Implementar menu de configuracion.");
        // TODO: Implementar menu de opciones
    }

    /// <summary>
    /// Vuelve al menu principal.
    /// </summary>
    public void ReturnToMainMenu()
    {
        PlayButtonSound();
        ResumeGame(); // Restaurar timeScale antes de cambiar escena

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene("MainMenu");
        }
        else
        {
            Debug.LogError("[PauseMenu] SceneLoader no encontrado. No se puede cargar el menu principal.");
        }
    }

    /// <summary>
    /// Sale del juego.
    /// </summary>
    public void QuitGame()
    {
        PlayButtonSound();

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.QuitGame();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    // ============ METODOS PRIVADOS ============

    private void ShowPauseMenu()
    {
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = true;
            pauseCanvasGroup.blocksRaycasts = true;
            StartCoroutine(FadeInCoroutine());
        }
    }

    private void HidePauseMenu()
    {
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
            StartCoroutine(FadeOutCoroutine());
        }
    }

    private void HideInstant()
    {
        if (pauseCanvasGroup != null)
        {
            pauseCanvasGroup.alpha = 0f;
            pauseCanvasGroup.interactable = false;
            pauseCanvasGroup.blocksRaycasts = false;
        }
    }

    private void SetupButtons()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(OpenOptions);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }

    private void ValidateReferences()
    {
        if (pauseCanvasGroup == null)
        {
            pauseCanvasGroup = GetComponent<CanvasGroup>();
            if (pauseCanvasGroup == null)
            {
                Debug.LogError("[PauseMenu] No se encontro CanvasGroup. Asignalo manualmente.");
            }
        }
    }

    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
            audioSource.PlayOneShot(buttonClickSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    // ============ CORUTINAS DE ANIMACION ============

    private IEnumerator FadeInCoroutine()
    {
        isAnimating = true;
        float elapsed = 0f;

        // Escala del panel (pequeño a normal)
        if (pausePanel != null)
        {
            pausePanel.localScale = panelDefaultScale * 0.8f;
        }

        while (elapsed < Mathf.Max(fadeInDuration, panelScaleInDuration))
        {
            elapsed += Time.unscaledDeltaTime; // unscaled porque el juego esta pausado

            // Fade
            if (elapsed <= fadeInDuration && pauseCanvasGroup != null)
            {
                pauseCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            }

            // Scale
            if (elapsed <= panelScaleInDuration && pausePanel != null)
            {
                float scaleT = elapsed / panelScaleInDuration;
                // Easing suave
                scaleT = Mathf.SmoothStep(0f, 1f, scaleT);
                pausePanel.localScale = Vector3.Lerp(panelDefaultScale * 0.8f, panelDefaultScale, scaleT);
            }

            yield return null;
        }

        // Asegurar valores finales
        if (pauseCanvasGroup != null)
            pauseCanvasGroup.alpha = 1f;
        if (pausePanel != null)
            pausePanel.localScale = panelDefaultScale;

        isAnimating = false;
    }

    private IEnumerator FadeOutCoroutine()
    {
        isAnimating = true;
        float startAlpha = pauseCanvasGroup != null ? pauseCanvasGroup.alpha : 1f;
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (pauseCanvasGroup != null)
            {
                pauseCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            }

            yield return null;
        }

        if (pauseCanvasGroup != null)
            pauseCanvasGroup.alpha = 0f;

        isAnimating = false;
    }

    // ============ GIZMOS ============

    private void OnDrawGizmosSelected()
    {
        // Indicador visual en el editor
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
