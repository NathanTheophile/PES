extends Node2D

var posit
var caster
export(int) var id
export(String) var glyphName
export(String, MULTILINE) var glyphDesc
 
export(Script) var effects
export(Array, PackedScene) var extraStates

func _ready():
	if effects:
		$Effects.set_script(effects)
		$Effects.setup()
