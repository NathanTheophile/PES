extends Node


var collision = preload("res://sfx/impact 1.mp3")
var warning = preload("res://sfx/warning.mp3")
var songNode
func _ready():
	songNode = AudioStreamPlayer.new()
	songNode.bus = "Song"
	add_child(songNode)

var currentSong
func songPlay(stream):
	songNode.stream = stream
	songNode.play()

func songStop():
	songNode.stop()

func play(stream, info={}):
	var newAudio = AudioStreamPlayer.new()
	var streamUse
	if info.has("randp"):
		streamUse = AudioStreamRandomPitch.new()
		streamUse.audio_stream = stream
		streamUse.random_pitch = info["randp"] if info["randp"] is float else 1.1
	else:
		streamUse = stream
	
	newAudio.stream = streamUse
	newAudio.bus = "SFX"
	if info.has("reverb"):
		newAudio.bus = "SFX Reverb"
	if info.has("pitch"):
		newAudio.pitch_scale = info["pitch"]
	if info.has("db"):
		newAudio.volume_db = info["db"]
	
	add_child(newAudio)
	
	var from = 0 if !info.has("from") else info["from"]
	newAudio.play(from)
	
	yield(newAudio,"finished")
	
	newAudio.queue_free()
