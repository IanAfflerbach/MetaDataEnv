using System.Collections;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using Unity.Jobs;
using UnityEngine;

public struct PointCloudInfo
{
    public ulong numPoints;
    public long pPrime;
    public ulong pointOffset;
    public ushort pointRecordLength;

    public double xScale, xOffset, xAvg;
    public double yScale, yOffset, yAvg;
    public double zScale, zOffset, zAvg;
};

public class BaseImporter
{
    //Singleton
    public static BaseImporter Instance { get; } = new BaseImporter();

    //MMF
    public string fileName;
    public MemoryMappedFile MMFile;

    [Range(1, 3)]
    public int vd = 3;
    public int cd = 4;
    public int[] vertexAxes;
    public int[] colorAxes;
    public Vector2[] colorRanges;
    public Mesh mesh;

    //Amounts;
    public readonly long MaxPoints = 2500000;
    public long NumMeshes;

    public JobHandle PermHandle;

    //Flags
    public bool Ready = false;

    //DataPacket
    public PointCloudInfo Info { get; protected set; }

    public virtual void SetAxes(int[] vAxes, Vector3[] cAxes)
    {
        vd = vAxes.Length;
        vertexAxes = new int[vAxes.Length];
        vAxes.CopyTo(vertexAxes, 0);

        cd = cAxes.Length;
        colorAxes = new int[cAxes.Length];
        cAxes.Select(x => (int)x.x).ToArray().CopyTo(colorAxes, 0);
        colorRanges = new Vector2[cAxes.Length];
        cAxes.Select(x => new Vector2(x.y, x.z)).ToArray().CopyTo(colorRanges, 0);
    }

    public virtual void Import(string name)
    {
    }

    public virtual void CreateFile()
    {
        MMFile = MemoryMappedFile.CreateFromFile(fileName, FileMode.Open);
    }

    public virtual void CloseFile()
    {
        MMFile.Dispose();
    }

    public virtual IEnumerator GenerateMeshes(Transform transform, Shader shader)
    {
        yield return new WaitUntil(() => true);
    }
}