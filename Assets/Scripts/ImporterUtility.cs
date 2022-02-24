using System.Collections;
using System.IO;
using System.IO.MemoryMappedFiles;
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

    //Amounts;
    public readonly long MaxPoints = 2500000;
    public long NumMeshes;

    public JobHandle PermHandle;

    //Flags
    public bool Ready = false;

    //DataPacket
    public PointCloudInfo Info { get; protected set; }

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