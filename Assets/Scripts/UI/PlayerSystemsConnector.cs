using UnityEngine;

/// <summary>
/// PlayerSystemsConnector - Conecta todos los sistemas del jugador.
/// 
/// Este script actua como puente entre PlayerHealth y PlayerStress.
/// Cuando el jugador recibe daño, automaticamente aumenta el estrés.
/// 
/// Coloca este script en el mismo GameObject que PlayerHealth y PlayerStress,
/// o en un GameObject separado que tenga referencias a ambos.
/// </summary>
public class PlayerSystemsConnector : MonoBehaviour
{
    [Header("Referencias a Sistemas")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStress playerStress;

    [Header("Configuracion de Conexion")]
    [Tooltip("Multiplicador de estres por punto de daño recibido")]
    [SerializeField] private float stressPerDamagePoint = 0.8f;
    [Tooltip("Estres adicional base al recibir cualquier golpe")]
    [SerializeField] private float baseStressOnHit = 2f;

    void Awake()
    {
        ValidateReferences();
    }

    void OnEnable()
    {
        SubscribeToEvents();
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void ValidateReferences()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
        if (playerStress == null)
            playerStress = GetComponent<PlayerStress>();

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerStress == null)
            playerStress = FindFirstObjectByType<PlayerStress>();
    }

    private void SubscribeToEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamageTaken.AddListener(HandleDamageTaken);
            playerHealth.OnDeath.AddListener(HandlePlayerDeath);
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamageTaken.RemoveListener(HandleDamageTaken);
            playerHealth.OnDeath.RemoveListener(HandlePlayerDeath);
        }
    }

    /// <summary>
    /// Cuando el jugador recibe daño, convertirlo en estres.
    /// </summary>
    private void HandleDamageTaken(int damage)
    {
        if (playerStress != null)
        {
            float stressAmount = baseStressOnHit + (damage * stressPerDamagePoint);
            playerStress.AddStress(stressAmount);
        }
    }

    /// <summary>
    /// Cuando el jugador muere, aplicar un golpe grande de estres.
    /// </summary>
    private void HandlePlayerDeath()
    {
        if (playerStress != null)
        {
            // La muerte causa un estres inmediato (pero no burnout directo)
            playerStress.AddStress(30f);
        }
    }
}
