using UnityEngine;

[CreateAssetMenu(fileName = "BallsRendererSettings", menuName = "MEL/Balls Rendering Settings")]
public class BallsRendererSettings : ScriptableObject
{
    #region Defaults

    private static readonly string batchSettingsPath = "BallsRenderingSettings/Batch";
    private static readonly string singleSettingsPath = "BallsRenderingSettings/Single";
    
    public static BallsRendererSettings Batch {
        get { return Resources.Load<BallsRendererSettings>(batchSettingsPath); }
    }

    public static BallsRendererSettings Single {
        get { return Resources.Load<BallsRendererSettings>(singleSettingsPath); }
    }

    public static BallsRendererSettings Get(BallsRenderer.Mode mode)
    {
        if (mode == BallsRenderer.Mode.BATCH)
            return Batch;
        else if (mode == BallsRenderer.Mode.SINGLE)
            return Single;

        return null;
    }

    #endregion

    public BallsRenderer.Mode mode;
    public Material material;
    public GameObject ballsRendererPrototype;
    public GameObject bucketPrototype;
    public GameObject ballObjectPrototype;
}
