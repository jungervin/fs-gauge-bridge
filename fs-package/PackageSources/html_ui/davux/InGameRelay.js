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
		["AIRCRAFT CROSSOVER ALTITUDE", "feet"],
		["AIRCRAFT ELEVATOR TRIM NEUTRAL", "percent"],
		["GAME UNIT IS METRIC", "bool"],
		["AIRCRAFT AOA ANGLE", "angl16"],
		["AIRCRAFT ORIENTATION AXIS", "xyz", ["lat", "lon", "alt", "pitch", "heading", "bank", "x", "y", "z"]],
		["AIRCRAFT FLAPS SPEED LIMIT", "knots"],
		["AIRCRAFT GREEN DOT SPEED", "knots"],
		["AIRCRAFT LOWEST SELECTABLE SPEED", "knots"],
		["AIRCRAFT STALL PROTECTION SPEED MIN", "knots"],
		["AIRCRAFT STALL PROTECTION SPEED MAX", "knots"],
		["AIRCRAFT STALL SPEED", "knots"],
		["AIRCRAFT MAX GEAR EXTENDED", "knots"],
		["CAMERA POS IN PLANE", "xyz", ["x", "y", "z"]],
		["AIRCRAFT INITIAL FUEL LEVELS", "fuellevels", (f) => f ? 1 : 0],
		["FLIGHT DURATION", "seconds"],
	];

	this.GlobalVars = [
		["ZULU TIME", "seconds"],
		["ZULU DAY OF MONTH", "number"],
		["ZULU MONTH OF YEAR", "number"],
		["ZULU YEAR", "number"],
		["LOCAL TIME", "seconds"],
		["LOCAL DAY OF MONTH", "number"],
		["LOCAL MONTH OF YEAR", "number"],
		["LOCAL YEAR", "number"],
	];

	function doVariableSync() {
		try {
			InGameRelay.GameVars.forEach((k, i) => {
				if (Array.isArray(k[2])) {
					var objectData = SimVar.GetGameVarValue(k[0], k[1]);
					for (let key of k[2]) {
						SimVar.SetSimVarValue("L:GV_" + k[0] + "_" + k[1] + "_" + key, "Number", objectData[key]);
					}
				} else if (k[2]) {
					SimVar.SetSimVarValue("L:GV_" + k[0] + "_" + k[1], "Number", k[2](SimVar.GetGameVarValue(k[0], k[1])));
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