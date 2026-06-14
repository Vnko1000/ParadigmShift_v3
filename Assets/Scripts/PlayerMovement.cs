using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;

    private Rigidbody2D rb;
    private float horizontalInput;
    public float jump = 8f;

    [SerializeField] public Transform GroundCheck;
    [SerializeField] public Vector2 groundcheckSize = new Vector2(0.25f, 0.25f);
    [SerializeField] public LayerMask groundLayer;
    private bool isGrounded;

    private Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateGroundState();
        CheckFilp();

        animator.SetFloat("Horizontal", Mathf.Abs(horizontalInput));
    }

    public void Move(InputAction.CallbackContext context)
    {
        horizontalInput = context.ReadValue<Vector2>().x;
    }
    private void FixedUpdate()
    {
        // Aplicamos la velocidad multiplicada por nuestro moveSpeed
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if(!context.performed || !isGrounded){return;}

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jump);
        
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(GroundCheck.position, groundcheckSize);
    }

    void UpdateGroundState()
    {
        isGrounded = Physics2D.OverlapBox(GroundCheck.position, groundcheckSize, 0, groundLayer);
    }

    void CheckFilp()
    {
        if (horizontalInput == 0) { return; }

        bool movingRight = horizontalInput > 0;
        bool facingRight = transform.localScale.x > 0;

        if (movingRight != facingRight)
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }

    }
}
