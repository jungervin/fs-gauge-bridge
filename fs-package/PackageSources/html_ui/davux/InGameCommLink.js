var InGameCommLink;

function CreateInGameCommLink() {
    InGameCommLink = this;

    function MessageSender(channelName) {
        var getVar = function (name) {
            return SimVar.GetSimVarValue("L:Bridge_" + channelName + "_" + name, "number");
        }
        var setVar = function (name, value) {
            return SimVar.SetSimVarValue("L:Bridge_" + channelName + "_" + name, "number", value);
        }

        var m_toSend = [];
        this.send = function (msg) {
            m_toSend.push(msg.split(""));
        }

        var lastRead = -1;
        var pump = function () {
            if (m_toSend.length > 0) {
                var didRead = getVar("didRead");
                var shouldRead = getVar("shouldRead");
                //  console.log("did: " + didRead + " should: " + shouldRead);
                if (didRead != lastRead) {
                    // set data
                    var d = m_toSend[0].shift();
                    console.log("set data: " + d);
                    setVar("data", d.charCodeAt(0));
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
                var d = getVar("data");
                var countLeft = getVar("remain");
                console.log("catch data: " + String.fromCharCode(d) + " of " + countLeft + " remaining");
                m_receiving.push(String.fromCharCode(d));

                if (countLeft == 0) {
                    recv(m_receiving.join(""));
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
            var mc = new MessageCatcher("EXT_TO_GAME", (ret) => {
                console.log(ret);
                ms.send(ret);
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