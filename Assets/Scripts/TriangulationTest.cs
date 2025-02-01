using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangulationTest : MonoBehaviour
{
    [SerializeField, Range(1, 128)] int pointCount;
    [SerializeField, Range(0.0001f, 1)] float gizmoRadius;
    Vector3[] points;

    void Start()
    {
        points = new Vector3[pointCount];
        for (int i = 0; i < pointCount; i++) {
            points[i] = GenerateRandomPositionInSphere(10);
        }
    }

    void Update()
    {
        
    }

    Vector3 GenerateRandomPositionInSphere(float amplitude) {
        return new Vector3(Random.Range(-amplitude, amplitude), Random.Range(-amplitude, amplitude), Random.Range(-amplitude, amplitude)).normalized;
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        if (points != null) {
            foreach (Vector3 point in points) {
                Gizmos.DrawCube(point, gizmoRadius * Vector3.one);
            }
        }
    }

    struct Point {
        public Vector3 position;
        public Point[] neighbors;
        public Point(Vector3 position) {
            this.position = position;
            this.neighbors = new Point[0];
        }
    }
}
