extends Node
var menu
var fade = preload("res://menus/Fade.tscn")
var win = preload("res://menus/Win.tscn")
var winS = preload("res://menus/WinScore.tscn")
var lose = preload("res://menus/Lose.tscn")
var root
var runNode

func loadArea(scene, pack=false):
	yield(get_tree(),"idle_frame")
	if not root:
		root = menu.get_parent()
	var newFade = fade.instance()
	root.add_child(newFade)
	newFade.get_node("AnimationPlayer").play("fade")
	yield(get_tree().create_timer(0.5),"timeout")
	root.remove_child(menu)
	var newScene = load(scene).instance() if not pack else scene.instance()
	root.add_child(newScene)
	yield(get_tree().create_timer(0.5),"timeout")
	newFade.get_node("AnimationPlayer").play_backwards("fade")
	yield(get_tree().create_timer(0.5),"timeout")
	newFade.queue_free()

func returnToMenu():
	var newFade = fade.instance()
	root.add_child(newFade)
	newFade.get_node("AnimationPlayer").play("fade")
	yield(get_tree().create_timer(0.5),"timeout")
	BattleMechanics.walkActive = false
	BattleMechanics.skillEffectActive = false
	BattleMechanics.skillActive = false
	BattleMechanics.area.queue_free()
	root.add_child(menu)
	menu.update()
	newFade.get_node("AnimationPlayer").play_backwards("fade")
	yield(get_tree().create_timer(0.5),"timeout")
	newFade.queue_free()

func winBattle():
	
	if BattleMechanics.area.lvl==5:
		var winScene = winS.instance()
		BattleMechanics.area.add_child(winScene)
		return
		
	var winScene = win.instance()
	BattleMechanics.area.add_child(winScene)

func loseBattle():
	var loseScene = lose.instance()
	BattleMechanics.area.add_child(loseScene)
