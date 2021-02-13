var fs9gps;
function CreateFS9GPS() {
    fs9gps = this;

    this.db = null;

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