using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spin : MonoBehaviour {
    public float speed = 50f; // Base rotation speed
    private Vector3 rotationDirection;

    void Start() {
        ChangeDirection();
        StartCoroutine(ChangeDirectionRoutine());
        float radius = FindFirstObjectByType<PlanetGenerator>().GetPlanetRadius() * 2.1f;
        transform.localScale = new Vector3(radius, radius, radius);
    }

    void Update() {
        transform.Rotate(rotationDirection * speed * Time.deltaTime);
    }

    void ChangeDirection() {
        // Generate a new random rotation direction
        rotationDirection = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized; // Normalize to keep consistent speed
    }

    IEnumerator ChangeDirectionRoutine() {
        while (true) {
            yield return new WaitForSeconds(Random.Range(1f, 3f)); // Random time interval
            ChangeDirection();
        }
    }
}