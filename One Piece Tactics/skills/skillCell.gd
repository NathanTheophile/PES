extends Polygon2D
export(PackedScene) var aoeCell

var skill

var arrayCells = []
var cells = []
var disabled = false
var posit
func _ready():
	posit = BattleMechanics.area.tilemap.world_to_map(position)
func hover(x, enemyTgt=false):
	if (not disabled):
		if x:
			if posit != skill.caster.posit:
				skill.caster.turn_to(BattleMechanics.directionTo(skill.caster.posit,posit))
			match skill.aoeShape:
				0: # Single
					var newAoe = aoeCell.instance() 
					newAoe.position = position
					cells.append(newAoe)
					
					newAoe.visible = !enemyTgt
					
					arrayCells.append(posit)
					get_parent().add_child(newAoe)
				1: # Circle
					for i in range(skill.aoeSize*-1,skill.aoeSize+1):
						for j in range(skill.aoeSize*-1,skill.aoeSize+1):
							if abs(i)+abs(j)<=skill.aoeSize:
								var newAoe = aoeCell.instance() 
								newAoe.position = BattleMechanics.area.tilemap.map_to_world(posit+Vector2(i,j)) 
								cells.append(newAoe)
								newAoe.visible = !enemyTgt
								if i==0 and j==0:
									arrayCells.append([posit+Vector2(i,j)])
								else:
									arrayCells.append(posit+Vector2(i,j))
									
								get_parent().add_child(newAoe)
				2: # Square
					for i in range(skill.aoeSize*-1,skill.aoeSize+1):
						for j in range(skill.aoeSize*-1,skill.aoeSize+1):
							var newAoe = aoeCell.instance() 
							newAoe.position = BattleMechanics.area.tilemap.map_to_world(BattleMechanics.area.tilemap.world_to_map(position)+Vector2(i,j)) 
							cells.append(newAoe)
							newAoe.visible = !enemyTgt
							if i==0 and j==0:
								arrayCells.append([BattleMechanics.area.tilemap.world_to_map(position)+Vector2(i,j)])
							else:
								arrayCells.append(BattleMechanics.area.tilemap.world_to_map(position)+Vector2(i,j))
								
							get_parent().add_child(newAoe)
				3: # Cross
					for i in range(skill.aoeSize*-1,skill.aoeSize+1):
						for j in range(skill.aoeSize*-1,skill.aoeSize+1):
							if i==0 or j==0:
								var newAoe = aoeCell.instance() 
								newAoe.position = BattleMechanics.area.tilemap.map_to_world(BattleMechanics.area.tilemap.world_to_map(position)+Vector2(i,j)) 
								cells.append(newAoe)
								newAoe.visible = !enemyTgt
								if i==0 and j==0:
									arrayCells.append([BattleMechanics.area.tilemap.world_to_map(position)+Vector2(i,j)])
								else:
									arrayCells.append(BattleMechanics.area.tilemap.world_to_map(position)+Vector2(i,j))
									
								get_parent().add_child(newAoe)
				4: # X
					for i in range(skill.aoeSize*-1,skill.aoeSize+1):
						for j in range(skill.aoeSize*-1,skill.aoeSize+1):
							if abs(i)==abs(j):
								var newAoe = aoeCell.instance() 
								newAoe.position = BattleMechanics.area.tilemap.map_to_world(BattleMechanics.area.tilemap.world_to_map(position)+Vector2(i,j)) 
								cells.append(newAoe)
								newAoe.visible = !enemyTgt
								if i==0 and j==0:
									arrayCells.append([BattleMechanics.area.tilemap.world_to_map(position)+Vector2(i,j)])
								else:
									arrayCells.append(BattleMechanics.area.tilemap.world_to_map(position)+Vector2(i,j))
									
								get_parent().add_child(newAoe)
				5: #VLine
					for i in range(skill.aoeSize*-1,skill.aoeSize+1):
						var direction = BattleMechanics.directionTo(BattleMechanics.area.tilemap.world_to_map(position), skill.caster.posit)
						var offset = Vector2(0,1)
						if abs(direction.x)>abs(direction.y):
							offset = Vector2(1,0)
						
						var check = false
						if direction.x+direction.y > 0:
							check = true if i<=0 else false
						else:
							check = true if i>=0 else false
						
						if check:
							var newAoe = aoeCell.instance() 
							var posit = BattleMechanics.area.tilemap.map_to_world(BattleMechanics.area.tilemap.world_to_map(position)+(offset*i))
							newAoe.position = posit
							cells.append(newAoe)
							newAoe.visible = !enemyTgt
							if i==0:
								arrayCells.append([BattleMechanics.area.tilemap.world_to_map(position)+(offset*i)])
							else:
								arrayCells.append(BattleMechanics.area.tilemap.world_to_map(position)+(offset*i))
								
							get_parent().add_child(newAoe)
				6: #HLine
					var newAoe = aoeCell.instance() 
					newAoe.position = position
					cells.append(newAoe)
					
					newAoe.visible = !enemyTgt
					
					arrayCells.append([posit])
					get_parent().add_child(newAoe)
					
					for i in range(1,skill.aoeSize+1):
						var offset = Vector2(1,0) if BattleMechanics.directionTo(skill.caster.posit,posit).y!=0 else Vector2(0,1)
						
						for j in 2:
							newAoe = aoeCell.instance() 
							var mapPosit = BattleMechanics.area.tilemap.world_to_map(position)+((offset*(-1 if j == 0 else 1))*i)
							newAoe.position = BattleMechanics.area.tilemap.map_to_world(mapPosit)
							cells.append(newAoe)
							newAoe.visible = !enemyTgt
							arrayCells.append(mapPosit)
								
							get_parent().add_child(newAoe)
		else:
			if cells.size()>0:
				for i in cells.size():
					cells[i].queue_free()
			cells = []
			arrayCells = []

func noVision():
	visualOnly = true
	color.a = 0.376471

func customSelect():
	hover(true,true)
	BattleMechanics.useSkill(arrayCells, skill, skill.caster)

var visualOnly = false

func disable():
	disabled = true
	modulate.a = 0.5

func able(_a,_b,before):
	if not before:
		disabled = false
		modulate.a = 1
		
func select(_viewport, event, _shape_idx):
	if not visualOnly and not disabled:
		if event is InputEventMouseButton and event.button_index == 1 and not event.pressed:
			#
			
			if cells.size()==0:
				hover(true)
			
			BattleMechanics.useSkill(arrayCells, skill, skill.caster)
			
			#hover(false)
			#for i in BattleMechanics.area.cells.get_children():
			#	if i.has_method("disable"):
			#		i.disable()
			
			#if not BattleMechanics.cellCondition(skill, skill.caster, posit):
			#	queue_free()
			#for i in BattleMechanics.area.cells.get_children():
			#	if i.has_method("disable"):
			#		if not BattleMechanics.cellCondition(skill,skill.caster, i.posit):
			#			i.queue_free()
			BattleMechanics.cancelSkill(skill.caster)
			
			if (
				(skill.apCost > skill.caster.ap - skill.caster.ap_used) or
				(skill.usePerTurn > 0 and skill.useAmt >= skill.usePerTurn) or
				(skill.cooldown > 0 and skill.cooldownTurns > 0)
				):
				for i in BattleMechanics.area.skillsContainer.get_children():
					
						#BattleMechanics.area.skillsContainer.get_child(i).pushed = false
						#BattleMechanics.area.skillsContainer.get_child(i).get_node("AnimationPlayer").play_backwards("press")
					if i.has_method("push"):
						i.push(false)
