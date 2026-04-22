extends Polygon2D

var posit
export(PackedScene) var predictScene
var predict

func _ready():
	posit = BattleMechanics.area.tilemap.world_to_map(position)
	
	
	
	var t = BattleMechanics.getEntityAt(posit)
	var s = BattleMechanics.currentActiveSkill
	var p = BattleMechanics.currentPlaying
	if p.player and t and s.base > 0 and t!=p:
		connect("tree_exiting",self,"exit")
		predict = predictScene.instance()
		
		
		
		var mod = 1.0
		if s.get_node("Effects").has_method("dmgMod"):
			mod = s.get_node("Effects").dmgMod(t)
		
		var totalmod = mod * p.dmgModMltGeneral * t.dmgReceiveMltGeneral
		var rearmod = BattleMechanics.getDirectionalDmg(p,t)
		var calc = s.base * totalmod * rearmod * p.dmgModProgression
		
		calc = ceil(calc)
		
		if ceil(s.base * p.dmgModProgression)<calc:
			predict.modulate = Color(0.380392, 1, 0.294118)
		elif ceil(s.base * p.dmgModProgression)>calc:
			predict.modulate = Color(0.870588, 0.298039, 0.298039)
		
		if 1 * totalmod * rearmod > 1:
			var percent = predict.get_node("c/Label2")
			percent.visible = true
			percent.text = "+"+str(100*totalmod*rearmod-100)+"%"
		
		predict.get_node("c/Label").text = "-"+str(calc)
		predict.global_position = t.get_node("HPBar").global_position+Vector2(0,+15)
		predict.position.x = min(predict.position.x, OS.get_screen_size().x-15)
		predict.position.y = min(predict.position.y, OS.get_screen_size().y-15)
		BattleMechanics.area.overvisual.add_child(predict)

func exit():
	predict.queue_free()
