extends Polygon2D

var entity = false
var fieldPosit = Vector2(0,0)

func hover(x):
	if x:
		color = Color(0.476715, 0.846642, 0.992188)
	else:
		color = Color(0.223529, 0.701961, 0.890196)

func select(_viewport, event, _shape_idx):
	if event is InputEventMouseButton and event.pressed:

		if entity and not BattleMechanics.selectedForPlacing:
			
			BattleMechanics.selectForPlacing(self)
			get_node("../..").changeLabel("Confirm character position")
			return

		elif BattleMechanics.selectedForPlacing and BattleMechanics.selectedForPlacing!=self:
			var positTo = BattleMechanics.selectedForPlacing.position
			var positToMap = BattleMechanics.selectedForPlacing.fieldPosit
			
			BattleMechanics.selectedForPlacing.position = position
			BattleMechanics.selectedForPlacing.fieldPosit = fieldPosit
			position = positTo
			fieldPosit = positToMap
			
			BattleMechanics.selectedForPlacing.entity.position = BattleMechanics.selectedForPlacing.position
			BattleMechanics.selectedForPlacing.entity.posit = BattleMechanics.selectedForPlacing.fieldPosit
			
			if entity:
				entity.position = positTo
				entity.posit = positToMap
