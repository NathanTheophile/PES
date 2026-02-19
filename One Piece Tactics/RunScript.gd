extends Node2D

export(String) var battle

func _ready():
	Run.loadChars()

func loadArea():
	GlobalFunctions.loadArea(battle)
