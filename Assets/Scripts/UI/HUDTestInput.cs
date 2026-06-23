using UnityEngine;

/// <summary>
/// HUDTestInput - Script temporal para probar el HUD y los sistemas de Vida/Estres.
/// 
/// Controles de prueba (solo en modo Play):
/// - Q: Aplicar 10 de daño
/// - W: Curar 15 de vida
/// - E: Matar instantaneamente
/// - A: Añadir 15 de estres
/// - S: Reducir 10 de estres
/// - D: Simular fallo de puzzle (+25 estres)
/// - F: Limpiar todo el estres
/// 
/// NOTA: Elimina este script antes de la build final. Es solo para testing.
/// </summary>
public class HUDTestInput : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStress playerStress;

    [Header("Configuracion")]
    [SerializeField] private bool enableTestInput = true;

    void Update()
    {
        if (!enableTestInput) return;

        ValidateReferences();

        if (playerHealth == null || playerStress == null) return;

        // --- TEST DE VIDA ---

        // Q: Aplicar daño (10)
        if (Input.GetKeyDown(KeyCode.Q))
        {
            playerHealth.TakeDamage(10);
            Debug.Log("[HUDTest] Daño aplicado: 10");
        }

        // W: Curar (15)
        if (Input.GetKeyDown(KeyCode.W))
        {
            playerHealth.Heal(15);
            Debug.Log("[HUDTest] Curacion aplicada: 15");
        }

        // E: Muerte instantanea
        if (Input.GetKeyDown(KeyCode.E))
        {
            playerHealth.Kill();
            Debug.Log("[HUDTest] Muerte forzada");
        }

        // --- TEST DE ESTRES ---

        // A: Añadir estres (15)
        if (Input.GetKeyDown(KeyCode.A))
        {
            playerStress.AddStress(15f);
            Debug.Log("[HUDTest] Estres añadido: 15");
        }

        // S: Reducir estres (10)
        if (Input.GetKeyDown(KeyCode.S))
        {
            playerStress.ReduceStress(10f);
            Debug.Log("[HUDTest] Estres reducido: 10");
        }

        // D: Simular fallo de puzzle (+25 estres)
        if (Input.GetKeyDown(KeyCode.D))
        {
            playerStress.OnPuzzleFailed(25f);
            Debug.Log("[HUDTest] Puzzle fallado (+25 estres)");
        }

        // F: Limpiar estres
        if (Input.GetKeyDown(KeyCode.F))
        {
            playerStress.ClearStress();
            Debug.Log("[HUDTest] Estres limpiado");
        }

        // R: Full heal + clear stress
        if (Input.GetKeyDown(KeyCode.R))
        {
            playerHealth.FullHeal();
            playerStress.ClearStress();
            Debug.Log("[HUDTest] Full restore");
        }
    }

    private void ValidateReferences()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerStress == null)
            playerStress = FindFirstObjectByType<PlayerStress>();
    }

    private void OnGUI()
    {
        if (!enableTestInput) return;

        // Dibujar un panel de ayuda en pantalla con los controles de test
        GUILayout.BeginArea(new Rect(10, 10, 250, 280), "HUD Test Controls", "box");
        GUILayout.Label("=== VIDA ===");
        GUILayout.Label("Q: Dañar (-10 HP)");
        GUILayout.Label("W: Curar (+15 HP)");
        GUILayout.Label("E: Matar");
        GUILayout.Label("");
        GUILayout.Label("=== ESTRES ===");
        GUILayout.Label("A: +15 Stress");
        GUILayout.Label("S: -10 Stress");
        GUILayout.Label("D: Fallar Puzzle (+25)");
        GUILayout.Label("F: Limpiar Stress");
        GUILayout.Label("");
        GUILayout.Label("=== GENERAL ===");
        GUILayout.Label("R: Full Restore");
        GUILayout.EndArea();
    }
}
