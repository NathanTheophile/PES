extends Node

# targetArray = Array contendo as posições que esse feitiço atingiu
#               A célula central será contida em um Array sozinha, para fácil identificação
var skill
func effects(targetArray):
	skill = get_parent()
	
	BattleMechanics.playAnimEntity(skill.caster, "Shoot")
	yield(get_tree().create_timer(0.35),"timeout")
	yield(BattleMechanics.shootProjectile(skill.visuals[1], skill.caster.posit, targetArray[0]),"completed")
	
	BattleMechanics.dmgEntityAt(targetArray[0], [3,"w"], skill.caster)
	BattleMechanics.playAnim(skill.visuals[0],targetArray[0])
	
	var randCell = BattleMechanics.getCellInRadius(targetArray[0], 3, true, 1, [1])
	if randCell:
		BattleMechanics.shootProjectile(skill.visuals[2], targetArray[0], randCell, 0.6, Vector2(0,21))
		yield(get_tree().create_timer(0.6),"timeout")
		BattleMechanics.placeGlyph(skill.states[0], randCell, skill.caster)
		BattleMechanics.playAnim(skill.visuals[0],randCell)
	
	
