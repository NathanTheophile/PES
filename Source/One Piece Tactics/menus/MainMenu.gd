extends Node2D

onready var characters = $Personagens
export(PackedScene) var run 

func _ready():
	Save.load_save()
	$Disclaimer.visible = Save.data["disclaimer"]
	GlobalFunctions.menu = self
	update()
	

func update():
	for i in $PanelContainer/ScrollContainer/VBoxContainer.get_children():
		if i.has_method("update"):
			i.update()

func selectChar(button_pressed, id):
	if button_pressed:
		Run.charToLoad.append(characters.path[id])
	else:
		Run.charToLoad.erase(characters.path[id])
	print(Run.charToLoad)


func fullscreen(toggle):
	OS.window_fullscreen = toggle 


func reset_save():
	_ready()
	Save.reset()


func checkControls():
	$Controls.visible = true
