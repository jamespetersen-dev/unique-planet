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
    [SerializeField, Range(0, 1)] float rodLength;
    [SerializeField, Range(0, 8)] float gravitationalRange = 4;
    [SerializeField] Material material;

    public ComputeShader computeShader;
    public ComputeBuffer sectorBuffer;
    public ComputeBuffer rodBuffer;
    public ComputeBuffer triangleMeshBuffer;
    public ComputeBuffer vertexMeshBuffer;
    public ComputeBuffer normalMeshBuffer;
    public ComputeBuffer moonBuffer;

    GameObject[] sectorObjs;
    GameObject[] moonObjs;

    int[] triangles;
    Vector3[] vertices;
    Vector3[] normals;
    SectorData[] sectors;
    RodData[] rods;
    MoonData[] moons;

    //Vector3[]

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
        public Vector3 perpendicularity;
        public float displacement;
    }
    struct MoonData {
        public Vector3 position;
        public float size;
    }

    private void Awake() {
        this.sectors = InitializeSectors();
        InitializeMoons();
        ComputePlanet();
        Render();
    }

    private void Update() {
        if (CheckMoonDataChanged()) {
            PullRods();
        }
    }
    private bool CheckMoonDataChanged() {
        bool changed = false;
        for (int i = 0; i < moonObjs.Length; i++) {
            if (moonObjs[i].transform.position != moons[i].position) {
                changed = true;
                moons[i].position = moonObjs[i].transform.position;
            }
            if (moonObjs[i].transform.localScale.x != moons[i].size) {
                changed = true;
                moons[i].size = moonObjs[i].transform.localScale.x;
            }
        }
        return changed;
    }
    private SectorData[] InitializeSectors() {
        float phi = (1 + Mathf.Sqrt(5)) / 2;
        Vector3[] icosahedronVertices = new Vector3[12] {
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
    private void InitializeMoons() {
        moonObjs = GameObject.FindGameObjectsWithTag("Moon");
        MoonData[] moonDatas = new MoonData[moonObjs.Length];
        for (int i = 0; i < moonDatas.Length; i++) {
            moonDatas[i] = new MoonData();
            moonDatas[i].position = moonObjs[i].transform.position;
            moonDatas[i].size = moonObjs[i].transform.localScale.x;
        }
        moons = moonDatas;
        computeShader.SetInt("moonCount", moons.Length);
        //Debug.Log(moons[0].size + " " + moons[0].position);
        moonBuffer = new ComputeBuffer(moons.Length, sizeof(float) * 4);
    }

    private void ComputePlanet() {
        int kernelHandle0 = computeShader.FindKernel("SubdivideSector");
        int kernelHandle1 = computeShader.FindKernel("PopScatterSector");

        int rodCountPerSector = (int)Mathf.Pow(resolution - 1, 2);
        int rodCountTotal = rodCountPerSector * 20;
        sectorBuffer = new ComputeBuffer(20, sizeof(int) + sizeof(float) * 3 * 3);
        rodBuffer = new ComputeBuffer(rodCountTotal, sizeof(int) + sizeof(float) * 9 + sizeof(float) * 9 + sizeof(float) + sizeof(float) * 3 + sizeof(float) * 3 + sizeof(float));
        triangleMeshBuffer = new ComputeBuffer(rodCountPerSector * 21, sizeof(int));
        vertexMeshBuffer = new ComputeBuffer(rodCountTotal * 6, sizeof(float) * 3);
        normalMeshBuffer = new ComputeBuffer(rodCountTotal * 6, sizeof(float) * 3);

        computeShader.SetBuffer(kernelHandle0, "sectors", sectorBuffer);
        sectorBuffer.SetData(sectors);
        computeShader.SetBuffer(kernelHandle0, "rods", rodBuffer);
        computeShader.SetBuffer(kernelHandle0, "triangulation", triangleMeshBuffer);

        computeShader.SetInt("rodCountPerSector", rodCountPerSector);
        computeShader.SetInt("rodCountTotal", rodCountTotal);
        computeShader.SetInt("resolution", resolution);
        computeShader.SetFloat("displacementFactor", displacementFactor);
        computeShader.SetFloat("rodLength", rodLength);

        int threadGroupSize = Mathf.CeilToInt(rodCountTotal / 64.0f);
        computeShader.Dispatch(kernelHandle0, threadGroupSize, 1, 1);

        computeShader.SetBuffer(kernelHandle1, "rods", rodBuffer);
        computeShader.SetBuffer(kernelHandle1, "vertices", vertexMeshBuffer);
        computeShader.SetBuffer(kernelHandle1, "normals", normalMeshBuffer);
        computeShader.Dispatch(kernelHandle1, threadGroupSize, 1, 1);

        SectorData[] updatedSectors = new SectorData[20];
        sectorBuffer.GetData(updatedSectors);
        sectors = updatedSectors;

        RodData[] updatedRods = new RodData[rodCountTotal];
        rodBuffer.GetData(updatedRods);
        rods = updatedRods;

        int[] updatedTriangles = new int[rodCountPerSector * 21];
        triangleMeshBuffer.GetData(updatedTriangles);
        triangles = updatedTriangles;

        Vector3[] updatedVertices = new Vector3[rodCountTotal * 6];
        vertexMeshBuffer.GetData(updatedVertices);
        vertices = updatedVertices;

        Vector3[] updatedNormals = new Vector3[rodCountTotal * 6];
        normalMeshBuffer.GetData(updatedNormals);
        normals = updatedNormals;
    }

    private void Render() {

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
            mesh.triangles = triangles;

            Vector3[] sectorNormals = new Vector3[sectorLength]; // This should match the expected number of vertices per sector
            Array.Copy(vertices, i * sectorLength, sectorNormals, 0, sectorLength); // Copy the correct chunk of vertices for this sector
            mesh.normals = sectorNormals;

            mesh.RecalculateTangents();

            // Add components to the sector object
            sectorObjs[i].AddComponent<MeshRenderer>().material = material;
            sectorObjs[i].AddComponent<MeshFilter>().mesh = mesh;
        }
    }

    private void PullRods() {
        if (moons.Length > 0) {
            //Debug.Log("Pulling Rods");
            int kernelHandle = computeShader.FindKernel("MoonAffect");

            moonBuffer.SetData(moons);
            computeShader.SetFloat("gravitationalRange", gravitationalRange);

            computeShader.SetBuffer(kernelHandle, "rods", rodBuffer);
            computeShader.SetBuffer(kernelHandle, "vertices", vertexMeshBuffer);
            computeShader.SetBuffer(kernelHandle, "moons", moonBuffer);

            int threadGroupSize = Mathf.CeilToInt((float)rods.Length / 64.0f);
            computeShader.Dispatch(kernelHandle, threadGroupSize, 1, 1);

            Vector3[] updatedVertices = new Vector3[vertices.Length];
            vertexMeshBuffer.GetData(updatedVertices);
            vertices = updatedVertices;

            int sectorLength = vertices.Length / sectors.Length;

            // Assign updated vertex data to each sector
            for (int i = 0; i < sectors.Length; i++) {
                Mesh mesh = sectorObjs[i].GetComponent<MeshFilter>().mesh;

                Vector3[] sectorVertices = new Vector3[sectorLength];
                Array.Copy(vertices, i * sectorLength, sectorVertices, 0, sectorLength);

                mesh.vertices = sectorVertices;
                mesh.RecalculateBounds();
            }
        }
    }


    private void OnDestroy() {
        if (sectorBuffer != null) sectorBuffer.Release();
        if (rodBuffer != null) rodBuffer.Release();
        if (triangleMeshBuffer != null) triangleMeshBuffer.Release();
        if (vertexMeshBuffer != null) vertexMeshBuffer.Release();
        if (normalMeshBuffer != null) normalMeshBuffer.Release();
        if (moonBuffer != null) moonBuffer.Release();
    }
    private void OnDisable() {
        if (sectorBuffer != null) sectorBuffer.Release();
        if (rodBuffer != null) rodBuffer.Release();
        if (triangleMeshBuffer != null) triangleMeshBuffer.Release();
        if (vertexMeshBuffer != null) vertexMeshBuffer.Release();
        if (normalMeshBuffer != null) normalMeshBuffer.Release();
        if (moonBuffer != null) moonBuffer.Release();
    }


}
