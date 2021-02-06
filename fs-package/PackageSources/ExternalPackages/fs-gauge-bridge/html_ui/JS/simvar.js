var SimVar;

(function (SimVar) {
    Include.addScript("/JS/Types.js");

    class SimVarValue {
        constructor(_name = "", _unit = "number", _type) {
            this.__type = "SimVarValue";
            this.name = _name;
            this.type = _type;
            this.unit = _unit;
        }
    }
    SimVar.SimVarValue = SimVarValue;
    SimVar.IsReady = SimVarBridge.IsReady;
    SimVar.GetSimVarValue = SimVarBridge.GetSimVarValue;

    class SimVarBatch {
        constructor(_simVarCount, _simVarIndex) {
            this.wantedNames = [];
            this.wantedUnits = [];
            this.wantedTypes = [];
            this.simVarCount = _simVarCount;
            this.simVarIndex = _simVarIndex;
        }
        add(_name, _unit, _type = "") {
            this.wantedNames.push(_name);
            this.wantedUnits.push(_unit);
            this.wantedTypes.push(_type);
        }
        getCount() {
            return this.simVarCount;
        }
        getIndex() {
            return this.simVarIndex;
        }
        getNames() {
            return this.wantedNames;
        }
        getUnits() {
            return this.wantedUnits;
        }
        getTypes() {
            return this.wantedTypes;
        }
    }
    SimVar.SimVarBatch = SimVarBatch;
    function GetSimVarArrayValues(simvars, callback, dataSource = "") {
        // console.error('## GetSimVarArrayValues: ' + JSON.stringify(simvars));
        // TODO

        for (var i = 0; i < simvars.length; i++)
        {
            SimVarBridge.GetSimVarValue(simvars.wantedNames[i], simvars.wantedUnits[i]);
        }

      //  callback();
    }
    SimVar.GetSimVarArrayValues = GetSimVarArrayValues;
    SimVar.SetSimVarValue = SimVarBridge.SetSimVarValue;
    SimVar.GetGlobalVarValue = SimVarBridge.GetGlobalVarValue;

    SimVar.GetGameVarValue = SimVarBridge.GetGameVarValue;
    function SetGameVarValue(name, unit, value) {
        console.log('### SetGameVarValue: ' + name);
        return new Promise(function (resolve, reject) {
            resolve();
        });
    }
    SimVar.SetGameVarValue = SetGameVarValue;
})(SimVar || (SimVar = {}));
//# sourceMappingURL=simvar.js.map