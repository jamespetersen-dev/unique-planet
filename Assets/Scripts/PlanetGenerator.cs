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
    [Header ("Startup Parameters")]
    [SerializeField, Range(2, 103)] int resolution; //  Number of vertices along an edge
                                                    //  Resolution max is 128 because ((128 - 1)^2) * 4 = 64516, highest possible resolution without going over max number of vertices
    [SerializeField, Range(0, 1)] float displacementFactor; //Displaces each vertex by 1 / resolution * displacementFactor / 2 in a random vector3 direction
    [SerializeField, Range(0, 1)] float rodLength;
    [SerializeField, Range(0.5f, 50)] float radius = 1; //By default it is a 1 radius. So 
    [SerializeField] Material material;
    public ComputeShader computeShader;

    [Header("Runtime Parameters")]
    [SerializeField, Range(-30.0f, 30)] float rotationRate;
    [SerializeField, Range(0, 1)] float outerRange; // Border Rods // Will be outerRange + innerRange
    [SerializeField, Range(0, 1)] float innerRange; // Hole
    [SerializeField, Range(-0.5f, 0.5f)] float borderHeight;
    [SerializeField, Range(0, 1)] float anchorHeight;
    [SerializeField, Range(0, 12)] int anchorRate;
    [SerializeField] bool attachCamera;

    public ComputeBuffer sectorBuffer;
    public ComputeBuffer rodBuffer;
    public ComputeBuffer triangleMeshBuffer;
    public ComputeBuffer vertexMeshBuffer;
    public ComputeBuffer normalMeshBuffer;
    public ComputeBuffer moonBuffer;

    GameObject planet;
    GameObject orbit;
    GameObject sectorContainer;
    GameObject[] sectorObjs;
    GameObject[] moonObjs;
    Moon[] moonMoon;

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
        public float gravitationalRange;
        public float gravitationalStrength;
    }

    private void Awake() {
        planet = new GameObject("Planet");
        planet.transform.position = transform.position;
        this.sectors = InitializeSectors();
        InitializeMoons();
        ComputePlanet();
        Render();
        PullRods();
        float totalSurfaceArea = 0;
        for (int i = 0; i < rods.Length; i++) {
            totalSurfaceArea += rods[i].surfaceArea;
        }
        Debug.Log("Rods: " + rods.Length + " - Surface Area: " + totalSurfaceArea);
    }

    private void FixedUpdate() {
        Rotate();
        GetMoonData();
        PullRods();
    }
    private void Update() {
        if (planet != null) {
            if (attachCamera && Camera.main.gameObject.transform.parent != planet.transform) {
                Camera.main.gameObject.transform.SetParent(planet.transform);
                Camera.main.gameObject.transform.localPosition = new Vector3(0, 0, -radius * 2);
                Camera.main.gameObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
                Camera.main.gameObject.transform.localScale = Vector3.one;
            }
            else if (!attachCamera && Camera.main.gameObject.transform.parent == planet.transform) {
                Camera.main.gameObject.transform.SetParent(null);
            }
        }
    }
    void Rotate() {
        planet.transform.Rotate(Vector3.up, rotationRate * Time.deltaTime);
    }
    public float GetPlanetRadius() {
        return radius;
    }
    public void GetMoonData() {
        for (int i = 0; i < moons.Length; i++) {
            moons[i].position = moonMoon[i].GetPositionRelativeToPlanet(planet.transform);
            moons[i].size = moonMoon[i].GetSize();
            moons[i].gravitationalRange = moonMoon[i].GetGravityRange();
            moons[i].gravitationalStrength = moonMoon[i].GetGravityStrength();
        }
        //Debug.Log(transform.localRotation.y);
    }
    private SectorData[] InitializeSectors() {
        float phi = (1 + Mathf.Sqrt(5)) / 2;
        Vector3[] icosahedronVertices = new Vector3[12] {
        new Vector3(-1,  phi, 0) * radius,
        new Vector3( 1,  phi, 0) * radius,
        new Vector3(-1, -phi, 0) * radius,
        new Vector3( 1, -phi, 0) * radius,
        new Vector3(0, -1,  phi) * radius,
        new Vector3(0,  1,  phi) * radius,
        new Vector3(0, -1, -phi) * radius,
        new Vector3(0,  1, -phi) * radius,
        new Vector3( phi, 0, -1) * radius,
        new Vector3( phi, 0,  1) * radius,
        new Vector3(-phi, 0, -1) * radius,
        new Vector3(-phi, 0,  1) * radius}
        ;

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
        moonMoon = FindObjectsOfType<Moon>();
        MoonData[] moonDatas = new MoonData[moonMoon.Length];
        moonObjs = new GameObject[moonMoon.Length];
        orbit = new GameObject("Orbit");
        orbit.transform.position = planet.transform.position;
        orbit.transform.parent = planet.transform;
        for (int i = 0; i < moonDatas.Length; i++) {
            moonDatas[i] = new MoonData();
            moonObjs[i] = moonMoon[i].gameObject;
            moonObjs[i].transform.parent = orbit.transform;
            moonMoon[i].SetPlanetRadius(radius);
            moonMoon[i].SetOrbit(orbit);
            moonDatas[i].position = moonObjs[i].transform.position;
            moonDatas[i].size = moonMoon[i].GetSize();
            moonDatas[i].gravitationalRange = moonMoon[i].GetGravityRange();
            moonDatas[i].gravitationalStrength = moonMoon[i].GetGravityStrength();
        }
        moons = moonDatas;
        computeShader.SetInt("moonCount", moons.Length);
        moonBuffer = new ComputeBuffer(moons.Length, sizeof(float) * 6);
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
        computeShader.SetFloat("rodLength", rodLength * radius);
        computeShader.SetFloat("outerRange", outerRange);
        computeShader.SetFloat("innerRange", innerRange);
        computeShader.SetFloat("borderHeight", borderHeight);
        computeShader.SetFloat("anchorHeight", anchorHeight);
        computeShader.SetInt("pylonsRate", anchorRate);
        computeShader.SetFloat("planetRadius", radius);

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
        int sectorLength = vertices.Length / sectors.Length;
        sectorContainer = new GameObject("Sectors");
        sectorContainer.transform.position = planet.transform.position;
        sectorContainer.transform.parent = planet.transform;

        for (int i = 0; i < sectors.Length; i++) {
            sectorObjs[i] = new GameObject("Sector: " + i);
            sectorObjs[i].transform.parent = sectorContainer.transform;
            sectorObjs[i].transform.position = planet.transform.position;

            Mesh mesh = new Mesh();

            Vector3[] sectorVertices = new Vector3[sectorLength];
            Array.Copy(vertices, i * sectorLength, sectorVertices, 0, sectorLength); 

            mesh.vertices = sectorVertices;

            mesh.triangles = triangles;

            Vector3[] sectorNormals = new Vector3[sectorLength];
            Array.Copy(vertices, i * sectorLength, sectorNormals, 0, sectorLength);
            mesh.normals = sectorNormals;

            mesh.RecalculateTangents();

            sectorObjs[i].AddComponent<MeshRenderer>().material = material;
            sectorObjs[i].AddComponent<MeshFilter>().mesh = mesh;
        }
    }

    private void PullRods() {
        if (moons.Length > 0) {
            //Debug.Log("Pulling Rods");
            int kernelHandle = computeShader.FindKernel("MoonAffect");

            moonBuffer.SetData(moons);
            computeShader.SetFloat("outerRange", outerRange);
            computeShader.SetFloat("innerRange", innerRange);
            computeShader.SetFloat("borderHeight", borderHeight);
            computeShader.SetFloat("anchorHeight", anchorHeight);
            computeShader.SetInt("pylonsRate", anchorRate);

            computeShader.SetBuffer(kernelHandle, "rods", rodBuffer);
            computeShader.SetBuffer(kernelHandle, "vertices", vertexMeshBuffer);
            computeShader.SetBuffer(kernelHandle, "moons", moonBuffer);

            int threadGroupSize = Mathf.CeilToInt((float)rods.Length / 64.0f);
            computeShader.Dispatch(kernelHandle, threadGroupSize, 1, 1);

            Vector3[] updatedVertices = new Vector3[vertices.Length];
            vertexMeshBuffer.GetData(updatedVertices);
            vertices = updatedVertices;

            int sectorLength = vertices.Length / sectors.Length;

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

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);

        Gizmos.color = new Color(1.0f, 0.5f, 0.0f);
        if (rods != null) {
            for (int i = 0; i < rods.Length; i++) {
                if (i > 100) {
                    break;
                }
                Gizmos.DrawRay(rods[i].center, rods[i].perpendicularity);
            }
        }
    }
}
