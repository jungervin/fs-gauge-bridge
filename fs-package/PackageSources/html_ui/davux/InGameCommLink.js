var InGameCommLink;

function CreateInGameCommLink() {
    InGameCommLink = this;

    function MessageSender(channelName, dataSize) {
        var varHelper = new SharedUtils.VarHelper("L:Bridge_" + channelName);
        var m_toSend = [];
        this.send = function (msg) {
            m_toSend.push(SharedUtils.ChunkString(msg));
        }

        var lastRead = -1;
        var pump = function () {
            if (m_toSend.length > 0) {
                var didRead = varHelper.get("didRead");
                if (didRead != lastRead) {

                    for (var i = 0; i < dataSize; i++) {
                        varHelper.set("data" + i, m_toSend[0].length > 0 ? m_toSend[0].shift() : 0);
                    }
                    varHelper.set("remain", m_toSend[0].length);

                    if (m_toSend[0].length == 0) {
                        console.log("Send finished");
                        m_toSend.shift();
                    }

                    lastRead = didRead;
                    varHelper.set("shouldRead", didRead + 1);
                }
            }
            requestAnimationFrame(pump);
        }
        requestAnimationFrame(pump);
    }

    function MessageCatcher(channelName, dataSize, recv) {
        var varHelper = new SharedUtils.VarHelper("L:Bridge_" + channelName);

        var m_receiving = [];
        var pumpMessages = function () {
            var didRead = varHelper.get("didRead");
            var shouldRead = varHelper.get("shouldRead");
            if (shouldRead != didRead) {
                for (var i = 0; i < dataSize; i++) {
                    m_receiving.push(varHelper.get("data" + i));
                }

                if (varHelper.get("remain") == 0) {
                    try {
                        recv(SharedUtils.ReadChunkedString(m_receiving));
                    } catch (ex) {
                        console.error(ex);
                    }

                    m_receiving = [];
                }

                varHelper.set("didRead", shouldRead);
            }
            requestAnimationFrame(pumpMessages);
        }
        requestAnimationFrame(pumpMessages);
    }

    function RpcChannel(sendData) {

        let m_callbacks = {};

        let MakeCallback = (obj) => {
            return () => {
                sendData({
                    cb: 1,
                    id: obj.id,
                    args: JSON.stringify(Array.prototype.slice.call(arguments))
                });
            };
        };

        let cohernet_on = (args) => {
            return Coherent.on(args[0], MakeCallback(args[1]));
        };

        let cohernet_trigger = (args) => {
            return Coherent.trigger.apply(null, args);
        };

        let cohernet_call = (args) => {
            return Coherent.call(args[0], args[1]);
        };

        let processMessage = async (cmd) => {
            if (cmd.command === "eval") {
                return eval(cmd.args[0]);
            } else if (cmd.command === "coherent_on") {
                return cohernet_on(cmd.args);
            } else if (cmd.command === "coherent_trigger") {
                return cohernet_trigger(cmd.args);
            } else if (cmd.command === "coherent_call") {
                return cohernet_call(cmd.args);
            } else {
                throw new Error('unsupported command: ' + cmd.command);
            }
        };

        this.onMessage = async (msg) => {
            console.log(JSON.stringify(msg));
            try {
                var ret = await processMessage(msg.cmd);
                sendData({
                    ret,
                    seq: msg.seq,
                });
            } catch (ex) {
                console.error(ex);
                sendData({
                    error: ex,
                    seq: msg.seq,
                });
            }
        };
    }

    function onInstalled() {
        console.log("InGameCommLink onInstalled: " + InGameRelay.Id);

        SimVar.SetSimVarValue("L:Bridge_" + InGameRelay.Id, "number", 1);

        const comChannelWidth = InGameRelay.Id == 1 ? 10 : 2;
        var sender = new MessageSender("GAME_TO_EXT_" + InGameRelay.Id, comChannelWidth);
        var rpc = new RpcChannel((obj) => sender.send(JSON.stringify(obj)));
        new MessageCatcher("EXT_TO_GAME_" + InGameRelay.Id, comChannelWidth, (data) => rpc.onMessage(JSON.parse(data)));

        sender.send(JSON.stringify({reset:InGameCommLink.Version}));
    }

    setTimeout(onInstalled, 6000);

    this.Version = 44;
}
InGameCommLink = new CreateInGameCommLink();