using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private List<GameObject> segmentPrefabs = new List<GameObject>();

    private List<GameObject> activeSegments = new List<GameObject>();

    [Header("Settings")]
    [SerializeField] private float advanceDistance = 200f;
    [SerializeField] private float removeDistance = 205f;

    private float generatedDistance = 0f;
    private Transform previousEndTransform;

    private void Start() {
        previousEndTransform = transform;
    }

    private void FixedUpdate() {
        CheckPosition();
    }

    private void CheckPosition() {
        float distance = player.position.z + advanceDistance;

        if(distance > generatedDistance) {
            GenerateNewSegment();
        }

        //remove passed obstacles
        for (int i = activeSegments.Count - 1; i >= 0; i--) {
            GameObject segment = activeSegments[i];
            float segmentDistance = player.position.z - segment.transform.position.z;
            if (segmentDistance > removeDistance) {
                activeSegments.RemoveAt(i);
                Destroy(segment);
            }
        }
    }

    private void GenerateNewSegment() {
        //pick random segment
        int randomChoice = Random.Range(0, segmentPrefabs.Count);
        GameObject chosenPrefab = segmentPrefabs[randomChoice];
        GameObject segmentObject = Instantiate(chosenPrefab, transform);
        Segment segment = segmentObject.GetComponent<Segment>();

        segmentObject.transform.position = previousEndTransform.position;

        float segmentLength = segment.endTransform.localPosition.z;
        generatedDistance += segmentLength;

        activeSegments.Add(segmentObject);

        previousEndTransform = segment.endTransform;
    }
}