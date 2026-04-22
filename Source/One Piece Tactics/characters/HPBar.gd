extends Node2D

var entity
export (PackedScene) var mpHigh

func _ready():
	
	entity = get_parent()
	$Panel2.rect_global_position = entity.global_position + Vector2(-29,-14) + (Vector2(0,42) if not entity.big else Vector2(0,84))
	updateHP(true)
	$Name.text = entity.ent_name
	if entity.ult == 0:
		$Ult.visible = false

var hovering = false
func showAll(x):
	if x:
		hovering = true
		z_index+=1
		if BattleMechanics.battleState > 0:
			$Timer2.start()
		#$Panel.visible = true
		#$Panel2.self_modulate = Color(1, 1, 1)
		$Panel2.modulate = Color(1, 1, 1)
		$Ult.self_modulate = Color(1, 1, 1)
		$TextureProgress.modulate = Color(1,1,1)
		$Name.visible = true
		$States.rect_position += Vector2(0,-15)
	else:
		hovering = false
		z_index-=1
		$Timer2.stop()
		removePings()
		#$Panel.visible = false
		#$Panel2.self_modulate = Color(1, 1, 1, 0.54902)
		$Panel2.modulate = Color(1, 1, 1, 0.54902)
		$Ult.self_modulate = Color(1, 1, 1, 0.54902)
		$TextureProgress.modulate = Color(1, 1, 1, 0.152941)
		$Name.visible = false
		$States.rect_position -= Vector2(0,-15)

var dontUpdate = false
func updateHP(first = false):
	if first and entity.scoreboss:
		$Panel2/HBoxContainer/Label.text= "???"
		$TextureProgress.visible = false
		BattleMechanics.updateSkillBtns()
		dontUpdate = true
		return
	if dontUpdate:
		return
	
	$Panel2/HBoxContainer/Label.text=str(entity.hp-entity.hp_loss)
	
	$TextureProgress.max_value = entity.hp
	$TextureProgress.value = entity.hp-entity.hp_loss
	if entity.ult > 0:
		$Ult.max_value = entity.ult
		$Ult.value =  entity.currentUlt
		if entity.ult == entity.currentUlt:
			$Ult/AnimationPlayer.play("full")
		else:
			$Ult/AnimationPlayer.play("RESET")
	BattleMechanics.updateSkillBtns()
	if not first:
		$TextureProgress.modulate = Color(1, 1, 1)
		$Timer.start()


func stateUpdate():
	if $States.get_child_count()>0:
			for i in $States.get_child_count():
				$States.get_child(i).queue_free()
	if BattleMechanics.area.stateVisuals.get_children().size()>0:
		for i in BattleMechanics.area.stateVisuals.get_children():
			if i:
				i.queue_free()
	if entity.stateNode and entity.stateNode.get_child_count()>0:
		for i in entity.stateNode.get_children():
			if i.appearOver:
				var icon = i.get_node("Icon").duplicate()
				
				icon.modulate.a = 0.5
				if i.lvl==1 and i.stateName != "Phases":
					icon.get_node("LVl").visible = false
				else:
					icon.get_node("LVl").text = str(i.lvl)
				
				icon.visible = true
				$States.add_child(icon)
				
				i.currentIcon = icon
				icon.connect("mouse_entered",i,"showEffects")
				icon.connect("mouse_exited",i,"hideEffects")
				
				#icon.get_node("Window/Panel").details(i)
				

func timeout():
	if not hovering:
		$TextureProgress.modulate = Color(1, 1, 1, 0.356863)

var pings = []
func mpPing():
	if entity.hp - entity.hp_loss > 0:
		entity.emit_signal("mp_preview")
		var mp = entity.mp - entity.mp_used
		var cells = BattleMechanics.orderAoE(BattleMechanics.getCellInRadius(entity.posit, mp, false, 0, [], 0, true))
		var k = cells.keys()
		k.sort()
		
		for i in k:
			for j in cells[i]:
				if BattleMechanics.mp_line_simul(entity,j):
					pings.append(BattleMechanics.playAnim(mpHigh, j,false,"ysort",Vector2(0,-1),true))
			#yield(get_tree().create_timer(0.1),"timeout")

func removePings():
	if BattleMechanics.currentPlaying:
		BattleMechanics.setAStar()
	if pings.size():
		for i in pings:
			i.get_node("AnimationPlayer").play_backwards("loop")
		yield(get_tree().create_timer(0.51),"timeout")
		for i in pings:
			i.queue_free()
		pings = []
	
