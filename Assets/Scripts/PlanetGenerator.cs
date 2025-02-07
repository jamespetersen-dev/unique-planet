using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class PlanetGenerator : MonoBehaviour
{
    [SerializeField, Range(2, 128)] int resolution; //  Number of vertices along an edge
                                                    //  Resolution max is 128 because ((128 - 1)^2) * 4 = 64516, highest possible resolution without going over max number of vertices
    [SerializeField, Range(0, 1)] float displacementFactor; //Displaces each vertex by 1 / resolution * displacementFactor / 2 in a random vector3 direction
    [SerializeField] Material material;
    [SerializeField, Range(0, 1)] float gizmoSize;
    [SerializeField] bool rodsDisplay;
    [SerializeField, Range(0, 80)] int rodSelect;

    public ComputeShader computeShader;
    public ComputeBuffer sectorBuffer;
    public ComputeBuffer rodBuffer;

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
        public int triangulation0;
        public int triangulation1;
        public int triangulation2;
        public int triangulation3;
        public int triangulation4;
        public int triangulation5;
        public int triangulation6;
        public int triangulation7;
        public int triangulation8;
        public int triangulation9;
        public int triangulation10;
        public int triangulation11;
        public int triangulation12;
        public int triangulation13;
        public int triangulation14;
        public int triangulation15;
        public int triangulation16;
        public int triangulation17;
        public int triangulation18;
        public int triangulation19;
        public int triangulation20;
        public float surfaceArea;
        public Vector3 center;
    }

    float phi;
    Vector3[] icosahedronVertices;
    private void Awake() {
        this.sectors = InitializeSectors();
        Compute();
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

    private void Compute() {
        int kernelHandle = computeShader.FindKernel("SubdivideSector");

        int rodCountPerSector = (int)Mathf.Pow(resolution - 1, 2);
        int rodCountTotal = rodCountPerSector * 20;
        sectorBuffer = new ComputeBuffer(20, sizeof(int) + sizeof(float) * 3 * 3);
        rodBuffer = new ComputeBuffer(rodCountTotal, sizeof(int) + sizeof(float) * 9 + sizeof(float) * 9 + sizeof(float) + sizeof(float) * 3 + sizeof(int) * 21);

        computeShader.SetBuffer(kernelHandle, "sectors", sectorBuffer);
        sectorBuffer.SetData(sectors);
        computeShader.SetBuffer(kernelHandle, "rods", rodBuffer);

        computeShader.SetInt("rodCountPerSector", rodCountPerSector);
        computeShader.SetInt("rodCountTotal", rodCountTotal);
        computeShader.SetInt("resolution", resolution);
        computeShader.SetFloat("displacementFactor", displacementFactor);

        int threadGroupSize = Mathf.CeilToInt(rodCountTotal * 20 / 64.0f);
        Debug.Log("Dispatch");
        computeShader.Dispatch(kernelHandle, threadGroupSize, 1, 1);
        Debug.Log("Dispatch Complete");

        SectorData[] updatedSectors = new SectorData[20];
        sectorBuffer.GetData(updatedSectors);
        sectors = updatedSectors;

        RodData[] updatedRods = new RodData[rodCountTotal];
        rodBuffer.GetData(updatedRods);
        rods = updatedRods;

        Debug.Log("Number of Rods: " + rods.Length);
    }

    private void OnDestroy() {
        if (sectorBuffer != null) sectorBuffer.Release();
        if (rodBuffer != null) rodBuffer.Release();
    }
    private void OnDisable() {
        if (sectorBuffer != null) sectorBuffer.Release();
        if (rodBuffer != null) rodBuffer.Release();
    }

    private void OnDrawGizmos() {
        if (sectors == null) return;

        Gizmos.color = Color.red;

        for (int i = 0; i < sectors.Length; i++) {
            Vector3[] cornerVertices = new Vector3[] { sectors[i].cornerVertex0, sectors[i].cornerVertex1, sectors[i].cornerVertex2 };
            Vector3 middleVertex = (cornerVertices[0] + cornerVertices[1] + cornerVertices[2]) / 3.0f;
            for (int j = 0; j < cornerVertices.Length; j++) {
                Gizmos.DrawCube(cornerVertices[j], Vector3.one * 1.0f / resolution * 2.0f * gizmoSize);
            }
            Handles.Label(middleVertex * 1.1f, $"S{i}");
        }

        if (resolution <= 16) {
            for (int i = 0; i < rods.Length; i++) {
                Vector3[] cornerVert = new Vector3[] { rods[i].cornerVertex0, rods[i].cornerVertex1, rods[i].cornerVertex2 };
                for (int j = 0; j < cornerVert.Length; j++) {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(cornerVert[j], Vector3.one * 1.0f / resolution * gizmoSize);
                }
            }
        }
        /*if (resolution <= 16) {
            for (int i = 0; i < rods.Length; i++) {
                Vector3[] cornerVert = new Vector3[] { rods[i].cornerVertex0, rods[i].cornerVertex1, rods[i].cornerVertex2 };
                for (int j = 0; j < cornerVert.Length; j++) {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(cornerVert[j], Vector3.one * 1.0f / resolution * gizmoSize);
                }
            }
        }*/
        /*if (resolution <= 16 && rodSelect < rods.Length) {
            Vector3[] cornerVert = new Vector3[] { rods[rodSelect].cornerVertex0, rods[rodSelect].cornerVertex1, rods[rodSelect].cornerVertex2 };
            for (int j = 0; j < cornerVert.Length; j++) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawCube(cornerVert[j], Vector3.one * 1.0f / resolution * gizmoSize);
                switch (j) {
                    case 0:
                        Debug.Log("Rod: " + rodSelect + " + " + rods[rodSelect].triangulation5 + ", Corner " + j + ", Index: " + rods[rodSelect].triangulation0 + " ( " + rods[rodSelect].triangulation3 + " - " + rods[rodSelect].triangulation4 + " ) Position: " + cornerVert[j]);
                        break;
                    case 1:
                        Debug.Log("Rod: " + rodSelect + " + " + rods[rodSelect].triangulation5 + ", Corner " + j + ", Index: " + rods[rodSelect].triangulation1 + " ( " + rods[rodSelect].triangulation3 + " - " + rods[rodSelect].triangulation4 + " ) Position: " + cornerVert[j]);
                        break;
                    case 2:
                        Debug.Log("Rod: " + rodSelect + " + " + rods[rodSelect].triangulation5 + ", Corner " + j + ", Index: " + rods[rodSelect].triangulation2 + " ( " + rods[rodSelect].triangulation3 + " - " + rods[rodSelect].triangulation4 + " ) Position: " + cornerVert[j]);
                        break;
                }

            }
        }*/


        //rods 360 & 480 experience the bug.
        /*if (rods.Length > 361) {
            Gizmos.color = Color.green;
            Gizmos.DrawCube(rods[359].cornerVertex0, Vector3.one * 1.0f / resolution * gizmoSize * 1.2f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawCube(rods[359].cornerVertex1, Vector3.one * 1.0f / resolution * gizmoSize * 1.2f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawCube(rods[359].cornerVertex2, Vector3.one * 1.0f / resolution * gizmoSize * 1.2f);

            Gizmos.color = Color.green;
            Gizmos.DrawCube(rods[361].cornerVertex0, Vector3.one * 1.0f / resolution * gizmoSize * 1.2f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawCube(rods[361].cornerVertex1, Vector3.one * 1.0f / resolution * gizmoSize * 1.2f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawCube(rods[361].cornerVertex2, Vector3.one * 1.0f / resolution * gizmoSize * 1.2f);

            Gizmos.color = Color.white;
            Gizmos.DrawCube(rods[360].cornerVertex0, Vector3.one * 1.0f / resolution * gizmoSize * 1.2f);
            Gizmos.color = Color.gray;
            Gizmos.DrawCube(rods[360].cornerVertex1, Vector3.one * 1.0f / resolution * gizmoSize * 1.2f);
            Gizmos.color = Color.black;
            Gizmos.DrawCube(rods[360].cornerVertex2, Vector3.one * 1.0f / resolution * gizmoSize * 1.2f);
        }*/

        if (rodsDisplay) {
            for (int i = 0; i < rods.Length; i++) {
                Gizmos.color = Color.cyan;
                Vector3[] cv = new Vector3[] { rods[i].cornerVertex0, rods[i].cornerVertex1, rods[i].cornerVertex2 };
                Vector3 center = (cv[0] + cv[1] + cv[2]) / 3.0f;
                Gizmos.DrawCube(center, Vector3.one * 1.0f / resolution * gizmoSize * 1.1f);
                //Handles.Label((cv[0] + cv[1] + cv[2]) / 3.0f * 1.1f, $"R{i}");
            }
        }
    }


}
