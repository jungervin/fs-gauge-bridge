
function updateBridgeVariables() {
	var gameVars = [
		["AIRCRAFT DESIGN SPEED VS0", "knots"],
		["AIRCRAFT DESIGN SPEED VS1", "knots"],
		["AIRCRAFT DESIGN SPEED VFE", "knots"],
		["AIRCRAFT DESIGN SPEED VNE", "knots"],
		["AIRCRAFT DESIGN SPEED VNO", "knots"],
		["AIRCRAFT DESIGN SPEED VMIN", "knots"],
		["AIRCRAFT DESIGN SPEED VMAX", "knots"],
		["AIRCRAFT DESIGN SPEED VR", "knots"],
		["AIRCRAFT CROSSOVER SPEED", "knots"],
		["AIRCRAFT CRUISE MACH", "Mach"],
		["AIRCRAFT CROSSOVER SPEED FACTOR", "Number"],
		["AIRCRAFT ELEVATOR TRIM NEUTRAL", "percent"],
		["GAME UNIT IS METRIC", "bool"],
		["AIRCRAFT AOA ANGLE",  "angl16"],
	];
	var globalVars = [
		["AIRCRAFT DESIGN SPEED VS0", "knots"],
	];
	try {
		gameVars.forEach((k, i) => {
			SimVar.SetSimVarValue("L:GV_" + k[0] + "_" + k[1], "Number", SimVar.GetGameVarValue(k[0], k[1]));
		});

		var ori = SimVar.GetGameVarValue("AIRCRAFT ORIENTATION AXIS", "XYZ");
		var base = "L:GV_AIRCRAFT ORIENTATION AXIS_XYZ_";
		SimVar.SetSimVarValue(base + "lat", "Number", ori.lat);
		SimVar.SetSimVarValue(base + "lon", "Number", ori.lon);
		SimVar.SetSimVarValue(base + "alt", "Number", ori.alt);
		SimVar.SetSimVarValue(base + "pitch", "Number", ori.pitch);
		SimVar.SetSimVarValue(base + "heading", "Number", ori.heading);
		SimVar.SetSimVarValue(base + "bank", "Number", ori.bank);
		SimVar.SetSimVarValue(base + "x", "Number", ori.x);
		SimVar.SetSimVarValue(base + "y", "Number", ori.y);
		SimVar.SetSimVarValue(base + "z", "Number", ori.z);

		globalVars.forEach((k, i) => {
			SimVar.SetSimVarValue("L:GLOB_" + k[0] + "_" + k[1], "Number", SimVar.GetGlobalVarValue(k[0], k[1]));
		});

	} catch (e) {
		console.log("### " + e);
	}


	requestAnimationFrame(updateBridgeVariables);
	//setTimeout(updateBridgeVariables, 200);
}

function notifyInstalled() {
	console.log("Bridge shim installed");
}

setTimeout(notifyInstalled, 5000);
setTimeout(updateBridgeVariables, 5000);
