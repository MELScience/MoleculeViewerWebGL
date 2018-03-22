using System;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public static class MELUtils {

    public const double KelvinToCelsius = -273.15;
    public readonly static string assetNameTemplate = "{0}___{1}___{2}___{3}";
    
    public readonly static string ScreenImagesAsset = "ScreenImages";
    public readonly static string SoundsAsset = "Sounds";

    public static string GetAssetName(string assetType, string packName, string lessonName, string assetIndex)
    {
        return string.Format(assetNameTemplate, packName, lessonName, assetType, assetIndex);
    }
    
    public static T[] CreateArray<T>(T defaultValue, int size)
    {
        var arr = new T[size];
        for (int i = 0; i < size; i++)
            arr[i] = defaultValue;
        return arr;
    }

    public static void CleanupChildren(GameObject parentObject)
    {
		// Temp copy of transform children list.
		List<Transform> children = parentObject.transform.Cast<Transform> ().ToList ();

		foreach (Transform t in children) {
            
			t.gameObject.SetActive (false);

			if (Application.isEditor)
				GameObject.DestroyImmediate (t.gameObject);
			else 
				GameObject.DestroyObject (t.gameObject);
		}
    }

	public static float GetCameraDistance(float fov, float xFit, float yFit)
	{
		float tan = Mathf.Tan(0.5f * Mathf.Deg2Rad * fov);
		float yDist = yFit / tan;
        //float xDist = (xFit / Camera.main.aspect) / tan;
        float xDist = (xFit / 1.3f) / tan;

        return Mathf.Max(xDist, yDist);
	}
    
    public static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        if (Directory.Exists(targetDirectory))
            Directory.Delete(targetDirectory);

        Directory.CreateDirectory(targetDirectory);

        DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
        DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

        CopyDirectoryRecursive(diSource, diTarget);
    }

    public static void CopyDirectoryRecursive(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (FileInfo fi in source.GetFiles()) {
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories()) {
            DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            CopyDirectoryRecursive(diSourceSubDir, nextTargetSubDir);
        }
    }
    
    private static int GetSDKLevel()
    {
        var clazz = AndroidJNI.FindClass("android.os.Build$VERSION");
        var fieldID = AndroidJNI.GetStaticFieldID(clazz, "SDK_INT", "I");
        var sdkLevel = AndroidJNI.GetStaticIntField(clazz, fieldID);

        return sdkLevel;
    }
    
    public static IEnumerator Rotate(Transform t, float speed)
    {
        while (true)
        {
            t.Rotate(Vector3.up, Time.deltaTime * speed);
            yield return null;
        }
    }

    /*
    public static GameObject SpawnAtAnchor(string prefabName, string anchorName, string sceneName = null)
    {
        if (MelSceneManager.Instance == null) {
            Debug.LogWarning("MelSceneManager is not initialized");
            return null;
        }
        
        Transform anchor = MelSceneManager.Instance.GetAnchor(anchorName, sceneName);

        if (null == anchor) {
            Debug.LogWarning("Anchor not found, not spawning!");
            return null;
        }
        
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);

        if (null == prefab) {
            Debug.LogError("Prefab " + prefabName + " could not be loaded!");
            return null;
        }

        GameObject gameObject = GameObject.Instantiate<GameObject>(prefab);

        // UI-like objects should be parented under WorldSpaceCanvas
        if (gameObject.transform is RectTransform) {
            GameObject worldSpaceCanvas = GameObject.Find("WorldSpaceCanvas");
            gameObject.transform.SetParent(worldSpaceCanvas.transform);
        } else {
            gameObject.transform.SetParent(null);
        }
        
        gameObject.name = gameObject.name.Replace("(Clone)", "");

        gameObject.transform.position = Vector3.zero;
        gameObject.transform.rotation = Quaternion.identity;

        if (!string.IsNullOrEmpty(anchorName)) {
            ApplyAnchor(gameObject.transform, anchorName);
        }

        if (!string.IsNullOrEmpty(sceneName)) {
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName(sceneName));
        }

        return gameObject;
    }
    */

    public static IEnumerator LoadImageAsync(string imageName, Image targetImage, Action<Image, Sprite> onComplete = null)
    {
        var resName ="Textures/" + imageName;
        if (targetImage == null)
        {
            yield break;
        }
        ResourceRequest operation = Resources.LoadAsync<Sprite>(resName);
         
        yield return operation;

        var sprite = operation.asset as Sprite;
        if (sprite != null)
        {
            targetImage.enabled = true;
            targetImage.sprite = sprite;
            if (onComplete != null)
            {
                onComplete.Invoke(targetImage, sprite);
            }
        }
        else
        {
            Debug.LogErrorFormat("Could not load image {0}", resName);
            targetImage.enabled = false;
        }
    }

    public static void RemoveChildren<T>(Component root) where T : Component
    {
        var components = root.gameObject.GetComponentsInChildren<T>();
        foreach (var component in components)
        {
            GameObject.Destroy(component.gameObject);
        }
    }

    public static void AllRemoveChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            GameObject.Destroy(root.GetChild(i).gameObject);
    }

    public static void RemoveInstance<T>(ref T obj) where T : Component
    {
        if (obj != null)
        {
            var gameObject = obj.gameObject;
            UnityEngine.Object.Destroy(gameObject);
            obj = null;
        }
    }

    public static void RemoveInstance(ref GameObject obj)
    {
        if (obj != null)
        {
            UnityEngine.Object.Destroy(obj);
            obj = null;
        }
    }

    public static IEnumerator WaitForState(Animator a, string stateName, int layer = 0)
    {
        //Debug.LogFormat("WaitForState {0}: {1}", a.gameObject.name, stateName);
        int stateHash = Animator.StringToHash(stateName);
        while (a.GetCurrentAnimatorStateInfo(layer).fullPathHash != stateHash)
        {
            yield return null;
        }

        yield break; 
    }

    public static IEnumerator WaitForState(Animator a, int stateHash, int layer = 0)
    {
        //Debug.LogFormat("WaitForState {0}: {1}", a.gameObject.name, stateHash);
        while (a.GetCurrentAnimatorStateInfo(layer).fullPathHash != stateHash)
        {
            yield return null;
        }

        yield break;
    }

    public static IEnumerator WaitForSeconds(float time, Boxed<bool> skipped, Func<bool> skipFunction)
    {
        while (time > 0f)
        {
            if (skipFunction != null)
            {
                var skip = skipFunction.Invoke();
                if (skip)
                {
                    skipped.value = skip;
                    yield break;
                }
            }
            time -= Time.deltaTime;
            yield return null;
        }
    }

    public static IEnumerator WaitForSeconds(float time, Func<bool> skipFunction)
    {
        while (time > 0f && (skipFunction != null && !skipFunction.Invoke()))
        {
            time -= Time.deltaTime;
            yield return null;
        }
    }

    public static IEnumerator WaitForSecondsRealtime(float time, Func<bool> skipFunction)
    {
        while (time > 0f && (skipFunction != null && !skipFunction.Invoke()))
        {
            time -= Time.unscaledDeltaTime;
            yield return null;
        }
    }

    public static IEnumerator WaitAndThen(float time, IEnumerator runAfterDelay, Func<bool> skipFunction = null)
    {
        if (skipFunction == null)
        {
            yield return new WaitForSeconds(time);
        }
        else
        {
            while (time > 0f && !skipFunction.Invoke())
            {
                time -= Time.deltaTime;
                yield return null;
            }
        }

        yield return runAfterDelay;
    }

    // ToDo: Make skipfuction obligatory
    public static IEnumerator WaitAndThen(float time, Action runAfterDelay, Func<bool> skipFunction = null)
    {
        if (skipFunction == null)
        {
            yield return new WaitForSeconds(time);
        }
        else
        {
            while (time > 0f && !skipFunction.Invoke())
            {
                time -= Time.deltaTime;
                yield return null;
            }
        }
        runAfterDelay.Invoke();
    }

    // run all coroutines in parallel and wait all to finish
    public static IEnumerator WaitAll(MonoBehaviour monoBehaviour, params IEnumerator[] coroutines)
    {
        var counter = new Boxed<int>(coroutines.Length);

        foreach (var c in coroutines)
            monoBehaviour.StartCoroutine(Wrapper(c, counter));

        while (counter.value > 0)
            yield return null;
    }

    // waiting wrapper
    private static IEnumerator Wrapper(IEnumerator coroutine, Boxed<int> counter)
    {
        try
        {
            yield return coroutine;
        }
        finally
        {
            counter.value--;
        }
    }

    public static void RunAll(MonoBehaviour monoBehaviour, params IEnumerator[] coroutines)
    {
        foreach (var c in coroutines)
            monoBehaviour.StartCoroutine(c);
    }

    public static IEnumerator RunInOrder(params IEnumerator[] coroutines)
    {
        foreach (var c in coroutines)
            yield return c;
    }

    public static IEnumerator DoNothing()
    {
        yield break;
    }
    
    public static void SetTransformTo(Transform destination, Transform source, bool useLossyScale = false)
    {
        destination.position = source.position;
        destination.rotation = source.rotation;
        destination.localScale = useLossyScale ? source.lossyScale : source.localScale;
        /*source.localPosition = source.root.InverseTransformPoint(target.position);
        source.localRotation = Quaternion.Inverse(source.root.transform.rotation) * target.rotation;
        source.localScale = target.localScale;*/
    }

    public static string EnsureFolder(string path)
    {
        string directoryName = Path.GetDirectoryName(path);

        if (!Directory.Exists(directoryName))
            Directory.CreateDirectory(directoryName);

        return path;
    }

    private static float DeviceDiagonalSizeInInches()
    {
        var dpi = Screen.dpi;
        float screenWidth = Screen.width / dpi;
        float screenHeight = Screen.height / dpi;
        float diagonalInches = Mathf.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight);

        return diagonalInches;
    }

    private static bool? _isTablet;
    public static bool IsTablet {
        get {
            if (_isTablet == null)
            {
                _isTablet = DeviceDiagonalSizeInInches() > 6.55f;
            }
            return _isTablet.Value;
        }
    }

    private static string _deviceName = "";
    public static string DeviceName {
        get {
            if (!string.IsNullOrEmpty(_deviceName))
                return _deviceName;

#if UNITY_ANDROID && !UNITY_EDITOR
            try {
                AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
                AndroidJavaObject contentResolver = context.Call<AndroidJavaObject>("getContentResolver");
                AndroidJavaClass globalSettingsClass = new AndroidJavaClass("android.provider.Settings$Global");

                _deviceName = globalSettingsClass.CallStatic<string>("getString", contentResolver, "device_name");
            } catch {
                _deviceName = SystemInfo.deviceName;
            }
#else
            _deviceName = SystemInfo.deviceName;
#endif

            return _deviceName;
        }
    }
    
    public static string GetLaunchURI()
    {
        string result = "";

#if UNITY_ANDROID && !UNITY_EDITOR
        
        try {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject intent = activity.Call<AndroidJavaObject>("getIntent");
            AndroidJavaObject data = intent.Call<AndroidJavaObject>("getData");

            result = data.Call<string>("toString");
        } catch (System.Exception ex) {
            result = "";
        }

#endif

        return result;
    }

    public static bool IsWiFiEnabled()
    {
        bool result = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        try {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass contextClass = new AndroidJavaClass("android.content.Context");
            AndroidJavaObject context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");

            string Context_WIFI_SERVICE = contextClass.GetStatic<string>("WIFI_SERVICE");
            AndroidJavaObject wifiService = context.Call<AndroidJavaObject>("getSystemService", Context_WIFI_SERVICE);
            result = wifiService.Call<bool>("isWifiEnabled");
        } catch (System.Exception ex) {
            Debug.LogError("Failed to get WiFi status!");
            result = false;
        }
#else
        result = true;
#endif

        return result;
    }

    public static void NormalizeDistribution<T>(KeyValuePair<T, float>[] distributionValues)
    {
        float cumulative = 0.0f;
        for (var i = distributionValues.Length - 1; i >= 0; i--)
        {
            cumulative += distributionValues[i].Value;
        }
        if (cumulative > 0)
        {
            for (var i = distributionValues.Length - 1; i >= 0; i--)
            {
                distributionValues[i] = new KeyValuePair<T, float>(distributionValues[i].Key, distributionValues[i].Value / cumulative);
            }
        }
    }

    public static T GetRandomDistributed<T>(KeyValuePair<T, float>[] distributionValues)
    {
        double diceRoll = Random.Range(0f, 1f);
        var result = distributionValues[distributionValues.Length-1].Key;

        var cumulative = 0.0f;
        for (int i = 0; i< distributionValues.Length; i++)
        {
            cumulative += distributionValues[i].Value;
            if (diceRoll<=cumulative)
            {
                result = distributionValues[i].Key;
                break;
            }
        }

        return result;
    }

    // https://stackoverflow.com/questions/15369566/putting-space-in-camel-case-string-using-regular-expression
    private static Regex _caseRaplaceQuery;

    public static string CamelCaseToWords(string tableName)
    {
        if (_caseRaplaceQuery == null)
        {
            _caseRaplaceQuery = new Regex("([A-Z])([A-Z])([a-z])|([a-z])([A-Z])", RegexOptions.Multiline);
        }
        return _caseRaplaceQuery.Replace(tableName,"$1$4 $2$3$5");
    }

    // http://referencesource.microsoft.com/#mscorlib/system/string.cs,55e241b6143365ef
    public static bool IsNullOrWhiteSpace(string value)
    {
        if (value == null) return true;

        for (int i = 0; i < value.Length; i++) {
            if (!System.Char.IsWhiteSpace(value[i])) return false;
        }

        return true;
    }
}
