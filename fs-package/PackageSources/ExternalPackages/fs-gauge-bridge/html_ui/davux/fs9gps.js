var fs9gps;
function CreateFS9GPS() {
    fs9gps = this;

    this.db = null;

    let m_registerNames = {};

    m_registerNames["FLIGHTPLANISLOADEDAPPROACH"] = false;
    m_registerNames["FLIGHTPLANISACTIVEAPPROACH"] = false;


    m_registerNames["NEARESTAIRPORTCURRENTLATITUDE"] = 0;
    m_registerNames["NEARESTAIRPORTCURRENTLONGITUDE"] = 0;
    m_registerNames["NEARESTAIRPORTMAXIMUMITEMS"] = 0;
    m_registerNames["NEARESTAIRPORTMAXIMUMDISTANCE"] = 0;
    m_registerNames["NEARESTAIRPORTITEMSNUMBER"] = 10;
    

    m_registerNames["NEARESTINTERSECTIONCURRENTLATITUDE"] = 0;
    m_registerNames["NEARESTINTERSECTIONCURRENTLONGITUDE"] = 0;
    m_registerNames["NEARESTINTERSECTIONMAXIMUMDISTANCE"] = 0;
    m_registerNames["NEARESTINTERSECTIONMAXIMUMITEMS"] = 0;
    m_registerNames["NEARESTINTERSECTIONITEMSNUMBER"] = 0;
    m_registerNames["NEARESTNDBCURRENTLATITUDE"] = 0;
    m_registerNames["NEARESTNDBCURRENTLONGITUDE"] = 0;
    m_registerNames["NEARESTNDBMAXIMUMDISTANCE"] = 0;
    m_registerNames["NEARESTNDBMAXIMUMITEMS"] = 0;
    m_registerNames["NEARESTNDBITEMSNUMBER"] = 0;
    m_registerNames["NEARESTVORCURRENTLATITUDE"] = 0;
    m_registerNames["NEARESTVORCURRENTLONGITUDE"] = 0;
    m_registerNames["NEARESTVORMAXIMUMDISTANCE"] = 0;
    m_registerNames["NEARESTVORMAXIMUMITEMS"] = 0;
    m_registerNames["NEARESTVORITEMSNUMBER"] = 0;
    
    this.GetSimVarValue = (name, unit) => {
        name = name.toUpperCase();
        name = name.substring("C:fs9gps:".length);

        if (name in m_registerNames) {
            return m_registerNames[name];
        }
        

        console.log("FS9GPSget: " + name);
    };

    this.SetSimVarValue = (name, unit, value) => {
        name = name.substring("C:fs9gps:".length);

        if (name in m_registerNames) {
            m_registerNames[name] = value;
            let myResolve = null;
            var ret = new Promise(function (resolve, reject) {
                // resolve();
                myResolve = resolve;
             });

             requestAnimationFrame(() => myResolve());

             return ret;
        }

        console.log("FS9GPSset: " + name);

        return new Promise(function (resolve, reject) {
           // resolve();
        });
    };

    this.GetSimVarArrayValues = (simvars, callback) => {
        Coherent.GetSimVarArrayValues(simvars, callback);
        // console.error('## GetSimVarArrayValues: ' + JSON.stringify(simvars));
        // TODO: fs9gps
        for (var i = 0; i < simvars.length; i++)
        {
          //  SimVarBridge.GetSimVarValue(simvars.wantedNames[i], simvars.wantedUnits[i]);
        }
    };

    this.Initialize = function () {
        console.log("gps9 disabled");

        /*
        config = {
            locateFile: filename => `/davux/sqljs/${filename}`
        }

        initSqlJs(config).then(function (SQL) {
            console.log("sqlite ready. Downloading database...");
            fetch("/davux/db.bin").then(res => res.arrayBuffer()).then((buffer) =>{
                console.log("database downloaded");
                fs9gps.db = new SQL.Database(new Uint8Array(buffer));
                console.log("database created");
            });
        });
        */
    }
}
fs9gps = new CreateFS9GPS();