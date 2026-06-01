using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform modelTransform;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private PlayerAnimationController animationController;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSmoothSpeed = 12f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 25f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private int maxJumps = 1;
    [SerializeField] private float gravity = -20f;
    

    [Header("Climbing")]
    public bool canClimb = false;
    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] public float climbSpeed = 4f;
    [SerializeField] public float maxClimbTime = 2.5f;
    [SerializeField] public float detectionLength = 0.7f;
    [SerializeField] public float sphereCastRadius = 0.3f;
    [SerializeField] public float maxWallLookAngle = 35f;
    [SerializeField] public float wallJumpBackForce = 7f;
    [SerializeField] public float wallJumpUpForce = 8f;

    [Header("Ball Mode")]
    [SerializeField] private GameObject humanVisuals;
    [SerializeField] private GameObject ballObject;
    [SerializeField] private float ballMoveSpeed = 20f;
    [SerializeField] private KeyCode ballToggleKey = KeyCode.LeftShift;

    [Header("Sprint")]
    [SerializeField] private float sprintMultiplier = 1.5f; 
    public bool IsRunning { get; private set; } 

    [Header("HyperRoll Dash")]
    public float dashForce = 50f;
    public float dashCooldown = 1f;
    private float dashTimer;
    public bool isDashing = false;

    [Header("Ball Boost System")]
    [SerializeField] private KeyCode boostKey = KeyCode.F; 
    [SerializeField] private float ballBoostSpeed = 25f;   
    [SerializeField] private float maxBoostEnergy = 100f;  
    [SerializeField] private float boostDrainSpeed = 20f;  
    [SerializeField] private float boostRechargeSpeed = 20f;

    [Header("Boost UI Visuals")]
    [SerializeField] private UnityEngine.UI.Image boostScreenTint; 
    [SerializeField] private Color boostColorTint = new Color(1f, 0f, 0f, 0.15f);
    [SerializeField] private Slider boostSlider;

    private float currentBoostEnergy;
    private bool isBoosting = false;
    private bool canStartBoost = true;

    [Header("Transformation Effects")]
    [SerializeField] private ParticleSystem transformationEffect;
    [SerializeField] private float visualDelay = 0.15f;

    [Header("VFX")]
    [SerializeField] private ParticleSystem ballTrailSystem;
    [SerializeField] private GameObject trailRendererObject;
    [SerializeField] private ParticleSystem jumpVFXSystem;


    

    private Rigidbody ballRb;
    private Vector3 velocity;

    public float VerticalVelocity => velocity.y;
    public bool IsGrounded => controller.isGrounded;

    private int jumpCount;
    private float pitch;
    private bool isTransforming = false;
    private float climbTimer;
    private RaycastHit frontWallHit;
    private bool wallFront;
    private float wallLookAngle;

    public float CurrentMoveAmount { get; private set; }
    public bool IsBallMode { get; private set; }
    public bool IsClimbing { get; private set; }

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (ballObject != null)
        {
            ballRb = ballObject.GetComponent<Rigidbody>();
            ballObject.SetActive(false);
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        climbTimer = maxClimbTime;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        climbTimer = maxClimbTime;

        currentBoostEnergy = maxBoostEnergy;
        if (boostScreenTint != null) boostScreenTint.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(ballToggleKey) && !isTransforming)
        {
            StartCoroutine(ToggleBallModeRoutine());
        }

        HandleMouseLook();

        HandleBoostEnergy();

        if (IsBallMode)
        {
            HandleBallMovement();
            HandleDash();
            

            if (Input.GetButtonDown("Jump"))
            {
                if (Mathf.Abs(ballRb.linearVelocity.y) < 0.1f)
                    ballRb.AddForce(Vector3.up * jumpHeight * 2f, ForceMode.Impulse);
            }
        }

        else if (controller.enabled)
        {
            WallCheck();
            if (IsClimbing) HandleClimbingMovement();
            else
            {
                HandleMovement();
                HandleGravity();
            }
            HandleJump();
        }
        else
        {
            if (boostScreenTint != null) boostScreenTint.gameObject.SetActive(false);
            isBoosting = false;
        }
    }

    private void WallCheck()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 1f;
        wallFront = Physics.SphereCast(rayOrigin, sphereCastRadius, modelTransform.forward, out frontWallHit, detectionLength, whatIsWall);

        if (wallFront) wallLookAngle = Vector3.Angle(modelTransform.forward, -frontWallHit.normal);

        if (controller.isGrounded) climbTimer = maxClimbTime;

        bool isTryingToClimb = wallFront && Input.GetAxisRaw("Vertical") > 0 && wallLookAngle < maxWallLookAngle && !controller.isGrounded;
        IsClimbing = (isTryingToClimb && climbTimer > 0 && canClimb);
    }

    private void HandleClimbingMovement()
    {
        velocity.y = 0;
        controller.Move(Vector3.up * climbSpeed * Time.deltaTime);
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
        CurrentMoveAmount = inputDirection.magnitude;

        IsRunning = Input.GetKey(KeyCode.LeftControl) && CurrentMoveAmount > 0;
        float currentSpeed = IsRunning ? moveSpeed * sprintMultiplier : moveSpeed;
        

        Vector3 moveDirection = (playerCamera.forward * vertical + playerCamera.right * horizontal);
        moveDirection.y = 0;
        moveDirection.Normalize();

        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        if (modelTransform != null && inputDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            modelTransform.rotation = Quaternion.Slerp(modelTransform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
        }
    }

    private void HandleJump()
    {
        if (controller.isGrounded)
        {
            if (velocity.y < 0f) velocity.y = -2f;
            jumpCount = 0;
        }

        if (Input.GetButtonDown("Jump"))
        {
            if (IsClimbing)
            {
                IsClimbing = false;
                velocity.y = wallJumpUpForce;
                Vector3 bounceDir = frontWallHit.normal * wallJumpBackForce;
                velocity.x = bounceDir.x;
                velocity.z = bounceDir.z;
                animationController?.PlayJump();

                if (jumpVFXSystem != null)
                {
                    jumpVFXSystem.Play(true);
                }
            }
            else if (jumpCount < maxJumps)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

                if (jumpCount == 0)
                {
                    animationController?.PlayJump(); 
                }
                else
                {
                    animationController?.PlayDoubleJump();
                }

                if (jumpVFXSystem != null) jumpVFXSystem.Play(true);
                jumpCount++;
            }
        }
    }

    private void HandleGravity()
    {
        if (IsClimbing) return;

        velocity.x = Mathf.MoveTowards(velocity.x, 0, Time.deltaTime * 2f);
        velocity.z = Mathf.MoveTowards(velocity.z, 0, Time.deltaTime * 2f);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private IEnumerator ToggleBallModeRoutine()
    {
        PlayerStats stats = GetComponent<PlayerStats>();
        if (stats != null && !stats.hasHyperRoll && !IsBallMode)
        {
            Debug.Log("Ještì nemáš HyperRoll!");
            yield break;
        }

        isTransforming = true;
        if (transformationEffect != null)
        {
            transformationEffect.transform.position = transform.position + Vector3.up * 0.5f;
            transformationEffect.Play();
        }

        IsBallMode = !IsBallMode;

        if (IsBallMode)
        {
            if (ballTrailSystem != null) ballTrailSystem.Play();
            if (trailRendererObject != null)
            {
                trailRendererObject.SetActive(true);
                var tr = trailRendererObject.GetComponent<TrailRenderer>();
                if (tr != null) tr.Clear();
            }

            humanVisuals.SetActive(false);
            controller.enabled = false;

            ballObject.transform.SetParent(null);

            if (ballRb != null)
            {
                ballRb.linearVelocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;
            }

            ballObject.transform.position = transform.position + Vector3.up * 0.5f;
            ballObject.SetActive(true);
        }
        else
        {
            if (ballTrailSystem != null) ballTrailSystem.Stop();
            if (trailRendererObject != null) trailRendererObject.SetActive(false);

            yield return new WaitForSeconds(visualDelay);

            transform.position = ballObject.transform.position;

            ballObject.SetActive(false);

            ballObject.transform.SetParent(transform);
            ballObject.transform.localPosition = Vector3.zero;

            controller.enabled = true;
            humanVisuals.SetActive(true);
        }
        isTransforming = false;
    }

    void HandleDash()
    {
        if (IsBallMode && Input.GetMouseButtonDown(0) && Time.time > dashTimer)
        {
            Rigidbody rb = GetComponentInChildren<Rigidbody>(); 
            if (rb != null)
            {
                Vector3 dashDir = playerCamera.forward;
                rb.AddForce(dashDir * dashForce, ForceMode.Impulse);

                isDashing = true;
                dashTimer = Time.time + dashCooldown;

                Invoke("ResetDash", 0.5f);
            }
        }
    }
    private void HandleBoostEnergy()
    {
        if (currentBoostEnergy >= maxBoostEnergy)
        {
            canStartBoost = true;
        }

        if (IsBallMode && Input.GetKey(boostKey) && canStartBoost)
        {
            isBoosting = true;
            currentBoostEnergy -= boostDrainSpeed * Time.deltaTime;

            if (boostScreenTint != null)
            {
                boostScreenTint.gameObject.SetActive(true);
                boostScreenTint.color = boostColorTint;
            }

            if (currentBoostEnergy <= 0)
            {
                canStartBoost = false;
            }
        }
        else
        {
            if (isBoosting)
            {
                canStartBoost = false; 
            }

            isBoosting = false;

            if (currentBoostEnergy < maxBoostEnergy)
            {
                currentBoostEnergy += boostRechargeSpeed * Time.deltaTime;
            }

            if (boostScreenTint != null) boostScreenTint.gameObject.SetActive(false);
        }

        currentBoostEnergy = Mathf.Clamp(currentBoostEnergy, 0f, maxBoostEnergy);

        if (boostSlider != null)
        {
            boostSlider.gameObject.SetActive(IsBallMode);
            boostSlider.value = currentBoostEnergy / maxBoostEnergy;
        }
    }

    void ResetDash() { isDashing = false; }

    private void HandleBallMovement()
    {
        transform.position = ballObject.transform.position;

        Vector3 moveDirection = (playerCamera.forward * Input.GetAxisRaw("Vertical") + playerCamera.right * Input.GetAxisRaw("Horizontal"));
        moveDirection.y = 0;
        moveDirection.Normalize();

        if (ballRb != null)
        {
            float currentMaxSpeed = isBoosting ? ballBoostSpeed : ballMoveSpeed;

            Vector3 targetVelocity = moveDirection * currentMaxSpeed;

            float responsiveness = 15f;
            float newX = Mathf.Lerp(ballRb.linearVelocity.x, targetVelocity.x, responsiveness * Time.deltaTime);
            float newZ = Mathf.Lerp(ballRb.linearVelocity.z, targetVelocity.z, responsiveness * Time.deltaTime);

            ballRb.linearVelocity = new Vector3(newX, ballRb.linearVelocity.y, newZ);
        }
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(0f, mouseX, 0f);
        pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);
        if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    public void UnlockDoubleJump()
    {
        maxJumps = 2; 
        Debug.Log("Dvojskok odemknut!");
    }

    public void UnlockClimbing()
    {
        canClimb = true;
        climbSpeed = 4f; 
        Debug.Log("Lezení odemknuto!");
    }
}