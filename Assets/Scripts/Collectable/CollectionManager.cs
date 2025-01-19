using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectionManager : MonoBehaviour
{
    public static CollectionManager Instance { get; private set; }

    private void Awake() {
        if(Instance != null) {
            Destroy(this);
        }
        else {
            Instance = this;
            DontDestroyOnLoad(this);
        }
    }
}