using UnityEngine;

/// <summary>
/// Cámara 2D estilo Hollow Knight:
/// - Dead zones horizontales y verticales (la cámara no se mueve si el target está dentro de la zona)
/// - Damping (suavizado) configurable por eje
/// - Look-ahead horizontal (la cámara se adelanta en la dirección de movimiento)
/// - Snap vertical al aterrizar
/// - Límites de nivel opcionales (camera bounds)
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Dead Zone")]
    [Tooltip("Qué tan lejos puede moverse el target horizontalmente antes de que la cámara lo siga")]
    [SerializeField] private float deadZoneX = 1.5f;
    [Tooltip("Qué tan lejos puede moverse el target verticalmente antes de que la cámara lo siga")]
    [SerializeField] private float deadZoneY = 1.0f;

    [Header("Damping")]
    [Tooltip("Suavizado horizontal. Más alto = más lento. 0 = instantáneo")]
    [SerializeField] private float dampingX = 0.15f;
    [Tooltip("Suavizado vertical. Más alto = más lento")]
    [SerializeField] private float dampingY = 0.1f;
    [Tooltip("Damping extra al bajar (caída más dramática)")]
    [SerializeField] private float dampingYFall = 0.05f;

    [Header("Look-Ahead")]
    [Tooltip("Cuánto se adelanta la cámara en la dirección de movimiento")]
    [SerializeField] private float lookAheadDistance = 2.5f;
    [Tooltip("Velocidad a la que se desplaza el look-ahead")]
    [SerializeField] private float lookAheadSpeed = 3f;

    [Header("Camera Bounds (opcional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private float boundsMinX = -50f;
    [SerializeField] private float boundsMaxX = 50f;
    [SerializeField] private float boundsMinY = -20f;
    [SerializeField] private float boundsMaxY = 20f;

    // Estado interno
    private Vector3 currentVelocity;      // usado por SmoothDamp
    private float lookAheadTarget;        // destino del look-ahead
    private float currentLookAhead;       // valor actual suavizado

    private float targetX;                // posición X objetivo de la cámara
    private float targetY;                // posición Y objetivo de la cámara

    private float lastTargetX;
    private Camera cam;
    private float halfWidth;
    private float halfHeight;

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("[CameraController] No hay target asignado.");
            return;
        }

        cam = GetComponent<Camera>();
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * cam.aspect;

        // Inicializar en la posición del target sin suavizado
        targetX = target.position.x;
        targetY = target.position.y;
        lastTargetX = targetX;
        transform.position = new Vector3(targetX, targetY, transform.position.z);
    }

    void LateUpdate()
    {
        if (target == null) return;

        UpdateLookAhead();
        UpdateTargetPosition();
        ApplyBounds();
        ApplyPosition();
    }

    void UpdateLookAhead()
    {
        float moveDeltaX = target.position.x - lastTargetX;

        if (Mathf.Abs(moveDeltaX) > 0.01f)
            lookAheadTarget = Mathf.Sign(moveDeltaX) * lookAheadDistance;

        currentLookAhead = Mathf.Lerp(currentLookAhead, lookAheadTarget, lookAheadSpeed * Time.deltaTime);
        lastTargetX = target.position.x;
    }

    void UpdateTargetPosition()
    {
        float desiredX = target.position.x + currentLookAhead;
        float desiredY = target.position.y;

        // --- Dead zone horizontal ---
        float diffX = desiredX - targetX;
        if (Mathf.Abs(diffX) > deadZoneX)
            targetX = desiredX - Mathf.Sign(diffX) * deadZoneX;

        // --- Dead zone vertical ---
        float diffY = desiredY - targetY;
        if (Mathf.Abs(diffY) > deadZoneY)
            targetY = desiredY - Mathf.Sign(diffY) * deadZoneY;

        // Damping: usar dampingYFall si el target está cayendo
        bool falling = (target.position.y < transform.position.y - deadZoneY);
        float smoothY = falling ? dampingYFall : dampingY;

        float newX = Mathf.SmoothDamp(transform.position.x, targetX, ref currentVelocity.x, dampingX);
        float newY = Mathf.SmoothDamp(transform.position.y, targetY, ref currentVelocity.y, smoothY);

        transform.position = new Vector3(newX, newY, transform.position.z);
    }

    void ApplyBounds()
    {
        if (!useBounds) return;

        float clampedX = Mathf.Clamp(transform.position.x, boundsMinX + halfWidth, boundsMaxX - halfWidth);
        float clampedY = Mathf.Clamp(transform.position.y, boundsMinY + halfHeight, boundsMaxY - halfHeight);
        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }

    void ApplyPosition()
    {
        // Z fijo para no romper la profundidad de la cámara 2D
        transform.position = new Vector3(transform.position.x, transform.position.y, -10f);
    }

    // --- Gizmos para visualizar dead zone en el editor ---
    private void OnDrawGizmos()
    {
        if (target == null) return;

        // Dead zone (rectángulo centrado en la cámara)
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireCube(transform.position, new Vector3(deadZoneX * 2f, deadZoneY * 2f, 0f));

        // Look-ahead preview
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * lookAheadDistance);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * lookAheadDistance);

        // Bounds del nivel
        if (useBounds)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            Vector3 center = new Vector3((boundsMinX + boundsMaxX) / 2f, (boundsMinY + boundsMaxY) / 2f, 0f);
            Vector3 size = new Vector3(boundsMaxX - boundsMinX, boundsMaxY - boundsMinY, 0f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
