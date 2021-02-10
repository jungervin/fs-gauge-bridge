var SimVarBridge;

function CreateSimVarBridge() {
    let ALL = {};
    let keys_list = [];
    let keys_data = [];
    let ws = null;

    this.AllData = ALL;

    function doSend(data) {
        try {
            ws.send(JSON.stringify(data));
        } catch (ex) {
            console.log("WS: Send failed: " + ex);
        }
    }

    function doConnect() {
        ws = new WebSocket("ws://" + window.location.host + "/ws");
        ws.onopen = () => {
            window.WS = ws;
            console.log("WS: Connected");
            doSend({type:"hello", values: keys_data});
        };
        ws.onmessage = (msgEvent) => {
            let msg = JSON.parse(msgEvent.data);
            if (msg.type === "data")
            {
                for (var v in msg.values) {
                    var v = msg.values[v];
                    ALL[v.name] = v.value;
                }
            } else {
                console.log("WS: Unknown message: " + msg.type);
            }
        };
        ws.onclose = () => {
            console.log("WS: Disconnected. Reconnecting in 2 seconds...");
            setTimeout(doConnect, 2000);
        };
    }
    
    function AdviseSimVar(name, unit) {
        var key = name + "#" + unit;
        if (!keys_list.includes(key)) {
            keys_data.push({name, unit});
            keys_list.push(key);

            doSend({type:"read", values: [
                {name, unit}
            ]});
        }
    }
    
    function GetSimVarValue(name, unit, dataSource = "") 
    {
        if (name.startsWith("A:")) {
            name = name.substring(2);
        }
        name = name.toUpperCase();
        unit = unit.toLowerCase();
        AdviseSimVar(name, unit);

        switch (unit.toLowerCase()) {
            case "latlonalt":
            case "latlonaltpbh":
            case "pbh":
            case "pid_struct":
            case "xyz":
                console.log("### datatype ERR: " + name)
                break;
        }

        if (name in ALL) {
            if (unit == "bool" || unit == "boolean") {
                return !!(ALL[name]);
            }
            return ALL[name];
        }
        return 0;
    }

    function SetSimVarValue(name, unit, value, dataSource = "") {
        name = name.toUpperCase();
        unit = unit.toLowerCase();

        if (name.startsWith("A:")) {
            name = name.substring(2);
        }
        if (name.startsWith("K:")) {
            doSend({type:"write", values: [ {name, unit, value: value.toString()} ]});
        } else {
            if (name in ALL) {
                var currentValue = ALL[name];
                if (value !== currentValue) {
                    ALL[name] = value;

                    let newValue = {name, unit, value: value.toString()};
                    try { 
                        ws.send(JSON.stringify({type:"write", values: [newValue]}));
                    } catch (ex) {
                        console.log(ex);
                    }
                }
            }
            else {
                AdviseSimVar(name, unit);
            }
        }
        return new Promise(function (resolve, reject) { resolve(); });
    }

    function GetGameVarValue(name, unit, param1 = 0, param2 = 0) {
        name = name.toUpperCase();
        unit = unit.toLowerCase();
        if (!name && unit === "glasscockpitsettings") {
            var ret = {
                AirSpeed: {
                    Initialized: true,
                    ...VCockpitExternal.cockpitCfg
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
            name == "AIRCRAFT AOA ANGLE" ||
            name.includes("AIRCRAFT DESIGN SPEED")) {
            var read = "L:GV_" + name + "_" + unit;
            return SimVarBridge.GetSimVarValue(read, "Number");
        }
        // Any double can be shimmed via InGrameBridge.
        console.log('### GetGameVarValue: ' + name + ' ' + unit);
        return {};
    }

    function GetGlobalVarValue(name, unit) {
        if (name == "ZULU TIME" && unit == "seconds") {
            var read = "L:GLOB_" + name + "_" + unit;
            return SimVarBridge.GetSimVarValue(read, "Number");
        }
        // Any double can be shimmed via InGrameBridge.
        console.log('### GetGlobalVarValue: ' + name + " unit=" + unit);
        return null;
    }

    this.GetSimVarValue = GetSimVarValue;
    this.SetSimVarValue = SetSimVarValue;
    this.GetGameVarValue = GetGameVarValue;
    this.GetGlobalVarValue = GetGlobalVarValue;
    this.IsReady = function() { return false; }

    setTimeout(doConnect, 0);
}

SimVarBridge = new CreateSimVarBridge();