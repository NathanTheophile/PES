extends AudioStreamPlayer

export(AudioStream) var bossTheme
export(AudioStream) var bossAproaching
onready var tween = $Tween
func bossAproach():
	pass
	tween.interpolate_property(self, 'volume_db', 0, -50, 1)
	tween.start()
	yield(tween, "tween_completed")
	
	
	stream = bossAproaching
	playing = true
	tween.interpolate_property(self, 'volume_db', -50, 0, 1)
	tween.start()

func bossAppear():
	pass
	volume_db = 0
	stream = bossTheme
	play()

func finish():
	pass
	#play()
