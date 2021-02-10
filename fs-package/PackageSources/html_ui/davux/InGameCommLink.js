var InGameCommLink;

function CreateInGameCommLink() {
    InGameCommLink = this;

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


    function MessageSender(channelName) {
        var getVar = function (name) {
            return SimVar.GetSimVarValue("L:Bridge_" + channelName + "_" + name, "number");
        }
        var setVar = function (name, value) {
       //     console.log("SET: L:Bridge_" + channelName + "_" + name + ": " + value);
            SimVar.SetSimVarValue("L:Bridge_" + channelName + "_" + name, "number", value).then(() => {
              //  console.log("VERIFY: " + getVar(name));

            });

        }

        var m_toSend = [];
        this.send = function (msg) {
            m_toSend.push(ChunkString(msg));
        }

        var lastRead = -1;
        var pump = function () {
            if (m_toSend.length > 0) {
                var didRead = getVar("didRead");
                var shouldRead = getVar("shouldRead");
                //  console.log("did: " + didRead + " should: " + shouldRead);
                if (didRead != lastRead) {
                    // set data
                    // var d = m_toSend[0].shift();
                    // console.log("set data: " + d);
                    // setVar("data", d.charCodeAt(0));

                    for (var i = 0; i < 10; i++) {
                        setVar("data" + i, m_toSend[0].length > 0 ? m_toSend[0].shift() : 0);
                    }
                    setVar("remain", m_toSend[0].length);

                    if (m_toSend[0].length == 0) {
                        console.log("Send finished");
                        m_toSend.shift();
                    }

                    lastRead = didRead;
                    setVar("shouldRead", didRead + 1);
                }
                else {
                    //console.log("waiting ");
                }
            }

            requestAnimationFrame(pump);
        }

        //	setVar("didRead", 0);
        //	setVar("shouldRead", 0);

        requestAnimationFrame(pump);
    }

    function MessageCatcher(channelName, recv) {
        var getVar = function (name) {
            return SimVar.GetSimVarValue("L:Bridge_" + channelName + "_" + name, "number");
        }
        var setVar = function (name, value) {
            return SimVar.SetSimVarValue("L:Bridge_" + channelName + "_" + name, "number", value);
        }

        var m_receiving = [];
        var pumpMessages = function () {
            var didRead = getVar("didRead");
            var shouldRead = getVar("shouldRead");
            if (shouldRead != didRead) {
                //  var d = getVar("data");
                //  var countLeft = getVar("remain");
                //  console.log("catch data: " + String.fromCharCode(d) + " of " + countLeft + " remaining");
                //  m_receiving.push(String.fromCharCode(d));
                for (var i = 0; i < 10; i++) {
                    m_receiving.push(getVar("data" + i));
                }

                if (getVar("remain") == 0) {
                    recv(ReadChunkedString(m_receiving));
                    m_receiving = [];
                }

                setVar("didRead", shouldRead);
            }
            requestAnimationFrame(pumpMessages);
        }
        //  setVar("didRead", 0);
        //  setVar("shouldRead", 0);
        requestAnimationFrame(pumpMessages);
    }

    function onInstalled() {
        console.log("InGameCommLink");

        if (InGameRelay.Id == 1) {
            console.log("InGameCommLink Online");
            var ms = new MessageSender("GAME_TO_EXT");
            var mc = new MessageCatcher("EXT_TO_GAME", (command) => {
                console.log(command);
                try {
                    ms.send(JSON.stringify({
                        ret: eval(command),
                    }));
                } catch(ex) {
                    ms.send(JSON.stringify({
                        error: ex,
                    }));
                }
            });
        } else {
            console.log("Not primary relay, no comm link");
        }
    }

    setTimeout(onInstalled, 6000);

    //  this.MessageCatcher = MessageCatcher;
    //	this.MessageSender = MessageSender;
}
InGameCommLink = new CreateInGameCommLink();