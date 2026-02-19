extends HBoxContainer

func setup(array, text):
	for i in array.size():
		if array[i]:
			get_child(i).get_node("AnimationPlayer").play("set")
		else:
			get_child(i).get_node("AnimationPlayer").play("unset")
	for i in text.size():
		get_child(i).hint_tooltip = text[i]

func fail(i):
	get_child(i).get_node("AnimationPlayer").play("fail")
