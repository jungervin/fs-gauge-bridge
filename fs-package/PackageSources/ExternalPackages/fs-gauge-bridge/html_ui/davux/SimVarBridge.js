var SimVarBridge;

function CreateSimVarBridge() {

    let ALL = {};
    let keys_list = [];
    let keys_data = [];
    
    function AdviseSimVar(name, unit) {
        var key = name + "#" + unit;
        if (!keys_list.includes(key)) {
            keys_data.push({name, unit});
            keys_list.push(key);
        }
    }
    
    function do_sync() {
        fetch('/GetSimVarValue', { method: "POST", body: JSON.stringify(keys_data),}).then(response => response.json())
            .then(data => { 
                ALL = data;
            });
    
        setTimeout(do_sync, 100);
    }

    function GetSimVarValue(name, unit, dataSource = "") 
    {
        unit = unit.toLowerCase();
        AdviseSimVar(name, unit);

        if (name in ALL) {
            if (unit == "bool" || unit == "boolean") {
                return !!(ALL[name]);
            }

            return ALL[name];
        }

        switch (unit.toLowerCase()) {
            case "latlonalt":
            case "latlonaltpbh":
            case "pbh":
            case "pid_struct":
            case "xyz":
                console.log("### datatype ERR: " + name)
                break;
        }
        return 0;
    }

    function SetSimVarValue(name, unit, value, dataSource = "") {
        unit = unit.toLowerCase();
        if (name.startsWith("K:")) {
            fetch('/SetSimVarValue', { method: "POST", body: JSON.stringify([name, unit, value.toString()])});
        } else {
            AdviseSimVar(name, unit);

            if (name in ALL) {
                var currentValue = ALL[name];
                if (value != currentValue && currentValue) {
                    ALL[name] = value;
                    fetch('/SetSimVarValue', { method: "POST",  body: JSON.stringify([name, unit, value.toString()])});
                }
            }
        }
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }

    function GetGameVarValue(name, unit, param1 = 0, param2 = 0) {
        if (!name && unit === "GlassCockpitSettings") {
            var ret = {
                // TODO: this is cockpit.cfg
                AirSpeed: {
                    Initialized: true,
                    ...window.cockpitcfg
                }
            };
            return ret;
        }

        if (unit.toLowerCase() === "boolean")
        {
            unit = "bool";
        }

        if (name == "AIRCRAFT ORIENTATION AXIS") {
            var base = "L:GV_AIRCRAFT ORIENTATION AXIS_XYZ_";

            return new XYZ({
                x: SimVarBridge.GetSimVarValue(base + "x", "Number"),
                y: SimVarBridge.GetSimVarValue(base + "y", "Number"),
                z: SimVarBridge.GetSimVarValue(base + "z", "Number"),
                bank: SimVarBridge.GetSimVarValue(base + "bank", "Number"),
                pitch: SimVarBridge.GetSimVarValue(base + "pitch", "Number"),
                heading: SimVarBridge.GetSimVarValue(base + "heading", "Number"),
                lat: SimVarBridge.GetSimVarValue(base + "lat", "Number"),
                lon: SimVarBridge.GetSimVarValue(base + "lon", "Number"),
                alt: SimVarBridge.GetSimVarValue(base + "alt", "Number"),
            });
        }

        if (name == "AIRCRAFT CROSSOVER SPEED" ||
            name == "AIRCRAFT CRUISE MACH" ||
            name == "AIRCRAFT CROSSOVER SPEED FACTOR" ||
            name == "AIRCRAFT CROSSOVER SPEED FACTOR" ||
            name == "AIRCRAFT ELEVATOR TRIM NEUTRAL" ||
            name == "GAME UNIT IS METRIC" ||
            name.includes("AIRCRAFT DESIGN SPEED")) {
            var read = "L:GV_" + name + "_" + unit;
            return SimVarBridge.GetSimVarValue(read, "Number");
        }
        // Any double can be shimmed.
        console.log('### GetGameVarValue: ' + name + ' ' + unit);
        return {};
    }

    this.GetSimVarValue = GetSimVarValue;
    this.SetSimVarValue = SetSimVarValue;
    this.GetGameVarValue = GetGameVarValue;
    this.IsReady = function() { return false; }

    setTimeout(do_sync, 0);
}

SimVarBridge = new CreateSimVarBridge();