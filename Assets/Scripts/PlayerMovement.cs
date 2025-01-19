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

    [Header("Settings")]
    [SerializeField] private Vector3 runningDirection = Vector3.forward;

    [Header("Basic Stats")]
    [SerializeField] private float runSpeed;
    [SerializeField] private float horizontalSpeed = 2f;

    [Header("Jumping")]
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpCooldown = 0.2f;
    private Coroutine queuedJumpCoroutine = null;
    private bool queuedJump = false;
    private bool jumpReady = true;

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

    [Header("Collider")]
    [SerializeField] private float colliderHeight = 2f;
    [SerializeField] private float colliderRadius = 0.2f;

    private Vector2 inputVector = Vector2.zero;

    private void Start() {
        if(inputManager == null) {
            inputManager = PlayerInputManager.Instance;
        }
    }

    private void Update() {
        inputVector.x = inputManager.GetInputActions().Movement.Horizontal.ReadValue<float>();
        inputVector.y = inputManager.GetInputActions().Movement.Vertical.ReadValue<float>();

        UpdateAnimations();
    }

    private void FixedUpdate() {
        GroundCheck();
        Run();
        Hover();
        CheckJump();

        wasGrounded = isGrounded;
    }

    private void UpdateAnimations() {
        nateAnimator.SetBool("IsGrounded", isGrounded);
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

        Physics.SphereCast(groundCheckRay, groundCheckRadius, out RaycastHit rayHit, checkDistance, walkable, QueryTriggerInteraction.Ignore);
        if(rayHit.collider != null) {
            groundCheckHit = rayHit;
            isGrounded = true;
        }
        else {
            isGrounded = false;
        }

        if (!isGrounded) {
            timeSinceLastGrounded += Time.fixedDeltaTime;
        }
        else {
            timeSinceLastGrounded = 0f;
        }
    }

    #region Old broken spherecasting
    /*
        //A very very verbose way of finding the best ground hit
        //neceessary for the hovering system to work with steep slopes
        RaycastHit[] hits = Physics.SphereCastAll(groundCheckRay, groundCheckRadius, checkDistance, walkable, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        float highestDistance = float.MinValue;
        isGrounded = false;
        
        for (int i = 0; i < hits.Length; i++) {
            RaycastHit hit = hits[i];

            float angle = Vector3.Angle(hit.normal, transform.up);

            if (angle <= maxSlopeAngle) {

                float distance = transform.InverseTransformPoint(hit.point).y;

                Debug.Log($"Valid hit {i}  {distance}");

                if (distance > highestDistance) {
                    highestDistance = distance;
                    groundCheckHit = hit;
                    isGrounded = true;
                }
            }
        }
        */
    #endregion
    //Sphercastall gives different results than spherecast
    //Spherecast: 1st hit is used always, smooth
    //Spherecastall: higher hit is used, but is always the 2nd for some reason
    //despite logically being hit 2nd, after already colliding with the first
    //point, therefore being further away?

    //'floats' the player to avoid collision issues and better for slopes generally
    private void Hover() {
        if(isGrounded && jumpReady) {
            Vector3 surfaceNormal = groundCheckHit.normal;

            Vector3 relativeNormal = transform.InverseTransformDirection(surfaceNormal);
            float dot = Vector3.Dot(GetFlatVelLocal(), relativeNormal);

            Vector3 relativePoint = transform.InverseTransformPoint(groundCheckHit.point);
            float distance = relativePoint.y + colliderHeight * 0.5f;

            float verticalVelocity = GetSelfVel().y;
            float verticalFactor = Mathf.Abs(Mathf.Clamp(verticalVelocity, 1f, 10f));

            float force = (distance * hoverDistanceMutliplier * hoverForce * verticalFactor) - (dot * slopeOffsetCoefficient);
            force = Mathf.Clamp(force, -maxHoverForce, maxHoverForce);

            Vector3 newVelocity = GetFlatVelWrld() + GetSurfaceVel() + transform.up * force;
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
            playerCollider.center = new Vector3(0f, 0f, stepHeight / 2f);
        }
    }

    private void OnEnable() {
        inputManager.GetInputActions().Movement.Jump.performed += TryJump;
    }

    private void OnDisable() {
        inputManager.GetInputActions().Movement.Jump.performed -= TryJump;
    }
}