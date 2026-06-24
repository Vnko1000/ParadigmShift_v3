using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// GameOverScreen - Pantalla de muerte del jugador.
/// 
/// Caracteristicas:
/// - Se activa automaticamente al morir (escucha el evento OnDeath de PlayerHealth)
/// - Fondo oscuro con fade in
/// - Icono de muerte con animacion
/// - Texto "MISION FALLIDA" o similar
/// - Estadisticas de la muerte
/// - Botones: Reintentar, Menu Principal
/// - Animaciones dramaticas de entrada
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    [Header("Referencias de UI")]
    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    [SerializeField] private RectTransform gameOverPanel;
    [SerializeField] private Image deathIcon;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("Botones")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Configuracion de Animacion")]
    [SerializeField] private float backgroundFadeDuration = 1.0f;
    [SerializeField] private float iconAppearDelay = 0.5f;
    [SerializeField] private float iconScaleDuration = 0.6f;
    [SerializeField] private float textAppearDelay = 1.2f;
    [SerializeField] private float textTypeSpeed = 0.05f;
    [SerializeField] private float buttonsAppearDelay = 2.0f;

    [Header("Configuracion de Contenido")]
    [SerializeField] private string deathTitle = "MISION FALLIDA";
    [SerializeField] private string[] deathSubtitles = new string[]
    {
        "El aprendiz ha caido...",
        "La burocracia galactica te ha vencido...",
        "Tu nave te necesita...",
        "Chip ha perdido a otro piloto...",
        "El vacio del espacio te reclama..."
    };

    [Header("Audio (opcional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip ambientMusic;

    // --- Estado ---
    private bool isShowing = false;
    private bool isAnimating = false;
    private Vector3 panelDefaultScale;
    private Vector3 iconDefaultScale;

    // --- Referencias a sistemas ---
    private PlayerHealth playerHealth;

    void Start()
    {
        ValidateReferences();
        SetupButtons();
        HideInstant();

        // Suscribirse al evento de muerte del jugador
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnDeath.AddListener(OnPlayerDeath);
        }
        else
        {
            Debug.LogWarning("[GameOverScreen] No se encontro PlayerHealth. La pantalla de muerte no se activara automaticamente.");
        }

        if (gameOverPanel != null)
            panelDefaultScale = gameOverPanel.localScale;
        if (deathIcon != null)
            iconDefaultScale = deathIcon.transform.localScale;
    }

    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDeath.RemoveListener(OnPlayerDeath);
        }
    }

    // ============ EVENT HANDLERS ============

    /// <summary>
    /// Se llama automaticamente cuando el jugador muere.
    /// </summary>
    private void OnPlayerDeath()
    {
        ShowGameOver();
    }

    // ============ METODOS PUBLICOS ============

    /// <summary>
    /// Muestra la pantalla de Game Over con todas sus animaciones.
    /// </summary>
    public void ShowGameOver()
    {
        if (isShowing || isAnimating) return;

        isShowing = true;

        // Configurar contenido
        SetupContent();

        // Iniciar animaciones
        StartCoroutine(GameOverSequenceCoroutine());

        // Audio
        PlaySound(deathSound);

        Debug.Log("[GameOverScreen] Mostrando pantalla de muerte.");
    }

    /// <summary>
    /// Reintentar el nivel actual.
    /// </summary>
    public void RetryLevel()
    {
        PlayButtonSound();

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.ReloadCurrentScene();
        }
        else
        {
            Debug.LogWarning("[GameOverScreen] SceneLoader no encontrado. Recargando escena...");
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    /// <summary>
    /// Volver al menu principal.
    /// </summary>
    public void ReturnToMainMenu()
    {
        PlayButtonSound();

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene("MainMenu");
        }
        else
        {
            Debug.LogError("[GameOverScreen] SceneLoader no encontrado.");
        }
    }

    /// <summary>
    /// Fuerza la visualizacion de la pantalla (para testing).
    /// </summary>
    [ContextMenu("Force Show Game Over")]
    public void ForceShow()
    {
        OnPlayerDeath();
    }

    // ============ METODOS PRIVADOS ============

    private void SetupContent()
    {
        // Titulo
        if (titleText != null)
            titleText.text = deathTitle;

        // Subtitulo aleatorio
        if (subtitleText != null && deathSubtitles.Length > 0)
        {
            int randomIndex = Random.Range(0, deathSubtitles.Length);
            subtitleText.text = deathSubtitles[randomIndex];
        }

        // Estadisticas (placeholder - se pueden extender)
        if (statsText != null)
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            statsText.text = $"Nivel: {currentScene}\n" +
                           $"Causa: Daño fatal";
            // TODO: Agregar mas estadisticas cuando existan (tiempo, enemigos, etc.)
        }
    }

    private void HideInstant()
    {
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.interactable = false;
            gameOverCanvasGroup.blocksRaycasts = false;
        }

        // Ocultar elementos individuales
        if (deathIcon != null)
            deathIcon.color = new Color(1f, 1f, 1f, 0f);
        if (titleText != null)
            titleText.alpha = 0f;
        if (subtitleText != null)
            subtitleText.alpha = 0f;
        if (statsText != null)
            statsText.alpha = 0f;
        if (retryButton != null)
            retryButton.gameObject.SetActive(false);
        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(false);
    }

    private void SetupButtons()
    {
        if (retryButton != null)
            retryButton.onClick.AddListener(RetryLevel);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void ValidateReferences()
    {
        if (gameOverCanvasGroup == null)
        {
            gameOverCanvasGroup = GetComponent<CanvasGroup>();
            if (gameOverCanvasGroup == null)
            {
                Debug.LogError("[GameOverScreen] No se encontro CanvasGroup. Asignalo manualmente.");
            }
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void PlayButtonSound()
    {
        // Reutilizar el mismo AudioSource si existe
        // TODO: Agregar un AudioClip especifico para botones si se desea
    }

    // ============ CORUTINA PRINCIPAL DE ANIMACION ============

    private IEnumerator GameOverSequenceCoroutine()
    {
        isAnimating = true;

        // 1. Fondo negro fade in
        yield return StartCoroutine(BackgroundFadeCoroutine());

        // 2. Icono de muerte aparece con escala
        yield return new WaitForSecondsRealtime(iconAppearDelay);
        yield return StartCoroutine(IconAppearCoroutine());

        // 3. Texto aparece (typewriter effect)
        yield return new WaitForSecondsRealtime(textAppearDelay - iconAppearDelay - iconScaleDuration);
        yield return StartCoroutine(TextAppearCoroutine());

        // 4. Botones aparecen
        yield return new WaitForSecondsRealtime(buttonsAppearDelay - textAppearDelay);
        yield return StartCoroutine(ButtonsAppearCoroutine());

        // Activar interaccion
        if (gameOverCanvasGroup != null)
        {
            gameOverCanvasGroup.interactable = true;
            gameOverCanvasGroup.blocksRaycasts = true;
        }

        isAnimating = false;
    }

    private IEnumerator BackgroundFadeCoroutine()
    {
        if (gameOverCanvasGroup == null) yield break;

        float elapsed = 0f;

        while (elapsed < backgroundFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / backgroundFadeDuration;
            // Easing suave
            t = Mathf.SmoothStep(0f, 1f, t);
            gameOverCanvasGroup.alpha = t;
            yield return null;
        }

        gameOverCanvasGroup.alpha = 1f;
    }

    private IEnumerator IconAppearCoroutine()
    {
        if (deathIcon == null) yield break;

        deathIcon.color = Color.white;
        deathIcon.transform.localScale = Vector3.zero;

        float elapsed = 0f;

        while (elapsed < iconScaleDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / iconScaleDuration;
            // Overshoot easing (rebote)
            t = Mathf.Sin(t * Mathf.PI * 0.5f); // Easing suave
            deathIcon.transform.localScale = Vector3.Lerp(Vector3.zero, iconDefaultScale * 1.2f, t);
            yield return null;
        }

        // Settle back to normal
        float settleElapsed = 0f;
        float settleDuration = 0.15f;
        while (settleElapsed < settleDuration)
        {
            settleElapsed += Time.unscaledDeltaTime;
            float t = settleElapsed / settleDuration;
            deathIcon.transform.localScale = Vector3.Lerp(iconDefaultScale * 1.2f, iconDefaultScale, t);
            yield return null;
        }

        deathIcon.transform.localScale = iconDefaultScale;

        // Pulso continuo del icono
        StartCoroutine(IconPulseCoroutine());
    }

    private IEnumerator IconPulseCoroutine()
    {
        while (isShowing)
        {
            float t = 0f;
            float pulseDuration = 2f;

            while (t < pulseDuration)
            {
                if (!isShowing) yield break;
                t += Time.unscaledDeltaTime;
                float pulse = 1f + Mathf.Sin(t * Mathf.PI * 2f / pulseDuration) * 0.05f;
                if (deathIcon != null)
                    deathIcon.transform.localScale = iconDefaultScale * pulse;
                yield return null;
            }
        }
    }

    private IEnumerator TextAppearCoroutine()
    {
        // Titulo
        if (titleText != null)
        {
            titleText.alpha = 0f;
            float elapsed = 0f;
            float fadeDuration = 0.5f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                titleText.alpha = elapsed / fadeDuration;
                yield return null;
            }
            titleText.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(0.3f);

        // Subtitulo con efecto typewriter
        if (subtitleText != null)
        {
            subtitleText.alpha = 1f;
            string fullText = subtitleText.text;
            subtitleText.text = "";

            foreach (char c in fullText)
            {
                subtitleText.text += c;
                yield return new WaitForSecondsRealtime(textTypeSpeed);
            }
        }

        yield return new WaitForSecondsRealtime(0.3f);

        // Estadisticas
        if (statsText != null)
        {
            float elapsed = 0f;
            float fadeDuration = 0.5f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                statsText.alpha = elapsed / fadeDuration;
                yield return null;
            }
            statsText.alpha = 1f;
        }
    }

    private IEnumerator ButtonsAppearCoroutine()
    {
        if (retryButton != null)
        {
            retryButton.gameObject.SetActive(true);
            CanvasGroup retryCG = retryButton.GetComponent<CanvasGroup>();
            if (retryCG == null) retryCG = retryButton.gameObject.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                retryCG.alpha = elapsed / 0.3f;
                yield return null;
            }
            retryCG.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(0.15f);

        if (mainMenuButton != null)
        {
            mainMenuButton.gameObject.SetActive(true);
            CanvasGroup menuCG = mainMenuButton.GetComponent<CanvasGroup>();
            if (menuCG == null) menuCG = mainMenuButton.gameObject.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                menuCG.alpha = elapsed / 0.3f;
                yield return null;
            }
            menuCG.alpha = 1f;
        }
    }
}
