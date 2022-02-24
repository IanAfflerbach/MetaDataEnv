using System.IO;
using System.IO.MemoryMappedFiles;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Collections;

public enum LASSettings { NONE = 0x0, PERMUTE = 0x1 }

public struct LASThread
{
    public PointCloudMesh mesh;
    public LASParsingJob job;
    public JobHandle handle;
}

//For LAS 1.2
public sealed class LASImporter : BaseImporter
{
    //Singleton
    public static new LASImporter Instance { get; } = new LASImporter();

    //Settings Flags
    private int flags = 0x0;

    //Header Data
    private char[] fileSignature;
    private ushort fileSourceID;
    private ushort globalEncoding;
    private ulong projectIDA;
    private ulong projectIDB;
    private int versionMajor;
    private int versionMinor;
    private char[] systemIdentifier;
    private char[] generatingSoftware;
    private ushort dayofYearCreated;
    private ushort yearCreated;
    private ushort headerSize;
    private ulong pointOffset;
    private ulong numVarRecords;
    private int pointDataFormat;
    private ushort pointRecordLength;
    private ulong numPoints;
    private ulong[] numPointsByReturn;

    private double xScale;
    private double yScale;
    private double zScale;
    private double xOffset;
    private double yOffset;
    private double zOffset;

    private double xMax;
    private double xMin;
    private double yMax;
    private double yMin;
    private double zMax;
    private double zMin;

    private ulong waveformDataRecord;

    //Extra Data
    private double xAvg;
    private double yAvg;
    private double zAvg;

    //Permutation
    private long pPrime; //WIP
    public List<long[]> Permutations;

    public void LoadSettings(LASSettings[] settings)
    {
        flags = 0x0;

        foreach (LASSettings s in settings)
            flags |= (int)s;
    }

    public override void Import(string name)
    {
        fileName = name;

        using (BinaryReader file = new BinaryReader(File.Open(fileName, FileMode.Open)))
        {
            //Load Header Information
            fileSignature = file.ReadChars(4);
            fileSourceID = file.ReadUInt16();
            globalEncoding = file.ReadUInt16();
            projectIDA = file.ReadUInt64();
            projectIDB = file.ReadUInt64();
            versionMajor = file.ReadChar();
            versionMinor = file.ReadChar();
            systemIdentifier = file.ReadChars(32);
            generatingSoftware = file.ReadChars(32);
            dayofYearCreated = file.ReadUInt16();
            yearCreated = file.ReadUInt16();
            headerSize = file.ReadUInt16();
            pointOffset = file.ReadUInt32();
            numVarRecords = file.ReadUInt32();
            pointDataFormat = file.ReadChar();
            pointRecordLength = file.ReadUInt16();
            numPoints = file.ReadUInt32();

            numPointsByReturn = new ulong[5];
            for (int i = 0; i < 5; i++)
                numPointsByReturn[i] = file.ReadUInt32();

            xScale = file.ReadDouble();
            yScale = file.ReadDouble();
            zScale = file.ReadDouble();
            xOffset = file.ReadDouble();
            yOffset = file.ReadDouble();
            zOffset = file.ReadDouble();

            xMax = file.ReadDouble();
            xMin = file.ReadDouble();

            yMax = file.ReadDouble();
            yMin = file.ReadDouble();
            zMax = file.ReadDouble();
            zMin = file.ReadDouble();

            //waveformDataRecord = file.ReadUInt64();
        }

        xAvg = (xMax + xMin) / 2d;
        yAvg = (yMax + yMin) / 2d;
        zAvg = (zMax + zMin) / 2d;

        pPrime = 253638631;
        NumMeshes = (long)Mathf.Ceil((float)numPoints / MaxPoints);

        Info = new PointCloudInfo()
        {
            numPoints = numPoints,
            pPrime = pPrime,
            pointOffset = pointOffset,
            pointRecordLength = pointRecordLength,
            xOffset = xOffset,
            xScale = xScale,
            xAvg = xAvg,
            yOffset = yOffset,
            yScale = yScale,
            yAvg = yAvg,
            zOffset = zOffset,
            zScale = zScale,
            zAvg = zAvg
        };

        if (CheckSetting(LASSettings.PERMUTE))
        {
            LASPermuteJob permJob = new LASPermuteJob()
            {
                points = (long)numPoints,
                pPrime = pPrime
            };
            PermHandle = permJob.Schedule();
        }
        else Ready = true;
    }

    public bool CheckSetting(LASSettings setting)
    {
        return ((flags & (int)setting) != 0x0);
    }

    public override IEnumerator GenerateMeshes(Transform transform, Shader shader)
    {
        long numMesh = NumMeshes;
        const long maxJobs = 10;
        Queue<LASThread> jobs = new Queue<LASThread>();

        yield return new WaitUntil(() => Ready);
        PermHandle.Complete();
        CreateFile();

        for (long i = 0; i < numMesh; i++)
        {
            GameObject newMesh = new GameObject("Mesh " + i);
            newMesh.transform.parent = transform;
            newMesh.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

            newMesh.AddComponent<MeshFilter>();
            newMesh.AddComponent<MeshRenderer>();
            PointCloudMesh pcMesh = newMesh.AddComponent<PointCloudMesh>();

            NativeArray<Vector3> vertices = new NativeArray<Vector3>((int)MaxPoints, Allocator.TempJob);
            NativeArray<int> colors = new NativeArray<int>((int)MaxPoints, Allocator.TempJob);

            LASParsingJob parse = new LASParsingJob()
            {
                vertices = vertices,
                colors = colors,
                index = i,
                num = MaxPoints,
                info = Info
            };

            LASThread thread = new LASThread()
            {
                mesh = pcMesh,
                job = parse,
                handle = parse.Schedule()
            };

            jobs.Enqueue(thread);

            if (i < maxJobs - 1) continue;

            LASThread newThread = jobs.Dequeue();
            yield return new WaitUntil(() => newThread.handle.IsCompleted);
            newThread.handle.Complete();
            newThread.mesh.Initialize(shader, newThread.job.vertices, newThread.job.colors);
        }

        while (jobs.Count > 0)
        {
            LASThread newThread = jobs.Dequeue();
            yield return new WaitUntil(() => newThread.handle.IsCompleted);
            newThread.handle.Complete();
            newThread.mesh.Initialize(shader, newThread.job.vertices, newThread.job.colors);
        }

        CloseFile();
    }
}

public struct LASPermuteJob : IJob
{
    public long points;
    public long pPrime;

    public void Execute()
    {
        long max = LASImporter.Instance.MaxPoints;
        long numMesh = LASImporter.Instance.NumMeshes;
        List<long[]> permList = new List<long[]>();

        for (int i = 0; i < numMesh; i++)
        {
            permList.Add(new long[max]);

            for (int j = 0; j < max; j++)
            {
                long p;
                long k = i * max + j;

                if (k <= pPrime / 2) p = (k * k) % pPrime;
                else if (k < pPrime) p = pPrime - (k * k) % pPrime;
                else p = k;

                permList[i][j] = p;
            }
        }

        LASImporter.Instance.Permutations = permList;
        LASImporter.Instance.Ready = true;
    }
}

public struct LASParsingJob : IJob
{
    public NativeArray<Vector3> vertices;
    public NativeArray<int> colors;

    public long index;
    public long num;

    public PointCloudInfo info;

    public void Execute()
    {
        LASImporter las = LASImporter.Instance;
        bool permuteBool = las.CheckSetting(LASSettings.PERMUTE);
        long[] permute = permuteBool ? las.Permutations[(int)index] : null;

        long newOffset = (long)info.pointOffset + info.pointRecordLength * num * index;
        if (num * (index + 1) >= (long)info.numPoints) num = (long)info.numPoints - num * index;

        using (var record = las.MMFile.CreateViewAccessor(permuteBool ? (long)info.pointOffset : newOffset, 0, MemoryMappedFileAccess.Read))
        {
            for (long i = 0; i < num; i++)
            {
                long pIndex = permuteBool ? permute[i] : i;
                long offset = pIndex * info.pointRecordLength;

                long x = record.ReadInt32(offset + 0);
                long y = record.ReadInt32(offset + 4);
                long z = record.ReadInt32(offset + 8);

                ushort r = record.ReadUInt16(offset + 28);
                ushort g = record.ReadUInt16(offset + 30);
                ushort b = record.ReadUInt16(offset + 32);

                vertices[(int)i] = new Vector3()
                {
                    x = (float)(x * info.xScale + info.xOffset - info.xAvg),
                    y = (float)(y * info.yScale + info.yOffset - info.yAvg),
                    z = (float)(z * info.zScale + info.zOffset - info.zAvg)
                };

                byte br = (byte)(((float)r / 0xFFFF) * 255f);
                byte bg = (byte)(((float)g / 0xFFFF) * 255f);
                byte bb = (byte)(((float)b / 0xFFFF) * 255f);
                byte ba = (byte)255f;

                colors[(int)i] = (br << 24) ^ (bg << 16) ^ (bb << 8) ^ (ba);
            }
        }
    }
}