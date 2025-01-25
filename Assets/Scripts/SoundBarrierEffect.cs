using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundBarrierEffect : MonoBehaviour
{
    [SerializeField] private Rigidbody playerRB;
    [SerializeField] private float offset;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private ParticleSystem particles;

    private void LateUpdate() {
        float speed = movement.GetFlatVelWrld().magnitude;
        if (speed > movement.runSpeedMinMax.y - 1f) {
            if (!particles.isPlaying) {
                particles.Play();
            }
            Vector3 direction = playerRB.velocity.normalized;
            transform.position = playerRB.transform.position + (direction * offset);
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = targetRotation;
        }
        else {
            if (!particles.isStopped) {
                particles.Stop();
            }
        }
    }
}