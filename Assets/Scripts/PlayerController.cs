using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple player controller for 2D top-down action RPG
/// WASD movement, left click attack, right click special attack
/// </summary>

//I like your summary -Todd :)


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerAttackController))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Combat")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float specialAttackCooldown = 2f;
    
    private Rigidbody2D rb;
    private Camera mainCamera;
    private Vector2 moveInput;
    
    private PlayerAttackController attackController;
    private float attackTimer;
    private float specialAttackTimer;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        
        // Configure Rigidbody2D for top-down movement
        rb.gravityScale = 0f;
        rb.linearDamping = 10f;
        rb.freezeRotation = true;

        //configure attack controller
        attackController = GetComponent<PlayerAttackController>();

    }

    private void Update()
    {
        HandleMovementInput();
        HandleCombatInput();
        UpdateCooldowns();
    }
    
    private void FixedUpdate()
    {
        // Apply movement
        rb.linearVelocity = moveInput * moveSpeed;
    }
    
    private void HandleMovementInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        
        moveInput = Vector2.zero;
        
        if (keyboard.wKey.isPressed) moveInput.y += 1f;
        if (keyboard.sKey.isPressed) moveInput.y -= 1f;
        if (keyboard.aKey.isPressed) moveInput.x -= 1f;
        if (keyboard.dKey.isPressed) moveInput.x += 1f;
        
        // Normalize to prevent faster diagonal movement
        moveInput = moveInput.normalized;
    }
    
    private void HandleCombatInput()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;
        
        // Left click - Attack
        if (mouse.leftButton.wasPressedThisFrame && attackTimer <= 0f)
        {
            PerformAttack();
        }
        
        // Right click - Special Attack
        if (mouse.rightButton.wasPressedThisFrame && specialAttackTimer <= 0f)
        {
            PerformSpecialAttack();
        }
    }
    
    private void UpdateCooldowns()
    {
        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;
            
        if (specialAttackTimer > 0f)
            specialAttackTimer -= Time.deltaTime;
    }

    private void PerformAttack()
    {
        attackTimer = attackCooldown;

        Vector2 direction = GetDirectionToMouse();
        RotateToDirection(direction);

        attackController.BasicAttack(direction);
    }

    private void PerformSpecialAttack()
    {

        //created a new script to handle the attack logic
        //this helps us to keep this to a more readable size
        //the only downside is that you can easily get lost jumping between scripts
        //So... note time

        //Note: special and basic attack logic moved to PlayerAttackController
        //which also pulls data from PlayerWeaponController to determine damage/range/etc
        //this allows us to make different weapons with different stats easily
        //just be careful not to get lost in the sauce....

        specialAttackTimer = specialAttackCooldown;

        Vector2 direction = GetDirectionToMouse();
        RotateToDirection(direction);

        attackController.SpecialAttack(direction);
    }

    private Vector2 GetMouseWorldPosition()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("No main camera found!");
                return transform.position;
            }
        }
        
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return transform.position;
        
        Vector3 mouseScreenPos = mouse.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;
        return mouseWorldPos;
    }
    
    /// <summary>
    /// Get the direction from player to mouse cursor
    /// </summary>
    public Vector2 GetDirectionToMouse()
    {
        Vector2 mouseWorldPos = GetMouseWorldPosition();
        return (mouseWorldPos - (Vector2)transform.position).normalized;
    }

    private void RotateToDirection(Vector2 direction)
    {
        if (direction.magnitude < 0.1f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }


}
