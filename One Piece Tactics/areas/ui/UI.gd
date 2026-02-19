extends Node2D

onready var crowns = $Trophies
func changeRound(val):
	$RoundLbl.text = "Round "+str(val)
	$AnimationRound.play("ShineRound")

func setCurrentTeam(who):
	match who:
		"player":
			$UpperUI/AnimationTurn.play("StartPlayerTurn")
		"enemy":
			$UpperUI/AnimationTurn.play("StartEnemyTurn")

func advanceWaves(val, waveUpdt=false):
	pass

var time = 0.0
func _process(delta):
	time += delta
	$Time.text = timeFormat(time)

func timeFormat(t,allDecimal=false):
	
	var uT = floor(t)
	
	var m = floor(float(uT) / 60)
	
	var h = floor(float(m) / 60)
	
	var s = t - (3600 * h) - (60 * m)
	
	if h>1:
		m = ("0" if m <10 else "") +str(m)
	else:
		m = str(m)
	h = str(h)
	var dec = 0.1
	if allDecimal:
		dec = 0.01
	s = ("0" if s <10 else "") + str(stepify(s,dec))
	
	return h+":"+m+":"+s if t>3600 else m+":"+s

func help(event):
	if event is InputEventMouseButton and event.button_index == 1 and event.pressed:
		$Tutorial.visible = true


func turnEndButton():
	BattleMechanics.area.confirm()
