extends Panel

# air = f09ee5
# earth = 4ba41e
# water = 23b9ed
# fire = fa4709

onready var skillName = $SpellNodes/RichTextLabel
onready var skillNamePreview = $SpellNodes/Preview/VBoxContainer/HBoxContainer/SpellName
onready var apCost = $SpellNodes/Preview/VBoxContainer/HBoxContainer/AP

onready var los = $SpellNodes/Preview/VBoxContainer/Range/HBoxContainer2/Los
onready var noLos = $SpellNodes/Preview/VBoxContainer/Range/HBoxContainer2/NoLos
onready var line = $SpellNodes/Preview/VBoxContainer/Range/HBoxContainer2/Line

onready var useLimits = $SpellNodes/Preview/VBoxContainer/UseLimits
onready var description = $SpellNodes/Preview/VBoxContainer/Desc

onready var icon1 = $SpellNodes/Icon1
onready var icon2 = $SpellNodes/Icon1/Icon2

var pushed = false

var skill
func setAttributes(skillProvided):
	skill = skillProvided
	updateDts()

func setShortCut(i):
	var a = [KEY_1, KEY_2, KEY_3, KEY_4, KEY_5, KEY_6, KEY_6, KEY_7, KEY_8, KEY_9]
	$SpellNodes/shortcut/txt.bbcode_text = "[center]"+str(i+1)
	shortcut = a[i]
	#var l = ["Q","W","E","R","T","T","Y","U","I","O","P"];$SpellNodes/shortcut/txt.bbcode_text = "[center]"+l[i]

var shortcut 
func _input(event):
	if event is InputEventKey and event.scancode == shortcut and event.pressed:
		if pushed:
			push(false)
		else:
			push(true)

func updateDts():
	$SpellNodes.get_child(skill.element).visible=true
	
	if !icon1.texture:
		icon1.texture = skill.icon1
		if skill.icon2 and !icon2.texture:
			icon2.texture = skill.icon2
			icon1.position -= Vector2(7,0)

	
	skillName.bbcode_text = skill.skillName
	skillNamePreview.bbcode_text = skill.skillName
	
	if skill.rangeMin != skill.rangeMax:
		$SpellNodes/Preview/VBoxContainer/Range.bbcode_text = "Range of "+ str(skill.rangeMin) + " to " + str(skill.rangeMax) + " cells"
	else:
		$SpellNodes/Preview/VBoxContainer/Range.bbcode_text = "Range of "+ str(skill.rangeMin) + " cell"+("s" if skill.rangeMin != 1 else "")
	
	
	$Panel/ApCost.text = str(skill.apCost) if !skill.xvalue else "X"
	apCost.bbcode_text =(str(skill.apCost) if !skill.xvalue else "X")+" AP"
	los.visible = true if skill.sight else false
	noLos.visible =  true if not skill.sight else false
	line.visible = skill.linear
	
	if !(skill.usePerTurn==0 and skill.usePerTgt==0 and skill.cooldown==0):
		#$SpellNodes/Preview/VBoxContainer/UseLimits.modulate = Color(1,1,1,0)
		

		$SpellNodes/Preview/VBoxContainer/UseLimits.visible = true
		var limitText = ""
		if skill.usePerTgt > 0:
			limitText += str(skill.usePerTgt)+" use"+("s" if skill.usePerTgt > 1 else "")+" per target" 
			if skill.usePerTurn > 0 or skill.cooldown > 0:
				limitText += "\n"
		
		if skill.usePerTurn > 0:
			limitText += str(skill.usePerTurn)+" use"+("s" if skill.usePerTurn > 1 else "")+ " per turn"
			if skill.cooldown > 0:
				limitText += "\n"
		
		if skill.cooldown > 0:
			limitText += "Cooldown of " + str(skill.cooldown)+" turn"+("s" if skill.cooldown>1 else "")
		$SpellNodes/Preview/VBoxContainer/UseLimits.bbcode_text = limitText
	
	match skill.aoeShape:
		0:
			$SpellNodes/Preview/VBoxContainer/Target.bbcode_text = "Single Target"
		1:
			$SpellNodes/Preview/VBoxContainer/Target.bbcode_text = "Circle " + str(skill.aoeSize)
		2:
			$SpellNodes/Preview/VBoxContainer/Target.bbcode_text = "Square " + str(skill.aoeSize)
		3:
			$SpellNodes/Preview/VBoxContainer/Target.bbcode_text = "Cross " + str(skill.aoeSize)
		4:
			$SpellNodes/Preview/VBoxContainer/Target.bbcode_text = "Diagonal Cross " + str(skill.aoeSize)
		5:
			$SpellNodes/Preview/VBoxContainer/Target.bbcode_text = "Vertical Line " + str(1+skill.aoeSize)
		6:
			$SpellNodes/Preview/VBoxContainer/Target.bbcode_text = "Horizontal Line " + str(1+skill.aoeSize)
	
	var desc = skill.description
	var calc = [
		ceil((skill.base) * skill.caster.dmgModMltGeneral * skill.caster.dmgModProgression),
		ceil((skill.base2) * skill.caster.dmgModMltGeneral * skill.caster.dmgModProgression),
		]
	
	if calc[0] > ceil(skill.base * skill.caster.dmgModProgression):
		calc[0] = "[color=#3de831]"+str(calc[0])+"[/color]"
		
	if calc[1] > ceil(skill.base2 * skill.caster.dmgModProgression):
		calc[1] = "[color=#3de831]"+str(calc[1])+"[/color]"
		
	desc = desc.format({"b": str(calc[0]), "b2": str(calc[1])})
	description.bbcode_text = desc 
	
	if skill.helpertext.size() > 0:
		for i in $SpellNodes/Preview/HelperBox.get_children():
			i.queue_free()
		for i in skill.helpertext:
			var newH = i.instance()
			$SpellNodes/Preview/HelperBox.add_child(newH)
	

func mouse_entered():
	$SpellNodes/Preview.visible=true


func mouse_exited():
	$SpellNodes/Preview.visible=false


func _on_VBoxContainer_resized():
	$SpellNodes/Preview.rect_size = $SpellNodes/Preview/VBoxContainer.rect_size+Vector2(10,10)
	$SpellNodes/Preview/VBoxContainer.rect_position = Vector2(5,5)
	
	$SpellNodes/Preview.rect_position.y = $SpellNodes/Preview.rect_position.y
	$SpellNodes/Preview.rect_global_position = Vector2(
		clamp($SpellNodes/Preview.rect_global_position.x, 0, get_viewport().size.x - $SpellNodes/Preview.rect_size.x), 
		clamp($SpellNodes/Preview.rect_global_position.y, 0, get_viewport().size.y - $SpellNodes/Preview.rect_size.y))


func gui_input(event):
	if event is InputEventMouseButton and event.button_index == 1 and event.pressed:
		if pushed:
			push(false)
		else:
			push(true)

func push(down):
	if down:
		if not disabled and not BattleMechanics.walkActive and not BattleMechanics.skillEffectActive:
			if BattleMechanics.skillActive:
				BattleMechanics.cancelSkill(skill.caster)
			pushed=true
			BattleMechanics.area.mpLine.points = []
			BattleMechanics.area.currentLine = []
			$AnimationPlayer.play("press")
			
			BattleMechanics.skillRange(skill, skill.caster)
	elif pushed:
		pushed=false
		$AnimationPlayer.play_backwards("press")
		if BattleMechanics.skillActive:
			BattleMechanics.cancelSkill(skill.caster)

var disabled = false
func disable(reable = false):
	if not reable:
		disabled = true
		$SpellNodes.modulate = Color(0.423529, 0.423529, 0.423529)
		if skill.cooldownTurns>0:
			$CooldownVisual.visible = true
			$CooldownVisual/Label.text = str(skill.cooldownTurns)+" turn"+("" if skill.cooldownTurns==1 else "s")
	else:
		disabled = false
		$SpellNodes.modulate = Color(1,1,1)
