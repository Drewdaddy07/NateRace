using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Collectable : MonoBehaviour
{
    public virtual void Collect() {
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other) {
        if(other.gameObject.layer == 3) {
            Collect();
        }
    }
}