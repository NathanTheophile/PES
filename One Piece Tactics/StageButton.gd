extends PanelContainer

export(String) var scene
export(int) var lvl
onready var crowns = $VBoxContainer/HBoxContainer/crowns
onready var best = $VBoxContainer/HBoxContainer2/VBoxContainer2/bt
onready var bestD = $VBoxContainer/HBoxContainer2/VBoxContainer3/btD
onready var bestAC = $VBoxContainer/HBoxContainer2/VBoxContainer2/btac
onready var bestACD = $VBoxContainer/HBoxContainer2/VBoxContainer3/btacD
onready var score

func _ready():
	if has_node("VBoxContainer/HBoxContainer2/VBoxContainer5/score"):
		score = get_node("VBoxContainer/HBoxContainer2/VBoxContainer5/score")

func gui_input(event):
	if event is InputEventMouseButton and event.pressed and event.button_index == 1:
		GlobalFunctions.loadArea(scene)


func timeFormat(t):
	
	var uT = floor(t)
	
	var m = floor(float(uT) / 60)
	
	var h = floor(float(m) / 60)
	
	var s = t - (3600 * h) - (60 * m)
	
	if h>1:
		m = ("0" if m <10 else "") +str(m)
	else:
		m = str(m)
	h = str(h)
	s = ("0" if s <10 else "") + str(stepify(s,0.01))
	
	return (h+":"+m+":"+s if t>3600 else m+":"+s).split(".")

func update():
	if lvl > 1 and !Save.data["stagesInfo"].has(lvl-1):
		modulate = Color(1,1,1,0.3)
	else:
		modulate = Color(1,1,1)
	if Save.data["stagesInfo"].has(lvl):
		var data = Save.data["stagesInfo"][lvl]
		if data["crowns"].size()==3:
			for i in data["crowns"].size():
				if data["crowns"][i]:
					crowns.get_child(i).modulate = Color(1,1,1)
				else:
					crowns.get_child(i).modulate = Color(0.301961, 0.301961, 0.301961, 0.54902)
		else:
			for i in crowns.get_children():
				i.modulate = Color(1,1,1)
				i.get_child(0).emitting = true
		
		
		if data["bestTime"] == 0:
			best.modulate = Color(1,1,1,0.2)
			bestD.modulate = Color(1,1,1,0.2)
			
		else:
			best.modulate = Color(1,1,1)
			bestD.modulate = Color(1,1,1)
			var bTime = timeFormat(data["bestTime"])
			best.text = bTime[0]
			bestD.text = "."+bTime[1]
		
		
		if data["bestTimeAllCrowns"] == 0:
			bestAC.modulate = Color(1,1,1,0.2)
			bestACD.modulate = Color(1,1,1,0.2)
		else:
			bestAC.modulate = Color(1,1,1)
			bestACD.modulate = Color(1,1,1)
			var bTimeAC = timeFormat(data["bestTimeAllCrowns"])
			bestAC.text =  bTimeAC[0]
			if bTimeAC.size()>1:
				bestACD.text =  "."+bTimeAC[1]
			else:
				bestACD.text =  ".00"
		
		if score:
			if data.has("score"):
				score.text = str(data["score"])
	else:
		bestAC.modulate = Color(1,1,1,0.2)
		bestACD.modulate = Color(1,1,1,0.2)
		bestD.modulate = Color(1,1,1,0.2)
		best.modulate = Color(1,1,1,0.2)
