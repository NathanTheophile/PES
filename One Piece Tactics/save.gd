extends Node

const SAVEFILE = "user://OnePieceTactics.save"
var data = {}

func save():
	var file = File.new()
	file.open(SAVEFILE,File.WRITE)
	file.store_var(data)
	file.close()

func reset():
	data = {
			"disclaimer":			true,
			"tutorial":				true,
			"stagesInfo":			{},
		}
	save()
	

func load_save():
	var file = File.new()
	if not file.file_exists(SAVEFILE):
		data = {
			"disclaimer":			true,
			"tutorial":				true,
			"stagesInfo":			{
				# EXAMPLE
				#1:	{
					#"crowns": 				[true,true,true],
					#"bestTime:" 			80.26,
					#"bestTimeAllCrowns:	98.45,
				#	}
			},
		}
		save()
	file.open(SAVEFILE,File.READ)
	data = file.get_var()
	file.close()
