# fs-gauge-bridge

Copy `.\MSFS\Debug\BridgeGauge.wasm` to the `Panel` folder in an aircraft layout.

Add a new `VCockpitXX` section in `panel.cfg`:

```
[Vcockpit06]
size_mm=0,0
pixel_size=0,0
texture=$PFD
background_color=0,0,0
htmlgauge00=WasmInstrument/WasmInstrument.html?wasm_module=BridgeGauge.wasm&wasm_gauge=BridgeGauge,0,0,1,1
```

Load the package, resync the aircraft.


Turn on landing lights:
`1 (>A:LIGHT LANDING,Bool)`

Query landing lights (will update whenever the value changes, current query is always 'active':
`(A:LIGHT LANDING,Bool)`

Only double value support.