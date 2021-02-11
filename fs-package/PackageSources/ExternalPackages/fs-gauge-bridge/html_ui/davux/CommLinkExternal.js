var CommLinkExternal;
var VCockpitExternal;

function CreateCommLinkExternal() {
    CommLinkExternal = this;

    function ReadChunkedString(arr) {
        var ret = [];

        //   console.log("unchunking");
        for (var i = 0; i < arr.length; i++) {
            var buffer = new ArrayBuffer(4);
            new Uint32Array(buffer)[0] = arr[i];
            var bytes = new Uint8Array(buffer);
            for (var j = 0; j < 3; j++) {
                if (bytes[j] > 0) {
                    ret.push(String.fromCharCode(bytes[j]));
                }
            }
            //   console.log("advance");
        }
        return ret.join("");
        //  return arr.map(c => String.fromCharCode(c)).join("");
    }

    function ChunkString(str) {
        var chunks = [];
        for (var i = 0; i < str.length; i += 3) {
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
            return SimVar.SetSimVarValue("L:Bridge_" + channelName + "_" + name, "number", Number(value));
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
                    for (var i = 0; i < 2; i++) {
                        setVar("data" + i, m_toSend[0].length > 0 ? m_toSend[0].shift() : 0);
                    }
                    setVar("remain", m_toSend[0].length);

                    if (m_toSend[0].length == 0) {
                        //console.log("Send finished");
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
        var didRead = -1;
        var pumpMessages = function () {
            // var didRead = getVar("didRead");
            var shouldRead = getVar("shouldRead");
            if (shouldRead !== didRead) {

                for (var i = 0; i < 2; i++) {
                    //   console.log("READVAR: " + getVar("data" + i));
                    m_receiving.push(getVar("data" + i));
                }

                if (getVar("remain") == 0) {
                    recv(ReadChunkedString(m_receiving));
                    m_receiving = [];
                }
                didRead = shouldRead;
                setVar("didRead", shouldRead);
            }
            requestAnimationFrame(pumpMessages);
        }
        // setVar("didRead", 0);
        // setVar("shouldRead", 0);
        requestAnimationFrame(pumpMessages);
    }

    function onGotMessage(msg) {
        console.log("GOT MSG: " + msg);
    }

    function onInstalled() {
        CommLinkExternal.EXT_TO_GAME = [];
        CommLinkExternal.GAME_TO_EXT = [];

        //CommLinkExternal.MessageSender = MessageSender;
        VCockpitExternal.panelCfg.forEach((data, i) => {
            var name = VCockpitExternal.panelCfg[i].htmlgauge00.path;
            CommLinkExternal.EXT_TO_GAME[i] = new MessageSender("EXT_TO_GAME_" + i);
            CommLinkExternal.GAME_TO_EXT[i] = new MessageCatcher("GAME_TO_EXT_" + i, (msg) => {
                console.log(name + ": " + msg);
            });
        });
    }
    setTimeout(onInstalled, 8000);
}
CommLinkExternal = new CreateCommLinkExternal();