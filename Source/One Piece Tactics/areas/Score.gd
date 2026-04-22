extends PanelContainer

export(int) var minScore

var currentScore = 0
func updateVal(newVal):
	currentScore += newVal	
	if currentScore >= minScore:
		$HBoxContainer/Label2.modulate = Color(0.27451, 0.890196, 0.188235)
		get_parent().get_node("Target").modulate.a = 0.2
	$HBoxContainer/Label2.text = str(currentScore)
	$HBoxContainer/Label2/AnimationPlayer.play("m")
