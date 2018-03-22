mergeInto(LibraryManager.library, {

/*
    [DllImport("__Internal")]
    public static extern void LoadComplete_Callback();

    [DllImport("__Internal")]
    public static extern void CanShowCAS_Callback(string cas, bool result);

    [DllImport("__Internal")]
    public static extern void ShowCAS_Callback(string cas, bool result);
*/

  LoadComplete_Callback: function() {
    LoadComplete();
  },

  CanShowCAS_Callback: function(cas, result) {
    CanShowCAS_Result(Pointer_stringify(cas), result);
  },

  ShowCAS_Callback: function(cas, result) {
    ShowCAS_Result(Pointer_stringify(cas), result);
  }
});