extends Node2D

export(String) var stateName
export(String, MULTILINE) var description
var window = preload("res://states/StatePreview.tscn")

export(int, "State","Buff","Debuff","Passive") var type
export(Script) var effects
export(Array, PackedScene) var visuals
export(Array, PackedScene) var extraStates
export(bool) var appearOver = false
export(int) var maxStack = 0

var caster
var lvl = 1
export var stacks = false
export var modularDesc = false
export(float) var lvlMlt = 1
export(float) var lvl2Mlt = 1
export(float) var lvl3Mlt = 1
export(int) var lvlSub = 0
func _ready():
	$Effects.set_script(effects)
	$Effects.stateSetup()

var currentIcon
var newWindow
func showEffects():
	newWindow = window.instance()
	newWindow.icon = currentIcon
	newWindow.state = self
	BattleMechanics.area.stateVisuals.add_child(newWindow)
	newWindow.details()
	newWindow.showSelf()

func hideEffects():
	if newWindow:
		if newWindow.has_method("hideSelf"):
			newWindow.hideSelf()
			newWindow.queue_free()
