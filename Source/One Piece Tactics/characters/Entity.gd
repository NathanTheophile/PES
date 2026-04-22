extends Node2D

# ----------------------------------------
# -- Valores customizáveis

export(String) var ent_name

export(int) var hp

export(int) var ap
export(int) var mp
export(int) var ult
var currentUlt = 0

export var specialFlags = 0
export var summondeath = false

export(int, "Fire", "Earth", "Air", "Water", "None") var weakness = 4
export(int, "Fire", "Earth", "Air", "Water", "None") var resist = 4
export(Array, PackedScene) var spells
export(Array, PackedScene) var passive
export(Script) var ai
export(int, "weakFirst", "strongFirst", "closeFirst") var aiType

export(bool) var player
export(float) var speed
export(bool) var boss
export(bool) var big
export(Array, int) var phases
export(bool) var counts = true
# ----------------------------------------
# -- Sinais

signal walked(from, to); signal teleported(from, to, teleporter); signal pushed(cells, pusher)
signal pushTravel(from, to); signal switchWith(who)

signal dmg_taken(dmg, attacker, before); signal dmg_dealt(dmg, attacked, before)
signal healed(heal, healer, before); signal healing(heal, healed, before)
signal killed(killer)

signal phaseTransition(to,from)

signal skill_used(skill, cell, before)
signal turn_end; signal turn_start
signal mp_preview

var canBePushed = true
var canBeTpd = true
export(Array, PackedScene) var extraStuff
export(bool) var plays = true
export(bool) var scoreboss = false
# ----------------------------------------
# -- Váriaveis extras

var extraRange = 0

var dmgModMltGeneral = 1.0
var dmgReceiveMltGeneral = 1.0
var healModGeneral = 0
var healModMltGeneral = 1.0

export var dmgModProgression = 1.0
	# -- [f, e, a, w]

var mp_used = 0
var ap_used = 0 
var hp_loss = 0

var direction = Vector2(1,0)
var posit = Vector2(0,0)
var allPosits = []
var busy = false
var reDirect = false

onready var walkTween = $WalkTween
onready var pushTween = $PushTween

var stateNode

var skillsUsable = []
var notPlayed = true
var taunting = false
var dead = false

var allyStab = false
var enmyStab = false
var tlptStab = false
var pushStab = false

var hideSts = false
var summon = false


export(float) var debugMagn = 1
export(AudioStream) var stepSound
export(Array, AudioStream) var audioEX
export var jumpOnHit = true
export var jumpOnWalkEnd = true
var pushMod = 0
# ----------------------------------------
# -- 

func showSts():
	hideSts = false
	$HPBar.visible = true

func fhideSts():
	hideSts = true
	$HPBar.visible = false

export var size:float
func _ready():
	dmgModMltGeneral *= debugMagn
	stateNode = $States
	if not player:
		dmgModMltGeneral = round(BattleMechanics.globalDMGMult * dmgModMltGeneral)
		hp = round(hp * BattleMechanics.globalHPMult)
		$HPBar.updateHP(true)
	$VisualFront.scale *= size
	if has_node("VisualBack"):
		$VisualBack.scale *= size
	if has_node("AI"):
		$AI.set_script(ai)
	if spells.size()>0:
		for i in spells.size():
			var addSpell = spells[i].instance()
			addSpell.caster = self
			
			$Skills.add_child(addSpell)
			skillsUsable.append(addSpell)
	if passive.size()>0:
		for i in passive.size():
			BattleMechanics.addState(passive[i], self, self)
	if scoreboss:
		connect("dmg_taken",self,"scoreupdate")
	connect("killed",self,"stopAll")
	if boss and not summondeath:
		connect("killed",BattleMechanics,"bossKill",[self])
	if phases.size()>0:
		connect("dmg_taken",self,"phaseChange")

func scoreupdate(dmg,_2, before):
	if !before:
		BattleMechanics.area.get_node("Score").updateVal(dmg)

func stopAll(_a,_b):
	if has_node("AI"):
		$AI.queue_free()


func _input(event):
	if BattleMechanics.currentPlaying and BattleMechanics.currentPlaying == self and player and not busy and not BattleMechanics.skillActive:
		if event is InputEventKey and event.pressed and not event.echo:
			match event.scancode:
				KEY_UP, KEY_W:
					turn_to(Vector2.UP)
					#BattleMechanics.bounceTgt(self)
				KEY_DOWN, KEY_S:
					turn_to(Vector2.DOWN)
					#BattleMechanics.bounceTgt(self)
				KEY_RIGHT, KEY_D:
					turn_to(Vector2.RIGHT)
					#BattleMechanics.bounceTgt(self)
				KEY_LEFT, KEY_A:
					turn_to(Vector2.LEFT)
					#BattleMechanics.bounceTgt(self)
			
			

func turn_to(directionTo):
	direction = directionTo
	if has_node("VisualBack"):
		match directionTo:
			Vector2(1,0):
				$VisualFront.scale.x = size 
				$VisualBack.scale.x = size
			Vector2(0,1):
				$VisualFront.scale.x = -size
				$VisualBack.scale.x = -size
			Vector2(0,-1):
				$VisualFront.scale.x = -size
				$VisualBack.scale.x = -size
			Vector2(-1,0):
				$VisualFront.scale.x = size
				$VisualBack.scale.x = size
		if direction.x+direction.y>0:
			$VisualBack.visible = false
			$VisualFront.visible = true
		else:
			$VisualBack.visible = true
			$VisualFront.visible = false
		

func select(_a, event, _b):
	if event is InputEventMouseButton and event.pressed:
		if plays and player and BattleMechanics.battleState == 1 and not (self in BattleMechanics.havePlayedThisTurn) and not BattleMechanics.currentPlaying:
			BattleMechanics.setPlaying(self)
			busy = true
			yield(get_tree().create_timer(0.1),"timeout")
			busy = false

func animation_finished(anim_name):
	if anim_name != "idle" and anim_name != "walk":
		BattleMechanics.playAnimEntity(self, "idle")

var currentPhase = 1
func phaseChange(_a,_b,before):
	if not before and BattleMechanics.hasState(self,"Phases"):
		var percent = (hp-hp_loss) / hp * 100
		if currentPhase-1 < phases.size():
			if percent <= phases[currentPhase-1]:
				emit_signal("phaseTransition",currentPhase,currentPhase+1)
				currentPhase +=1
				BattleMechanics.procUp(BattleMechanics.getState(self, "Phases"),1)
				AM.play(AM.warning)
				
