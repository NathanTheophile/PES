extends Node

var currentCharacters = []
var charToLoad = []

func _ready():
	pass

func loadChars():
	for i in charToLoad:
		currentCharacters.append(load(i).instance())
