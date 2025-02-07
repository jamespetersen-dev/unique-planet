using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class PlanetGenerator : MonoBehaviour
{
    [SerializeField, Range(2, 103)] int resolution; //  Number of vertices along an edge
                                                    //  Resolution max is 128 because ((128 - 1)^2) * 4 = 64516, highest possible resolution without going over max number of vertices
    [SerializeField, Range(0, 1)] float displacementFactor; //Displaces each vertex by 1 / resolution * displacementFactor / 2 in a random vector3 direction
    [SerializeField] Material material;
    [SerializeField, Range(0, 1)] float gizmoSize;
    [SerializeField] bool rodsDisplay;
    [SerializeField, Range(0, 80)] int rodSelect;

    public ComputeShader computeShader;
    public ComputeBuffer sectorBuffer;
    public ComputeBuffer rodBuffer;
    public ComputeBuffer triangleMeshBuffer;
    public ComputeBuffer vertexMeshBuffer;

    GameObject[] sectorObjs;

    int[] triangles;
    Vector3[] vertices;
    SectorData[] sectors;
    RodData[] rods;

    struct SectorData { //  Rod Count (resolution-1)^2
        public int id;
        public Vector3 cornerVertex0;               //  3 corner vertices of the sector
        public Vector3 cornerVertex1;
        public Vector3 cornerVertex2;
    }
    struct RodData {
        public int sector;
        public Vector3 cornerVertex0;   //  3 Corner Vertices
        public Vector3 cornerVertex1;
        public Vector3 cornerVertex2;
        public Vector3 borderVertex0;   //  3 Border Vertices
        public Vector3 borderVertex1;
        public Vector3 borderVertex2;
        public float surfaceArea;
        public Vector3 center;
    }

    float phi;
    Vector3[] icosahedronVertices;
    private void Awake() {
        this.sectors = InitializeSectors();
        Compute();
        Render();
    }
    private SectorData[] InitializeSectors() {
        phi = (1 + Mathf.Sqrt(5)) / 2;
        icosahedronVertices = new Vector3[12] {
        new Vector3(-1,  phi, 0),
        new Vector3( 1,  phi, 0),
        new Vector3(-1, -phi, 0),
        new Vector3( 1, -phi, 0),
        new Vector3(0, -1,  phi),
        new Vector3(0,  1,  phi),
        new Vector3(0, -1, -phi),
        new Vector3(0,  1, -phi),
        new Vector3( phi, 0, -1),
        new Vector3( phi, 0,  1),
        new Vector3(-phi, 0, -1),
        new Vector3(-phi, 0,  1)
    };

        SectorData[] sector = new SectorData[20];

        int numberOfRods = (int)Mathf.Pow(resolution - 1, 2);

        sector[0] = new SectorData { id = 0, cornerVertex0 = icosahedronVertices[0], cornerVertex1 = icosahedronVertices[11], cornerVertex2 = icosahedronVertices[5] };
        sector[1] = new SectorData { id = 1, cornerVertex0 = icosahedronVertices[0], cornerVertex1 = icosahedronVertices[5], cornerVertex2 = icosahedronVertices[1] };
        sector[2] = new SectorData { id = 2, cornerVertex0 = icosahedronVertices[0], cornerVertex1 = icosahedronVertices[1], cornerVertex2 = icosahedronVertices[7] };
        sector[3] = new SectorData { id = 3, cornerVertex0 = icosahedronVertices[0], cornerVertex1 = icosahedronVertices[7], cornerVertex2 = icosahedronVertices[10] };
        sector[4] = new SectorData { id = 4, cornerVertex0 = icosahedronVertices[0], cornerVertex1 = icosahedronVertices[10], cornerVertex2 = icosahedronVertices[11] };
        sector[5] = new SectorData { id = 5, cornerVertex0 = icosahedronVertices[1], cornerVertex1 = icosahedronVertices[5], cornerVertex2 = icosahedronVertices[9] };
        sector[6] = new SectorData { id = 6, cornerVertex0 = icosahedronVertices[5], cornerVertex1 = icosahedronVertices[11], cornerVertex2 = icosahedronVertices[4] };
        sector[7] = new SectorData { id = 7, cornerVertex0 = icosahedronVertices[11], cornerVertex1 = icosahedronVertices[10], cornerVertex2 = icosahedronVertices[2] };
        sector[8] = new SectorData { id = 8, cornerVertex0 = icosahedronVertices[10], cornerVertex1 = icosahedronVertices[7], cornerVertex2 = icosahedronVertices[6] };
        sector[9] = new SectorData { id = 9, cornerVertex0 = icosahedronVertices[7], cornerVertex1 = icosahedronVertices[1], cornerVertex2 = icosahedronVertices[8] };
        sector[10] = new SectorData { id = 10, cornerVertex0 = icosahedronVertices[3], cornerVertex1 = icosahedronVertices[9], cornerVertex2 = icosahedronVertices[4] };
        sector[11] = new SectorData { id = 11, cornerVertex0 = icosahedronVertices[3], cornerVertex1 = icosahedronVertices[4], cornerVertex2 = icosahedronVertices[2] };
        sector[12] = new SectorData { id = 12, cornerVertex0 = icosahedronVertices[3], cornerVertex1 = icosahedronVertices[2], cornerVertex2 = icosahedronVertices[6] };
        sector[13] = new SectorData { id = 13, cornerVertex0 = icosahedronVertices[3], cornerVertex1 = icosahedronVertices[6], cornerVertex2 = icosahedronVertices[8] };
        sector[14] = new SectorData { id = 14, cornerVertex0 = icosahedronVertices[3], cornerVertex1 = icosahedronVertices[8], cornerVertex2 = icosahedronVertices[9] };
        sector[15] = new SectorData { id = 15, cornerVertex0 = icosahedronVertices[4], cornerVertex1 = icosahedronVertices[9], cornerVertex2 = icosahedronVertices[5] };
        sector[16] = new SectorData { id = 16, cornerVertex0 = icosahedronVertices[2], cornerVertex1 = icosahedronVertices[4], cornerVertex2 = icosahedronVertices[11] };
        sector[17] = new SectorData { id = 17, cornerVertex0 = icosahedronVertices[6], cornerVertex1 = icosahedronVertices[2], cornerVertex2 = icosahedronVertices[10] };
        sector[18] = new SectorData { id = 18, cornerVertex0 = icosahedronVertices[8], cornerVertex1 = icosahedronVertices[6], cornerVertex2 = icosahedronVertices[7] };
        sector[19] = new SectorData { id = 19, cornerVertex0 = icosahedronVertices[9], cornerVertex1 = icosahedronVertices[8], cornerVertex2 = icosahedronVertices[1] };

        return sector;
    }

    //To Do:
    //  Separate Triangulation from Rods
    //  Triangulation Data is the same for all meshes
    //  Generate Triangulation Data when first Subdividing
    //  Only need to save the data for one of the meshes for triangulation
    //  Generate Vertex Data

    private void Compute() {
        int kernelHandle0 = computeShader.FindKernel("SubdivideSector");
        int kernelHandle1 = computeShader.FindKernel("PopScatterSector");

        int rodCountPerSector = (int)Mathf.Pow(resolution - 1, 2);
        int rodCountTotal = rodCountPerSector * 20;
        sectorBuffer = new ComputeBuffer(20, sizeof(int) + sizeof(float) * 3 * 3);
        rodBuffer = new ComputeBuffer(rodCountTotal, sizeof(int) + sizeof(float) * 9 + sizeof(float) * 9 + sizeof(float) + sizeof(float) * 3);
        triangleMeshBuffer = new ComputeBuffer(rodCountPerSector * 3, sizeof(int));
        vertexMeshBuffer = new ComputeBuffer(rodCountTotal * 3, sizeof(float) * 3);

        computeShader.SetBuffer(kernelHandle0, "sectors", sectorBuffer);
        sectorBuffer.SetData(sectors);
        computeShader.SetBuffer(kernelHandle0, "rods", rodBuffer);
        computeShader.SetBuffer(kernelHandle0, "triangulation", triangleMeshBuffer);

        computeShader.SetInt("rodCountPerSector", rodCountPerSector);
        computeShader.SetInt("rodCountTotal", rodCountTotal);
        computeShader.SetInt("resolution", resolution);
        computeShader.SetFloat("displacementFactor", displacementFactor);

        int threadGroupSize = Mathf.CeilToInt(rodCountTotal * 20 / 64.0f);
        computeShader.Dispatch(kernelHandle0, threadGroupSize, 1, 1);

        computeShader.SetBuffer(kernelHandle1, "sectors", sectorBuffer);
        computeShader.SetBuffer(kernelHandle1, "rods", rodBuffer);
        computeShader.SetBuffer(kernelHandle1, "vertices", vertexMeshBuffer);
        computeShader.Dispatch(kernelHandle1, threadGroupSize, 1, 1);

        SectorData[] updatedSectors = new SectorData[20];
        sectorBuffer.GetData(updatedSectors);
        sectors = updatedSectors;

        RodData[] updatedRods = new RodData[rodCountTotal];
        rodBuffer.GetData(updatedRods);
        rods = updatedRods;

        int[] updatedTriangles = new int[rodCountPerSector * 3];
        triangleMeshBuffer.GetData(updatedTriangles);
        triangles = updatedTriangles;

        Vector3[] updatedVertices = new Vector3[rodCountTotal * 3];
        vertexMeshBuffer.GetData(updatedVertices);
        vertices = updatedVertices;
    }

    private void Render() {

        // Returned Buffers from the compute will have a single buffer with the vertices and a single buffer with the triangles
        // Need to separate those out into 20 different arrays
        //Alternatively...
        //  Triangulation and Vertex Data will be the exact same for all of the different meshes.
        //  Reuse

        sectorObjs = new GameObject[sectors.Length];
        int sectorLength = vertices.Length / sectors.Length; // Divide vertices equally among sectors

        for (int i = 0; i < sectors.Length; i++) {
            sectorObjs[i] = new GameObject("Sector: " + i);
            sectorObjs[i].transform.parent = transform;
            sectorObjs[i].transform.position = transform.position;

            Mesh mesh = new Mesh();

            // Assuming triangles is correctly defined elsewhere for the sector
            Vector3[] sectorVertices = new Vector3[sectorLength]; // This should match the expected number of vertices per sector
            Array.Copy(vertices, i * sectorLength, sectorVertices, 0, sectorLength); // Copy the correct chunk of vertices for this sector

            mesh.vertices = sectorVertices;

            // Ensure triangles and vertex count match
            mesh.triangles = triangles; // Make sure this is correctly set for each sector
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            // Add components to the sector object
            sectorObjs[i].AddComponent<MeshRenderer>().material = material;
            sectorObjs[i].AddComponent<MeshFilter>().mesh = mesh;
        }
    }

    private void OnDestroy() {
        if (sectorBuffer != null) sectorBuffer.Release();
        if (rodBuffer != null) rodBuffer.Release();
        if (triangleMeshBuffer != null) triangleMeshBuffer.Release();
        if (vertexMeshBuffer != null) vertexMeshBuffer.Release();
    }
    private void OnDisable() {
        if (sectorBuffer != null) sectorBuffer.Release();
        if (rodBuffer != null) rodBuffer.Release();
        if (triangleMeshBuffer != null) triangleMeshBuffer.Release();
        if (vertexMeshBuffer != null) vertexMeshBuffer.Release();
    }


}
