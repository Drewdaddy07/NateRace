using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Attachments")]
    [SerializeField] private Rigidbody rb;

    [Header("Settings")]
    [SerializeField] private Vector3 runningDirection = Vector3.forward;

    [Header("Basic Stats")]
    [SerializeField] private float runSpeed;
    [SerializeField] private float horizontalSpeed = 2f;

    private Vector2 inputVector = Vector2.zero;

    private PlayerInputManager inputManager;

    private void Start() {
        inputManager = PlayerInputManager.Instance;
    }

    private void Update() {
        inputVector.x = inputManager.GetInputActions().Movement.Horizontal.ReadValue<float>();
        inputVector.y = inputManager.GetInputActions().Movement.Vertical.ReadValue<float>();
    }

    private void FixedUpdate() {
        Run();
    }

    private void Run() {
        //Add running forces
        Vector3 force = runningDirection * runSpeed;

        Vector3 strafeDirection = Vector3.Cross(runningDirection, Vector3.up).normalized;

        force += strafeDirection * horizontalSpeed * -inputVector.x;

        force = new Vector3(force.x, rb.velocity.y, force.z);

        rb.velocity = force;

        //Speed control
    }


}