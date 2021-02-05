
fetch('/all_cfg', { method: "GET", }).then(response => response.json()).then(all_data => {

  window.cockpitcfg = {};
for (var d in all_data.cockpitcfg) {

  
  var key = d;
  if (key.indexOf('_') > 0) {
    var us = key.indexOf('_');
    var key2 = key.substring(0, us) + key.substring(us + 1, us + 2).toUpperCase() + key.substring(us + 2, key.length);
    key = key2;
  }
  window.cockpitcfg[key] =all_data.cockpitcfg[d];
}




  var data = all_data.gauges;
  var guid = 1;
  var instr = data.map(m => {
    let [url, x, y, w, h] = m.split(',').map(x => x.trim());
    var ret = {
      sUrl: url,
      iGUId: ++guid + "",
      vPosAndSize: { x: 0, y: 0, w: Number(h), z: Number(w) },
    }
    return ret;
  });

  var num = 0;
  if (window.location.search.startsWith("?id=")) {
    num = Number(window.location.search.substr(4, 1));
  }

  instr = [instr[num]];

  if (!instr[0]) {
    alert("Id is not valid");
  }

  window.globalPanelData = {
    sName: "External Gauge Bridge",
    vDisplaySize: { x: 1, y: 1 },
    vLogicalSize: { x: 1, y: 1 },
    sConfigFile: "panel.xml",
    daInstruments: instr,
    daAttributes: [],
  };

  function normalToFixed(num, p) { return num.toFixed(p); }
  window.fastToFixed = normalToFixed;
  BaseInstrument.allInstrumentsLoaded = true;

  SimVarBridge.IsReady = function () { return true; };
  SimVar.IsReady = SimVarBridge.IsReady;

  var panel = document.getElementById('main_panel');
  var vpanel = document.createElement('vcockpit-panel');
  vpanel.setAttribute('id', 'panel');
  panel.appendChild(vpanel);

  console.log('Loading...');
});