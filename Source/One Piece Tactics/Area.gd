extends Node2D
export(int) var lvl
export(PackedScene) var playerCell
export(PackedScene) var enemySpawn
export(PackedScene) var visualSelec
export(PackedScene) var reDirect
export(PackedScene) var skillBtn
export(PackedScene) var skillCell
export(PackedScene) var dmgBubble
export(PackedScene) var remBubble
export(PackedScene) var healBubble

export(Array, PackedScene) var enemies
export(Array, PackedScene) var players
export(Array, int) var enemyWeight
export(Array, int) var waveTurns
var waveIdx = 0

var trophies = [true,true,true]

export(Color) var pirateColor

onready var cells = $Cells
onready var tilemap = $TileMap
onready var entities = $GeneralYsort/Characters
onready var nextWaveHolder = $GeneralYsort/NextWave
onready var mpLine = $Line2D
onready var skillsContainer = $UI/HBoxContainer

onready var startPosNode = $StartingPositions
onready var enemySpwNode = $EnemySpawn
onready var ysortvisual = $GeneralYsort/SkillVisuals
onready var overvisual = $SkillVisualsOver
onready var undervisual = $SkillVisualsUnder
onready var glyphNode = $Glyphs
onready var ui = $UI
onready var entityShower = $UI/CurrentPlayer
onready var stateVisuals = $UI/State
export (int) var deathtowin

var freeEnemyCells = []
var obstacles = []
export var enemyAmount:int
export var enemyfirst:bool
func _ready():
	BattleMechanics.setUp(self)
	
	
	var startPositCount = startPosNode.get_child_count()
	var enemySpawnCount = enemySpwNode.get_child_count()
	enemySpwNode.visible= false
	for i in startPositCount:
		var newCell = playerCell.instance()
		newCell.position = tilemap.map_to_world(tilemap.world_to_map(startPosNode.get_child(i).position))
		newCell.fieldPosit = tilemap.world_to_map(startPosNode.get_child(i).position)
		newCell.name = str(newCell.position.x)+str(newCell.position.y)
		
		cells.add_child(newCell)
	
	
	for i in enemySpawnCount:
		#var newCell = enemySpawn.instance()
		#newCell.position = tilemap.map_to_world(tilemap.world_to_map(enemySpwNode.get_child(i).position))
		freeEnemyCells.append(tilemap.world_to_map(enemySpwNode.get_child(i).position))
	#	
		#cells.add_child(newCell)
	
	for i in players:
		randomize()
		var addPlayer = i.instance()
		var selecCellReference = tilemap.map_to_world(tilemap.world_to_map(startPosNode.get_child(wrapi(randi(), 0, startPositCount)).position))
		
		var selecCell = $Cells.get_node(str(selecCellReference.x)+str(selecCellReference.y))
		
		while selecCell.entity:
			randomize()
			selecCellReference = tilemap.map_to_world(tilemap.world_to_map(startPosNode.get_child(wrapi(randi(), 0, startPositCount)).position))
			selecCell = $Cells.get_node(str(selecCellReference.x)+str(selecCellReference.y))
		
		addPlayer.posit = tilemap.world_to_map(selecCell.position)
		addPlayer.position = tilemap.map_to_world(addPlayer.posit)
		selecCell.entity = addPlayer
		
		entities.add_child(addPlayer)
		
	
	yield(addMonsters(),"completed")
	

func updateEntityDetails(entity):
	if BattleMechanics.currentPlaying and entity == BattleMechanics.currentPlaying:
		$UI/CurrentPlayer/HBoxContainer/APPanel/HBoxContainer/Current.text = str(entity.ap-entity.ap_used)
		$UI/CurrentPlayer/HBoxContainer/APPanel/HBoxContainer/Total.text = "/"+str(entity.ap)
		
		$UI/CurrentPlayer/HBoxContainer/MPPanel/HBoxContainer/Current.text = str(entity.mp-entity.mp_used)
		$UI/CurrentPlayer/HBoxContainer/MPPanel/HBoxContainer/Total.text = "/"+str(entity.mp)
		
		$UI/CurrentPlayer/NamePnl/Name.text = entity.ent_name
		
		$UI/CurrentPlayer/HPPanel/HBoxContainer/Current.text = str(entity.hp-entity.hp_loss)
		$UI/CurrentPlayer/HPPanel/HBoxContainer/Total.text = "/"+str(entity.hp)
		
		$UI/CurrentPlayer/HPPanel/TextureProgress.max_value = entity.hp
		$UI/CurrentPlayer/HPPanel/TextureProgress.value = entity.hp-entity.hp_loss

var mI = 0
func addMonsters():
	yield(get_tree(),"idle_frame")
	
	for _b in enemyAmount:
		randomize()
		
		
		var addEnemy = enemies[mI].instance()
		mI = wrapi(mI + 1,0,enemies.size())
		var positStartIdx = wrapi(randi(), 0, freeEnemyCells.size())
		
		BattleMechanics.placeEnemy(addEnemy, freeEnemyCells[positStartIdx])
		
		freeEnemyCells.remove(positStartIdx)
		yield(get_tree(),"idle_frame")

func changeLabel(text):
	$UI/ConfirmIcon/Label.text = text

func confirm():
	match BattleMechanics.battleState:
		0:
			if not BattleMechanics.selectedForPlacing:
				for i in cells.get_child_count():
					cells.get_child(i).queue_free()
				
				BattleMechanics.startBattle(self)
			else:
				changeLabel("Start battle")
				BattleMechanics.finishSelection()
		1:
			if BattleMechanics.currentPlaying and not BattleMechanics.currentPlaying.busy:
				#if BattleMechanics.currentPlaying.reDirect:
				BattleMechanics.bounceTgt(BattleMechanics.currentPlaying)
				BattleMechanics.turnEnd(BattleMechanics.currentPlaying)
				
				#	return
				#else:
				#	BattleMechanics.reDirect(BattleMechanics.currentPlaying)
				#	return
var currentLine = []
func _input(event):
	if event is InputEventKey and (event.pressed or event.echo) and event.scancode==KEY_Z:
		for i in entities.get_children():
			if not i.hideSts:
				i.get_node("HPBar").visible = false
		for i in stateVisuals.get_children():
			i.queue_free()
	elif event is InputEventKey and (not event.pressed) and event.scancode==KEY_Z:
		for i in entities.get_children():
			if not i.hideSts:
				i.get_node("HPBar").visible = true
	match BattleMechanics.battleState:
		
		0:
			if event is InputEventKey and event.pressed and event.scancode==KEY_SPACE:
				confirm()
			if (event is InputEventKey and event.pressed and event.scancode==KEY_ESCAPE) or (
				event is InputEventMouseButton and event.pressed and event.button_index == 2):
				changeLabel("Start battle")
				BattleMechanics.finishSelection()
		
		1:
			if BattleMechanics.currentPlaying and not BattleMechanics.currentPlaying.busy:
				
				# Inputs pra terminar o turno
				if event is InputEventKey and event.pressed and not event.echo and event.scancode == KEY_SPACE:
					confirm()
					return
				
				# Input de deselecionar um personagem
				if not BattleMechanics.currentPlaying in BattleMechanics.havePlayedThisTurn:
					if (event is InputEventKey and event.pressed and event.scancode==KEY_ESCAPE) or (
						event is InputEventMouseButton and event.pressed and event.button_index == 2):
							BattleMechanics.deselectChar()
							return
				
				
				if not BattleMechanics.currentPlaying.reDirect:
					
					if not BattleMechanics.skillActive:
						if event is InputEventMouseMotion:
							var mousePos = tilemap.world_to_map(get_viewport().get_mouse_position())
							currentLine = BattleMechanics.mp_line_generate(mousePos)
							
							if currentLine.size()-1 <= BattleMechanics.currentPlaying.mp - BattleMechanics.currentPlaying.mp_used:
								mpLine.points = currentLine
							else:
								mpLine.points = []
						
						if event is InputEventMouseButton and event.button_index == 1 and event.pressed:
			
								if currentLine.size()>0 and currentLine.size()-1 <= BattleMechanics.currentPlaying.mp - BattleMechanics.currentPlaying.mp_used:
									yield(BattleMechanics.walk(BattleMechanics.currentPlaying, currentLine),"completed")
									currentLine = []
			elif BattleMechanics.currentPlaying and BattleMechanics.currentPlaying.busy and (BattleMechanics.skillActive or BattleMechanics.skillEffectActive):
				if event is InputEventMouseButton and event.button_index == 2 and not event.pressed:
					BattleMechanics.cancelSkill(BattleMechanics.currentPlaying)
				if event is InputEventKey and event.scancode == KEY_ESCAPE and event.pressed:
					BattleMechanics.cancelSkill(BattleMechanics.currentPlaying)
			
			elif not BattleMechanics.currentPlaying:
				if event is InputEventKey and event.pressed and not event.echo and event.scancode == KEY_SPACE:
					BattleMechanics.endPlayersTurn()


