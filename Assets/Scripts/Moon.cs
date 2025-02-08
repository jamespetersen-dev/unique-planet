using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moon : MonoBehaviour {
    [SerializeField, Range(0, 12)] float orbitDistance = 0.5f;
    [SerializeField, Range(0, 0.4f)] float size = 0.2f;
    [SerializeField, Range(-30, 30)] float speed = 3.0f;
    [SerializeField, Range(0, 360)] float path;
    [SerializeField, Range(0, 180)] float inclination;
    [SerializeField, Range(0, 360)] float inclinationDirection;
    [SerializeField] bool showOrbitGizmoDirection;

    private GameObject orbit;
    private bool isInitialized = false;

    public void Awake() {
        if (!isInitialized) {
            orbit = new GameObject("Orbit of " + name);
            orbit.transform.position = transform.position;
            transform.parent = orbit.transform;

            ApplyInclination();

            UpdateMoonPosition();

            isInitialized = true;
        }
    }

    void Update() {
        orbit.transform.Rotate(Vector3.up, speed * Time.deltaTime);

        UpdateMoonPosition();
    }

    private void UpdateMoonPosition() {
        float radians = path * Mathf.Deg2Rad;

        float x = Mathf.Cos(radians) * orbitDistance;
        float z = Mathf.Sin(radians) * orbitDistance;

        transform.localPosition = new Vector3(x, 0, z);
        transform.localScale = Vector3.one * size;
    }
    public Vector3 GetPositionRelativeToPlanet(Transform planetTransform) {
        return planetTransform.InverseTransformPoint(transform.position);
    }
    public float GetSize() {
        return size;
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

        orbit.transform.rotation = directionRotation * inclinationRotation;
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.cyan;

        int segments = 100;

        float angleStep = 360f / segments;

        Vector3 lastPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++) {
            float angle = i * angleStep * Mathf.Deg2Rad;

            float x = Mathf.Cos(angle) * orbitDistance;
            float z = Mathf.Sin(angle) * orbitDistance;

            Vector3 currentPoint = new Vector3(x, 0, z);

            currentPoint = Quaternion.Euler(inclination, 0, 0) * currentPoint;
            currentPoint = Quaternion.Euler(0, inclinationDirection, 0) * currentPoint;

            if (i > 0) {
                if (showOrbitGizmoDirection) {
                    Gizmos.DrawRay(lastPoint, (currentPoint - lastPoint) * 2);
                }
                else
                {
                    Gizmos.DrawLine(lastPoint, currentPoint);
                }
            }

            lastPoint = currentPoint;
        }

        float startAngle = path * Mathf.Deg2Rad;
        float startX = Mathf.Cos(startAngle) * orbitDistance;
        float startZ = Mathf.Sin(startAngle) * orbitDistance;

        Vector3 startPosition = new Vector3(startX, 0, startZ);

        startPosition = Quaternion.Euler(inclination, 0, 0) * startPosition;
        startPosition = Quaternion.Euler(0, inclinationDirection, 0) * startPosition;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(startPosition, 0.1f);
    }
}
