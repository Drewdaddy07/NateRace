using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectionManager : MonoBehaviour
{
    public static CollectionManager Instance { get; private set; }

    [SerializeField] private AudioSource collectAudioSource;
    [SerializeField] private AudioClip coinCollectSound;

    private void Awake() {
        if(Instance != null) {
            Destroy(this);
        }
        else {
            Instance = this;
            DontDestroyOnLoad(this);
        }
    }

    public void OnCollected(Collectable collectable) {
        if(collectable is CollectableCoin) {
            collectAudioSource?.PlayOneShot(coinCollectSound);
        }
    }
}