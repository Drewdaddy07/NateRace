using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Transform target;

    [SerializeField] private Vector3 offset;

    [SerializeField] private float smoothing = 0.67f;

    private void LateUpdate() {
        StandardCameraMovement();
    }

    private void StandardCameraMovement() {
        Vector3 targetPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothing);

        transform.position = smoothedPosition;
    }
}