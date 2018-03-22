using UnityEngine;
using System.Runtime.InteropServices;

public static class JSImports
{
    [DllImport("__Internal")]
    public static extern void LoadComplete_Callback();

    [DllImport("__Internal")]
    public static extern void CanShowCAS_Callback(string cas, bool result);

    [DllImport("__Internal")]
    public static extern void ShowCAS_Callback(string cas, bool result);
    
}