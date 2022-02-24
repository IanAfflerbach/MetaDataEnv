using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CSVModelLoader : MonoBehaviour
{
    private List<string> headers;
    private List<DataRecord> records;

    private Vector3 axes;
    private Mesh mesh;

    private void Start()
    {
        Initialize("Data/test.csv");
    }
    private void Initialize(string fileName)
    {

    }

    private void SetMesh()
    {
        mesh = new Mesh();
        mesh.name = "TestMesh";
        mesh.vertices = records.Select(r => new Vector3(r.GetValueFloat((int)axes.x), r.GetValueFloat((int)axes.y), r.GetValueFloat((int)axes.z))).ToArray();
        mesh.colors = Enumerable.Range(0, mesh.vertexCount).Select(x => Color.white).ToArray();
        mesh.triangles = Enumerable.Range(0, mesh.vertexCount + 3 - mesh.vertexCount % 3).ToList().Select(x => {
            if (x >= mesh.vertexCount) return x %= mesh.vertexCount;
            else return x;
        }).ToArray();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}
