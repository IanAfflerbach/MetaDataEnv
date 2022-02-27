using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class PointCloudMesh : MonoBehaviour
{
    private delegate void UpdateEvent();
    private UpdateEvent update;

    private MeshFilter filter;
    private MeshRenderer render;
    private ComputeBuffer posBuf;
    private ComputeBuffer colBuf;

    private Shader shader;
    private NativeArray<Vector3> vertices;
    private NativeArray<int> colors;

    public void Initialize(Shader shade, NativeArray<Vector3> verts, NativeArray<int> cols)
    {
        shader = shade;
        vertices = verts;
        colors = cols;

        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        const int pointRatio = 1;

        filter = gameObject.GetComponent<MeshFilter>();
        render = gameObject.GetComponent<MeshRenderer>();

        //Set Compute Buffers
        int vNum = vertices.Length;
        posBuf = new ComputeBuffer(vNum, 12, ComputeBufferType.Default);
        colBuf = new ComputeBuffer(vNum, 4, ComputeBufferType.Default);

        //Get new Vertices
        vNum /= pointRatio;
        NativeArray<Vector3> newVertices = new NativeArray<Vector3>(vNum, Allocator.TempJob);
        VerticeJob getVerts = new VerticeJob()
        {
            num = vNum,
            pointRatio = pointRatio,
            verts = vertices,
            newVerts = newVertices
        };

        JobHandle vertHandle = getVerts.Schedule();
        yield return new WaitUntil(() => vertHandle.IsCompleted);
        vertHandle.Complete();

        filter.mesh = new Mesh();
        filter.mesh.SetVertices(newVertices.ToList());
        newVertices.Dispose();

        vNum /= pointRatio;
        NativeArray<Vector4> newColors = new NativeArray<Vector4>(vNum, Allocator.TempJob);
        ColorJob getColors = new ColorJob()
        {
            num = vNum,
            cols = colors,
            newCols = newColors
        };

        JobHandle colHandle = getColors.Schedule();
        yield return new WaitUntil(() => colHandle.IsCompleted);
        colHandle.Complete();

        filter.mesh.SetColors(newColors);
        newColors.Dispose();

        //Get Triangles
        vNum = filter.mesh.vertexCount;
        //vNum -= vNum % 3;
        int extra = (vNum % 3 == 0) ? 0 : 3 - vNum % 3;
        NativeArray<int> triangles = new NativeArray<int>(vNum + extra, Allocator.TempJob);
        TriangleJob getTri = new TriangleJob()
        {
            num = vNum,
            extra = extra,
            triangles = triangles
        };

        JobHandle triHandle = getTri.Schedule();
        yield return new WaitUntil(() => triHandle.IsCompleted);
        triHandle.Complete();

        filter.mesh.SetTriangles(triangles.ToList(), 0);
        triangles.Dispose();

        //Load Data
        render.material = new Material(shader);
        render.material.SetBuffer("posBuf", posBuf);
        render.material.SetBuffer("colBuf", colBuf);
        posBuf.SetData(vertices);
        colBuf.SetData(colors);

        vertices.Dispose();
        colors.Dispose();
    }

    private void Update()
    {
        update?.Invoke();
    }

    private void OnDestroy()
    {
        if (posBuf != null) posBuf.Dispose();
        if (colBuf != null) colBuf.Dispose();
    }
}

public struct TriangleJob : IJob
{
    public int num;
    public int extra;
    public NativeArray<int> triangles;

    public void Execute()
    {
        for (int i = 0; i < num + extra; i++)
            triangles[i] = i % num;
    }
}

public struct ColorJob : IJob
{
    public int num;
    public NativeArray<int> cols;
    public NativeArray<Vector4> newCols;

    public void Execute()
    {
        for (int i = 0; i < num; i++)
        {
            float r = ((cols[i] & 0xFF000000) >> 24) / 255.0f;
            float g = ((cols[i] & 0x00FF0000) >> 16) / 255.0f;
            float b = ((cols[i] & 0x0000FF00) >> 8)  / 255.0f;
            float a = ((cols[i] & 0x000000FF) >> 0)  / 255.0f;

            newCols[i] = new Vector4(r, g, b, a);
        }
    }
}

public struct VerticeJob : IJob
{
    public int num;
    public int pointRatio;
    public NativeArray<Vector3> verts;
    public NativeArray<Vector3> newVerts;

    public void Execute()
    {
        for (int i = 0; i < num; i++)
            newVerts[i] = verts[i * pointRatio];
    }
}