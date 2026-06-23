using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de Vida del Jugador.
/// Gestiona HP actual/máximo, recibir daño, curarse, invencibilidad post-golpe y muerte.
/// Lanza eventos para que el UIManager u otros sistemas escuchen los cambios.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Configuracion de Vida")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Invencibilidad Post-Golpe")]
    [Tooltip("Segundos de invencibilidad tras recibir daño")]
    [SerializeField] private float invincibilityDuration = 1.0f;
    [Tooltip("Intervalo de parpadeo del sprite durante invencibilidad")]
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("Referencia Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    // --- Estado Interno ---
    private bool isInvincible = false;
    private float invincibilityTimer = 0f;
    private float blinkTimer = 0f;
    private bool isBlinkVisible = true;
    private bool isDead = false;

    // --- Eventos (para que el UI y otros sistemas escuchen) ---
    /// <summary>Se lanza cuando la vida cambia. Parametros: (vidaActual, vidaMaxima)</summary>
    public UnityEvent<int, int> OnHealthChanged;

    /// <summary>Se lanza cuando el jugador recibe daño. Parametro: cantidadDeDaño</summary>
    public UnityEvent<int> OnDamageTaken;

    /// <summary>Se lanza cuando el jugador se cura. Parametro: cantidadDeCuracion</summary>
    public UnityEvent<int> OnHealed;

    /// <summary>Se lanza cuando el jugador muere (vida llega a 0)</summary>
    public UnityEvent OnDeath;

    /// <summary>Se lanza cuando inicia/termina la invencibilidad. Parametro: (estaInvencible)</summary>
    public UnityEvent<bool> OnInvincibilityChanged;

    // --- Propiedades públicas de solo lectura ---
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsInvincible => isInvincible;
    public bool IsDead => isDead;
    public float HealthPercent => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

    void Awake()
    {
        // Inicializar vida al máximo
        currentHealth = maxHealth;
        isDead = false;

        // Auto-referenciar spriteRenderer si no está asignado
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        // Notificar el estado inicial al UI
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        HandleInvincibilityTimer();
    }

    // ============ METODOS PUBLICOS ============

    /// <summary>
    /// Aplica daño al jugador. Si está invencible o muerto, ignora el daño.
    /// </summary>
    /// <param name="damage">Cantidad de daño a aplicar</param>
    public void TakeDamage(int damage)
    {
        if (isDead || isInvincible || damage <= 0) return;

        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageTaken?.Invoke(damage);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartInvincibility();
        }
    }

    /// <summary>
    /// Cura al jugador. No puede exceder la vida máxima.
    /// </summary>
    /// <param name="amount">Cantidad de vida a recuperar</param>
    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        int healedAmount = currentHealth - previousHealth;

        if (healedAmount > 0)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnHealed?.Invoke(healedAmount);
        }
    }

    /// <summary>
    /// Cura al jugador al máximo.
    /// </summary>
    public void FullHeal()
    {
        if (isDead) return;
        int previousHealth = currentHealth;
        currentHealth = maxHealth;
        int healedAmount = currentHealth - previousHealth;

        if (healedAmount > 0)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnHealed?.Invoke(healedAmount);
        }
    }

    /// <summary>
    /// Mata instantáneamente al jugador (para trampas, caídas al vacío, etc.)
    /// </summary>
    public void Kill()
    {
        if (isDead) return;
        currentHealth = 0;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Die();
    }

    /// <summary>
    /// Fuerza el inicio del período de invencibilidad (útil para power-ups o transiciones)
    /// </summary>
    public void ForceInvincibility(float duration)
    {
        invincibilityDuration = duration;
        StartInvincibility();
    }

    // ============ METODOS PRIVADOS ============

    private void Die()
    {
        isDead = true;
        currentHealth = 0;

        // Asegurar que el sprite sea visible al morir (no parpadeando)
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        OnDeath?.Invoke();

        Debug.Log("[PlayerHealth] El jugador ha muerto.");
        // TODO: Aquí puedes agregar lógica de Game Over, respawn, etc.
    }

    private void StartInvincibility()
    {
        isInvincible = true;
        invincibilityTimer = invincibilityDuration;
        blinkTimer = 0f;
        isBlinkVisible = true;
        OnInvincibilityChanged?.Invoke(true);
    }

    private void HandleInvincibilityTimer()
    {
        if (!isInvincible) return;

        // Contador de invencibilidad
        invincibilityTimer -= Time.deltaTime;

        // Parpadeo del sprite
        blinkTimer -= Time.deltaTime;
        if (blinkTimer <= 0f)
        {
            blinkTimer = blinkInterval;
            isBlinkVisible = !isBlinkVisible;

            if (spriteRenderer != null)
                spriteRenderer.enabled = isBlinkVisible;
        }

        // Fin de invencibilidad
        if (invincibilityTimer <= 0f)
        {
            isInvincible = false;
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
            OnInvincibilityChanged?.Invoke(false);
        }
    }

    // ============ GIZMOS ============

    private void OnDrawGizmosSelected()
    {
        // Visualizar el estado de vida en el editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
