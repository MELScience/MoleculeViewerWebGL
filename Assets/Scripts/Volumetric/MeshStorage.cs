using System;
using System.Collections.Generic;
using UnityEngine;

public class MeshStorage
{
    private const float CONVEX_Z_DISPL = 0.05f; // to avoid z-fighting b/w billboards and convex meshes

    public enum MeshType
    {
        None = 0,
        Billboard8,
        Billboard16,
        Convex13,
        Convex25,
        Bond8,
        Convex85,
        Billboard24
    }

    private static Dictionary<int, Mesh> _meshes = new Dictionary<int, Mesh>() { { 0, null } };

    private static int GetHashCode(MeshType type, int count)
    {
        if (type == MeshType.None)
            return 0;
        return unchecked(1611625597 * (int)type + 1611624229 * count);
    }

    public static Mesh GetMesh(MeshType type, int instancesInMesh)
    {
        int hash = GetHashCode(type, instancesInMesh);
        Mesh mesh;
        if (_meshes.TryGetValue(hash, out mesh))
            return mesh;
        switch (type)
        {
            case MeshType.Billboard8:
                mesh = CreateBatch(billboard8Vert, billboard8Tris, instancesInMesh);
                break;
            case MeshType.Billboard16:
                mesh = CreateBatch(billboard16Vert, billboard16Tris, instancesInMesh);
                break;
            case MeshType.Convex13:
                mesh = CreateBatch(convex13Vert, convex13Tris, instancesInMesh);
                break;
            case MeshType.Convex25:
                mesh = CreateBatch(convex25Vert, convex25Tris, instancesInMesh);
                break;
            case MeshType.Bond8:
                mesh = CreateBatch(bondsVert, bondsNorm, bondsTri, instancesInMesh);
                break;
            case MeshType.Convex85:
                mesh = CreateBatch(convexHighVerts, convexHighTris, instancesInMesh);
                break;
            case MeshType.Billboard24:
                mesh = CreateBatch(billboard24Vert, billboard24Tris, instancesInMesh);
                break;
            default:
                throw new NotImplementedException("Unsupported mesh type " + type);
        }
#if UNITY_EDITOR
        mesh.name = type + "_" + instancesInMesh;
#endif
        mesh.UploadMeshData(true);
        _meshes.Add(hash, mesh);
        return mesh;
    }

    public static int GetVerticesPerObject(MeshType type)
    {
        switch (type)
        {
            case MeshType.None: return 0;
            case MeshType.Billboard8: return 8;
            case MeshType.Billboard16: return 16;
            case MeshType.Convex13: return 13;
            case MeshType.Convex25: return 25;
            case MeshType.Bond8: return 8;
            case MeshType.Convex85: return convexHighVerts.Length;
            case MeshType.Billboard24: return 24;
            default:
                throw new NotImplementedException("Unsupported mesh type " + type);
        }
    }

    public static float GetExtrudeFactor(MeshType type)
    {
        switch (type)
        {
            case MeshType.Billboard8: return 1.082393f;
            case MeshType.Billboard16: return 1.01959f;
            default: return 1f; // should not be used for AA drawing
        }
    }

    #region mesh generation functions

    private static Vector3[] billboard8Vert = new[] {
        new Vector3(0.92388f, 0.3826834f, 0f),
        new Vector3(0.3826834f, 0.92388f, 0f),
        new Vector3(-0.3826834f, 0.92388f, 0f),
        new Vector3(-0.92388f, 0.3826834f, 0f),
        new Vector3(-0.92388f, -0.3826834f, 0f),
        new Vector3(-0.3826834f, -0.92388f, 0f),
        new Vector3(0.3826834f, -0.92388f, 0f),
        new Vector3(0.92388f, -0.3826834f, 0f),
    };
    private static int[] billboard8Tris = new[]{
        3, 4, 2,            2, 4, 5,            2, 5, 1,
        5, 6, 1,            1, 6, 0,            0, 6, 7,
    };

    private static Vector3[] billboard16Vert = new[] {
        new Vector3(1f, 0f, 0f),
        new Vector3(0.923879f, 0.382683f, 0f),
        new Vector3(0.707107f, 0.707107f, 0f),
        new Vector3(0.382683f, 0.923879f, 0f),
        new Vector3(0f, 1f, 0f),
        new Vector3(-0.382683f, 0.923879f, 0f),
        new Vector3(-0.707107f, 0.707107f, 0f),
        new Vector3(-0.923879f, 0.382683f, 0f),
        new Vector3(-1f, 0f, 0f),
        new Vector3(-0.923879f, -0.382683f, 0f),
        new Vector3(-0.707107f, -0.707107f, 0f),
        new Vector3(-0.382683f, -0.923879f, 0f),
        new Vector3(0f, -1f, 0f),
        new Vector3(0.382683f, -0.923879f, 0f),
        new Vector3(0.707107f, -0.707107f, 0f),
        new Vector3(0.923879f, -0.382683f, 0f),
    };
    private static int[] billboard16Tris = new[] {
        0,  1,  2,        0,  2,  3,        0,  3,  4,
        0,  4,  5,        0,  5,  6,        0,  6,  7,
        0,  7,  8,        0,  8,  9,        0,  9, 10,
        0, 10, 11,        0, 11, 12,        0, 12, 13,
        0, 13, 14,        0, 14, 15
    };

    private static Vector3[] billboard24Vert = new[] {
        new Vector3(-1f, 0f, 0f),
        new Vector3(-0.9659259f, -0.258819f, 0f),
        new Vector3(-0.9659259f, 0.258819f, 0f),
        new Vector3(-0.8660254f, -0.5f, 0f),
        new Vector3(-0.8660254f, 0.5f, 0f),
        new Vector3(-0.7071068f, -0.7071068f, 0f),
        new Vector3(-0.7071068f, 0.7071068f, 0f),
        new Vector3(-0.5f, -0.8660254f, 0f),
        new Vector3(-0.5f, 0.8660254f, 0f),
        new Vector3(-0.258819f, -0.9659259f, 0f),
        new Vector3(-0.258819f, 0.9659259f, 0f),
        new Vector3(0f, -1f, 0f),
        new Vector3(0f, 1f, 0f),
        new Vector3(0.258819f, -0.9659259f, 0f),
        new Vector3(0.258819f, 0.9659259f, 0f),
        new Vector3(0.5f, -0.8660254f, 0f),
        new Vector3(0.5f, 0.8660254f, 0f),
        new Vector3(0.7071068f, -0.7071068f, 0f),
        new Vector3(0.7071068f, 0.7071068f, 0f),
        new Vector3(0.8660254f, -0.5f, 0f),
        new Vector3(0.8660254f, 0.5f, 0f),
        new Vector3(0.9659259f, -0.258819f, 0f),
        new Vector3(0.9659259f, 0.258819f, 0f),
        new Vector3(1f, 0f, 0f),
    };
    private static int[] billboard24Tris = new[]{
        0, 1, 2,     2, 1, 3,     2, 3, 4,     4, 3, 5,
        4, 5, 6,     6, 5, 7,     6, 7, 8,     8, 7, 9,
        8, 9, 10,     10, 9, 11,     10, 11, 12,     12, 11, 13,
        12, 13, 14,     14, 13, 15,     14, 15, 16,     16, 15, 17,
        16, 17, 18,     18, 17, 19,     18, 19, 20,     20, 19, 21,
        20, 21, 22,     22, 21, 23,
    };

    private static Vector3[] convex13Vert = new[] {
        new Vector3(0f, 0f, 1f),
        new Vector3(0f, 0.7071068f, 0.7071068f),
        new Vector3(-0.7071068f, 0f, 0.7071068f),
        new Vector3(-0.7071068f, 0.7071068f, CONVEX_Z_DISPL),
        new Vector3(-1f, 0f, CONVEX_Z_DISPL),
        new Vector3(0f, 1f, CONVEX_Z_DISPL),
        new Vector3(0.7071068f, 0f, 0.7071068f),
        new Vector3(0.7071068f, 0.7071068f, CONVEX_Z_DISPL),
        new Vector3(1f, 0f, CONVEX_Z_DISPL),
        new Vector3(0f, -0.7071068f, 0.7071068f),
        new Vector3(0.7071068f, -0.7071068f, CONVEX_Z_DISPL),
        new Vector3(0f, -1f, CONVEX_Z_DISPL),
        new Vector3(-0.7071068f, -0.7071068f, CONVEX_Z_DISPL),
    };
    private static int[] convex13Tris = new[] {
        0, 1, 2,     2, 1, 3,     2, 3, 4,     1, 5, 3,
        0, 6, 1,     1, 7, 5,     1, 6, 7,     6, 8, 7,
        0, 9, 6,     0, 2, 9,     6, 10, 8,     6, 9, 10,
        9, 11, 10,     9, 12, 11,     9, 2, 12,     2, 4, 12,
    };

    private static Vector3[] convex25Vert = new[] {
        new Vector3(0f, 0f, 1f),
        new Vector3(0f, 0.5f, 0.8660254f),
        new Vector3(-0.5f, 0f, 0.8660254f),
        new Vector3(-0.5477226f, 0.5477226f, 0.6324555f),
        new Vector3(-0.8660254f, 0f, 0.5f),
        new Vector3(0f, 0.8660254f, 0.5f),
        new Vector3(-0.8660254f, 0.5f, CONVEX_Z_DISPL),
        new Vector3(-1f, 0f, CONVEX_Z_DISPL),
        new Vector3(-0.5f, 0.8660254f, CONVEX_Z_DISPL),
        new Vector3(0f, 1f, CONVEX_Z_DISPL),
        new Vector3(0.5f, 0f, 0.8660254f),
        new Vector3(0.5477226f, 0.5477226f, 0.6324555f),
        new Vector3(0.8660254f, 0f, 0.5f),
        new Vector3(0.5f, 0.8660254f, CONVEX_Z_DISPL),
        new Vector3(0.8660254f, 0.5f, CONVEX_Z_DISPL),
        new Vector3(1f, 0f, CONVEX_Z_DISPL),
        new Vector3(0f, -0.5f, 0.8660254f),
        new Vector3(0.5477226f, -0.5477226f, 0.6324555f),
        new Vector3(0f, -0.8660254f, 0.5f),
        new Vector3(0.8660254f, -0.5f, CONVEX_Z_DISPL),
        new Vector3(0.5f, -0.8660254f, CONVEX_Z_DISPL),
        new Vector3(0f, -1f, CONVEX_Z_DISPL),
        new Vector3(-0.5477226f, -0.5477226f, 0.6324555f),
        new Vector3(-0.5f, -0.8660254f, CONVEX_Z_DISPL),
        new Vector3(-0.8660254f, -0.5f, CONVEX_Z_DISPL),
    };
    private static int[] convex25Tris = new[] {
        0, 1, 2,     2, 1, 3,     2, 3, 4,     1, 5, 3,
        4, 3, 6,     4, 6, 7,     3, 8, 6,     3, 5, 8,
        5, 9, 8,     0, 10, 1,     1, 11, 5,     1, 10, 11,
        10, 12, 11,     5, 13, 9,     5, 11, 13,     11, 14, 13,
        11, 12, 14,     12, 15, 14,     0, 16, 10,     10, 17, 12,
        10, 16, 17,     16, 18, 17,     12, 19, 15,     12, 17, 19,
        17, 20, 19,     17, 18, 20,     18, 21, 20,     16, 22, 18,
        16, 2, 22,     0, 2, 16,     2, 4, 22,     18, 23, 21,
        18, 22, 23,     22, 24, 23,     22, 4, 24,     4, 7, 24,
    };

    private static Vector3[] bondsVert = new[] {
        new Vector3(0f, -0.5f, 0f), new Vector3(1f, -0.5f, 0f), new Vector3(1f,  0.5f, 0f), new Vector3(0f,  0.5f, 0f),
        new Vector3(0f, -0.25f, 0.4330127f), new Vector3(1f, -0.25f, 0.4330127f), new Vector3(1f, 0.25f, 0.4330127f), new Vector3(0f, 0.25f, 0.4330127f)
    };
    private static Vector3[] bondsNorm = new[] {
        new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f), new Vector3(1f,  1f, 0f), new Vector3(-1f,  1f, 0f),
        new Vector3(-1f, -0.5f, 0.8660254f), new Vector3(1f, -0.5f, 0.8660254f), new Vector3(1f, 0.5f, 0.8660254f), new Vector3(-1f, 0.5f, 0.8660254f),
    };
    private static int[] bondsTri = new[] {
        0, 1, 5,        0, 5, 4,        4, 5, 6,
        4, 6, 7,        7, 6, 2,        7, 2, 3,
    };

    private static Vector3[] convexHighVerts = new[] {
        new Vector3(0f, 0f, 1f),
        new Vector3(0f, 0.258819f, 0.9659259f),
        new Vector3(-0.258819f, 0f, 0.9659259f),
        new Vector3(-0.2672613f, 0.2672613f, 0.9258202f),
        new Vector3(-0.5f, 0f, 0.8660254f),
        new Vector3(0f, 0.5f, 0.8660254f),
        new Vector3(-0.5248339f, 0.2792583f, 0.8040922f),
        new Vector3(-0.7071068f, 0f, 0.7071068f),
        new Vector3(-0.2792583f, 0.5248339f, 0.8040922f),
        new Vector3(0f, 0.7071068f, 0.7071068f),
        new Vector3(-0.7470702f, 0.2894343f, 0.5984262f),
        new Vector3(-0.8660254f, 0f, 0.5f),
        new Vector3(-0.5477226f, 0.5477226f, 0.6324555f),
        new Vector3(-0.2894343f, 0.7470702f, 0.5984262f),
        new Vector3(0f, 0.8660254f, 0.5f),
        new Vector3(-0.9033583f, 0.2867884f, 0.3188989f),
        new Vector3(-0.9659259f, 0f, 0.258819f),
        new Vector3(-0.7596943f, 0.5478313f, 0.3503504f),
        new Vector3(-0.5478313f, 0.7596943f, 0.3503504f),
        new Vector3(-0.2867884f, 0.9033583f, 0.3188989f),
        new Vector3(0f, 0.9659259f, 0.258819f),
        new Vector3(-0.9659259f, 0.258819f, CONVEX_Z_DISPL),
        new Vector3(-1f, 0f, CONVEX_Z_DISPL),
        new Vector3(-0.8660254f, 0.5f, CONVEX_Z_DISPL),
        new Vector3(-0.7071068f, 0.7071068f, CONVEX_Z_DISPL),
        new Vector3(-0.5f, 0.8660254f, CONVEX_Z_DISPL),
        new Vector3(-0.258819f, 0.9659259f, CONVEX_Z_DISPL),
        new Vector3(0f, 1f, CONVEX_Z_DISPL),
        new Vector3(0.2867884f, 0.9033583f, 0.3188989f),
        new Vector3(0.2894343f, 0.7470702f, 0.5984262f),
        new Vector3(0.2792583f, 0.5248339f, 0.8040922f),
        new Vector3(0.2672613f, 0.2672613f, 0.9258202f),
        new Vector3(0.258819f, 0f, 0.9659259f),
        new Vector3(0.5f, 0f, 0.8660254f),
        new Vector3(0.5248339f, 0.2792583f, 0.8040922f),
        new Vector3(0.7071068f, 0f, 0.7071068f),
        new Vector3(0.5477226f, 0.5477226f, 0.6324555f),
        new Vector3(0.7470702f, 0.2894343f, 0.5984262f),
        new Vector3(0.8660254f, 0f, 0.5f),
        new Vector3(0.5478313f, 0.7596943f, 0.3503504f),
        new Vector3(0.7596943f, 0.5478313f, 0.3503504f),
        new Vector3(0.9033583f, 0.2867884f, 0.3188989f),
        new Vector3(0.9659259f, 0f, 0.258819f),
        new Vector3(0.5f, 0.8660254f, CONVEX_Z_DISPL),
        new Vector3(0.258819f, 0.9659259f, CONVEX_Z_DISPL),
        new Vector3(0.7071068f, 0.7071068f, CONVEX_Z_DISPL),
        new Vector3(0.8660254f, 0.5f, CONVEX_Z_DISPL),
        new Vector3(0.9659259f, 0.258819f, CONVEX_Z_DISPL),
        new Vector3(1f, 0f, CONVEX_Z_DISPL),
        new Vector3(0.9033583f, -0.2867884f, 0.3188989f),
        new Vector3(0.7470702f, -0.2894343f, 0.5984262f),
        new Vector3(0.7596943f, -0.5478313f, 0.3503504f),
        new Vector3(0.5477226f, -0.5477226f, 0.6324555f),
        new Vector3(0.5248339f, -0.2792583f, 0.8040922f),
        new Vector3(0.2672613f, -0.2672613f, 0.9258202f),
        new Vector3(0f, -0.258819f, 0.9659259f),
        new Vector3(0f, -0.5f, 0.8660254f),
        new Vector3(0.2792583f, -0.5248339f, 0.8040922f),
        new Vector3(0f, -0.7071068f, 0.7071068f),
        new Vector3(0.2894343f, -0.7470702f, 0.5984262f),
        new Vector3(0f, -0.8660254f, 0.5f),
        new Vector3(0.5478313f, -0.7596943f, 0.3503504f),
        new Vector3(0.2867884f, -0.9033583f, 0.3188989f),
        new Vector3(0f, -0.9659259f, 0.258819f),
        new Vector3(0.5f, -0.8660254f, CONVEX_Z_DISPL),
        new Vector3(0.7071068f, -0.7071068f, CONVEX_Z_DISPL),
        new Vector3(0.8660254f, -0.5f, CONVEX_Z_DISPL),
        new Vector3(0.9659259f, -0.258819f, CONVEX_Z_DISPL),
        new Vector3(0.258819f, -0.9659259f, CONVEX_Z_DISPL),
        new Vector3(0f, -1f, CONVEX_Z_DISPL),
        new Vector3(-0.2867884f, -0.9033583f, 0.3188989f),
        new Vector3(-0.2894343f, -0.7470702f, 0.5984262f),
        new Vector3(-0.2792583f, -0.5248339f, 0.8040922f),
        new Vector3(-0.2672613f, -0.2672613f, 0.9258202f),
        new Vector3(-0.5248339f, -0.2792583f, 0.8040922f),
        new Vector3(-0.5477226f, -0.5477226f, 0.6324555f),
        new Vector3(-0.7470702f, -0.2894343f, 0.5984262f),
        new Vector3(-0.5478313f, -0.7596943f, 0.3503504f),
        new Vector3(-0.7596943f, -0.5478313f, 0.3503504f),
        new Vector3(-0.9033583f, -0.2867884f, 0.3188989f),
        new Vector3(-0.5f, -0.8660254f, CONVEX_Z_DISPL),
        new Vector3(-0.258819f, -0.9659259f, CONVEX_Z_DISPL),
        new Vector3(-0.7071068f, -0.7071068f, CONVEX_Z_DISPL),
        new Vector3(-0.8660254f, -0.5f, CONVEX_Z_DISPL),
        new Vector3(-0.9659259f, -0.258819f, CONVEX_Z_DISPL),
    };
    private static int[] convexHighTris = new[] {
        0, 1, 2,     2, 1, 3,     2, 3, 4,     1, 5, 3,
        4, 3, 6,     4, 6, 7,     3, 8, 6,     3, 5, 8,
        5, 9, 8,     7, 6, 10,     7, 10, 11,     6, 12, 10,
        6, 8, 12,     8, 13, 12,     8, 9, 13,     9, 14, 13,
        11, 10, 15,     11, 15, 16,     10, 17, 15,     10, 12, 17,
        12, 18, 17,     12, 13, 18,     13, 19, 18,     13, 14, 19,
        14, 20, 19,     16, 15, 21,     16, 21, 22,     15, 23, 21,
        15, 17, 23,     17, 24, 23,     17, 18, 24,     18, 25, 24,
        18, 19, 25,     19, 26, 25,     19, 20, 26,     20, 27, 26,
        14, 28, 20,     14, 29, 28,     9, 29, 14,     9, 30, 29,
        5, 30, 9,     5, 31, 30,     1, 31, 5,     1, 32, 31,
        0, 32, 1,     32, 33, 31,     31, 34, 30,     31, 33, 34,
        33, 35, 34,     30, 36, 29,     30, 34, 36,     34, 37, 36,
        34, 35, 37,     35, 38, 37,     29, 39, 28,     29, 36, 39,
        36, 40, 39,     36, 37, 40,     37, 41, 40,     37, 38, 41,
        38, 42, 41,     28, 39, 43,     28, 43, 44,     20, 28, 44,
        20, 44, 27,     39, 45, 43,     39, 40, 45,     40, 46, 45,
        40, 41, 46,     41, 47, 46,     41, 42, 47,     42, 48, 47,
        38, 49, 42,     38, 50, 49,     50, 51, 49,     50, 52, 51,
        53, 52, 50,     35, 53, 50,     33, 53, 35,     33, 54, 53,
        32, 54, 33,     32, 55, 54,     0, 55, 32,     55, 56, 54,
        54, 57, 53,     54, 56, 57,     53, 57, 52,     56, 58, 57,
        35, 50, 38,     57, 59, 52,     57, 58, 59,     58, 60, 59,
        52, 61, 51,     52, 59, 61,     59, 62, 61,     59, 60, 62,
        60, 63, 62,     61, 62, 64,     61, 64, 65,     51, 61, 65,
        51, 65, 66,     49, 51, 66,     49, 66, 67,     42, 49, 67,
        42, 67, 48,     62, 68, 64,     62, 63, 68,     63, 69, 68,
        60, 70, 63,     60, 71, 70,     58, 71, 60,     58, 72, 71,
        56, 72, 58,     56, 73, 72,     55, 73, 56,     55, 2, 73,
        0, 2, 55,     2, 4, 73,     73, 74, 72,     73, 4, 74,
        4, 7, 74,     72, 75, 71,     72, 74, 75,     74, 76, 75,
        74, 7, 76,     7, 11, 76,     71, 77, 70,     71, 75, 77,
        75, 78, 77,     75, 76, 78,     76, 79, 78,     76, 11, 79,
        11, 16, 79,     70, 77, 80,     70, 80, 81,     63, 70, 81,
        63, 81, 69,     77, 82, 80,     77, 78, 82,     78, 83, 82,
        78, 79, 83,     79, 84, 83,     79, 16, 84,     16, 22, 84,
    };



    private static Mesh CreateBatch(Vector3[] sourceVerts, int[] sourceTris, int atomsCount)
    {
        var verts = sourceVerts.Length;
        var tris = sourceTris.Length;

        var vertices = new Vector3[verts * atomsCount];
        var triangles = new int[tris * atomsCount];
        
        for (int ii = 0; ii < atomsCount; ii++)
        {
            var iiVerts = ii * verts;
            Array.Copy(sourceVerts, 0, vertices, iiVerts, verts);

            int iiTris = ii * tris;
            for (int i = 0; i < tris; i++)
                triangles[iiTris + i] = iiVerts + sourceTris[i];
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.bounds = new Bounds(new Vector3(), 2500f.asVector3());
        return mesh;
    }
    private static Mesh CreateBatch(Vector3[] sourceVerts, Vector3[] sourceNormals, int[] sourceTris, int atomsCount)
    {
        var verts = sourceVerts.Length;
        var tris = sourceTris.Length;

        var vertices = new Vector3[verts * atomsCount];
        var normals = new Vector3[verts * atomsCount];
        var triangles = new int[tris * atomsCount];

        for (int ii = 0; ii < atomsCount; ii++)
        {
            var iiVerts = ii * verts;
            Array.Copy(sourceVerts, 0, vertices, iiVerts, verts);
            Array.Copy(sourceNormals, 0, normals, iiVerts, verts);

            int iiTris = ii * tris;
            for (int i = 0; i < tris; i++)
                triangles[iiTris + i] = iiVerts + sourceTris[i];
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.triangles = triangles;
        mesh.bounds = new Bounds(new Vector3(), 2500f.asVector3());
        return mesh;
    }

    #endregion
}
