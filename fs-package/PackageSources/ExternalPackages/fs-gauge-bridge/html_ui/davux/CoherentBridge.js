var Coherent;

function CreateCoherentBridge() {
    Coherent = this;
    window.engine = this;

    let m_sender = null;
    let m_callbacks = {};
    let m_lastCallbackId = 0;
    let m_toSend = [];
    let m_inflightCalls = {};

    const SerializeCallback = (callbackFn) => {
        m_callbacks[++m_lastCallbackId] = callbackFn;
        return { id: m_lastCallbackId };
    }

    const send = (toSend) => {
        if (m_sender) {
            m_sender.send(toSend);
        } else {
            m_toSend.push(toSend);
        }
    };

    this.on = function (eventName, callback) {
      //  console.log("coherent.on " + eventName);
        return;
        send({
            command: 'coherent_on',
            args: [eventName, SerializeCallback(callback)]
        });
        // callback: _sender, _event
        //  console;.log("coherent.on " + eventName);
    }


    this.off = function (eventName, callback) {
    //    console.log("coherent.on " + eventName);
        return;
        send({
            command: 'coherent_on',
            args: [eventName, SerializeCallback(callback)]
        });
        // callback: _sender, _event
        //  console;.log("coherent.on " + eventName);
    }
    this.trigger = function () {
      //  console.log("coherent.trigger " + arguments[0]);
        return; 
        send({
            command: 'coherent_trigger',
            args: Array.prototype.slice.call(arguments)
        });
    };

    this.call = async (eventName, dataValue, anotherPromise) => {
          
        //  return new Promise((a, r) => {});

      //  if (m_inflightCalls[eventName]) {
            return new Promise((a, r) => { });
       // }
        console.log("coherent.call " + eventName);
        m_inflightCalls[eventName] = true;
        var ret = await m_sender.send({
            command: 'coherent_call',
            args: [eventName, dataValue]
        });
        console.log("coherent.call-complete " + eventName);
        console.log(ret);

        m_inflightCalls[eventName] = false;
        if (anotherPromise === undefined) {
            return Promise.resolve(ret);
        } else {
            return anotherPromise(ret);
        }
    };


    this.call333 = async (eventName, dataValue, anotherPromise) => {
        //    console.log("coherent.call " + eventName);
        return new Promise((a, r) => { });
        var sendPromise = m_sender.send({
            command: 'coherent_call',
            args: [eventName, dataValue]
        });
        if (anotherPromise === undefined) {
            return sendPromise;
        } else {
            await sendPromise;
            return anotherPromise;
        }
    }
    this.translate = (txt) => txt;

    let onCallback = (msg) => {
        console.log("coherent.onCallback " + msg.id);
        m_callbacks[msg.id].apply(m_callbacks[msg.id], msg.args);
    };

    this.Initialize = function (sender, receiver) {
        m_sender = sender;
        receiver.on('msg', (msg) => {
            try {
                var msg = JSON.parse(data);
                if (!msg) { return; }

                if (msg.cb) {
                    onCallback(msg);
                }
            } catch (ex) {
                return;
            }
        });
        console.log("create coherent bridge");

        m_toSend.forEach((m) => m_sender.send(m));
        m_toSend = [];

    }
}

Coherent = new CreateCoherentBridge();