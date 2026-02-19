extends Control

export(Array, bool) var locked
export(Array, String) var path

onready var cont = $"Panel/ScrollContainer/HBoxContainer"
onready var label = $PersonagensLabel
func _ready():
	for i in cont.get_children():
		for j in i.get_children():
			j.connect("toggled",self,"button_press",[j])
			if j is Button and locked[int(j.name)]:
				j.disabled = true
				var lock = Sprite.new()
				lock.texture = load("res://lock.png")
				lock.position = Vector2(95,10)
				j.add_child(lock)
var pressed = 0
func button_press(press,button):
	if press:
		pressed += 1
	else:
		pressed -= 1
	label.text = "Characters " + str(pressed) +"/2"
