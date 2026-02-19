extends Node

# 0 = posit 
# 1 = player
# 2 = enemy
# 3 = cncl


var globalDMGMult =	 1
var globalHPMult =	 1

var battleState = 0
var currentObstacles = {}

var spellLabel = preload("res://SpellLabel.tscn")

var selectedForPlacing = false
func selectForPlacing(cell):
	selectedForPlacing = cell
	cell.get_node("Light").visible = true
	
	cell.get_node("Arrow").visible = true
	cell.get_node("Arrow2").visible = true
	cell.get_node("Arrow3").visible = true
	cell.get_node("Arrow4").visible = true

func finishSelection():
	if selectedForPlacing:
		selectedForPlacing.get_node("Light").visible = false
		
		selectedForPlacing.get_node("Arrow").visible = false
		selectedForPlacing.get_node("Arrow2").visible = false
		selectedForPlacing.get_node("Arrow3").visible = false
		selectedForPlacing.get_node("Arrow4").visible = false
		
		selectedForPlacing = false

var currentPlaying = false
var havePlayedThisTurn = []
var enemiesArray = []

func placeEnemy(enemy, posit):
	enemy.posit = posit
	enemy.position = area.tilemap.map_to_world(posit)
	if not enemy.big:
		area.tilemap.set_cellv(posit, 1)
	else:
		for i in range(-1,2):
			for j in range(-1,2):
				enemy.allPosits.append(Vector2(i,j)+posit)
		for i in enemy.allPosits:
			area.tilemap.set_cellv(i, 1)
			
	
	area.entities.add_child(enemy)
	emit_signal("entityAdded",enemy,true)
	
	enemy.turn_to(findDirectionToLook(enemy))

func setUp(areaNode):
	area = areaNode
	currentPlaying = false
	currentRound = 1
	astar.clear()
	enemyOrder = []
	enemyOrderIdx = 0
	battleState = 0
	currentObstacles = {}

func startBattle(areaNode):
	currentDead = 0
	if !area.enemyfirst:
		playerTurnStart()
	else:
		currentRound=0
		endPlayersTurn()
	area.ui.get_node("Button").text = "End Turn"
	emit_signal("battleStart")

signal battleStart
signal endPlayersTurn
signal endEnemysTurn
signal entityAdded(entity, wave)
var currentRound = 1

var astar = AStar2D.new()
var area = false

func obstacleAdd(obst):
	currentObstacles[str(obst.posit)] = obst

func obstacleRemove(obst):
	currentObstacles.erase(str(obst.posit))

var selecVisuals = []
func playerTurnStart():
	area.ui.setCurrentTeam("player")
	area.ui.get_node("Button").disabled=false
	battleState = 1
	havePlayedThisTurn = []
	var first = false
	for i in area.entities.get_children():
		if i.player:
			area.tilemap.set_cellv(i.posit, 2)
			
			if i.plays:
				selecVisuals.append(playAnim(area.visualSelec,i.posit,false,"over",Vector2(0,0),true))
				
				if first:
					first = true
				
				if !first:
					first = i
				
		else:
			if not i.get("obstacle"):
				area.tilemap.set_cellv(i.posit, 1)
	
	if not first is bool:
		setPlaying(first)
		first.busy = true
		yield(get_tree().create_timer(0.1),"timeout")
		first.busy = false



# Cria o AStar
func setAStar(exception = false, includeCurrentPlayer = true):
	astar.clear()
	var cells = area.tilemap.get_used_cells_by_id(0)
	
	if currentPlaying and includeCurrentPlayer:
		if not currentPlaying.big:
			cells.append(currentPlaying.posit)
		else:
			cells.append_array(currentPlaying.allPosits)
	if exception:
		cells.append(exception.posit)
	
	
	for i in cells.size():
		var flag = true
		if currentPlaying and includeCurrentPlayer and currentPlaying.big:
			for j in range(-1,2):
				for k in range(-1,2):
					if not (cells[i]+Vector2(j,k)) in cells:
						flag = false
						break
				if not flag:
					break
		if flag:
			var id = cells[i].x*200+cells[i].y
			if astar.has_point(id):
				continue
			astar.add_point(id, area.tilemap.map_to_world(cells[i]))
			
			if astar.has_point(id+1) and not astar.are_points_connected(id,id+1):
				astar.connect_points(id,id+1)
			
			if astar.has_point(id-1) and not astar.are_points_connected(id,id-1):
				astar.connect_points(id,id-1)
			
			if astar.has_point(id+200) and not astar.are_points_connected(id,id+200):
				astar.connect_points(id,id+200)
			
			if astar.has_point(id-200) and not astar.are_points_connected(id,id-200):
				astar.connect_points(id,id-200)

# Converte a posição pra um ID do AStar
func toAStarId(posit):
	return posit.x*200+posit.y

# Gera a linha de PM para uma entidade andar, partindo dela até a coordenada dada
func mp_line_generate(mouse):
	var line = []
	if astar.has_point(toAStarId(currentPlaying.posit)) and astar.has_point(toAStarId(mouse)):
		var astarPath = astar.get_id_path(
			toAStarId(currentPlaying.posit), 
			toAStarId(mouse))
		
		if astarPath.size()>0:
			for i in astarPath.size():
				line.append(astar.get_point_position(astarPath[i]))
	
	return line

func mp_line_simul(entity, posit):
	setAStar(entity, false)
	if astar.has_point(toAStarId(entity.posit)) and astar.has_point(toAStarId(posit)):
		
		var astarPath = astar.get_id_path(
			toAStarId(entity.posit), 
			toAStarId(posit))
		
		if astarPath.size()-1 <= entity.mp - entity.mp_used:
			return true
	return false
	

# Faz a entidade andar a linha dada
var forceStop = false
var walkActive = false
func walk(entity, line):
	yield(get_tree(),"idle_frame")
	if line and line.size()>1:
		area.mpLine.points = []
		walkActive = true
		
		entity.busy=true
		var cellsTransversed = 0
		
		playAnimEntity(entity,["walkStart","walk"])
		
		for i in line.size()-1:
			#if i == line.size()-2 and entity.get_node("AnimationsF").has_animation("walkStop"):
				#playAnimEntity(entity,"walkStop")
			
			setAStar()
			if entity.hp-entity.hp_loss<=0:
				return
			entity.walkTween.interpolate_property(
				entity,
				"position",
				entity.position,
				line[i+1],
				entity.speed
			)
			
			if entity.ent_name != "Jango":
				entity.turn_to(directionTo(entity.posit, area.tilemap.world_to_map(line[i+1])))
			else:
				entity.turn_to(directionTo(entity.posit, area.tilemap.world_to_map(line[i+1]))*-1)
			entity.walkTween.start()
			
			if not entity.big:
				area.tilemap.set_cellv(entity.posit, 0)
			else:
				area.tilemap.set_cellv(entity.posit, 0)
				for j in entity.allPosits:
					area.tilemap.set_cellv(j, 0)
			
			if entity.player:
				area.tilemap.set_cellv(area.tilemap.world_to_map(line[i+1]), 2)
			else:
				var center = area.tilemap.world_to_map(line[i+1])
				area.tilemap.set_cellv(center, 1)
				if entity.big:
					for j in range(-1,2):
						for k in range(-1,2):
							area.tilemap.set_cellv(center+Vector2(j,k), 1)
			
						
			
			yield(entity.walkTween,"tween_completed")
			
			if entity.stepSound:
				AM.play(entity.stepSound,{"randp": 1.5})
			entity.emit_signal("walked",entity.posit,area.tilemap.world_to_map(line[i+1]))
			cellsTransversed+=1
			entity.posit = area.tilemap.world_to_map(entity.position)
			if entity.big:
				entity.allPosits = []
				for j in range(-1,2):
					for k in range(-1,2):
						entity.allPosits.append(Vector2(j,k)+entity.posit)
			if forceStop:
				break
		
		
		entity.mp_used+=cellsTransversed
		playAnimEntity(entity,"walkStop")
		if entity.ent_name == "Jango":
			entity.turn_to(entity.direction *-1)
		setAStar()
		entity.busy = false
		forceStop = false
		walkActive = false
		for i in area.cells.get_children():
			i.queue_free()
		area.updateEntityDetails(entity)
		yield(get_tree(),"idle_frame")

# Função que retorna a direção de ponto A a B
func directionTo(a,b):
	if abs(a.y-b.y)>abs(a.x-b.x):
		if a.y-b.y>0:
			return Vector2(0,-1)
		else:
			return Vector2(0,1)
	else:
		if a.x-b.x>0:
			return Vector2(-1,0)
		else:
			return Vector2(1,0)

#func _ready():
#	while 1:
#		yield(get_tree().create_timer(0.5),"timeout")
#		var b = null
#		if currentPlaying:
#			b = currentPlaying.busy
#		print("skillActive = "+str(skillActive)+", skillEffectActive = "+str(skillEffectActive) + ", busy = " + str(b))

# Função que define a entidade jogadora a jogar agora
var yieldTurnStart = 0
var turnStartSkip = false
func setPlaying(entity):
	entity.emit_signal("turn_start")
	if entity.player:
		var redirect = area.ui.get_node("ReDirect")
		redirect.entity = entity
		redirect.visible = true
	if yieldTurnStart>0:
		yield(get_tree().create_timer(yieldTurnStart),"timeout")
		yieldTurnStart = 0
	if not turnStartSkip:
		area.entityShower.visible = true
		currentPlaying = entity
		area.updateEntityDetails(entity)
		
		entity.notPlayed = false
		setAStar()
		entity.connect("walked",self,"acted")
		entity.connect("skill_used",self,"acted")
		
		for i in area.cells.get_child_count():
			area.cells.get_child(i).queue_free()
		for i in selecVisuals:
			i.queue_free()
		selecVisuals = []
		
		area.changeLabel("End this character's move")
		addSkills(entity)
		
		entity.set_process_input(true)
	else:
		turnStartSkip = false
		turnEnd(entity)

func acted(_a,_b,_c=false):
	havePlayedThisTurn.append(currentPlaying)
	currentPlaying.disconnect("walked",self,"acted")
	currentPlaying.disconnect("skill_used",self,"acted")

func isStillAlive(entity):
	if entity.get_parent():
		return true
	return false

func deselectChar():
	
	var c=0
	for i in area.entities.get_children():
		if i.player and not i in havePlayedThisTurn:
			c+=1
	if c>1:
		currentPlaying = false
		area.mpLine.points = []
		
		for i in area.skillsContainer.get_child_count():
			area.skillsContainer.get_child(i).queue_free()
			
		for i in area.cells.get_child_count():
			area.cells.get_child(i).queue_free()
		
		for i in area.entities.get_children():
			if not i.get("obstacle") and i.player and not i in havePlayedThisTurn:
				selecVisuals.append(playAnim(area.visualSelec,i.posit,false,"over",Vector2(0,0),true))
		
		area.entityShower.visible = false

func allOfTeam(player=true):
	var result = []
	for i in area.entities.get_child_count():
		if area.entities.get_child(i).player==player:
			result.append(area.entities.get_child(i))
	return result

var separator = preload("res://menus/Separator.tscn")
func addSkills(entity):
	if area.skillsContainer.get_child_count()>0:
		for i in area.skillsContainer.get_child_count():
			area.skillsContainer.get_child(i).queue_free()
	
	for i in entity.skillsUsable.size():
		var newBtn = area.skillBtn.instance()
		
			
			
		area.skillsContainer.add_child(newBtn)
		newBtn.setAttributes(entity.skillsUsable[i])
		newBtn.setShortCut(i)
		
	updateSkillBtns()

func cellCondition(skill, entity, cell) -> bool:
	if skill.usePerTgt>0:
		if not skill.tgtTypeEntity:
			if not skill.usedOn.has(str(cell)) or (skill.usedOn.has(str(cell)) and skill.usedOn[str(cell)]<skill.usePerTgt):
				return true
		else:
			if skill.usedOn.size()>0:
				var keys = skill.usedOn.keys()
				for i in keys.size():
					if area.entities.has_node(keys[i]):
						var e = area.entities.get_node(keys[i])
						if (!e.big and e.posit == cell) or (e.big and cell in e.allPosits):
							if skill.usedOn[keys[i]] < skill.usePerTgt:
								return true
							else:
								return false
				return true
			else:
				return true
	else:
		return true
	
	return false

var aiMonsterYield = 0

var skillActive = false
var currentActiveSkill = false
func skillRange(skill, entity, setTgt=false):
	if setTgt:
		skillunable = true
	entity.busy = true
	if not setTgt:
		skillActive = true
		currentActiveSkill = skill
	var rangeU = ((skill.rangeMax+skill.caster.extraRange) if skill.rangeMax+skill.caster.extraRange > skill.rangeMin else skill.rangeMin)
	for i in range(rangeU*-1,1+rangeU):
		for j in range(rangeU*-1,1+rangeU):
			var distance = abs(i)+abs(j)
			if ((distance <= skill.rangeMax+(skill.caster.extraRange if skill.rangeMax > 0 else 0)) if skill.rangeMax+(skill.caster.extraRange if skill.rangeMax > 0 else 0)>skill.rangeMin else distance == skill.rangeMin) and distance >= skill.rangeMin:
				if ((i==0 or j==0) and skill.linear) or not skill.linear:
					if cellCondition(skill, entity, entity.posit + Vector2(i,j)):
							
						if area.tilemap.get_cellv(Vector2(i,j)+entity.posit)>-1:
							var skillCell = area.skillCell.instance()
							
							skillCell.position = area.tilemap.map_to_world(entity.posit + Vector2(i,j))
							skillCell.skill = skill
							if setTgt:
								skillCell.visualOnly = true
								skillCell.visible = false
							
							if skill.sight and checkSight(entity.posit, entity.posit+Vector2(i,j)):
								skillCell.noVision()
							
							entity.connect("skill_used",skillCell, "able")
							area.cells.add_child(skillCell)
							if setTgt and setTgt == entity.posit + Vector2(i,j):
								skillCell.customSelect()
								skillunable = false

func getDirectionalDmg(caster,tgt):
	if tgt.direction == directionTo(caster.posit, tgt.posit):
		return 1.25
	
	elif abs(directionTo(caster.posit, tgt.posit).x)>abs(tgt.direction.x) or abs(directionTo(caster.posit, tgt.posit).y)>abs(tgt.direction.y):
		return 1.1
	
	return 1

func cancelSkill(entity):
	if entity.busy and not skillEffectActive:
		entity.busy = false
	skillActive = false
	currentActiveSkill = false
	for i in area.cells.get_child_count():
		area.cells.get_child(i).queue_free()
	for i in area.skillsContainer.get_children():
		if i.get("skill"):
			i.push(false)

# Função que permite a entidade jogadora se redirecionar no fim de seu movimento
func reDirect(entity):
	var newReDirect = area.reDirect.instance()
	
	newReDirect.position = entity.position
	newReDirect.entity = entity
	
	for i in area.skillsContainer.get_children():
		if i.get("skill"):
			i.disable()
	entity.reDirect = true
	area.mpLine.points = []
	area.cells.add_child(newReDirect)

# Função que termina o turno de uma entidade jogadora
func turnEnd(entity):
	if entity.player:
		
		var redirect = area.ui.get_node("ReDirect")
		redirect.entity = false
		redirect.visible = false
		
		entity.mp_used = 0
		entity.ap_used = 0
		
		entity.reDirect = false
		entity.busy = false
		currentPlaying = false
		
		entity.modulate = Color(0.65, 0.65, 0.65)
		area.mpLine.points = []
		
		entity.set_process_input(false)
		
		for i in area.skillsContainer.get_child_count():
			area.skillsContainer.get_child(i).queue_free()
		
		for i in area.cells.get_child_count():
			area.cells.get_child(i).queue_free()
		if not entity in havePlayedThisTurn:
			havePlayedThisTurn.append(entity)
		
		
		
		for i in entity.skillsUsable.size():
			if entity.skillsUsable[i].cooldown > 0 and entity.skillsUsable[i].cooldownTurns > 0:
				entity.skillsUsable[i].cooldownTurns -= 1
			entity.skillsUsable[i].useAmt = 0
			entity.skillsUsable[i].usedOn = {}
		
		
		
		entity.emit_signal("turn_end")
		
		area.entityShower.visible = false
	setAStar()
	
	var skip = true
	var first
	for i in area.entities.get_children():
		if not i.get("obstacle") and i.player and not i in havePlayedThisTurn and i.plays:
			selecVisuals.append(playAnim(area.visualSelec,i.posit,false,"over",Vector2(0,0),true))
			skip = false
			
			if first:
				first = true
			if !first:
				first = i
	
	if skip:
		yield(get_tree(),"idle_frame")
		endPlayersTurn()
		return
	
	if not first is bool:
		setPlaying(first)
		first.busy = true
		yield(get_tree().create_timer(0.1),"timeout")
		first.busy = false
	
	

# Função que encerra o turno do jogador
func endPlayersTurn():
	battleState = 2
	
	for i in area.cells.get_child_count():
		area.cells.get_child(i).queue_free()
	for i in selecVisuals:
		i.queue_free()
		selecVisuals = []
		
	area.ui.get_node("Button").disabled=true
	
	for i in area.entities.get_child_count():
		if not area.entities.get_child(i).get("obstacle") and not area.entities.get_child(i).player and not area.entities.get_child(i) in enemyOrder and area.entities.get_child(i).plays:
			enemyOrder.append(area.entities.get_child(i))
		elif area.entities.get_child(i).player:
			area.entities.get_child(i).modulate=Color(1,1,1)
			if area.entities.get_child(i).notPlayed:
				for j in area.entities.get_child(i).skillsUsable.size():
					area.entities.get_child(i).skillsUsable[j].cooldownTurns -= 1
					area.entities.get_child(i).skillsUsable[j].useAmt = 0
					area.entities.get_child(i).skillsUsable[j].usedOn = {}
			
			area.entities.get_child(i).notPlayed = true
	
	area.skillsContainer.visible=false
	enemyOrderIdx = 0
	emit_signal("endPlayersTurn")
	if area.enemyfirst:
		currentRound+=1
		area.ui.changeRound(currentRound)
	if enemyOrder.size()>0:
		enemyStart(enemyOrder[enemyOrderIdx])
		area.ui.setCurrentTeam("enemy")
		
	else:
		endEnemyTurn()

var enemyOrder = []
var enemyOrderIdx = 0

# Função que inicia o turno de uma entidade
func enemyStart(entity):
	entity.emit_signal("turn_start")
	if yieldTurnStart>0:
		yield(get_tree().create_timer(yieldTurnStart),"timeout")
		yieldTurnStart = 0
	
	
	if not turnStartSkip:
		area.entityShower.visible = true
		
		currentPlaying = entity
		area.updateEntityDetails(entity)
		#addSkills(entity)
		setAStar()
		entity.get_node("AI").ai_loop()
		
	else:
		turnStartSkip = false
		if entity!=null:
			enemyEnd(entity)
		else:
			enemyDeadWhilePlaying()

func enemyDeadWhilePlaying():
	enemyOrderIdx+=1
		
	for i in area.cells.get_child_count():
		area.cells.get_child(i).queue_free()
	
	if enemyOrderIdx==enemyOrder.size():
		currentPlaying = false
		endEnemyTurn()
		return
	
	enemyStart(enemyOrder[enemyOrderIdx])

var playedEnemies = []

func enemyEnd(entity):
	if entity!=null:
		entity.emit_signal("turn_end")
		entity.mp_used = 0
		entity.ap_used = 0
		
		for i in entity.skillsUsable.size():
			if entity.skillsUsable[i].cooldown > 0 and entity.skillsUsable[i].cooldownTurns > 0:
				entity.skillsUsable[i].cooldownTurns -= 1
			entity.skillsUsable[i].useAmt = 0
			entity.skillsUsable[i].usedOn = {}
	
	for i in area.skillsContainer.get_child_count():
		area.skillsContainer.get_child(i).queue_free()
	
	playedEnemies.append(entity)
	
	enemyOrderIdx+=1
	while enemyOrderIdx!=enemyOrder.size() and enemyOrder[enemyOrderIdx] in playedEnemies:
		enemyOrderIdx += 1
	
	for i in area.cells.get_child_count():
		area.cells.get_child(i).queue_free()
	
	if enemyOrderIdx==enemyOrder.size():
		currentPlaying = false
		endEnemyTurn()
		return
	
	enemyStart(enemyOrder[enemyOrderIdx])


func findDirectionToLook(entity):
	var closest 
	if not area:
		area = entity.get_node("../..")
	
	for i in area.entities.get_child_count():
		if not area.entities.get_child(i).get("obstacle") and area.entities.get_child(i).player:
			if not closest:
				closest = area.entities.get_child(i)
			else:
				if distanceTo(entity.posit, area.entities.get_child(i).posit) < distanceTo(entity.posit, closest.posit):
					closest = area.entities.get_child(i)
	
	return directionTo(entity.posit, closest.posit)
	
func setPriorityAoE(skill, sizeC = false, emptyOnly = false):
	var size
	if not sizeC:
		size = skill.aoeSize
	else:
		size = sizeC
	var list = []
	
	for i in area.entities.get_children():
		if not i.get("obstacle") and i.player:
			list.append(i)
	
	var cells = []
	
	for i in list:
		var radius = getCellInRadius(i.posit,size,emptyOnly,0)
		if radius:
			for j in radius:
				var val = 0
				for k in list:
					if distanceTo(k.posit,j)<=size:
						val += 1
				cells.append([j,val])
	
	cells.sort_custom(self,"aoeArrayOrder")
	
	var s = {}
	for i in cells:
		if s.has(i[1]):
			s[i[1]].append(i[0])
		else:
			s[i[1]] = [i[0]]
	
	var k = s.keys()
	k.sort_custom(self,"highval")
	
	cells = []
	
	for i in k:
		s[i].sort_custom(self,"closest")
		cells.append_array(s[i])
	
	return cells
	
func closest(a,b):
	return distanceTo(currentPlaying.posit, a) < distanceTo(currentPlaying.posit, b)

func highval(a,b):
	return a>b

func aoeArrayOrder(a,b):
	return a[1]>b[1]
	

func setPriorityEnemies():
	var list = []
	
	for i in area.entities.get_children():
		if not i.get("obstacle") and i.player:
			list.append(i)
	
	list.sort_custom(self, "orderList")
	#for i in list:
	#	print(i.ent_name)
	return list

func setPriorityAllies(exception = false):
	var list = []
	for i in area.entities.get_child_count():
		if not area.entities.get_child(i).get("obstacle") and not area.entities.get_child(i).player:
			if not exception or (exception and exception != area.entities.get_child(i)):
				list.append(area.entities.get_child(i))
	
	
	list.sort_custom(self, "orderList")
	return list

func orderList(a,b):
	match currentPlaying.aiType:
		0:
			return a.hp-a.hp_loss<b.hp-b.hp_loss
		1:
			return a.hp-a.hp_loss>b.hp-b.hp_loss
		2:
			return distanceTo(currentPlaying.posit, a.posit) < distanceTo(currentPlaying.posit, b.posit)

func findPathtoRun(entity, coward = false):
	if not coward:
		if not entity.big:
			var priorityList = setPriorityEnemies()
			
			setAStar(priorityList[0])
			var line = mp_line_generate(priorityList[0].posit)
			setAStar()
			line.pop_back()
			
			var lineUse = []
			var mpRest = -1
			for i in line.size():
				mpRest += 1
				
				lineUse.append(line[i])
				if mpRest==entity.mp-entity.mp_used:
					break
				
			return lineUse

		var cells=[]
		for i in range((entity.mp-entity.mp_used)*-1, (entity.mp-entity.mp_used)+1):
			for j in range((entity.mp-entity.mp_used)*-1, (entity.mp-entity.mp_used)+1):
				if abs(i)+abs(j) <= entity.mp-entity.mp_used:
					
					var value=0
					for k in area.entities.get_child_count():
						if area.entities.get_child(k).player:
							value+=distanceTo(entity.posit+Vector2(i,j), area.entities.get_child(k).posit)
					
					cells.append([Vector2(i,j),value])
		
		cells.sort_custom(self,"orderCellsR")
		
		for i in cells.size():
			var line
			line = mp_line_generate(cells[i][0]+entity.posit)
			if line and line.size()>0 and line.size()<=entity.mp-entity.mp_used+1:
				return line
		
		
	else:
		var cells=[]
		for i in range((entity.mp-entity.mp_used)*-1, (entity.mp-entity.mp_used)+1):
			for j in range((entity.mp-entity.mp_used)*-1, (entity.mp-entity.mp_used)+1):
				if abs(i)+abs(j) <= entity.mp-entity.mp_used:
					
					var value=0
					for k in area.entities.get_child_count():
						if area.entities.get_child(k).player:
							value+=distanceTo(entity.posit+Vector2(i,j), area.entities.get_child(k).posit)
					
					cells.append([Vector2(i,j),value])
		
		cells.sort_custom(self,"orderCells")
		
		for i in cells.size():
			var line
			line = mp_line_generate(cells[i][0]+entity.posit)
			if line and line.size()>0 and line.size()<=entity.mp-entity.mp_used+1:
				return line

func orderCells(a,b):
	return a[1]>b[1]

func orderCellsR(a,b):
	return a[1]<b[1]

func runTowards(entity,tgt):
	setAStar(tgt)
	var line = mp_line_generate(tgt.posit)
	setAStar()
	line.pop_back()
	
	var lineUse = []
	var mpRest = -1
	for i in line.size():
		mpRest += 1
		
		lineUse.append(line[i])
		if mpRest==entity.mp-entity.mp_used:
			break
		
	return lineUse

func distanceTo(a,b):
	return abs(a.x-b.x)+abs(a.y-b.y)

func endEnemyTurn():
	playedEnemies = []
	area.entityShower.visible = false
	area.skillsContainer.visible=true
	if !area.enemyfirst:
		currentRound+=1
		area.ui.changeRound(currentRound)
	area.ui.setCurrentTeam("player")
	emit_signal("endEnemysTurn")
	
	playerTurnStart()

func bossKill(_a,deadBoss):
	for i in area.entities.get_children():
		if not i.player and i.boss and not i==deadBoss:
			return
	battleWin()

func ultProc(entity):
	entity.currentUlt = min(entity.currentUlt+1,entity.ult)
	entity.get_node("HPBar").updateHP()

func ultEmpty(entity):
	entity.currentUlt = 0
	entity.get_node("HPBar").updateHP()

func checkSight(p1,p2,exception=[], excludeCurrent=true)->bool:
	if p1!=p2:
		var cells = area.tilemap.get_used_cells_by_id(1)
		cells.append_array(area.tilemap.get_used_cells_by_id(2))
		var exceptPosits = []
		
		if exception.size()>0:
			for i in exception:
				if i is bool:
					continue
				
				if i.big:
					exceptPosits.append_array(i.allPosits)
					continue
				
				exceptPosits.append(i.posit)
		
		if excludeCurrent and currentPlaying:
			if not currentPlaying.big:
				exceptPosits.append(currentPlaying.posit)
			else:
				exceptPosits.append_array(currentPlaying.allPosits)
		
		for i in cells:
			if not i in exceptPosits and i!=p2:
				var arrayp3=[i+Vector2(0.5,-0.5),i+Vector2(-0.5,-0.5)]
				var arrayp4=[i+Vector2(-0.5,0.5),i+Vector2(0.5,0.5)]
				for j in 2:
					var p3=arrayp3[j]
					var p4=arrayp4[j]
					var t1=(p1.x-p3.x)*(p3.y-p4.y)-(p1.y-p3.y)*(p3.x-p4.x)
					var t2=(p1.x-p2.x)*(p3.y-p4.y)-(p1.y-p2.y)*(p3.x-p4.x)
					var t
					if t1==0 or t2==0:
						t=0
					else:
						t=t1/t2
					var P=Vector2(p1.x+t*(p2.x-p1.x),p1.y+t*(p2.y-p1.y))
					if (
						P.x<i.x+0.5 and
						P.x>i.x-0.5 and
						P.y<i.y+0.5 and
						P.y>i.y-0.5 and
						directionTo(p1,P)==directionTo(p1,p2) and
						abs(p1.x-P.x)<=abs(p1.x-p2.x) and
						abs(p1.y-P.y)<=abs(p1.y-p2.y) 
					):
						return true
	return false

func skillLabel(n):
	var label = playAnim(spellLabel,Vector2(0,0),false,"over",Vector2(0,0),true,true)
	label.get_node("Label").text = n
	yield(label.get_node("Label/AnimationPlayer"),"animation_finished")
	endAnim(0, label)

var skillEffectActive = false
func useSkill(tgtArray, skill, caster):
	var focus
	skillEffectActive = true
	
	# Achando a célula clicada
	
	if tgtArray.size()==1:
		focus = tgtArray[0]
	else:
		for i in tgtArray.size():
			if tgtArray[i] is Array:
				focus = tgtArray[i][0]
	
	
	# Aumentando usos por alvo
	if not skill.tgtTypeEntity:
		if skill.usedOn.has(str(focus)):
			skill.usedOn[str(focus)] = skill.usedOn[str(focus)]+1
		else:
			skill.usedOn[str(focus)] = 1
	else:
		if getEntityAt(focus):
			if skill.usedOn.has(getEntityAt(focus).name):
				skill.usedOn[getEntityAt(focus).name] = skill.usedOn[getEntityAt(focus).name]+1
			else:
				skill.usedOn[getEntityAt(focus).name] = 1
	
	# Aumentando usos por turno
	skill.useAmt += 1
	
	# Diminuindo os PA do lançador
	caster.ap_used += skill.apCost
	
	# Cooldown
	skill.cooldownTurns = skill.cooldown
	
	# Virando o lançador para a direção
	if caster.posit != focus:
		caster.turn_to(directionTo(caster.posit, focus))
	
	if caster.player:
		updateSkillBtns()
	
	area.updateEntityDetails(caster)
	caster.emit_signal("skill_used", skill, focus, true)
	skillLabel(skill.skillName)
	yield(skill.get_node("Effects").effects(tgtArray),"completed")
	skillEffectActive = false
	caster.emit_signal("skill_used", skill, focus, false)
	if caster.player:
		updateSkillBtns()
	area.updateEntityDetails(caster)
	if not skillActive:
		caster.busy = false

func updateSkillBtns():
	if area.skillsContainer.get_child_count()>0 and currentPlaying:
		var caster = currentPlaying
		for i in area.skillsContainer.get_children():
			if i.get("skill"):
				i.disable(true)
				i.updateDts()
				if i.skill.element == 1 and caster.currentUlt != caster.ult:
					i.disable() 
				
				if caster.ap-caster.ap_used<i.skill.apCost:
					i.disable()
				
				if i.skill.usePerTurn>0:
					if i.skill.useAmt == i.skill.usePerTurn:
						i.disable()
				
				if i.skill.cooldownTurns >0:
					i.disable()
	

func getCellInRadius(from, radius, empty = true, amount = 1, excludeGlyphs=[], minRad = 0, center=false):
	var allCells = []
	var resultCells = []
	for i in range(radius*-1,radius+1):
		for j in range(radius*-1,radius+1):
			if area.tilemap.get_cellv(from+Vector2(i,j))>-1 and abs(i)+abs(j) <= radius and area.tilemap.get_cellv(from+Vector2(i,j))>-1 and abs(i)+abs(j) >= minRad:
				if not empty or (empty and area.tilemap.get_cellv(Vector2(i,j)+from)==0):
					if not allCurrentGlyphs.has(str(from+Vector2(i,j))) or not allCurrentGlyphs[str(from+Vector2(i,j))] in excludeGlyphs:
						allCells.append(from+Vector2(i,j))
	if center:
		allCells.append([from])
		allCells.erase(from)
	
	if allCells.size()==0:
		return false
	
	if amount > 0 and amount <= allCells.size():
		for _i in amount:
			randomize()
			var index = wrapi(randi(), 0, allCells.size())
			
			resultCells.append(allCells[index])
			allCells.remove(index)
		
		if amount==1:
			return resultCells[0]
		else:
			return resultCells
	
	return allCells
	
func battleWin(_a=false):
	area.ui.set_process(false)
	yield(get_tree().create_timer(0.1),"timeout")
	battleState = 2
	if currentPlaying:
		currentPlaying.busy = true
	enemiesDown = []
	alliesDown = []
	
	if !Save.data["stagesInfo"].has(area.lvl):
		Save.data["stagesInfo"][area.lvl] = {
			"crowns": 				area.trophies if not area.trophies == [true,true,true] else [area.trophies],
			"bestTime": 			BattleMechanics.area.ui.time,
			"bestTimeAllCrowns":	BattleMechanics.area.ui.time if area.trophies == [true,true,true] else 0,
		}
		if area.has_node("Score"):
			Save.data["stagesInfo"][area.lvl]["score"] = area.get_node("Score").currentScore
	else:
		if area.trophies!=[true,true,true] and Save.data["stagesInfo"][area.lvl]['crowns'].size()!=1:
			for i in area.trophies.size():
				if area.trophies[i] and not Save.data["stagesInfo"][area.lvl]["crowns"][i]:
					Save.data["stagesInfo"][area.lvl]["crowns"][i] = true
		else:
			Save.data["stagesInfo"][area.lvl]["crowns"] = [area.trophies]
		if area.ui.time < Save.data["stagesInfo"][area.lvl]["bestTime"]:
			Save.data["stagesInfo"][area.lvl]["bestTime"] = area.ui.time
		if area.trophies == [true,true,true]:
			if Save.data["stagesInfo"][area.lvl]["bestTimeAllCrowns"]==0 or area.ui.time < Save.data["stagesInfo"][area.lvl]["bestTimeAllCrowns"]:
				Save.data["stagesInfo"][area.lvl]["bestTimeAllCrowns"] = area.ui.time
		if area.has_node("Score"):
			if Save.data["stagesInfo"][area.lvl].has("score"):
				if Save.data["stagesInfo"][area.lvl]["score"] < area.get_node("Score").currentScore:
					Save.data["stagesInfo"][area.lvl]["score"] = area.get_node("Score").currentScore
			else:
				Save.data["stagesInfo"][area.lvl]["score"] = area.get_node("Score").currentScore
	Save.save()
	
	yield(get_tree().create_timer(1),"timeout")
	var allEntities = area.entities.get_children()
	for i in allEntities.size():
		if not allEntities[i].player and not allEntities[i].boss:
			killEntity(allEntities[i], false)
			yield(get_tree().create_timer(0.3),"timeout")
	yield(get_tree().create_timer(1),"timeout")
	GlobalFunctions.winBattle()

func battleLose():
	battleState = 0
	enemiesDown = []
	alliesDown = []
	yield(get_tree().create_timer(1),"timeout")
	GlobalFunctions.loseBattle()

var dmgHolder = {}
func dealDmg(dmg,tgt:Node2D,caster, additionalOffset = Vector2(0,0)):
	var dmgDealt
	match typeof(dmg):
		TYPE_ARRAY:
			dmgDealt = {"base": dmg[0]}
		TYPE_INT:
			dmgDealt = {"base": dmg}
		TYPE_DICTIONARY:
			dmgDealt = dmg.duplicate()
			
	if dmgDealt.has("FF") and (!dmgDealt["FF"] and tgt.player==caster.player):
		return
	
	if caster and not caster.dead:
		dmgDealt["base"] *= caster.dmgModMltGeneral
		dmgDealt["base"] *= caster.dmgModProgression
	dmgDealt["base"] *= tgt.dmgReceiveMltGeneral
	
	
	var direc = "front"
	if !dmgDealt.has("ignoreDir"):
		var dirmod = getDirectionalDmg(caster,tgt)
		dmgDealt["base"] *= dirmod
		
		match dirmod:
			1.25:
				direc = "rear"
			1.1:
				direc = "side"
	
	
	dmgDealt["base"] = ceil(dmgDealt["base"])
	dmgHolder[tgt.name] = dmgDealt
	
	
	tgt.emit_signal("dmg_taken",dmgHolder[tgt.name]["base"], caster, true)
	if caster and not caster.dead:
		caster.emit_signal("dmg_dealt",dmgHolder[tgt.name]["base"], tgt, true)
	
	tgt.hp_loss += dmgHolder[tgt.name]["base"]
	
	if dmgDealt.has("lifesteal") and dmgDealt["base"]>0:
		heal(round(float(dmgHolder[tgt.name]["base"]) * dmgDealt["lifesteal"]),caster,caster)
	
	
	tgt.emit_signal("dmg_taken",dmgHolder[tgt.name]["base"], caster, false)
	if caster and not caster.dead:
		caster.emit_signal("dmg_dealt",dmgHolder[tgt.name]["base"], tgt, false)
	
	
	
	
	if dmgDealt["base"]>0:
		playAnimEntity(tgt,"hurt")
		if tgt.jumpOnHit:
			bounceTgt(tgt)
	
	var delay = 0
	if dmg is Dictionary and dmg.has("collision"):
		delay = 0.1
	dmgVisual(
		tgt.position+tgt.get_node("HPBar").position+Vector2(0,+15)+additionalOffset, 
		dmgHolder[tgt.name]["base"], 
		caster.dmgModMltGeneral * tgt.dmgReceiveMltGeneral, 
		direc,
		tgt,
		delay)
	
	if tgt.hp_loss >= tgt.hp:
		if !dmgDealt.has("nonLethal"):
			tgt.hp_loss = tgt.hp
			killEntity(tgt, caster)
		else:
			tgt.hp_loss = tgt.hp-1
	
	tgt.get_node("HPBar").updateHP()

func dmgAtAoe(targetArray, dmgDict, caster, visual=false, delay=0, offset = Vector2(0,0)):
	var affected = []
	
	var order = orderAoE(targetArray)
	var k = order.keys()
	k.sort()
	
	for j in k:
		for i in order[j]:
			var tgt = getEntityAt(i[0] if i is Array else i)
			
			if visual:
				playAnim(visual, i[0] if i is Array else i, "ysort")
			
			if tgt and not tgt in affected and not tgt == caster:
				dealDmg(dmgDict, tgt, caster, offset)
				affected.append(tgt)
				
		if delay>0:
			yield(get_tree().create_timer(delay),"timeout")

var healHolder = {}
func heal(heal, tgt, caster, additionalOffset = Vector2(0,0)):
	healHolder[tgt.name] = heal
	
	tgt.emit_signal("healed", heal, caster, true)
	var wr = weakref(caster)
	if (wr.get_ref()):
		caster.emit_signal("healing", heal, tgt, true)
	
	if tgt.hp_loss-heal < 0:
		healHolder[tgt.name] = tgt.hp_loss
		tgt.hp_loss = 0
	else:
		tgt.hp_loss -= heal
	
	tgt.emit_signal("healed", healHolder[tgt.name], caster, false)
	wr = weakref(caster)
	if (wr.get_ref()):
		caster.emit_signal("healing", healHolder[tgt.name], tgt, false)
	tgt.get_node("HPBar").updateHP()
	
	healPopup(healHolder[tgt.name], tgt.position+tgt.get_node("HPBar").position+Vector2(0,-20)+additionalOffset)
func healPopup(value, posit):
	var newPopup = area.healBubble.instance()
	newPopup.position = posit
	newPopup.get_node("Heal").text = "+"+str(value)
	area.overvisual.add_child(newPopup)

func getEntityAt(posit):
	var entity = false
	for i in area.entities.get_children():
		if not i.get("obstacle"):
			if not i.big:
				if i.posit == posit:
					entity = i
			else:
				if posit in i.allPosits:
					entity = i
	return entity


func removal(what, amount, caster, tgt, delayAnim = 1.1):
	match what:
		"mp":
			if tgt.mp_used + amount <= tgt.mp:
				tgt.mp_used += amount
			else:
				tgt.mp_used = tgt.mp
			removalVisual(tgt.get_node("HPBar").global_position+Vector2(0,-15),what,amount, delayAnim)
		"ap":
			if tgt.ap_used + amount <= tgt.ap:
				tgt.ap_used += amount
			else:
				tgt.ap_used = tgt.ap
			removalVisual(tgt.get_node("HPBar").global_position+Vector2(0,-15),what,amount, delayAnim)

func removalVisual(posit, what, amount, delay):
	if delay > 0:
		yield(get_tree().create_timer(delay),"timeout")
	match what:
		"mp":
			var removalAnim = area.remBubble.instance()
			removalAnim.position = posit
			removalAnim.get_node("MP").visible = true
			removalAnim.get_node("MP").text = str(-1*amount)+" MP" if amount>0 else "+"+str(amount*-1)+" MP"
			
			
			area.overvisual.add_child(removalAnim)
			yield(removalAnim.get_node("AnimationPlayer"),"animation_finished")
			removalAnim.queue_free()
		"ap":
			var removalAnim = area.remBubble.instance()
			removalAnim.position = posit
			removalAnim.get_node("AP").visible = true
			removalAnim.get_node("AP").text = str(-1*amount)+" AP" if amount>0 else "+"+str(amount*-1)+" AP"
			
			
			area.overvisual.add_child(removalAnim)
			yield(removalAnim.get_node("AnimationPlayer"),"animation_finished")
			removalAnim.queue_free()

var deathAnim = preload("res://characters/DeathAnim.tscn")
func killEntity(entity, killer, animate = true):
	if killer and killer.ent_name == "Dracule Mihawk":
		var score = area.get_node("Score")
		if score.minScore <= score.currentScore:
			yield(get_tree(),"idle_frame")
			if killer.has_node("AI"):
				killer.get_node("AI").queue_free()
			entity.hp_loss = entity.hp - 1
			entity.get_node("HPBar").updateHP()
			playAnimEntity(entity,"death")
			battleWin()
			return
	if not entity.dead:
		yield(get_tree(),"idle_frame")
		entity.dead = true
		if not entity.player:
			enemyOrder.erase(entity)
			if enemyOrderIdx >0:
				enemyOrderIdx -= 1
		
		var gameOver = false
		if entity.player:
			gameOver = true
			for i in area.entities.get_child_count():
				if not area.entities.get_child(i).get("obstacle") and area.entities.get_child(i).player and not area.entities.get_child(i).dead:
					gameOver = false
					break
		entity.get_node("HPBar").removePings()
		
		if gameOver and not currentPlaying.player:
			currentPlaying.stopAll(true,true)
		if gameOver:
			battleLose()
		
		if currentPlaying and not currentPlaying.player:
			aiMonsterYield = 0.8
		if currentPlaying and entity == currentPlaying and enemyOrder.size()>1:
			enemyEnd(entity)
		
		
		entity.emit_signal("killed", killer)
		if entity.counts and not entity.player:
			winProc()
		area.tilemap.set_cellv(entity.posit,0)
		if animate:
			playAnimEntity(entity,"death")
			entity.get_node("DeathAnim").play("death")
			entity.modulate = Color(1, 0.619608, 0.619608)
			yield(get_tree().create_timer(0.5 if not entity.boss else 1),"timeout")
			playAnim(deathAnim, entity.posit, false, "over")
			yield(get_tree().create_timer(0.3),"timeout")
		
	
		enemiesDown.append(entity)
		area.entities.remove_child(entity)
		if not entity.big:
			if !getEntityAt(entity.posit):
				area.tilemap.set_cellv(entity.posit,0)
		else:
			for i in entity.allPosits:
				if !getEntityAt(i):
					area.tilemap.set_cellv(i,0)
			
		setAStar()

var enemiesDown = []
var alliesDown = []
var currentDead = 0
func winProc():
	currentDead += 1
	if currentDead == area.deathtowin:
		battleWin('')
var dmgVisualQueue = []
var dmgVisualActive = false

func dmgVisualQueueAdd(scene):
	dmgVisualQueue.append(scene)
	if not dmgVisualActive:
		dmgVisualExecute()

func dmgVisualExecute():
	dmgVisualActive = true
	while dmgVisualQueue.size()>0:
		dmgVisualAdd(dmgVisualQueue[0])
		dmgVisualQueue.pop_front()
		yield(get_tree().create_timer(0.12),"timeout")
	dmgVisualActive = false

func dmgVisualAdd(scene):
	area.overvisual.add_child(scene)
	if scene.get_node("Label").text == "-0":
		scene.get_node("AnimationPlayer").play("alt")
	else:
		scene.get_node("AnimationPlayer").play("main")
	yield(scene.get_node("AnimationPlayer"),"animation_finished")
	scene.queue_free()

func dmgVisual(posit,dmg, value, direc, tgt, delay = 0):
	var newBubble = area.dmgBubble.instance()
	randomize()
	var add = false
	if delay>0:
		yield(get_tree().create_timer(delay),"timeout")
	for i in area.overvisual.get_children():
		if i.get("tgt") and i.tgt == tgt:
			add = i
	if add:
		add.value += dmg
		add.get_node("AnimationPlayer").stop()
		if value > 0:
			add.get_node("AnimationPlayer").play("main")
		else:
			add.get_node("AnimationPlayer").play("alt")
		add.get_node("Label").text = "-"+str(add.value)
		return
	newBubble.tgt = tgt
	newBubble.value  = dmg
	newBubble.position = posit + Vector2(wrapi(randi(), -15, 15),wrapi(randi(), -15, 15))
	newBubble.global_position.x = min(newBubble.global_position.x, OS.get_screen_size().x-15)
	newBubble.global_position.y = min(newBubble.global_position.y, OS.get_screen_size().y-30)
	newBubble.get_node("Label").text = "-"+str(newBubble.value)
	if dmg>0:
		match direc:
			"rear":
				newBubble.get_node("Direction/Rear").visible = true
			"side":
				newBubble.get_node("Direction/Side").visible = true
	
	if value != 1:
		if value > 1:
			newBubble.get_node("Mod/Weak").bbcode_text = "[color=#a9ff81][b]+"+str((value * 100)-100) + "%[/b]"
			newBubble.get_node("Mod/Weak").visible = true
		else:
			newBubble.get_node("Mod/Resist").bbcode_text = "[color=#f63939][b]-"+str(100 - (value * 100)) + "%[/b]"
			newBubble.get_node("Mod/Resist").visible = true
	
	dmgVisualQueueAdd(newBubble)

func armorVisual(scene,dmg,posit,give=false):
	var newBubble = scene.instance()
	newBubble.position = posit
	var s = "-" if not give else "+"
	newBubble.get_node("Label").text = s+str(dmg)
	area.overvisual.add_child(newBubble)
	yield(newBubble.get_node("AnimationPlayer"),"animation_finished")
	newBubble.queue_free()

func bounceTgt(tgt):
	tgt.get_node("BounceAnim").stop()
	tgt.get_node("BounceAnim").play("bounce")

func playAnim(animScene, posit, flip=false, where="ysort", offset=Vector2(0,0), stay=false,anywhere = false):
	if (area.tilemap.get_cellv(posit)>-1 and not anywhere) or anywhere:
		var anim = animScene.instance()
		anim.position = area.tilemap.map_to_world(posit) + offset
		match where:
			"ysort":
				area.ysortvisual.add_child(anim)
			"over":
				area.overvisual.add_child(anim)
			"under":
				area.undervisual.add_child(anim)
		
		if flip:
			anim.scale.x*=-1
		
		if not stay:
			anim.get_node("AnimationPlayer").connect("animation_finished",self,"endAnim",[anim])
		return anim

func endAnim(_a,anim):
	anim.queue_free()

func dmgEntityAt(position, dmg, caster):
	for i in area.entities.get_child_count():
		if not area.entities.get_child(i).get("obstacle"):
			if not area.entities.get_child(i).big:
				if area.entities.get_child(i).posit == position:
					dealDmg(dmg, area.entities.get_child(i), caster)
					return
			else:
				if position in area.entities.get_child(i).allPosits:
					dealDmg(dmg, area.entities.get_child(i), caster)
					return

var allCurrentGlyphs = {}
var mostRecentGlyph
func placeGlyph(glyph, posit, caster, overlap = false):
	if area.tilemap.get_cellv(posit)>-1:
		var newGlyph = glyph.instance()
		newGlyph.posit = posit
		newGlyph.caster = caster
		newGlyph.position = area.tilemap.map_to_world(posit)
		
		var dont = false
		
		if not allCurrentGlyphs.has(str(posit)):
			allCurrentGlyphs[str(posit)] = [newGlyph.id]
		else:
			if (newGlyph.id in allCurrentGlyphs[str(posit)] and overlap) or not newGlyph.id in allCurrentGlyphs[str(posit)]:
				allCurrentGlyphs[str(posit)].append(newGlyph.id)
			else:
				dont = true
		if not dont:
			area.glyphNode.add_child(newGlyph)
			mostRecentGlyph = newGlyph
			return
	mostRecentGlyph = false

func removeGlyph(glyph):
	if allCurrentGlyphs[str(glyph.posit)].size()==1:
		allCurrentGlyphs.erase(str(glyph.posit))
	else:
		allCurrentGlyphs[str(glyph.posit)].erase(glyph.id)
	glyph.queue_free()

func shootProjectile(animScene, positFrom, positTo, time=1, tgtOffset = Vector2(0,0), castOffset = Vector2(0,0), lob=true, flip=false, extraTime=0, angle=false):
	yield(get_tree(),"idle_frame")
	if area.tilemap.get_cellv(positTo)>-1:
		if lob:
			var newAnim = animScene.instance()
			if flip:
				newAnim.scale.x *= -1
			var tween = newAnim.get_node("Tween")
			tween.interpolate_property(
				newAnim,
				"position",
				area.tilemap.map_to_world(positFrom)+castOffset,
				area.tilemap.map_to_world(positTo)+tgtOffset,
				time
			)
			
			area.overvisual.add_child(newAnim)
			tween.start()
			yield(tween,"tween_completed")
			if extraTime>0:
				yield(get_tree().create_timer(extraTime),"timeout")
			
			newAnim.queue_free()
			return
		else:
			var newAnim = animScene.instance()
			if flip:
				newAnim.scale.x *= -1
			var tween = newAnim.get_node("Tween")
			tween.interpolate_property(
				newAnim,
				"position",
				area.tilemap.map_to_world(positFrom)+castOffset,
				area.tilemap.map_to_world(positTo)+tgtOffset,
				time * BattleMechanics.distanceTo(positFrom,positTo)
			)
			if angle:
				var a = (area.tilemap.map_to_world(positFrom)+castOffset).angle_to(area.tilemap.map_to_world(positTo)+tgtOffset)
				newAnim.rotation_degrees = rad2deg(a)
			
			area.overvisual.add_child(newAnim)
			tween.start()
			yield(tween,"tween_completed")
			if extraTime>0:
				yield(get_tree().create_timer(extraTime),"timeout")
			
			newAnim.queue_free()
			return

# a = Array
# e = Centro
func orderAoE(a):
	var c = {}
	var e
	for i in a:
		if i is Array:
			e = i[0]
			c[0]=[e]
			break
	for i in a:
		if not i is Array:
			if c.has(distanceTo(i,e)):
				c[distanceTo(i,e)].append(i)
			else:
				c[distanceTo(i,e)]=[i]
	
	return c

func playAnimEntity(entity, anim):
	if !anim is Array:
		if entity.dead and anim!="death":
			return
		if ((anim == "hurt" and entity.get_node("AnimationsF").current_animation in ["idle"]) or anim!="hurt"):
			
			entity.get_node("AnimationsF").stop()
			if entity.has_node("AnimationsB"):
				entity.get_node("AnimationsB").stop()
			
			if anim == "hurt":
				entity.get_node("AnimationsF").play("RESET")
				if entity.has_node("AnimationsB"):
					entity.get_node("AnimationsB").play("RESET")
				yield(get_tree(),"idle_frame")
			
			if entity.get_node("AnimationsF").has_animation(anim):
				entity.get_node("AnimationsF").play(anim)
				if entity.has_node("AnimationsB"):
					entity.get_node("AnimationsB").play(anim)
			
			else:
				entity.get_node("AnimationsF").play("idle")
				if entity.has_node("AnimationsB"):
					entity.get_node("AnimationsB").play("idle")
			return
	else:
		for i in anim:
			if entity.dead and i!="death":
				return
			entity.get_node("AnimationsF").stop()
			if entity.has_node("AnimationsB"):
				entity.get_node("AnimationsB").stop()
			
			if entity.get_node("AnimationsF").has_animation(i):
				entity.get_node("AnimationsF").play(i)
				if entity.has_node("AnimationsB"):
					entity.get_node("AnimationsB").play(i)
				yield(entity.get_node("AnimationsF"),"animation_finished")
			else:
				entity.get_node("AnimationsF").play("idle")
				if entity.has_node("AnimationsB"):
					entity.get_node("AnimationsB").play("idle")
		
		

func findTgt(allTgts, skill):
	var allPossibleCells = {}
	var tgtUse = []
	
	for i in allTgts.size(): # Faz questão de que o array apenas contém os vetores de posição de cada entidade
		if not allTgts[i] is Vector2:
			tgtUse.append(allTgts[i].posit)
		else:
			tgtUse.append(allTgts[i])
	
	for l in tgtUse.size():
		var tgt = tgtUse[l]
		allPossibleCells[l] = []
		
		
		if !cellCondition(skill, skill.caster, tgt):
			continue
		
		if !(skill.caster.ap-skill.caster.ap_used>=skill.apCost):
			continue
		
		if skill.cooldownTurns != 0:
			continue
			
		if !((skill.useAmt<skill.usePerTurn and skill.usePerTurn > 0) or skill.usePerTurn == 0):
			continue 
		
		
		var rangeU = ((skill.rangeMax+skill.caster.extraRange) if skill.rangeMax+skill.caster.extraRange > skill.rangeMin else skill.rangeMin)
		
		for i in range(rangeU*-1,1+rangeU):
			for j in range(rangeU*-1,1+rangeU):
				var distance = abs(i)+abs(j)
				
				if !(((distance <= skill.rangeMax+skill.caster.extraRange) if skill.rangeMax+skill.caster.extraRange>skill.rangeMin else distance == skill.rangeMin) and distance >= skill.rangeMin):
					continue
				
				if !(((i==0 or j==0) and skill.linear) or not skill.linear):
					continue
				
				if !(not skill.sight or (skill.sight and not checkSight(tgt, tgt+Vector2(i,j),[getEntityAt(tgt)]))):
					continue
				
				#var node = Node2D.new()
				#node.z_index = 10
				#var label=Label.new()
				#label.text = str(mp_line_generate(Vector2(i,j)+tgt).size())
				
				#node.position = area.tilemap.map_to_world(Vector2(i,j)+tgt)
				#area.cells.add_child(node)
				#node.add_child(label)
				
				allPossibleCells[l].append(
					[ 
						mp_line_generate(Vector2(i,j)+tgt),
						tgt
					])
	
	var keys = allPossibleCells.keys()
	keys.sort()
	
	for k in keys:
		allPossibleCells[k].sort_custom(self, "findClosestCell")
		for i in allPossibleCells[k]:
			if i[0].size()-1<=skill.caster.mp-skill.caster.mp_used and i[0].size()>0:
				return i
	
	return []

func turnEndRun(entity, coward=false, delay=0.2):
	yield(get_tree(),"idle_frame")
	while entity.mp-entity.mp_used > 0:
		if aiMonsterYield > 0:
			yield(get_tree().create_timer(BattleMechanics.aiMonsterYield),"timeout")
			aiMonsterYield = 0
		var path = findPathtoRun(entity,coward)
		if path.size()<2:
			break
		yield(walk(entity, path),"completed")
		yield(get_tree().create_timer(delay),"timeout")


func aiUseSkills(skills, walkyield=0.2, aoe=[]):
	yield(get_tree(),"idle_frame")
	var foundTgt
	var flags = []
	for i in skills:
		flags.append(false)
	
	
	for i in skills.size():
		foundTgt = true
		
		var e
		if aoe == [] or not aoe[i] is Array:
			e = setPriorityEnemies()
		else:
			e = setPriorityAoE(skills[i], aoe[i][0], aoe[i][1])
		
		while foundTgt and currentPlaying.hp-currentPlaying.hp_loss>0:
			
			foundTgt = false
			var tgt = findTgt(e, skills[i])
			
			if aiMonsterYield > 0:
				yield(get_tree().create_timer(BattleMechanics.aiMonsterYield),"timeout")
				aiMonsterYield = 0
			
			if tgt.size()>0 and flags[i] is bool:
				
				if tgt[0].size()>1:
					yield(walk(currentPlaying, tgt[0]), "completed")
					if walkyield>0:
						yield(get_tree().create_timer(walkyield),"timeout")
					if area.tilemap.world_to_map(tgt[0][tgt[0].size()-1]) == currentPlaying.posit:
						flags[i] = tgt[1]
					foundTgt = true
					continue
				elif tgt[0].size()==1:
					flags[i] = tgt[1]
					foundTgt = true
					continue
			
			if flags[i] is Vector2 and tgt.size()==0:
				print("Fuck")
			
			if flags[i] is Vector2:
				
				currentPlaying.connect("skill_used",self,"aiskillyield")
				yield(enemyUseSkill(skills[i], currentPlaying, flags[i]),"completed")
				flags[i] = false
				yield(self,"aiskillover")
				currentPlaying.disconnect("skill_used",self,"aiskillyield")
				foundTgt = true
				continue
			
			
signal aiskillover
func aiskillyield(_skill, _cell, before):
	if not before:
		emit_signal("aiskillover")
		

func findClosestCell(a,b):
	return a[0].size()<b[0].size()

var skillunable = false
func enemyUseSkill(skill,entity,tgtCell,delay=0.4):
	yield(get_tree(),"idle_frame")
	
	skillRange(skill, entity, tgtCell)
	if skillunable:
		emit_signal("aiskillover")
	if delay>0:
		yield(get_tree().create_timer(delay),"timeout")
	
	for i in area.cells.get_child_count():
		area.cells.get_child(i).queue_free()
	

var forcePushStop = {}
func push(entity, caster, amount, dir, dmg=false):
	yield(get_tree(),"idle_frame")
	if entity.hp-entity.hp_loss>0 and not entity.big:
		var tween = entity.get_node("PushTween")
		
		var cellsTransverse = []
		for i in range(1,amount+1+caster.pushMod):
			if area.tilemap.get_cellv(entity.posit + (dir * i)) == 0:
				cellsTransverse.append(entity.posit + (dir * i))
			else:
				break
		
		forcePushStop[entity.name] = false
		var cellsTransversed = 0
		
		if cellsTransverse.size()>0 and not entity.pushStab:
			for i in cellsTransverse.size():
				var p = entity.position
				tween.interpolate_property(
					entity,
					"position",
					entity.position,
					area.tilemap.map_to_world(cellsTransverse[i]),
					0.1
				)
				tween.start()
				area.tilemap.set_cellv(entity.posit, 0)
				
				yield(tween,"tween_completed")
				
				
				cellsTransversed += 1
				entity.posit = cellsTransverse[i]
				area.tilemap.set_cellv(entity.posit, (2 if entity.player else 1))
				
				entity.emit_signal("pushTravel", p, cellsTransverse[i])
				
				if forcePushStop[entity.name]:
					break
		
		setAStar()
		if dmg and cellsTransversed!=amount+caster.pushMod:
			var dmgToBe = (dmg[0] if dmg is Array else dmg) * (amount+caster.pushMod - cellsTransversed)
			dealDmg({"base": dmgToBe, "ignoreDir": true, "indirect": true, "collision": true},entity,caster, Vector2(-15,-10))
			var other = getEntityAt(entity.posit + dir)
			if other:
				dealDmg({"base": dmgToBe, "ignoreDir": true, "indirect": true, "collision": true},other,caster, Vector2(-15,-10))
			AM.play(AM.collision, {"randp": 1.2})
		forcePushStop.erase(entity.name)
		entity.emit_signal("pushed", cellsTransversed, caster)

func shake():
	area.get_node("Shake").play("shake")

func addState(stateScene, target, caster, lvl=1):
	var newState = stateScene.instance()
	if target.stateNode.get_child_count()>0:
		for i in target.stateNode.get_children():
			if i.stateName == newState.stateName:
				if newState.stacks:
					if i.maxStack == 0:
						i.lvl += lvl
					else:
						if i.lvl + lvl <= i.maxStack:
							i.lvl += lvl
						else:
							i.lvl = i.maxStack
							
					if newState.stateName == "Armadura":
						armorVisual(newState.visuals[0], lvl, target.get_node("HPBar").global_position+Vector2(0,-30),true)
					if i.get_node("Effects").has_method("lvlUpdate"):
						i.get_node("Effects").lvlUpdate()
					yield(get_tree(),"idle_frame")
					target.get_node("HPBar").stateUpdate()
				return
	
	
	newState.caster = caster
	newState.lvl = lvl
	if newState.stateName == "Armadura":
		armorVisual(newState.visuals[0], lvl, target.get_node("HPBar").global_position+Vector2(0,-30),true)
	target.stateNode.add_child(newState)
	yield(get_tree(),"idle_frame")
	target.get_node("HPBar").stateUpdate()

# Para remover o estado diretamente pelo Node:
#	"byName" tem que ser false 
#	"state" tem que ser o Node

# Para remover por id
#	"byName" tem que ser a entidade portadora do estado
#	"state" tem que ser o nome do estado

# Parabéns, Natan do passado, por fazer esse espaguete que tive que decifrar agora.
# De nada, Natan do futuro, após algum hiato que fez você esquecer como tudo funciona.

func removeState(state, byName = false):
	if not byName:
		var entity = state.get_parent().get_parent()
		state.queue_free()
		yield(get_tree(),"idle_frame")
		entity.get_node("HPBar").stateUpdate()
	else:
		var entity = byName
		for i in entity.stateNode.get_children():
			if i.stateName == state:
				i.queue_free()
				yield(get_tree(),"idle_frame")
				entity.get_node("HPBar").stateUpdate()

func hasState(entity, stateName) -> bool:
	if entity.stateNode.get_child_count()>0:
		for i in entity.stateNode.get_child_count():
			if entity.stateNode.get_child(i).stateName == stateName:
				return true
	return false

func getState(entity, stateName):
	if entity.stateNode.get_child_count()>0:
		for i in entity.stateNode.get_child_count():
			if entity.stateNode.get_child(i).stateName == stateName:
				return entity.stateNode.get_child(i)
	return false

func procDown(state, amount=1):
	if amount>=state.lvl:
		removeState(state)
	else:
		state.lvl -= amount
		if state.get_node("Effects").has_method("lvlUpdate"):
			state.get_node("Effects").lvlUpdate()
	
	state.get_parent().get_parent().get_node("HPBar").stateUpdate()

func procUp(state, amount):
	if state.maxStack == 0:
		state.lvl += amount
	elif state.lvl != state.maxStack:
		if state.lvl + amount <= state.maxStack:
			state.lvl += amount
		else:
			state.lvl = state.maxStack
	if state.name == "Armor":
		armorVisual(state.visuals[0], amount, state.porter.get_node("HPBar").global_position+Vector2(0,-30),true)
	if state.get_node("Effects").has_method("lvlUpdate"):
		state.get_node("Effects").lvlUpdate()
	state.get_parent().get_parent().get_node("HPBar").stateUpdate()

func teleport(entity, cell, caster, switchIfOcp = false):
	if area.tilemap.get_cellv(cell)==0:
		var p = entity.posit
		area.tilemap.set_cellv(entity.posit, 0)
		entity.position = area.tilemap.map_to_world(cell)
		entity.posit = cell
		area.tilemap.set_cellv(entity.posit, (2 if entity.player else 1))
		setAStar()
		entity.emit_signal("teleported", p, cell, caster)
		if skillActive:
			cancelSkill(currentPlaying)
		return
	if switchIfOcp:
		switchPos(entity,getEntityAt(cell))

func jumpTo(entity,cell,caster,time=1):
	if area.tilemap.get_cellv(cell)!=0:
		return
	
	var tween = entity.pushTween
	tween.interpolate_property(
		entity,
		"position",
		entity.position,
		area.tilemap.map_to_world(cell),
		time
	)
	tween.start()
	area.tilemap.set_cellv(entity.posit, 0)
	
	yield(tween,"tween_completed")
	
	var p = entity.posit
	
	entity.position = area.tilemap.map_to_world(cell)
	entity.posit = cell
	area.tilemap.set_cellv(entity.posit, (2 if entity.player else 1))
	setAStar()
	entity.emit_signal("teleported", p, cell, caster)
	if skillActive:
		cancelSkill(currentPlaying)
	
	


func getRandPlayer():
	var p = []
	for i in area.entities.get_children():
		if i.player:
			p.append(i)
	
	randomize()
	return p[wrapi(randi(),0,p.size())]
	
	

func summonMonster(entity, posit, counts=true, summoner=false, stateInitial = false, stateInitialLvl = 1):
	var newEntity = entity.instance()
	newEntity.posit = posit
	newEntity.position = area.tilemap.map_to_world(posit)
	area.tilemap.set_cellv(posit, 1)
	setAStar()
	
	area.entities.add_child(newEntity)
	if newEntity.plays:
		enemyOrder.append(newEntity)
	if stateInitial:
		addState(stateInitial, newEntity, summoner, stateInitialLvl)
	newEntity.counts = counts
	
	emit_signal("entityAdded", newEntity, false)
	return newEntity

func summonPlayer(entity, posit, summoner, stateInitial = false, stateInitialLvl = 1):
	var newEntity = entity.instance()
	newEntity.posit = posit
	newEntity.position = area.tilemap.map_to_world(posit)
	area.tilemap.set_cellv(posit, 2)
	setAStar()
	newEntity.summon = true
	
	area.entities.add_child(newEntity)
	
	if stateInitial:
		addState(stateInitial, newEntity, summoner, stateInitialLvl)
	
	emit_signal("entityAdded", newEntity, false)
	return newEntity

func switchPos(tgt1, tgt2):
	var positTemp = [tgt1.posit,tgt1.position]
	tgt1.position = tgt2.position
	tgt1.posit = tgt2.posit
	tgt2.position = positTemp[1]
	tgt2.posit = positTemp[0]
	
	area.tilemap.set_cellv(tgt1.posit, (2 if tgt1.player else 1))
	area.tilemap.set_cellv(tgt2.posit, (2 if tgt2.player else 1))
	
	tgt1.emit_signal("switchedWith", tgt2)
	tgt2.emit_signal("switchedWith", tgt1)
	if skillActive:
		cancelSkill(currentPlaying)
	setAStar()
	
