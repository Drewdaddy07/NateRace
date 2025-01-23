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
    [SerializeField] private Transform pivotTransform;
    private Rigidbody surfaceRB = null;

    [SerializeField] private GameObject hitDebugPrefab;
    [SerializeField] private Material debugMat;

    [Header("Settings")]
    [SerializeField] private Vector3 runningDirection = Vector3.forward;

    [Header("Basic Stats")]
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float horizontalSpeed = 2f;
    [SerializeField] private float horizontalDecceleration = 0.6f;
    [SerializeField] private Vector2 runSpeedMinMax = new Vector2(6f, 20f);
    [SerializeField] private float passiveGroundedMomentumLoss = 2f;
    private float currentRunSpeed = 6f;

    [Header("Jumping")]
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpCooldown = 0.2f;
    [SerializeField] private float downwardJumpPower = 7f;
    private Coroutine queuedJumpCoroutine = null;
    private bool queuedJump = false;
    private bool jumpReady = true;
    private bool downJumpReady = true;

    [Header("Sliding")]
    [SerializeField] private float momentumGain = 0.4f;
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
    private Vector3 realGroundNormal = Vector3.up;

    [Header("Hovering")]
    [SerializeField] private float stepHeight = 0.3f;
    [SerializeField] private float hoverForce = 20f;
    [SerializeField] private float hoverDistanceMutliplier = 1.4f;
    [SerializeField] private float maxHoverForce = 3f;
    [SerializeField] private float slopeOffsetCoefficient = 0.8f;
    [SerializeField] private float slidingHoverHeight = 0.3f;
    [SerializeField] private float hoverDamping = 15f;

    [Header("Collider")]
    [SerializeField] private float colliderHeight = 2f;
    [SerializeField] private float colliderRadius = 0.2f;
    [SerializeField] private float colliderSlidingHeight = 0.5f;

    [Header("Misc")]
    [SerializeField] private float turnSmoothing = 5f;
    [SerializeField] private float surfaceNormalSmoothing = 3f;

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

        UpdateAnimations();
    }

    private void FixedUpdate() {
        GroundCheck();
        Run();
        Slide();
        Hover();
        Momentum();
        CheckJump();
        UpdateTurn();

        wasGrounded = isGrounded;
    }

    private void Run() {
        //Add running forces
        Vector3 dir = Vector3.ProjectOnPlane(runningDirection, realGroundNormal);
        Vector3 force = dir * acceleration;

        playerRB.AddForce(force, ForceMode.VelocityChange);

        float forwardSpeed = GetFlatVelWrld().z;
        if (forwardSpeed > currentRunSpeed) {
            float amountOver = forwardSpeed / currentRunSpeed;
            amountOver = Mathf.Clamp(amountOver, 1f, 4f);

            Vector3 dirToStop = -runningDirection.normalized;

            Vector3 opposingForce = dirToStop * acceleration * amountOver;

            playerRB.AddForce(opposingForce, ForceMode.VelocityChange);
        }

        //strafing forces
        Vector3 strafeDirection = Vector3.Cross(runningDirection, Vector3.up).normalized;
        Vector3 strafeForce = strafeDirection * horizontalSpeed * -inputVector.x;

        if (isSliding) {
            strafeForce = Vector3.zero;
        }

        Vector3 strafeVelocity = new Vector3(GetFlatVelWrld().x, 0f, 0f);

        if (strafeForce.magnitude > 0.01f) {
            playerRB.AddForce(strafeForce, ForceMode.VelocityChange);
        }
        else {
            Vector3 slowdownForce = -strafeVelocity * horizontalDecceleration;
            playerRB.AddForce(slowdownForce, ForceMode.Acceleration);
        }
        
        if (strafeVelocity.magnitude > horizontalSpeed) {

            Vector3 excessVelocity = strafeVelocity.normalized * (strafeVelocity.magnitude - horizontalSpeed);

            playerRB.AddForce(-excessVelocity, ForceMode.VelocityChange);
        }
    }


    private void Momentum() {
        //Add momentum for sliding
        if(isGrounded) {

            if (isSliding) {
                float angle = Vector3.Angle(transform.forward, realGroundNormal) - 90f;
                currentRunSpeed -= (angle * momentumGain) * Time.fixedDeltaTime;
            }
            else {
                currentRunSpeed -= passiveGroundedMomentumLoss * Time.fixedDeltaTime;
            }
        }

        currentRunSpeed = Mathf.Clamp(currentRunSpeed, runSpeedMinMax.x, runSpeedMinMax.y);
    }

    private void TryDownwardJump(InputAction.CallbackContext context) {
        if (!isGrounded && downJumpReady) {
            DownJump();
        }
    }

    private void DownJump() {
        downJumpReady = false;
        float verticalVel = GetSelfVel().y;
        verticalVel = Mathf.Clamp(verticalVel, float.MinValue, 0f);
        Vector3 newVel = GetFlatVelWrld() + -transform.up * (downwardJumpPower + Mathf.Abs(verticalVel));
        playerRB.velocity = newVel;
    }

    private void Slide() {
        //start slide
        if(!isSliding && isGrounded && pressingSlide) {
            isSliding = true;
            CalculateCollider(colliderRadius, colliderSlidingHeight, stepHeight);
        }

        //end slide
        if (isSliding) {
            Ray upCheckRay = new Ray(transform.position + -transform.up * 0.5f, transform.up);
            Physics.SphereCast(upCheckRay, colliderRadius, out RaycastHit hit, 1.5f - colliderRadius, walkable, QueryTriggerInteraction.Ignore);

            if(!isGrounded || (hit.collider == null && !pressingSlide)){
                isSliding = false;
                CalculateCollider(colliderRadius, colliderHeight, stepHeight);
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

        Vector3 upDir = Vector3.up;
        if (isGrounded) {
            upDir = realGroundNormal;
        }
        Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, upDir).normalized;
        Quaternion targetPivot = Quaternion.LookRotation(projectedForward, upDir);
        Quaternion smoothedPivot = Quaternion.Slerp(pivotTransform.rotation, targetPivot, Time.deltaTime * surfaceNormalSmoothing);
        pivotTransform.rotation = smoothedPivot;
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
    
    private void GroundCheck() {
        Vector3 position = transform.position + transform.up * playerCollider.center.z;
        Ray groundCheckRay = new Ray(position, -transform.up);
        float checkDistance = 1f - groundCheckRadius + groundCheckAdditional + playerCollider.center.z;

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

        if (isGrounded) {
            downJumpReady = true;

            Ray normalCheckRay = new Ray(groundCheckHit.point + transform.up * 0.04f, -transform.up);
            Physics.Raycast(normalCheckRay, out RaycastHit hit, 0.1f, walkable, QueryTriggerInteraction.Ignore);
            realGroundNormal = hit.normal;
        }
        else {
            realGroundNormal = Vector3.up;
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
            Vector3 surfaceNormal = realGroundNormal;

            Vector3 relativeNormal = transform.InverseTransformDirection(surfaceNormal);
            float dot = Vector3.Dot(GetFlatVelLocal(), relativeNormal);
            Vector3 relativePoint = transform.InverseTransformPoint(groundCheckHit.point);

            float targetHeight = 1f;
            float currentHeight = relativePoint.y;
            float distance = currentHeight + targetHeight;
            Debug.Log(distance);

            float springForce = distance * hoverDistanceMutliplier * hoverForce;

            float verticalVelocity = GetSelfVel().y;
            float damperForce = -verticalVelocity * hoverDamping;

            float totalForce = springForce + damperForce - (dot * slopeOffsetCoefficient);
            totalForce = Mathf.Clamp(totalForce, -maxHoverForce, maxHoverForce);

            Vector3 newVelocity = new Vector3(playerRB.velocity.x, 0f, playerRB.velocity.z) + transform.up * totalForce;
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
        if(playerCollider != null && !Application.isPlaying) {
            CalculateCollider(colliderRadius, colliderHeight, stepHeight);
        }
    }

    private void CalculateCollider(float radius, float height, float stepSpace) {
        playerCollider.radius = radius;
        float realHeight = height - stepSpace;
        playerCollider.height = realHeight;
        playerCollider.center = new Vector3(0f, 0f, (-1f + height / 2f) + stepSpace / 2f);
    }

    private void OnEnable() {
        inputManager.GetInputActions().Movement.Jump.performed += TryJump;
        inputManager.GetInputActions().Movement.Slide.performed += TryDownwardJump;
    }

    private void OnDisable() {
        inputManager.GetInputActions().Movement.Jump.performed -= TryJump;
        inputManager.GetInputActions().Movement.Slide.performed -= TryDownwardJump;
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