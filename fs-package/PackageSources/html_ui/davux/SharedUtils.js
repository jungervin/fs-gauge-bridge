var SharedUtils;

// max value 16777215 to -16777215

function CreateSharedUtils() {
    SharedUtils = this;

    function ReadChunkedString(arr) {
        var ret = [];
        for (var i = 0; i < arr.length; i++) {
            var buffer = new ArrayBuffer(4);
            new Uint32Array(buffer)[0] = arr[i];
            var bytes = new Uint8Array(buffer);
            for (var j = 0; j < 3; j++) {
                if (bytes[j] > 0) {
                    ret.push(String.fromCharCode(bytes[j]));
                }
            }
        }
        return ret.join("");
      //  return arr.map(c => String.fromCharCode(c)).join("");
    }

    function ChunkString(str) {
        var chunks = [];
        for (var i = 0; i < str.length; i+=3) {
            var buffer = new ArrayBuffer(4);
            var bytes = new Uint8Array(buffer);
            for (var j = 0; j < 3; j++) {
                bytes[j] = (i + j) < str.length ? str.charCodeAt(i + j) : 0;
            }
            chunks.push(new Uint32Array(buffer)[0]);
        }
        return chunks;
       // return str.split("").map(c => c.charCodeAt(0));
    }

    function VarHelper(namespace) {
        var getVar = function (name) {
            return SimVar.GetSimVarValue(namespace + "_" + name, "number");
        }
        var setVar = function (name, value) {
            return SimVar.SetSimVarValue(namespace + "_" + name, "number", value);
        }
        this.get = getVar;
        this.set = setVar;
    }

    this.ReadChunkedString = ReadChunkedString;
    this.ChunkString = ChunkString;
    this.VarHelper = VarHelper;
    this.Version = 4;
}

SharedUtils = new CreateSharedUtils();