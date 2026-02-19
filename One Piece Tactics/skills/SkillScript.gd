extends Node2D

export(String) var skillName
export(String, MULTILINE) var description
export(Texture) var icon1
export(Texture) var icon2

export(int) var base
export(int) var base2
export(int) var apCost
export(int,"pos","neg","misc","special") var element

export(int) var rangeMin = 1
export(int) var rangeMax = 1

export(bool) var linear
export(bool) var sight

export(int, "Single","Circle","Square","Cross","X","VLine","HLine","Cone","ConeReverse") var aoeShape
export(int) var aoeSize = 1

export(int) var usePerTgt = 0
export(int) var usePerTurn = 0
export(int) var cooldown = 0

export(bool) var xvalue = false
export(bool) var tgtTypeEntity = false

export(Array,PackedScene) var visuals
export(Script) var effects
export(Array,PackedScene) var states
export(Array, PackedScene) var helpertext

var caster
var usedOn = {}
var useAmt = 0

var cooldownTurns = 0


func _ready():
	$Effects.set_script(effects)
