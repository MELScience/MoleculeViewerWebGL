// Called from backend when loading finished
function LoadComplete() {
	console.log("LoadComplete");

	// TODO: replace with calls to main app
	ShowCAS("184170-89-6");
}

// Call to show molecule, do not touch
function CanShowCAS(cas) {
	console.log("CanShowCAS " + cas);

    gameInstance.SendMessage("MoleculeViewer", "CanShowCAS", cas);
};

// Called from backend when molecule is ready to be shown
function CanShowCAS_Result(cas, result) {
    console.log(cas + " " + result);

    // TODO: add calls to main app
};

// Call to check if CAS can be shown, do not touch
function ShowCAS(cas) {	
	console.log("ShowCAS " + cas);

    gameInstance.SendMessage("MoleculeViewer", "ShowCAS", cas);
};

// Called from backend with result
function ShowCAS_Result(cas, result) {
    console.log(cas + " " + result);

    // TODO: add calls to main app
};
