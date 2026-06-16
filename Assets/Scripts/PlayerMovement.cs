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
    private CapsuleCollider2D capsuleCollider;

    [Header("Slide Collider")]
    [SerializeField] private float slideColliderHeight = 0.5f;
    [SerializeField] private float slideColliderYOffset = -0.8f;

    private Vector2 defaultColliderSize;
    private Vector2 defaultColliderOffset;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();

        if (capsuleCollider != null)
        {
            defaultColliderSize = capsuleCollider.size;
            defaultColliderOffset = capsuleCollider.offset;
        }
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
            SetSlideCollider();
        }
        else if (context.canceled)
        {
            isSliding = false;
            animator.SetBool("IsSliding", false);
            ResetCollider();
        }
    }

    public void Duck(InputAction.CallbackContext context)
    {
        // TODO: ajustar collider al agacharse si es necesario
        if (context.started && isGrounded)
        {
            animator.SetBool("IsDucking", true);
            SetSlideCollider();
        }
        else if (context.canceled)
        {
            animator.SetBool("IsDucking", false);
            ResetCollider();
        }
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

    // --- Collider (Slide) ---

    void SetSlideCollider()
    {
        if (capsuleCollider == null) return;

        // Calcula el Y de los pies a partir del collider original (de pie)
        float feetY = defaultColliderOffset.y - (defaultColliderSize.y / 2f);

        // Centra el nuevo collider (más bajo) para que su base siga en los pies
        float slideOffsetY = (feetY + (slideColliderHeight / 2f)) + 0.3f;

        capsuleCollider.size = new Vector2(defaultColliderSize.x, slideColliderHeight);
        capsuleCollider.offset = new Vector2(defaultColliderOffset.x, slideOffsetY);
    }

    void ResetCollider()
    {
        if (capsuleCollider == null) return;

        capsuleCollider.size = defaultColliderSize;
        capsuleCollider.offset = defaultColliderOffset;
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
