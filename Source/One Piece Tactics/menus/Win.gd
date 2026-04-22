extends Node2D

func _ready():
	for i in $VBoxContainer.get_child_count():
		$VBoxContainer.get_child(i).get_node("cond").text = BattleMechanics.area.get_node("Trophies").text[i]
	$Label/time.text = "Your time: " + BattleMechanics.area.ui.timeFormat(BattleMechanics.area.ui.time,true)
	
	if has_node("Label/score"):
		get_node("Label/score").text = "Your score: " + str(BattleMechanics.area.get_node("Score").currentScore)
	
	yield(get_tree().create_timer(2),"timeout")
	for i in $VBoxContainer.get_child_count():
		if BattleMechanics.area.trophies[i]:
			$VBoxContainer.get_child(i).get_node("TextureRect/AnimationPlayer").play("m")
			yield(get_tree().create_timer(0.25),"timeout")
		
func _on_Button_button_up():
	GlobalFunctions.returnToMenu()
