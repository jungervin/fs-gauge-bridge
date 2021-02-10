var InGameRelay;
function CreateInGameRelay() {
	this.GameVars = [
		["AIRCRAFT DESIGN SPEED VS0", "knots"],
		["AIRCRAFT DESIGN SPEED VS1", "knots"],
		["AIRCRAFT DESIGN SPEED VFE", "knots"],
		["AIRCRAFT DESIGN SPEED VNE", "knots"],
		["AIRCRAFT DESIGN SPEED VNO", "knots"],
		["AIRCRAFT DESIGN SPEED VMIN", "knots"],
		["AIRCRAFT DESIGN SPEED VMAX", "knots"],
		["AIRCRAFT DESIGN SPEED VR", "knots"],
		["AIRCRAFT CROSSOVER SPEED", "knots"],
		["AIRCRAFT CRUISE MACH", "mach"],
		["AIRCRAFT CROSSOVER SPEED FACTOR", "number"],
		["AIRCRAFT ELEVATOR TRIM NEUTRAL", "percent"],
		["GAME UNIT IS METRIC", "bool"],
		["AIRCRAFT AOA ANGLE", "angl16"],
		["AIRCRAFT ORIENTATION AXIS", "xyz", ["lat", "lon", "alt", "pitch", "heading", "bank", "x", "y", "z"]]
	];

	this.GlobalVars = [
		["ZULU TIME", "seconds"],
	];

	function doVariableSync() {
		try {
			InGameRelay.GameVars.forEach((k, i) => {
				if (Array.isArray(k[2])) {
					var objectData = SimVar.GetGameVarValue(k[0], k[1]);
					for (let key of k[2]) {
						SimVar.SetSimVarValue("L:GV_" + k[0] + "_" + k[1] + "_" + key, "Number", objectData[key]);
					}
				} else {
					SimVar.SetSimVarValue("L:GV_" + k[0] + "_" + k[1], "Number", SimVar.GetGameVarValue(k[0], k[1]));
				}
			});

			InGameRelay.GlobalVars.forEach((k, i) => {
				SimVar.SetSimVarValue("L:GLOB_" + k[0] + "_" + k[1], "Number", SimVar.GetGlobalVarValue(k[0], k[1]));
			});
		} catch (e) {
			console.log("### doVariableSync Fatal Error ###: " + e);
			return;
		}
		requestAnimationFrame(doVariableSync);
	}

	function onInstalled() {
		if (InGameRelay.IsRunningExternally) {
			console.log("Bridge mode EXTERNAL");
		} else {
			InGameRelay.Id = Number(window.globalPanelData.sName.slice(-2));
			if (InGameRelay.Id == 1) {
				console.log("Bridge mode PRIMARY");
				doVariableSync();
			} else {
				console.log("Bridge mode SECONDARY");
			}
		}
	}

	setTimeout(onInstalled, 5000);
	this.IsRunningExternally = false;
}
InGameRelay = new CreateInGameRelay();