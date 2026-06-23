using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema de Estres/Burnout del Jugador.
/// Gestiona la barra de estres que aumenta con errores, daño y puzzles fallidos.
/// Si llega al maximo, el jugador sufre un colapso nervioso (Burnout) que:
/// - Fuerza el fallo de la mision actual
/// - Aplica una factura medica (costo en creditos)
/// - Reinicia el nivel de estres
/// 
/// El estres puede reducirse con consumibles anti-estres o descansando.
/// </summary>
public class PlayerStress : MonoBehaviour
{
    [Header("Configuracion de Estres")]
    [SerializeField] private float maxStress = 100f;
    [SerializeField] private float currentStress = 0f;

    [Header("Umbrales de Estres")]
    [Tooltip("Porcentaje donde empieza la advertencia visual (0-1)")]
    [SerializeField] private float warningThreshold = 0.6f;
    [Tooltip("Porcentaje donde la advertencia se vuelve critica (0-1)")]
    [SerializeField] private float criticalThreshold = 0.8f;

    [Header("Configuracion de Burnout")]
    [Tooltip("Costo en creditos al sufrir un burnout")]
    [SerializeField] private int burnoutMedicalCost = 500;
    [Tooltip("Segundos de penalizacion al sufrir burnout")]
    [SerializeField] private float burnoutPenaltyDuration = 3f;

    [Header("Regeneracion Natural")]
    [Tooltip("Estres que se reduce por segundo cuando el jugador esta tranquilo")]
    [SerializeField] private float naturalDecayRate = 0.5f;
    [Tooltip("Segundos sin recibir estres para que empiece la regeneracion natural")]
    [SerializeField] private float calmCooldown = 5f;

    // --- Estado Interno ---
    private bool isBurnedOut = false;
    private bool isCalm = true;
    private float calmTimer = 0f;
    private float burnoutTimer = 0f;

    // --- Eventos ---
    /// <summary>Se lanza cuando el estres cambia. Parametros: (estresActual, estresMaximo)</summary>
    public UnityEvent<float, float> OnStressChanged;

    /// <summary>Se lanza cuando el estres aumenta. Parametro: cantidad</summary>
    public UnityEvent<float> OnStressGained;

    /// <summary>Se lanza cuando el estres disminuye. Parametro: cantidad</summary>
    public UnityEvent<float> OnStressReduced;

    /// <summary>Se lanza cuando se alcanza el umbral de advertencia. Parametro: (esCritico)</summary>
    public UnityEvent<bool> OnWarningThreshold;

    /// <summary>Se lanza al sufrir un Burnout completo</summary>
    public UnityEvent OnBurnout;

    /// <summary>Se lanza cuando termina la penalizacion de burnout</summary>
    public UnityEvent OnBurnoutRecovered;

    /// <summary>Se lanza cuando cambia el estado de calma. Parametro: (estaCalmo)</summary>
    public UnityEvent<bool> OnCalmStateChanged;

    // --- Propiedades publicas de solo lectura ---
    public float MaxStress => maxStress;
    public float CurrentStress => currentStress;
    public float StressPercent => maxStress > 0 ? currentStress / maxStress : 0f;
    public bool IsBurnedOut => isBurnedOut;
    public bool IsCalm => isCalm;
    public bool IsWarning => !isBurnedOut && StressPercent >= warningThreshold && StressPercent < criticalThreshold;
    public bool IsCritical => !isBurnedOut && StressPercent >= criticalThreshold;
    public int BurnoutMedicalCost => burnoutMedicalCost;

    void Awake()
    {
        currentStress = 0f;
        isBurnedOut = false;
        isCalm = true;
        calmTimer = 0f;
    }

    void Start()
    {
        OnStressChanged?.Invoke(currentStress, maxStress);
    }

    void Update()
    {
        if (isBurnedOut)
        {
            HandleBurnoutTimer();
            return;
        }

        HandleCalmTimer();
        ApplyNaturalDecay();
    }

    // ============ METODOS PUBLICOS ============

    /// <summary>
    /// Añade estres al jugador. Si llega al maximo, activa el Burnout.
    /// </summary>
    /// <param name="amount">Cantidad de estres a añadir</param>
    public void AddStress(float amount)
    {
        if (isBurnedOut || amount <= 0) return;

        float previousStress = currentStress;
        currentStress = Mathf.Clamp(currentStress + amount, 0, maxStress);
        float addedAmount = currentStress - previousStress;

        if (addedAmount > 0)
        {
            // Resetear el timer de calma ya que recibio estres
            isCalm = false;
            calmTimer = 0f;
            OnCalmStateChanged?.Invoke(false);

            OnStressChanged?.Invoke(currentStress, maxStress);
            OnStressGained?.Invoke(addedAmount);

            CheckThresholds(previousStress);

            // Verificar burnout
            if (currentStress >= maxStress)
            {
                TriggerBurnout();
            }
        }
    }

    /// <summary>
    /// Reduce el estres (usar consumibles anti-estres, descansar, etc.)
    /// </summary>
    /// <param name="amount">Cantidad de estres a reducir</param>
    public void ReduceStress(float amount)
    {
        if (isBurnedOut || amount <= 0 || currentStress <= 0) return;

        float previousStress = currentStress;
        currentStress = Mathf.Clamp(currentStress - amount, 0, maxStress);
        float reducedAmount = previousStress - currentStress;

        if (reducedAmount > 0)
        {
            OnStressChanged?.Invoke(currentStress, maxStress);
            OnStressReduced?.Invoke(reducedAmount);
        }
    }

    /// <summary>
    /// Reduce todo el estres (full restore)
    /// </summary>
    public void ClearStress()
    {
        if (isBurnedOut || currentStress <= 0) return;

        float previousStress = currentStress;
        currentStress = 0f;

        OnStressChanged?.Invoke(currentStress, maxStress);
        OnStressReduced?.Invoke(previousStress);
        OnWarningThreshold?.Invoke(false); // Salir del estado de advertencia
    }

    /// <summary>
    /// Añade estres por recibir daño (llamar desde PlayerHealth.OnDamageTaken)
    /// </summary>
    /// <param name="damageTaken">Cantidad de daño recibido</param>
    public void OnPlayerTookDamage(int damageTaken)
    {
        // El estres aumenta proporcional al daño recibido
        float stressFromDamage = damageTaken * 0.8f;
        AddStress(stressFromDamage);
    }

    /// <summary>
    /// Añade estres por fallar un puzzle
    /// </summary>
    public void OnPuzzleFailed(float stressPenalty = 15f)
    {
        AddStress(stressPenalty);
    }

    /// <summary>
    /// Añade estres por fallar una entrega
    /// </summary>
    public void OnDeliveryFailed(float stressPenalty = 25f)
    {
        AddStress(stressPenalty);
    }

    /// <summary>
    /// Añade una pequeña cantidad de estres por obstaculos menores
    /// </summary>
    public void OnMinorFrustration(float stressAmount = 5f)
    {
        AddStress(stressAmount);
    }

    // ============ METODOS PRIVADOS ============

    private void TriggerBurnout()
    {
        isBurnedOut = true;
        burnoutTimer = burnoutPenaltyDuration;
        currentStress = maxStress;

        OnBurnout?.Invoke();

        Debug.Log($"[PlayerStress] BURNOUT! El jugador ha colapsado. Factura medica: {burnoutMedicalCost} creditos.");
        // TODO: Integrar con sistema de dinero para restar los creditos
        // TODO: Forzar fallo de mision actual
    }

    private void HandleBurnoutTimer()
    {
        burnoutTimer -= Time.deltaTime;
        if (burnoutTimer <= 0f)
        {
            RecoverFromBurnout();
        }
    }

    private void RecoverFromBurnout()
    {
        isBurnedOut = false;
        currentStress = 0f;
        isCalm = true;
        calmTimer = calmCooldown;

        OnStressChanged?.Invoke(currentStress, maxStress);
        OnBurnoutRecovered?.Invoke();
        OnWarningThreshold?.Invoke(false);
        OnCalmStateChanged?.Invoke(true);

        Debug.Log("[PlayerStress] El jugador se ha recuperado del burnout.");
    }

    private void HandleCalmTimer()
    {
        if (isCalm) return;

        calmTimer += Time.deltaTime;
        if (calmTimer >= calmCooldown)
        {
            isCalm = true;
            OnCalmStateChanged?.Invoke(true);
        }
    }

    private void ApplyNaturalDecay()
    {
        if (!isCalm || currentStress <= 0 || isBurnedOut) return;

        float decay = naturalDecayRate * Time.deltaTime;
        float previousStress = currentStress;
        currentStress = Mathf.Clamp(currentStress - decay, 0, maxStress);

        if (currentStress != previousStress)
        {
            OnStressChanged?.Invoke(currentStress, maxStress);

            // Verificar si salio del umbral de advertencia
            if (StressPercent < warningThreshold)
            {
                OnWarningThreshold?.Invoke(false);
            }
        }
    }

    private void CheckThresholds(float previousStressPercent)
    {
        float currentPercent = StressPercent;

        // Verificar umbrales
        if (currentPercent >= criticalThreshold && previousStressPercent < criticalThreshold)
        {
            OnWarningThreshold?.Invoke(true); // Critico
            Debug.Log("[PlayerStress] Nivel de estres CRITICO!");
        }
        else if (currentPercent >= warningThreshold && previousStressPercent < warningThreshold)
        {
            OnWarningThreshold?.Invoke(false); // Advertencia normal
            Debug.Log("[PlayerStress] Nivel de estres elevado.");
        }
    }

    // ============ GIZMOS ============

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
