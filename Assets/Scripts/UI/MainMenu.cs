using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// MainMenu - Sistema completo del menu principal.
/// 
/// Caracteristicas:
/// - Fondo animado (con opcion de parallax con el mouse)
/// - Logo del juego con animacion de flotacion
/// - Botones: Jugar, Opciones, Creditos, Salir
/// - Transiciones suaves entre estados del menu
/// - Navegacion con teclado, raton y gamepad
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Paneles Principales")]
    [SerializeField] private CanvasGroup mainPanel;
    [SerializeField] private CanvasGroup optionsPanel;
    [SerializeField] private CanvasGroup creditsPanel;

    [Header("Elementos del Menu Principal")]
    [SerializeField] private Image logoImage;
    [SerializeField] private RectTransform logoTransform;
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    [Header("Elementos de Opciones")]
    [SerializeField] private Button optionsBackButton;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Elementos de Creditos")]
    [SerializeField] private Button creditsBackButton;
    [SerializeField] private TextMeshProUGUI creditsText;

    [Header("Configuracion de Animaciones")]
    [SerializeField] private float logoFloatSpeed = 1.5f;
    [SerializeField] private float logoFloatAmount = 10f;
    [SerializeField] private float menuFadeDuration = 0.3f;
    [SerializeField] private float buttonStaggerDelay = 0.1f;
    [SerializeField] private float logoAppearDelay = 0.3f;

    [Header("Configuracion de Escenas")]
    [SerializeField] private string firstLevelSceneName = "SampleScene";

    [Header("Audio (opcional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip menuMusic;

    // --- Estado ---
    private MenuState currentState = MenuState.Main;
    private Vector3 logoStartPosition;
    private bool isTransitioning = false;

    private enum MenuState
    {
        Main,
        Options,
        Credits
    }

    void Start()
    {
        ValidateReferences();
        SetupButtons();

        // Guardar posicion inicial del logo
        if (logoTransform != null)
            logoStartPosition = logoTransform.anchoredPosition;

        // Inicializar paneles
        ShowPanelInstant(mainPanel);
        HidePanelInstant(optionsPanel);
        HidePanelInstant(creditsPanel);

        // Animacion de entrada
        StartCoroutine(MenuEntryAnimation());

        // Musica de fondo
        PlayMenuMusic();

        Debug.Log("[MainMenu] Menu principal inicializado.");
    }

    void Update()
    {
        // Animacion de flotacion del logo
        AnimateLogo();

        // Navegacion con Escape para volver atras
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == MenuState.Options)
                ShowMainMenu();
            else if (currentState == MenuState.Credits)
                ShowMainMenu();
        }
    }

    // ============ METODOS PUBLICOS - BOTONES ============

    /// <summary>
    /// Inicia el juego (carga la primera escena).
    /// </summary>
    public void PlayGame()
    {
        PlayButtonSound();
        Debug.Log($"[MainMenu] Iniciando juego - Cargando escena: {firstLevelSceneName}");

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene(firstLevelSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(firstLevelSceneName);
        }
    }

    /// <summary>
    /// Muestra el panel de opciones.
    /// </summary>
    public void ShowOptions()
    {
        PlayButtonSound();
        if (isTransitioning) return;
        StartCoroutine(TransitionToPanel(optionsPanel));
        currentState = MenuState.Options;
        LoadSettings();
    }

    /// <summary>
    /// Muestra el panel de creditos.
    /// </summary>
    public void ShowCredits()
    {
        PlayButtonSound();
        if (isTransitioning) return;
        StartCoroutine(TransitionToPanel(creditsPanel));
        currentState = MenuState.Credits;
        StartCoroutine(ScrollCredits());
    }

    /// <summary>
    /// Vuelve al menu principal desde cualquier sub-panel.
    /// </summary>
    public void ShowMainMenu()
    {
        PlayButtonSound();
        if (isTransitioning) return;

        CanvasGroup currentPanel = GetCurrentPanel();
        StartCoroutine(TransitionBackToMain(currentPanel));
        currentState = MenuState.Main;
    }

    /// <summary>
    /// Sale del juego.
    /// </summary>
    public void QuitGame()
    {
        PlayButtonSound();
        Debug.Log("[MainMenu] Saliendo del juego...");

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

    // ============ OPCIONES ============

    /// <summary>
    /// Guarda las opciones actuales (llamado al cambiar algun valor).
    /// </summary>
    public void SaveSettings()
    {
        if (masterVolumeSlider != null)
            PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        if (musicVolumeSlider != null)
            PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
        if (fullscreenToggle != null)
            PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

        PlayerPrefs.Save();

        // Aplicar cambios
        ApplySettings();
    }

    /// <summary>
    /// Carga las opciones guardadas.
    /// </summary>
    public void LoadSettings()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        ApplySettings();
    }

    private void ApplySettings()
    {
        // Volumen maestro
        if (masterVolumeSlider != null)
            AudioListener.volume = masterVolumeSlider.value;

        // Pantalla completa
        if (fullscreenToggle != null)
            Screen.fullScreen = fullscreenToggle.isOn;

        // TODO: Implementar control de volumen de musica y SFX por canales separados
    }

    // ============ METODOS PRIVADOS ============

    private void ValidateReferences()
    {
        if (mainPanel == null)
        {
            mainPanel = GetComponent<CanvasGroup>();
            if (mainPanel == null)
                Debug.LogError("[MainMenu] No se encontro CanvasGroup del panel principal.");
        }
    }

    private void SetupButtons()
    {
        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);

        if (optionsButton != null)
            optionsButton.onClick.AddListener(ShowOptions);

        if (creditsButton != null)
            creditsButton.onClick.AddListener(ShowCredits);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (optionsBackButton != null)
            optionsBackButton.onClick.AddListener(ShowMainMenu);

        if (creditsBackButton != null)
            creditsBackButton.onClick.AddListener(ShowMainMenu);

        // Sliders y toggles
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(_ => SaveSettings());
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(_ => SaveSettings());
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(_ => SaveSettings());
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(_ => SaveSettings());
    }

    private void AnimateLogo()
    {
        if (logoTransform == null) return;

        // Flotacion suave
        float yOffset = Mathf.Sin(Time.time * logoFloatSpeed) * logoFloatAmount;
        logoTransform.anchoredPosition = logoStartPosition + new Vector3(0f, yOffset, 0f);
    }

    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
            audioSource.PlayOneShot(buttonClickSound);
    }

    private void PlayMenuMusic()
    {
        if (audioSource != null && menuMusic != null && !audioSource.isPlaying)
        {
            audioSource.clip = menuMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private CanvasGroup GetCurrentPanel()
    {
        return currentState switch
        {
            MenuState.Options => optionsPanel,
            MenuState.Credits => creditsPanel,
            _ => mainPanel
        };
    }

    // ============ UTILIDADES DE PANELES ============

    private void ShowPanelInstant(CanvasGroup panel)
    {
        if (panel == null) return;
        panel.alpha = 1f;
        panel.interactable = true;
        panel.blocksRaycasts = true;
    }

    private void HidePanelInstant(CanvasGroup panel)
    {
        if (panel == null) return;
        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
    }

    // ============ CORUTINAS DE ANIMACION ============

    private IEnumerator MenuEntryAnimation()
    {
        isTransitioning = true;

        // Logo aparece con delay
        if (logoTransform != null)
        {
            logoTransform.localScale = Vector3.zero;
            yield return new WaitForSecondsRealtime(logoAppearDelay);

            float elapsed = 0f;
            float duration = 0.5f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0f, 1f, t);
                logoTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                yield return null;
            }
            logoTransform.localScale = Vector3.one;
        }

        // Botones aparecen con stagger
        Button[] buttons = new Button[] { playButton, optionsButton, creditsButton, quitButton };
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                CanvasGroup buttonCG = buttons[i].GetComponent<CanvasGroup>();
                if (buttonCG == null) buttonCG = buttons[i].gameObject.AddComponent<CanvasGroup>();

                buttonCG.alpha = 0f;
                StartCoroutine(FadeInButton(buttonCG, buttonStaggerDelay * i));
            }
        }

        yield return new WaitForSecondsRealtime(buttonStaggerDelay * buttons.Length + 0.3f);

        isTransitioning = false;
    }

    private IEnumerator FadeInButton(CanvasGroup buttonCG, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            buttonCG.alpha = elapsed / duration;
            yield return null;
        }
        buttonCG.alpha = 1f;
    }

    private IEnumerator TransitionToPanel(CanvasGroup targetPanel)
    {
        isTransitioning = true;

        // Fade out panel actual
        yield return StartCoroutine(FadePanelCoroutine(GetCurrentPanel(), 0f, menuFadeDuration));
        HidePanelInstant(GetCurrentPanel());

        // Fade in nuevo panel
        ShowPanelInstant(targetPanel);
        targetPanel.alpha = 0f;
        yield return StartCoroutine(FadePanelCoroutine(targetPanel, 1f, menuFadeDuration));

        isTransitioning = false;
    }

    private IEnumerator TransitionBackToMain(CanvasGroup currentPanel)
    {
        isTransitioning = true;

        // Fade out panel actual
        yield return StartCoroutine(FadePanelCoroutine(currentPanel, 0f, menuFadeDuration));
        HidePanelInstant(currentPanel);

        // Fade in main
        ShowPanelInstant(mainPanel);
        mainPanel.alpha = 0f;
        yield return StartCoroutine(FadePanelCoroutine(mainPanel, 1f, menuFadeDuration));

        isTransitioning = false;
    }

    private IEnumerator FadePanelCoroutine(CanvasGroup panel, float targetAlpha, float duration)
    {
        if (panel == null) yield break;

        float startAlpha = panel.alpha;
        float elapsed = 0f;

        // Activar/desactivar interactabilidad segun el target
        if (targetAlpha > 0.5f)
        {
            panel.interactable = true;
            panel.blocksRaycasts = true;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            panel.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        panel.alpha = targetAlpha;

        if (targetAlpha <= 0.5f)
        {
            panel.interactable = false;
            panel.blocksRaycasts = false;
        }
    }

    private IEnumerator ScrollCredits()
    {
        if (creditsText == null) yield break;

        // Resetear posicion
        creditsText.rectTransform.anchoredPosition = new Vector2(0f, -300f);

        // Esperar un momento antes de empezar el scroll
        yield return new WaitForSecondsRealtime(1f);

        float scrollSpeed = 40f;
        float resetDelay = 2f;

        while (currentState == MenuState.Credits)
        {
            // Mover hacia arriba
            Vector2 pos = creditsText.rectTransform.anchoredPosition;
            pos.y += scrollSpeed * Time.unscaledDeltaTime;
            creditsText.rectTransform.anchoredPosition = pos;

            // Si llego al final, resetear
            if (pos.y > creditsText.preferredHeight + 300f)
            {
                yield return new WaitForSecondsRealtime(resetDelay);
                if (currentState != MenuState.Credits) yield break;
                creditsText.rectTransform.anchoredPosition = new Vector2(0f, -300f);
            }

            yield return null;
        }
    }
}
