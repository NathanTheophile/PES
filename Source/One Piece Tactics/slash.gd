extends Node2D

export var amount = 4
onready var tween = $Tween
onready var dir = $dir

func _ready():
	position += Vector2(0,4)
	for i in amount:
		tween.interpolate_property(
			self, 
			"position", 
			position, 
			position+(dir.position*scale), 
			0.1)
		tween.start()
		yield(tween, "tween_completed")
		if !outing and i == amount-3:
			out()
			continue
		if !outing:
			var shadow = BattleMechanics.area.get_node("TileMap2")
			if shadow.get_cellv(shadow.world_to_map(global_position))==0:
				out()
	

var outing = false
func out():
	outing = true
	$AnimationPlayer.play("m")
	yield($AnimationPlayer, "animation_finished")
	queue_free()
