using UnityEngine;

[ExecuteInEditMode]
public class VolumetricCameraHelper : MonoBehaviour
{
    private int cameraVectorId;
    private int cameraUpVectorId;

    protected void Start()
    {
        Shader.SetGlobalFloat("_PixelWorldSpaceFactor", Mathf.Tan(Camera.main.fieldOfView / 2f / 180f * Mathf.PI) * 2f / Screen.height);
        cameraVectorId = Shader.PropertyToID("_MainCameraViewWorldspace");
        cameraUpVectorId = Shader.PropertyToID("_MainCameraUpWorldspace");
        Camera.onPreRender += ForwardVectorSetup;
    }
    private void ForwardVectorSetup(Camera cam)
    {
        var camTransform = cam.transform;
        var forward = -camTransform.forward;
        Shader.SetGlobalVector(cameraVectorId, forward.xyz0());
        var up = camTransform.up;
        Shader.SetGlobalVector(cameraUpVectorId, up.xyz0());
    }
    protected void OnDestroy()
    {
        Camera.onPreRender -= ForwardVectorSetup;
    }
}
