using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Attachments")]
    [SerializeField] private Rigidbody playerRB;
    [SerializeField] private CapsuleCollider playerCollider;
    [SerializeField] private PlayerInputManager inputManager;
    [SerializeField] private Animator nateAnimator;
    private Rigidbody surfaceRB = null;

    [SerializeField] private GameObject hitDebugPrefab;
    [SerializeField] private Material debugMat;

    [Header("Settings")]
    [SerializeField] private Vector3 runningDirection = Vector3.forward;

    [Header("Basic Stats")]
    [SerializeField] private float runSpeed;
    [SerializeField] private float horizontalSpeed = 2f;

    [Header("Jumping")]
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpCooldown = 0.2f;
    [SerializeField] private float WallCastOffset = -0.75f;
    private Coroutine queuedJumpCoroutine = null;
    private bool queuedJump = false;
    private bool jumpReady = true;

    [Header("Sliding")]
    private bool isSliding = false;
    private bool pressingSlide = false;

    [Header("Grounding")]
    [SerializeField] private LayerMask walkable;
    [SerializeField] private float maxSlopeAngle = 55f;
    [SerializeField] private float groundCheckAdditional = 0.1f;//the additional distance beyond the player's height / 2
    [SerializeField] private float groundCheckRadius = 0.2f;
    private RaycastHit groundCheckHit;
    private bool wasGrounded = false;
    private bool isGrounded = false;
    private float timeSinceLastGrounded = 0f;

    [Header("Hovering")]
    [SerializeField] private float stepHeight = 0.3f;
    [SerializeField] private float hoverForce = 20f;
    [SerializeField] private float hoverDistanceMutliplier = 1.4f;
    [SerializeField] private float maxHoverForce = 3f;
    [SerializeField] private float slopeOffsetCoefficient = 0.8f;
    [SerializeField] private float slidingHoverHeight = 0.3f;

    [Header("Collider")]
    [SerializeField] private float colliderHeight = 2f;
    [SerializeField] private float colliderRadius = 0.2f;
    [SerializeField] private float colliderSlidingHeight = 0.5f;

    [Header("Misc")]
    [SerializeField] private float turnSmoothing = 5f;

    private Vector2 inputVector = Vector2.zero;

    private void Start() {
        if(inputManager == null) {
            inputManager = PlayerInputManager.Instance;
        }
    }

    private void Update() {
        inputVector.x = inputManager.GetInputActions().Movement.Horizontal.ReadValue<float>();
        inputVector.y = inputManager.GetInputActions().Movement.Vertical.ReadValue<float>();
        pressingSlide = (inputManager.GetInputActions().Movement.Slide.ReadValue<float>() == 1f);
        Debug.Log(pressingSlide);

        UpdateAnimations();
    }

    private void FixedUpdate() {
        GroundCheck();
        Run();
        Slide();
        Hover();
        CheckJump();
        UpdateTurn();

        wasGrounded = isGrounded;
    }

    private void Slide() {
        //start slide
        if(!isSliding && isGrounded && pressingSlide) {
            isSliding = true;
        }

        //end slide
        if (isSliding) {
            Ray upCheckRay = new Ray(transform.position, transform.up);
            Physics.SphereCast(upCheckRay, colliderRadius, out RaycastHit hit, colliderHeight - slidingHoverHeight, walkable, QueryTriggerInteraction.Ignore);

            if(!isGrounded || (hit.collider == null && !pressingSlide)){
                isSliding = false;
            }
        }
    }

    private void UpdateTurn() {
        Vector3 direction = GetFlatVelWrld().normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction, transform.up);
        Quaternion smoothedRotation = Quaternion.Slerp(playerRB.rotation, targetRotation, Time.fixedDeltaTime * turnSmoothing);
        playerRB.MoveRotation(smoothedRotation);
    }

    private void UpdateAnimations() {
        nateAnimator.SetBool("IsGrounded", isGrounded);
        nateAnimator.SetBool("IsSliding", isSliding);
    }

    private void Jump() {
        Vector3 jumpForce = transform.up * jumpPower;
        Vector3 newVel = GetFlatVelWrld() + jumpForce;
        playerRB.velocity = newVel;
        jumpReady = false;
        Invoke("RefreshJump", jumpCooldown);
        nateAnimator.SetTrigger("Jump");
    }

    public void RefreshJump() {
        jumpReady = true;
    }

    private void CheckJump() {
        if(isGrounded && !wasGrounded && queuedJump && jumpReady) {
            Jump();
        }
    }

    private void TryJump(InputAction.CallbackContext context) {
        bool coyoteValid = timeSinceLastGrounded <= coyoteTime;
        if ((isGrounded || coyoteValid) && jumpReady) {
            Jump();
        }

        if (!isGrounded) {
            if (queuedJumpCoroutine != null) {
                StopCoroutine(queuedJumpCoroutine);
            }
            queuedJumpCoroutine = StartCoroutine(InverseCoyoteTime());
        }
    }

    private IEnumerator InverseCoyoteTime() {
        queuedJump = true;
        yield return new WaitForSeconds(coyoteTime);
        queuedJump = false;
    }

    private void Run() {
        //Add running forces
        Vector3 force = runningDirection * runSpeed;

        Vector3 strafeDirection = Vector3.Cross(runningDirection, Vector3.up).normalized;

        force += strafeDirection * horizontalSpeed * -inputVector.x;

        force = new Vector3(force.x, playerRB.velocity.y, force.z);

        playerRB.velocity = force;
    }

    private void GroundCheck() {
        Ray groundCheckRay = new Ray(transform.position, -transform.up);
        float checkDistance = (colliderHeight * 0.5f) - groundCheckRadius + groundCheckAdditional;

        RaycastHit[] hits = Physics.SphereCastAll(groundCheckRay, groundCheckRadius, checkDistance, walkable, QueryTriggerInteraction.Ignore);

        float highestDistance = float.MinValue;
        isGrounded = false;

        for (int i = 0; i < hits.Length; i++) {
            RaycastHit hit = hits[i];

            float angle = Vector3.Angle(hit.normal, transform.up);

            if (angle <= maxSlopeAngle) {

                float distance = transform.InverseTransformPoint(hit.point).y;

                if (distance > highestDistance) {
                    highestDistance = distance;
                    groundCheckHit = hit;
                    isGrounded = true;
                }
            }
        }

        if (!isGrounded) {
            timeSinceLastGrounded += Time.fixedDeltaTime;
        }
        else {
            timeSinceLastGrounded = 0f;
        }

        //if (!isGrounded)
        //{
        //    CheckForWalls();
        //}
    }

    //'floats' the player to avoid collision issues and better for slopes generally
    private void Hover() {
        if(isGrounded && jumpReady) {
            //need to get new normal bc spherecast gives silly results
            Ray normalCheckRay = new Ray(groundCheckHit.point + transform.up * 0.04f, -transform.up);
            Physics.Raycast(normalCheckRay, out RaycastHit hit, 0.1f, walkable, QueryTriggerInteraction.Ignore);

            Vector3 surfaceNormal = hit.normal;

            Vector3 relativeNormal = transform.InverseTransformDirection(surfaceNormal);
            float dot = Vector3.Dot(GetFlatVelLocal(), relativeNormal);

            Vector3 relativePoint = transform.InverseTransformPoint(groundCheckHit.point);

            float targetHeight = colliderHeight * 0.5f;

            float distance = relativePoint.y + targetHeight;

            float verticalVelocity = GetSelfVel().y;
            float verticalFactor = Mathf.Abs(Mathf.Clamp(verticalVelocity, 1f, 10f));

            float force = (distance * hoverDistanceMutliplier * hoverForce * verticalFactor) - (dot * slopeOffsetCoefficient);
            force = Mathf.Clamp(force, -maxHoverForce, maxHoverForce);

            Vector3 newVelocity = new Vector3(playerRB.velocity.x, 0f, playerRB.velocity.z) + transform.up * force;
            playerRB.velocity = newVelocity;
        }
    }

    #region Velocity utility functions
    public Vector3 GetSurfaceVel() {
        Vector3 vel = Vector3.zero;
        if (surfaceRB) {
            vel = surfaceRB.GetPointVelocity(groundCheckHit.point);
        }
        return vel;
    }
    public Vector3 GetRelSurfaceVel() {
        return playerRB.velocity - GetSurfaceVel();
    }
    public Vector3 GetSelfVel() {
        return transform.InverseTransformDirection(GetRelSurfaceVel());
    }
    public Vector3 GetFlatVelLocal() {
        Vector3 selfVel = GetSelfVel();
        Vector3 flatVel = new Vector3(selfVel.x, 0f, selfVel.z);

        return flatVel;
    }
    public Vector3 GetFlatVelWrld() {
        Vector3 flatVel = GetRelSurfaceVel();
        flatVel = transform.InverseTransformDirection(flatVel);
        flatVel = new Vector3(flatVel.x, 0f, flatVel.z);
        flatVel = transform.TransformDirection(flatVel);

        return flatVel;
    }
    #endregion

    private void OnValidate() {
        if(playerCollider != null) {
            playerCollider.radius = colliderRadius;
            float height = colliderHeight - stepHeight;
            playerCollider.height = height;
            playerCollider.center = new Vector3(0f, 0f, (-1f + colliderHeight / 2f) + stepHeight / 2f);
        }
    }

    private void OnEnable() {
        inputManager.GetInputActions().Movement.Jump.performed += TryJump;
    }

    private void OnDisable() {
        inputManager.GetInputActions().Movement.Jump.performed -= TryJump;
    }

    /*
    private void CheckForWalls()
    {
        float wallCheckDistance = 0.5f; // Adjust as needed
        Vector3 start = transform.position + Vector3.up * WallCastOffset; // Start slightly above the ground
        Vector3 direction = transform.forward;

        if (Physics.Raycast(start, direction, out RaycastHit hit, wallCheckDistance, walkable, QueryTriggerInteraction.Ignore))
        {
            // Prevent forward movement if a wall is detected
            if (Vector3.Angle(hit.normal, Vector3.up) > maxSlopeAngle)
            {
                runningDirection = Vector3.zero; // Prevent forward movement
            }
        }
        else
        {
            // Restore forward movement
            runningDirection = Vector3.forward;
        }

        // Optional: Debug the wall check
        Debug.DrawRay(start, direction * wallCheckDistance, Color.red);
    }
    */
}