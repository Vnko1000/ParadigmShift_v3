using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float runSpeedMultiplier = 1.8f;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isRunning;
    private bool isSliding;
    public float jump = 8f;

    [SerializeField] public Transform GroundCheck;
    [SerializeField] public Vector2 groundcheckSize = new Vector2(0.25f, 0.25f);
    [SerializeField] public LayerMask groundLayer;
    private bool isGrounded;

    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        UpdateGroundState();
        CheckFlip();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        // BUG FIX: antes se calculaba currentSpeed pero se usaba moveSpeed en linearVelocity
        float currentSpeed = moveSpeed * (isRunning ? runSpeedMultiplier : 1f);
        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
    }

    // --- Input Actions ---

    public void Move(InputAction.CallbackContext context)
    {
        horizontalInput = context.ReadValue<Vector2>().x;
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        // BUG FIX: usar started/canceled es más confiable que ReadValueAsButton
        if (context.started)
            isRunning = true;
        else if (context.canceled)
            isRunning = false;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (!context.performed || !isGrounded) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jump);
        animator.SetTrigger("Jump");
    }

    public void Slide(InputAction.CallbackContext context)
    {
        if (context.started && isGrounded && horizontalInput != 0)
        {
            isSliding = true;
            animator.SetBool("IsSliding", true);
        }
        else if (context.canceled)
        {
            isSliding = false;
            animator.SetBool("IsSliding", false);
        }
    }

    public void Duck(InputAction.CallbackContext context)
    {
        // TODO: ajustar collider al agacharse si es necesario
        if (context.started && isGrounded)
            animator.SetBool("IsDucking", true);
        else if (context.canceled)
            animator.SetBool("IsDucking", false);
    }

    // --- Animator ---

    void UpdateAnimator()
    {
        float speed = Mathf.Abs(rb.linearVelocity.x);
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsRunning", isRunning);
        // IsSliding e IsDucking se setean directo en los callbacks
    }

    // --- Helpers ---

    void UpdateGroundState()
    {
        isGrounded = Physics2D.OverlapBox(GroundCheck.position, groundcheckSize, 0, groundLayer);
    }

    void CheckFlip()
    {
        if (horizontalInput == 0) return;

        bool movingRight = horizontalInput > 0;
        bool facingRight = transform.localScale.x > 0;

        if (movingRight != facingRight)
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(GroundCheck.position, groundcheckSize);
    }
}
