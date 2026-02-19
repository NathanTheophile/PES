# Regression Checklist — Vertical Slice Battle Loop

Use this short checklist after each gameplay/system change affecting movement, targeting, combat resolution, or turn flow.

## Scope
- Scene: `PES_Unity/Assets/Scenes/Tests/VerticalSlice_BattleLoop.unity`
- Tests: EditMode suite under `PES_Unity/Assets/_Project/Tests/EditMode/`

## Manual Validation (Scene)
- [ ] **Move**: a valid move command updates the unit position to the expected tile.
- [ ] **Attack**: a valid basic attack in range applies damage to the target.
- [ ] **LOS blocked**: attack is rejected when vertical LOS rule is violated.
- [ ] **Height bonus**: attacker above target deals higher damage than equivalent flat attack.
- [ ] **Turn advance**: after each resolved action, simulation tick advances exactly by +1.

## Automated Validation (Test Runner)
- [ ] EditMode tests pass.
- [ ] MoveAction tests pass (valid + rejected cases).
- [ ] BasicAttackAction tests pass (success + out-of-range + missed + LOS blocked).
- [ ] Resolver pipeline tests pass (event log ordering + tick progression).
- [ ] Replay-oriented deterministic test passes (fixed seed + fixed action sequence + expected snapshot).

## Session Log (optional)
- Date:
- Branch/commit:
- Tester:
- Notes:
