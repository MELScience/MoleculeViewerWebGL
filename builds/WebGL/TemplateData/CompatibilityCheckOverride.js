// Copied from non-minified UnityLoader.js
UnityLoader.compatibilityCheck = function(gameInstance, onsuccess, onerror) {
	if (!UnityLoader.SystemInfo.hasWebGL) {
      gameInstance.popup("Your browser does not support WebGL",
        [{text: "OK", callback: onerror}]);
    } else if (UnityLoader.SystemInfo.mobile) {
      //gameInstance.popup("Please note that Unity WebGL is not currently supported on mobiles. Press OK if you wish to continue anyway.",
      //  [{text: "OK", callback: onsuccess}]);
      onsuccess();
    } else if (["Firefox", "Chrome", "Safari"].indexOf(UnityLoader.SystemInfo.browser) == -1) {
      //gameInstance.popup("Please note that your browser is not currently supported for this Unity WebGL content. Press OK if you wish to continue anyway.",
      //  [{text: "OK", callback: onsuccess}]);
      onsuccess();
    } else {
      onsuccess();
  	}
};