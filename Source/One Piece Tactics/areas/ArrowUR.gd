extends Node2D

export(Vector2) var dir
var key

func select(_viewport, event, _shape_idx):
	if event is InputEventMouseButton and event.pressed:
		push(true)
	if event is InputEventMouseButton and not event.pressed:
		push(false)

func _ready():
	match dir:
		Vector2(1,0):
			$Sprite.flip_v = true
			$Sprite2.flip_v = true
			
			$Sprite.flip_h = false
			$Sprite2.flip_h = false
			
			$Label.text = "D"
			if (get_parent().get("menu")==true and !get_parent().menu) or !get_parent().get("menu"):
				key = [KEY_RIGHT,KEY_D]
		
		Vector2(-1,0):
			$Sprite.flip_v = false
			$Sprite2.flip_v = false
			
			$Sprite.flip_h = true
			$Sprite2.flip_h = true
			
			$Label.text = "A"
			if (get_parent().get("menu")==true and !get_parent().menu) or !get_parent().get("menu"):
				key = [KEY_LEFT,KEY_A]
		
		Vector2(0,1):
			$Sprite.flip_v = true
			$Sprite2.flip_v = true
			
			$Sprite.flip_h = true
			$Sprite2.flip_h = true
			
			$Label.text = "S"
			if (get_parent().get("menu")==true and !get_parent().menu) or !get_parent().get("menu"):
				key = [KEY_DOWN,KEY_S]
		
		Vector2(0,-1):
			$Sprite.flip_v = false
			$Sprite2.flip_v = false
			
			$Sprite.flip_h = false
			$Sprite2.flip_h = false
			
			$Label.text = "W"
			if (get_parent().get("menu")==true and !get_parent().menu) or !get_parent().get("menu"):
				key = [KEY_UP,KEY_W]

func _input(event):
	if key:
		if (BattleMechanics.battleState == 0 and BattleMechanics.selectedForPlacing and get_parent() == BattleMechanics.selectedForPlacing) or (
			BattleMechanics.battleState == 1 and get_parent().entity
		):
			if event is InputEventKey and event.scancode in key and event.pressed and not beingPushed and not event.echo:
				push(true)
				return
			elif event is InputEventKey and event.scancode in key and beingPushed and  not event.pressed :
				push(false)
				return

var beingPushed = false
func push(down):
	if not down:
		beingPushed = false
		$Sprite2.visible = true
		
	else:
		beingPushed = true
		$Sprite2.visible = false
		get_parent().entity.turn_to(dir)
		#BattleMechanics.bounceTgt(get_parent().entity)
