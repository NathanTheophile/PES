extends Polygon2D

var posit
func _ready():
	update(true) 

func update(all=false):
	var parent = get_parent()
	var adj = []
	var corn = []
	
	for i in parent.get_children():
		if not i==self:
			var value = i.posit-posit
			
			if abs(value.x)+abs(value.y)==1:
				if all:
					i.update()
				adj.append(value)
			
			elif abs(value.x)+abs(value.y)==2 and abs(value.x)==abs(value.y):
				if all:
					i.update()
				corn.append(value)
	
	if adj.size()>0:
		for i in adj:
			var gname
			match i:
				Vector2(1,0):
					gname = "downright"
				Vector2(0,1):
					gname = "downleft"
				Vector2(-1,0):
					gname = "upleft"
				Vector2(0,-1):
					gname = "upright"
				
			for j in get_children():
				if j.get_child_count()>0:
					for k in j.get_children():
						if k.is_in_group(gname):
							k.visible=false
				
				if j.is_in_group(gname):
					j.visible=false
		for i in corn:
			var gname
			match i:
				Vector2(1,1):
					gname = "down"
				Vector2(-1,1):
					gname = "left"
				Vector2(-1,-1):
					gname = "up"
				Vector2(1,-1):
					gname = "right"
			if gname:
				for j in $corners.get_children():
					if j.is_in_group(gname):
						j.visible=false
				
