var CommLinkExternal;
var VCockpitExternal;

function CreateCommLinkExternal() {
    CommLinkExternal = this;

    function MessageSender(channelName, dataSize) {
        var varHelper = new SharedUtils.VarHelper("L:Bridge_" + channelName);

        var m_toSend = [];
        var m_sendCallbacks = {};
        var m_seq = 0;

        this.send = function (cmd) {
            var msg = {
                cmd,
                seq: m_seq++
            }

            m_toSend.push(SharedUtils.ChunkString(JSON.stringify(msg)));
            let savedAccept = 0;
            let savedReject = 0;
            var ret = new Promise((accept, reject) => {
                savedAccept = accept;
                savedReject = reject;
            });
            m_sendCallbacks[msg.seq] = { accept: savedAccept, reject: savedReject };
            return ret;
        }

        this.onResponse = function (data) {
            try {
                var msg = JSON.parse(data);
                if (!msg) { return; }
                if (msg.seq in m_sendCallbacks) {
                    if (msg.error) {
                        m_sendCallbacks[msg.seq].reject(msg.error);
                    } else {
                        m_sendCallbacks[msg.seq].accept(msg.ret);
                    }
                }
                delete m_sendCallbacks[msg.seq];
            } catch (ex) {
                return;
            }
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

    function MessageCatcher(channelName, dataSize) {
        var varHelper = new SharedUtils.VarHelper("L:Bridge_" + channelName);
        var m_receiving = [];
        var m_handlers = [];
        var didRead = -1;
        var pumpMessages = function () {
            var shouldRead = varHelper.get("shouldRead");
            if (shouldRead !== didRead) {

                for (var i = 0; i < dataSize; i++) {
                    m_receiving.push(varHelper.get("data" + i));
                }

                if (varHelper.get("remain") == 0) {
                    var ret = SharedUtils.ReadChunkedString(m_receiving);
                    m_handlers.forEach((h) => h(ret));
                    m_receiving = [];
                }
                didRead = shouldRead;
                varHelper.set("didRead", shouldRead);
            }
            requestAnimationFrame(pumpMessages);
        }

        this.on = (msg, handler) => {
            if (msg === "msg") {
                m_handlers.push(handler);
            } else {
                throw new Error("not msg");
            }
        };


        requestAnimationFrame(pumpMessages);
    }

    function onInstalled() {
        CommLinkExternal.EXT_TO_GAME = [];
        CommLinkExternal.GAME_TO_EXT = [];

        VCockpitExternal.panelCfg.forEach((data, i) => {
           // const name = VCockpitExternal.panelCfg[i].htmlgauge00.path;
            const ix = Number(i) + 1;
            const comChannelWidth = ix == 1 ? 10 : 2;
            const sender = new MessageSender("EXT_TO_GAME_" + ix, comChannelWidth);
            const receiver = new MessageCatcher("GAME_TO_EXT_" + ix, comChannelWidth);
            CommLinkExternal.EXT_TO_GAME[i] = sender;
            CommLinkExternal.GAME_TO_EXT[i] = receiver;
            receiver.on('msg', sender.onResponse); // async callbacks
        });

        Coherent.Initialize(CommLinkExternal.EXT_TO_GAME[0], CommLinkExternal.GAME_TO_EXT[0]);

        window.reval = async (msg, idx = 0) => {
            return await CommLinkExternal.EXT_TO_GAME[idx].send({ command: 'eval', args: [msg] });
        }
        window.rreload = async (idx = 0) => {
            await window.reval("window.location.reload()", idx);
        }
    }
    this.Initialize = onInstalled;
}
CommLinkExternal = new CreateCommLinkExternal();