using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class CSVImporter : BaseImporter
{
    //Singleton
    public static new CSVImporter Instance { get; } = new CSVImporter();

    private List<string> headers;
    private List<DataRecord> records;

    private Vector3 vertexAxes;
    private Vector4 colorAxes;
    private Mesh mesh;

    public override void Import(string name)
    {
        fileName = name;

        // Read Data from CSV File
        using (StreamReader reader = new StreamReader(fileName))
        {
            headers = reader.ReadLine().Split(',').ToList();
            records = new List<DataRecord>();

            while (!reader.EndOfStream)
            {
                string[] points = reader.ReadLine().Split(',');
                DataRecord record = new DataRecord(points.Length);

                for (int i = 0; i < points.Length; i++)
                    record.AddPoint(i, new DataPoint(points[i]));

                records.Add(record);
            }
        }

        NumMeshes = (long)Mathf.Ceil((float)records.Count / MaxPoints);

        // Set Axis Indices
        vertexAxes = new Vector3(0, 1, 2);
        colorAxes = new Vector4(3, 4, 5, 6);
        
        Ready = true;
    }

    public override IEnumerator GenerateMeshes(Transform transform, Shader shader)
    {
        long numMesh = NumMeshes;

        yield return new WaitUntil(() => Ready);

        for (long i = 0; i < numMesh; i++)
        {
            GameObject newMesh = new GameObject("Mesh " + i);
            newMesh.transform.parent = transform;
            newMesh.transform.rotation = Quaternion.identity;

            newMesh.AddComponent<MeshFilter>();
            newMesh.AddComponent<MeshRenderer>();
            PointCloudMesh pcMesh = newMesh.AddComponent<PointCloudMesh>();

            long numPoints = (records.Count < MaxPoints * (i + 1)) ? records.Count - (MaxPoints * (i)) : MaxPoints;

            NativeArray<Vector3> vertices = new NativeArray<Vector3>((int)numPoints, Allocator.TempJob);
            NativeArray<int> colors = new NativeArray<int>((int)numPoints, Allocator.TempJob);

            for (long j = 0; j < numPoints; j++)
            {
                vertices[(int)j] = new Vector3()
                {
                    x = records[(int)j].GetValueFloat((int)vertexAxes.x),
                    y = records[(int)j].GetValueFloat((int)vertexAxes.y),
                    z = records[(int)j].GetValueFloat((int)vertexAxes.z)
                };

                colors[(int)j] = 0;
                //colors[(int)j] ^= ((int)records[(int)j].GetValueFloat((int)colorAxes.x) << 24);
                //colors[(int)j] ^= ((int)records[(int)j].GetValueFloat((int)colorAxes.y) << 16);
                //colors[(int)j] ^= ((int)records[(int)j].GetValueFloat((int)colorAxes.z) << 8);
                //colors[(int)j] ^= ((int)records[(int)j].GetValueFloat((int)colorAxes.w) << 0);
            }

            pcMesh.Initialize(shader, vertices, colors);
        }
    }
}
