using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moon : MonoBehaviour {
    [Header("Moon Data")]
    [SerializeField, Range(0, 0.4f)] float size = 0.2f;
    [SerializeField, Range(0, 64)] float gravitationalRange = 4;
    [SerializeField, Range(0, 2)] float gravitationalStrength = 1;

    [Header("Orbit Data")]
    [SerializeField, Range(0, 36)] float orbitDistance = 0.5f;
    [SerializeField, Range(-30, 30)] float speed = 3.0f;
    [SerializeField, Range(0, 360)] float path;
    [SerializeField, Range(0, 180)] float inclination;
    [SerializeField, Range(0, 360)] float inclinationDirection;
    [SerializeField, Range(-0.3f, 0.3f)] float stretch;

    [Header("Gizmo")]
    [SerializeField] bool showOrbitGizmoDirection;

    private GameObject orbit;
    private bool isInitialized = false;
    private float currentAngle = 0f; // Tracks the moon's orbit position over time
    private float planetRadius;

    public void Awake() {
        if (!isInitialized) {
            if (orbit == null) {
                orbit = gameObject;
            }

            ApplyInclination();

            // Initialize the moon's position based on the "path" value
            currentAngle = path;
            UpdateMoonPosition();

            isInitialized = true;
        }
    }

    public void SetPlanetRadius(float planetRadius) {
        this.planetRadius = planetRadius;
    }

    void FixedUpdate() {
        currentAngle += speed * Time.deltaTime; // Increment angle over time
        UpdateMoonPosition();
    }

    private void UpdateMoonPosition() {
        float radians = currentAngle * Mathf.Deg2Rad;

        float semiMajorAxis = orbitDistance * (1 + stretch) * planetRadius;  // Stretch along X-axis
        float semiMinorAxis = orbitDistance / (1 + stretch) * planetRadius;  // Adjusted for ellipse

        float x = Mathf.Cos(radians) * semiMajorAxis;
        float z = Mathf.Sin(radians) * semiMinorAxis;

        Vector3 position = new Vector3(x, 0, z);
        position = Quaternion.Euler(inclination, 0, 0) * position; // Tilt orbit
        position = Quaternion.Euler(0, inclinationDirection, 0) * position; // Rotate orbit

        transform.position = orbit.transform.position + position;
        transform.localScale = Vector3.one * size * planetRadius;
    }

    public Vector3 GetPositionRelativeToPlanet(Transform planetTransform) {
        return planetTransform.InverseTransformPoint(transform.position);
    }

    public float GetSize() {
        return size * planetRadius;
    }
    public float GetGravityRange() {
        return gravitationalRange;
    }
    public float GetGravityStrength() {
        return gravitationalStrength;
    }

    public void SetOrbit(GameObject orbit) {
        this.orbit = orbit;
        transform.parent = orbit.transform;
        ApplyInclination();
        UpdateMoonPosition();
    }

    private void OnValidate() {
        if (orbit != null) {
            ApplyInclination();
            UpdateMoonPosition();
        }
    }

    private void ApplyInclination() {
        Quaternion inclinationRotation = Quaternion.Euler(inclination, 0, 0);
        Quaternion directionRotation = Quaternion.Euler(0, inclinationDirection, 0);
        //orbit.transform.rotation = directionRotation * inclinationRotation;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        int segments = 100;
        float angleStep = 360f / segments;
        Vector3 lastPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++) {
            float angle = i * angleStep * Mathf.Deg2Rad;

            float semiMajorAxis = orbitDistance * (1 + stretch) * planetRadius;
            float semiMinorAxis = orbitDistance / (1 + stretch) * planetRadius;

            float x = Mathf.Cos(angle) * semiMajorAxis;
            float z = Mathf.Sin(angle) * semiMinorAxis;

            Vector3 currentPoint = new Vector3(x, 0, z);
            currentPoint = Quaternion.Euler(inclination, 0, 0) * currentPoint;
            currentPoint = Quaternion.Euler(0, inclinationDirection, 0) * currentPoint;

            if (i > 0) {
                if (showOrbitGizmoDirection) {
                    Gizmos.DrawRay(lastPoint, (currentPoint - lastPoint) * 2);
                } else {
                    Gizmos.DrawLine(lastPoint, currentPoint);
                }
            }

            lastPoint = currentPoint;
        }

        float startAngle = path * Mathf.Deg2Rad;
        float startX = Mathf.Cos(startAngle) * orbitDistance * planetRadius;
        float startZ = Mathf.Sin(startAngle) * orbitDistance * planetRadius;

        Vector3 startPosition = new Vector3(startX, 0, startZ);
        startPosition = Quaternion.Euler(inclination, 0, 0) * startPosition;
        startPosition = Quaternion.Euler(0, inclinationDirection, 0) * startPosition;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(startPosition, 0.1f);
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, gravitationalRange * size * planetRadius);
        if (orbit != null) {
            Gizmos.DrawRay(transform.position, (orbit.transform.position - transform.position).normalized * gravitationalRange * size * planetRadius);
        }
    }
}
