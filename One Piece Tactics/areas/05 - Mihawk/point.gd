extends Node2D

func _ready():
	var mihawk
	for i in BattleMechanics.area.entities.get_children():
		if i.ent_name == "Dracule Mihawk":
			mihawk = i
	
	var dir = global_position.direction_to(mihawk.global_position-Vector2(0,50))
	rotation_degrees = rad2deg(Vector2(1,0).angle_to(dir))
