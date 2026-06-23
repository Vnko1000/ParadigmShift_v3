using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// HUDManager - Gestiona toda la interfaz de usuario en pantalla.
/// 
/// Conecta los sistemas de PlayerHealth y PlayerStress con los elementos visuales de UI.
/// Maneja:
/// - Barra de Vida (actualizacion en tiempo real)
/// - Barra de Estres (con fade in/out dinamico)
/// - Iconos de estado
/// - Animaciones y efectos visuales del HUD
/// 
/// Requiere: Canvas con el prefab de HUD asignado.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("Referencias a Sistemas del Jugador")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStress playerStress;

    [Header("Elementos de UI - Barra de Vida")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image healthBarFrame;
    [SerializeField] private Image healthIcon;
    [SerializeField] private TMPro.TextMeshProUGUI healthText;

    [Header("Elementos de UI - Barra de Estres")]
    [SerializeField] private Image stressBarFill;
    [SerializeField] private Image stressBarFrame;
    [SerializeField] private Image stressIcon;
    [SerializeField] private TMPro.TextMeshProUGUI stressText;
    [SerializeField] private CanvasGroup stressContainerCanvasGroup;

    [Header("Configuracion de Estilo")]
    [Tooltip("Color de la barra de vida cuando esta alta (>60%)")]
    [SerializeField] private Color healthColorHigh = new Color(0.2f, 0.8f, 1f);
    [Tooltip("Color de la barra de vida cuando esta media (30-60%)")]
    [SerializeField] private Color healthColorMedium = new Color(1f, 0.8f, 0.2f);
    [Tooltip("Color de la barra de vida cuando esta baja (<30%)")]
    [SerializeField] private Color healthColorLow = new Color(1f, 0.2f, 0.2f);

    [Tooltip("Color de la barra de estres normal")]
    [SerializeField] private Color stressColorNormal = new Color(1f, 0.7f, 0.2f);
    [Tooltip("Color de la barra de estres en advertencia")]
    [SerializeField] private Color stressColorWarning = new Color(1f, 0.5f, 0.1f);
    [Tooltip("Color de la barra de estres en critico")]
    [SerializeField] private Color stressColorCritical = new Color(1f, 0.1f, 0.1f);
    [Tooltip("Color de la barra durante burnout")]
    [SerializeField] private Color stressColorBurnout = new Color(0.5f, 0f, 0f);

    [Header("Animacion - Barra de Estres (Fade In/Out)")]
    [Tooltip("Segundos que tarda la barra de estres en aparecer")]
    [SerializeField] private float stressFadeInDuration = 0.3f;
    [Tooltip("Segundos que tarda la barra de estres en desaparecer")]
    [SerializeField] private float stressFadeOutDuration = 0.8f;
    [Tooltip("Segundos de inactividad antes de que la barra de estres desaparezca")]
    [SerializeField] private float stressHideDelay = 3f;

    [Header("Animacion - Hit Effects")]
    [Tooltip("Duracion del flash rojo al recibir daño")]
    [SerializeField] private float damageFlashDuration = 0.15f;
    [Tooltip("Color del flash de daño")]
    [SerializeField] private Color damageFlashColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private Image damageFlashOverlay;

    [Header("Animacion - Pulse")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseScale = 1.05f;

    // --- Estado Interno ---
    private float stressHideTimer = 0f;
    private bool isStressVisible = false;
    private Coroutine stressFadeCoroutine;
    private Coroutine damageFlashCoroutine;
    private Vector3 healthBarDefaultScale;
    private Vector3 stressBarDefaultScale;

    void Start()
    {
        ValidateReferences();
        SubscribeToEvents();
        InitializeUI();

        // Guardar escalas por defecto para animaciones
        if (healthBarFrame != null)
            healthBarDefaultScale = healthBarFrame.transform.localScale;
        if (stressBarFrame != null)
            stressBarDefaultScale = stressBarFrame.transform.localScale;
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    void Update()
    {
        HandleStressVisibilityTimer();
        AnimateBars();
    }

    // ============ INICIALIZACION ============

    private void ValidateReferences()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerStress == null)
            playerStress = FindFirstObjectByType<PlayerStress>();

        if (playerHealth == null)
            Debug.LogError("[HUDManager] No se encontro PlayerHealth en la escena.");
        if (playerStress == null)
            Debug.LogError("[HUDManager] No se encontro PlayerStress en la escena.");
    }

    private void SubscribeToEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(UpdateHealthBar);
            playerHealth.OnDamageTaken.AddListener(OnPlayerDamaged);
        }

        if (playerStress != null)
        {
            playerStress.OnStressChanged.AddListener(UpdateStressBar);
            playerStress.OnStressGained.AddListener(OnStressGained);
            playerStress.OnWarningThreshold.AddListener(OnStressWarning);
            playerStress.OnBurnout.AddListener(OnBurnout);
            playerStress.OnBurnoutRecovered.AddListener(OnBurnoutRecovered);
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
            playerHealth.OnDamageTaken.RemoveListener(OnPlayerDamaged);
        }

        if (playerStress != null)
        {
            playerStress.OnStressChanged.RemoveListener(UpdateStressBar);
            playerStress.OnStressGained.RemoveListener(OnStressGained);
            playerStress.OnWarningThreshold.RemoveListener(OnStressWarning);
            playerStress.OnBurnout.RemoveListener(OnBurnout);
            playerStress.OnBurnoutRecovered.RemoveListener(OnBurnoutRecovered);
        }
    }

    private void InitializeUI()
    {
        // Inicializar vida
        if (playerHealth != null)
            UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);

        // Inicializar estres (oculto al inicio)
        if (playerStress != null)
        {
            UpdateStressBar(playerStress.CurrentStress, playerStress.MaxStress);
            if (stressContainerCanvasGroup != null)
            {
                stressContainerCanvasGroup.alpha = 0f;
                isStressVisible = false;
            }
        }

        // Ocultar overlay de daño
        if (damageFlashOverlay != null)
            damageFlashOverlay.color = Color.clear;
    }

    // ============ ACTUALIZACION DE BARRAS ============

    /// <summary>
    /// Actualiza la barra de vida visualmente.
    /// </summary>
    public void UpdateHealthBar(int current, int max)
    {
        float percent = max > 0 ? (float)current / max : 0f;

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = percent;

            // Cambiar color segun el porcentaje
            if (percent > 0.6f)
                healthBarFill.color = healthColorHigh;
            else if (percent > 0.3f)
                healthBarFill.color = healthColorMedium;
            else
                healthBarFill.color = healthColorLow;
        }

        if (healthText != null)
            healthText.text = $"{current}/{max}";

        // Si la vida es baja, pulsar la barra
        if (percent <= 0.3f && healthBarFrame != null)
        {
            // El pulso se maneja en Update
        }
    }

    /// <summary>
    /// Actualiza la barra de estres visualmente.
    /// </summary>
    public void UpdateStressBar(float current, float max)
    {
        float percent = max > 0 ? current / max : 0f;

        if (stressBarFill != null)
        {
            stressBarFill.fillAmount = percent;

            // Color segun estado
            if (playerStress != null && playerStress.IsBurnedOut)
                stressBarFill.color = stressColorBurnout;
            else if (playerStress != null && playerStress.IsCritical)
                stressBarFill.color = stressColorCritical;
            else if (playerStress != null && playerStress.IsWarning)
                stressBarFill.color = stressColorWarning;
            else
                stressBarFill.color = stressColorNormal;
        }

        if (stressText != null)
            stressText.text = $"{Mathf.RoundToInt(current)}/{Mathf.RoundToInt(max)}";
    }

    // ============ EVENT HANDLERS ============

    private void OnPlayerDamaged(int damage)
    {
        // Flash rojo de daño
        if (damageFlashCoroutine != null)
            StopCoroutine(damageFlashCoroutine);
        damageFlashCoroutine = StartCoroutine(DamageFlashCoroutine());
    }

    private void OnStressGained(float amount)
    {
        // Mostrar barra de estres cuando gana estres
        ShowStressBar();
    }

    private void OnStressWarning(bool isCritical)
    {
        // El color ya se actualiza en UpdateStressBar
        // Aqui podrias agregar efectos adicionales (sonido, shake, etc.)
        ShowStressBar();
    }

    private void OnBurnout()
    {
        // Mostrar barra permanentemente durante burnout
        ShowStressBar();

        // Shake del HUD
        StartCoroutine(HUDShakeCoroutine(0.5f, 5f));
    }

    private void OnBurnoutRecovered()
    {
        // La barra se ocultara por el timer natural
    }

    // ============ VISIBILIDAD DE BARRA DE ESTRES ============

    private void ShowStressBar()
    {
        stressHideTimer = stressHideDelay;

        if (!isStressVisible)
        {
            isStressVisible = true;
            if (stressFadeCoroutine != null)
                StopCoroutine(stressFadeCoroutine);
            stressFadeCoroutine = StartCoroutine(FadeStressBar(1f, stressFadeInDuration));
        }
    }

    private void HideStressBar()
    {
        if (isStressVisible && !playerStress.IsBurnedOut)
        {
            isStressVisible = false;
            if (stressFadeCoroutine != null)
                StopCoroutine(stressFadeCoroutine);
            stressFadeCoroutine = StartCoroutine(FadeStressBar(0f, stressFadeOutDuration));
        }
    }

    private void HandleStressVisibilityTimer()
    {
        if (!isStressVisible || playerStress == null) return;

        // No ocultar si hay estres significativo o esta en burnout
        if (playerStress.IsBurnedOut || playerStress.StressPercent > 0.1f)
        {
            stressHideTimer = stressHideDelay;
            return;
        }

        stressHideTimer -= Time.deltaTime;
        if (stressHideTimer <= 0f)
        {
            HideStressBar();
        }
    }

    // ============ CORUTINAS DE ANIMACION ============

    private IEnumerator FadeStressBar(float targetAlpha, float duration)
    {
        if (stressContainerCanvasGroup == null) yield break;

        float startAlpha = stressContainerCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            stressContainerCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        stressContainerCanvasGroup.alpha = targetAlpha;
    }

    private IEnumerator DamageFlashCoroutine()
    {
        if (damageFlashOverlay == null) yield break;

        damageFlashOverlay.color = damageFlashColor;
        float elapsed = 0f;

        while (elapsed < damageFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / damageFlashDuration;
            damageFlashOverlay.color = Color.Lerp(damageFlashColor, Color.clear, t);
            yield return null;
        }

        damageFlashOverlay.color = Color.clear;
    }

    private IEnumerator HUDShakeCoroutine(float duration, float intensity)
    {
        Vector3 originalPosition = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            transform.localPosition = originalPosition + new Vector3(x, y, 0f);
            yield return null;
        }

        transform.localPosition = originalPosition;
    }

    // ============ ANIMACIONES CONTINUAS ============

    private void AnimateBars()
    {
        // Pulsar barra de vida cuando esta baja
        if (playerHealth != null && playerHealth.HealthPercent <= 0.3f && healthBarFrame != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed * 2f) * 0.03f;
            healthBarFrame.transform.localScale = healthBarDefaultScale * pulse;
        }
        else if (healthBarFrame != null && healthBarDefaultScale != Vector3.zero)
        {
            healthBarFrame.transform.localScale = healthBarDefaultScale;
        }

        // Pulsar barra de estres cuando esta en critico
        if (playerStress != null && playerStress.IsCritical && stressBarFrame != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed * 3f) * 0.04f;
            stressBarFrame.transform.localScale = stressBarDefaultScale * pulse;
        }
        else if (stressBarFrame != null && stressBarDefaultScale != Vector3.zero)
        {
            stressBarFrame.transform.localScale = stressBarDefaultScale;
        }
    }

    // ============ METODOS PUBLICOS (para testing/debug) ============

    /// <summary>
    /// Fuerza la actualizacion de ambas barras.
    /// </summary>
    [ContextMenu("Force Refresh UI")]
    public void ForceRefresh()
    {
        if (playerHealth != null)
            UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        if (playerStress != null)
            UpdateStressBar(playerStress.CurrentStress, playerStress.MaxStress);
    }
}
