extends Node2D

var posit
var player = false
var obstacle = true

var midBattle = false

func _ready():
	if not midBattle:
		yield(get_parent().get_parent().get_parent(),"ready")
	position = BattleMechanics.area.tilemap.map_to_world(BattleMechanics.area.tilemap.world_to_map(position))
	posit = BattleMechanics.area.tilemap.world_to_map(position)
	BattleMechanics.obstacleAdd(self)
	BattleMechanics.area.tilemap.set_cellv(posit,-1)
