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
    private List<DataTypes> headerTypes;
    private List<DataRecord> records;

    public override void Import(string name)
    {
        fileName = name;

        // Read Data from CSV File
        using (StreamReader reader = new StreamReader(fileName))
        {
            // Parse Header
            headers = reader.ReadLine().Split(',').ToList();

            // Parse Records
            records = new List<DataRecord>();
            while (!reader.EndOfStream)
            {
                string[] points = reader.ReadLine().Split(',');
                DataRecord record = new DataRecord(points.Length);

                for (int i = 0; i < points.Length; i++)
                    record.AddPoint(i, new DataPoint(points[i]));

                records.Add(record);
            }

            headerTypes = records[0].GetTypes();
        }

        NumMeshes = (long)Mathf.Ceil((float)records.Count / MaxPoints);
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
                    x = (vd >= 1) ? records[(int)j].GetValueFloat(vertexAxes[0]) : 0.0f,
                    y = (vd >= 2) ? records[(int)j].GetValueFloat(vertexAxes[1]) : 0.0f,
                    z = (vd >= 3) ? records[(int)j].GetValueFloat(vertexAxes[2]) : 0.0f
                };

                colors[(int)j] = 0;

                if (cd >= 1) colors[(int)j] ^= (int)(GetNormalizedColor((int)j, 0) * 255) << 24;
                if (cd >= 2) colors[(int)j] ^= (int)(GetNormalizedColor((int)j, 1) * 255) << 16;
                if (cd >= 3) colors[(int)j] ^= (int)(GetNormalizedColor((int)j, 2) * 255) << 8;
            }

            pcMesh.Initialize(shader, vertices, colors);
        }
    }

    private float GetNormalizedColor(int i, int j)
    {
        float val = records[i].GetValueFloat(colorAxes[j]);
        float min = colorRanges[j].x;
        float max = colorRanges[j].y;

        if (val > max) val = max;
        if (val < min) val = min;

        return (val - min) / (max - min);
    }
}
