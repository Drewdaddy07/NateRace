using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSim : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform player; 
    private ParticleSystem particleSystem;
    [SerializeField] Rigidbody rb;

    private void Start()
    {
        particleSystem = GetComponent<ParticleSystem>(); // Get the particle system attached to this GameObject
    }

    private void Update()
    {
        // Set the particle system's rotation to match the forward direction
        particleSystem.transform.rotation = Quaternion.identity;
        float speed = (rb.velocity.magnitude / 5) * 17;
        speed = Mathf.Clamp(speed, 17, 100);
        particleSystem.emissionRate = speed;
    }
}
