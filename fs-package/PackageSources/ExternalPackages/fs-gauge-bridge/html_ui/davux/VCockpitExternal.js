var VCockpitExternal;

function CreateVCockpitExternal() {
  function installShims() {
    // BUG: fastToFixed returns .-07, breaks everything.
    function normalToFixed(num, p) { return num.toFixed(p); }
    window.fastToFixed = normalToFixed;

    // Tell everything that we're ready
    BaseInstrument.allInstrumentsLoaded = true;
    SimVarBridge.IsReady = function () { return true; };
    SimVar.IsReady = SimVarBridge.IsReady;  }

  function CreatePanel() {
    var vpanel = document.createElement('vcockpit-panel');
    vpanel.setAttribute('id', 'panel');
    document.getElementById('main_panel').appendChild(vpanel);
  }

  // Asobo decided to convert the cfg file where some_key becomes someKey.
  function convertToCfg(cfgData) {
    let ret = {};
    for (var d in cfgData) {
      let key = d;
      if (key.indexOf('_') > 0) {
        var us = key.indexOf('_');
        var key2 = key.substring(0, us) + key.substring(us + 1, us + 2).toUpperCase() + key.substring(us + 2, key.length);
        key = key2;
      }
      ret[key] = cfgData[d];
    }
    return ret;
  }

  fetch('/all_cfg').then(response => response.json()).then(all_data => {
    if (all_data.error) {
      alert("Fatal Error: panel.cfg data not yet available");
      return;
    }

    VCockpitExternal.cockpitCfg = convertToCfg(all_data.cockpitcfg);
    VCockpitExternal.panelCfg = all_data.gauges;

    var allInstruments = all_data.gauges.map((m, idx) => ({
      sUrl: m.htmlgauge00.path,
      iGUId: idx.toString(),
      vPosAndSize: { x: 0, y: 0, w: Number(m.htmlgauge00.height), z: Number(m.htmlgauge00.width) },
    }));

    let selectedId = window.location.search.startsWith("?id=") ? Number(window.location.search.substr(4, 1)) : 0;
    let selectedInstrument = allInstruments[selectedId];
    let selectedInstrumentData = all_data.gauges[selectedId];
    if (!selectedInstrument) {
      alert("Id is not valid");
    }

    window.globalPanelData = {
      sName: "External Gauge Bridge",
      vDisplaySize: { x: selectedInstrument.pixel_size_x, y: selectedInstrument.pixel_size_y },
      vLogicalSize: { x: selectedInstrument.size_mm_x, y: selectedInstrument.size_mm_y },
      sConfigFile: selectedInstrumentData.panel_path,
      daInstruments: [selectedInstrument],
      daAttributes: [],
    };

    installShims();
    CreatePanel();
  });
}
VCockpitExternal = new CreateVCockpitExternal();