using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Object = UnityEngine.Object;

public static class Extensions {

    #region vectors
    public static Vector2 asVector2(this float f) { return new Vector2(f, f); }
    public static Vector3 asVector3(this float f) { return new Vector3(f, f, f); }
    public static Vector4 asVector4(this float f) { return new Vector4(f, f, f, f); }
    public static Vector2 xy(this Vector3 v) { return new Vector2(v.x, v.y); }
    public static Vector2 xz(this Vector3 v) { return new Vector2(v.x, v.z); }
    public static Vector2 yz(this Vector3 v) { return new Vector2(v.y, v.z); }
    public static Vector3 xy0(this Vector3 v) { return new Vector3(v.x, v.y, 0f); }
    public static Vector3 xz0(this Vector3 v) { return new Vector3(v.x, v.z, 0f); }
    public static Vector3 yz0(this Vector3 v) { return new Vector3(v.y, v.z, 0f); }
    public static Vector3 newX(this Vector3 v, float x) { return new Vector3(x, v.y, v.z); }
    public static Vector3 newY(this Vector3 v, float y) { return new Vector3(v.x, y, v.z); }
    public static Vector3 newZ(this Vector3 v, float z) { return new Vector3(v.x, v.y, z); }
    public static Vector4 xyz0(this Vector3 v) { return new Vector4(v.x, v.y, v.z, 0f); }

    public static Vector3 Min(Vector3 a, Vector3 b) { return new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z)); }
    public static Vector3 Max(Vector3 a, Vector3 b) { return new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z)); }

    public static Vector2 Round(this Vector2 v, float scale) { return new Vector2(Mathf.Round(v.x * scale) / scale, Mathf.Round(v.y * scale) / scale); }
    public static Vector3 Round(this Vector3 v, float scale) { return new Vector3(Mathf.Round(v.x * scale) / scale, Mathf.Round(v.y * scale) / scale, Mathf.Round(v.z * scale) / scale); }

    public static Vector3 Inverse(this Vector3 v) { return new Vector3(1f/v.x, 1f/v.y, 1f/v.z); }

    // root mean square
    public static float RMS(this Vector2 v) { var t = v; t.Scale(t); return Mathf.Sqrt((t.x + t.y) / 2); }
    public static float RMS(this Vector3 v) { var t = v; t.Scale(t); return Mathf.Sqrt((t.x + t.y + t.z) / 3); }

    #endregion

    public static void Add(this List<int> list, int a, int b, int c) { list.Add(a); list.Add(b); list.Add(c); }
    public static void Add(this List<Vector3> list, float a, float b, float c) { list.Add(new Vector3(a, b, c)); }
    
    public static Quaternion LineRotation(this Camera camera, Vector3 lineAxis)
    {
        //Debug.Log("Axis input in linerotation: " + lineAxis);
        Vector3 direction = lineAxis.normalized;

        Vector3 xAxis = direction;
        Vector3 z1Axis = Vector3.Cross(camera.transform.up, xAxis);
        Vector3 z2Axis = Vector3.Cross(camera.transform.right, xAxis);
        Vector3 zAxis = (z1Axis.sqrMagnitude > z2Axis.magnitude) ? z1Axis.normalized : z2Axis.normalized;
        Vector3 yAxis = Vector3.Cross(xAxis, zAxis).normalized;

        //Debug.Log("Calculated axises:\n" + xAxis + "\n" + yAxis + "\n" + zAxis);

        Quaternion q = new Quaternion();
        q.SetLookRotation(zAxis, yAxis);

//         Vector3 xA = q * Vector3.right;
//         Vector3 yA = q * Vector3.up;
//         Vector3 zA = q * Vector3.forward;

        //Debug.Log("Local basis:\n" + xA + "\n" + yA + "\n" + zA);

        return q;
    }
    
    public static IEnumerator RotationCoroutine(this Transform t, Quaternion target, float duration)
    {
        Quaternion start = t.rotation;
        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime / duration;
            t.rotation = Quaternion.Lerp(start, target, progress);
            yield return null;
        }
        t.rotation = target;
    }
    
    /// <summary>
    /// Unity does not serialize lightmap information inside renderers, so all lightmap settings are lost during
    /// regular instantiation. Use this method as a workaround
    /// </summary>
    /// <param name="go">original game object with baked light</param>
    /// <param name="parent">copy with lightmap settings preserved</param>
    /// <returns></returns>
    public static GameObject InstantiateLightmapped(this GameObject go, Transform parent = null, bool saveWorldPos = false)
    {
        Debug.Assert(!go.GetComponentsInChildren<Transform>(true).Contains(parent), "Impossible to lightmap-copy object into one of it's child transforms");
        GameObject copy = parent == null ? GameObject.Instantiate(go) : GameObject.Instantiate(go, parent, saveWorldPos);
        var originalRenderers = go.GetComponentsInChildren<Renderer>(true);
        var copyRenderers = copy.GetComponentsInChildren<Renderer>(true);
        Debug.Assert(originalRenderers.Length == copyRenderers.Length, "Instantiated objects contains different number of Renderers than the original one");
        for (int i = originalRenderers.Length - 1; i >= 0; i--)
        {
            var or = originalRenderers[i];
            var cr = copyRenderers[i];
            cr.lightmapIndex = or.lightmapIndex;
            cr.lightmapScaleOffset = or.lightmapScaleOffset;
        }
        return copy;
    } 

    public static Color SetAlpha(this Color c, float alpha)
    {
        return new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));
    }

    public static void SetAlpha(this Image image, float alpha)
    {
        var c = image.color;
        c.a = alpha;
        image.color = c;
    }
    public static void SetAlpha(this Text image, float alpha)
    {
        var c = image.color;
        c.a = alpha;
        image.color = c;
    }
    
    public static void SetColor(this GameObject gameObject, Color c)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();

        if (renderer == null)
            return;

        // TODO: proper map here.
        if (renderer.material.shader.name.Contains("Alpha"))
            renderer.material.SetColor("_TintColor", c);
        else
            renderer.material.SetColor("_Tint", c);
    }

    public static Color ToColor(this int c)
    {
        return new Color(((c & 0xff0000) >> 16) / 255f, ((c & 0xff00) >> 8) / 255f, (c & 0xff) / 255f);
    }

    public static Color ToColor(this uint c)
    {
        return new Color(((c & 0xff000000) >> 24) / 255f, ((c & 0xff0000) >> 16) / 255f, ((c & 0xff00) >> 8) / 255f, (c & 0xFF) / 255f);
    }

    public static bool HasFlag(this System.Enum e, System.Enum value)
    {
        return (System.Convert.ToInt32(e) & System.Convert.ToInt32(value)) != 0;
    }
    
    public static string GetPath(this GameObject go)
    {
        List<string> path = new List<string>();

        Transform current = go.transform;
        path.Add(current.name);

        while (current.parent != null)
        {
            path.Add(current.parent.name);
            current = current.parent;
        }
        path.Reverse();

        return string.Join("/", path.ToArray());
    }

    public static bool IsGameObjectWithComponent<T>(this Object o) where T : Component
    {
        if (o == null)
        {
            return false;
        }
        if (!(o is GameObject))
        {
            return false;
        }
        return ((GameObject)o).GetComponent<T>() != null;
    }

    public static void SetLayerRecursive(this Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform t in root)
        {
            t.gameObject.layer = layer;
            if (t.childCount > 0)
            {
                SetLayerRecursive(t, layer);
            }
        }
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        var n = list.Count;
        while (n > 1) {
            n--;
            var k = Random.Range(0, n + 1);
            if (n != k) {
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    private static readonly System.DateTime unixEpochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
    public static int ToUnixTimestamp(this System.DateTime value)
    {
        return (int)System.Math.Truncate((value.ToUniversalTime().Subtract(unixEpochStart)).TotalSeconds);
    }

    public static System.DateTime DateTimeFromUnixTimestamp(int unixTimestamp)
    {
        return unixEpochStart.AddSeconds(unixTimestamp).ToLocalTime();
    }

    public static string DumpParams(this UnityEngine.Networking.NetworkBehaviour behaviour)
    {
        return string.Format("isClient: {0}\nisServer: {1}\nhasAuthority: {2}",
            behaviour.isClient, behaviour.isServer, behaviour.hasAuthority);
    }
}
