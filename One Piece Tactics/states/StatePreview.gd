extends Panel

var state
var icon

func _ready():
	mouse_filter = Control.MOUSE_FILTER_IGNORE

func details(stateProvide=false):
	if not state:
		state = stateProvide
	$VBoxContainer/Name.text = state.stateName
	var desc = state.description
	
	if state.modularDesc:
		desc = desc.format({
			"lvl": str(floor((state.lvl-state.lvlSub)*state.lvlMlt)), 
			"lvl2": str(floor((state.lvl-state.lvlSub)*state.lvl2Mlt)),
			"lvl3": str(floor((state.lvl-state.lvlSub)*state.lvl3Mlt)),
			
			})
	
	$VBoxContainer/Desc.bbcode_text = desc
	resize()

func resize():
	rect_size = $VBoxContainer.rect_size+Vector2(10,10)
	rect_position = icon.rect_global_position+Vector2(20,0-rect_size.y)
	rect_position.x = clamp(rect_position.x,0,get_viewport().size.x-rect_size.x)
	rect_position.y = clamp(rect_position.y,30,get_viewport().size.y-rect_size.y)

var window
func showSelf():
	icon.modulate.a = 1
	
func hideSelf():
	icon.modulate.a = 0.5
