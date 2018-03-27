using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TestEditor : Editor {

    [MenuItem("Assets/TODELETE")]
	public static void ToDelete()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        MeshFilter meshFilter = go.GetComponent<MeshFilter>();

        Mesh mesh = meshFilter.mesh;

        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] = 2f * vertices[i];
        }
        mesh.vertices = vertices;
        mesh.UploadMeshData(false);

        AssetDatabase.CreateAsset(mesh, "Assets/Resources/mysphere.asset");

        DestroyImmediate(go);
    }
}
