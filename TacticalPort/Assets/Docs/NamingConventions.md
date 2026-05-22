# Naming Conventions

This document defines the current project naming conventions for Unity assets, scene hierarchy, and scripts.

## Assets

- `S_` for scenes.
- `P_` for gameplay, world, actor, map, and feedback prefabs.
- `UI_` for UI prefabs.
- `SO_` only if a ScriptableObject asset name would otherwise be ambiguous. Existing content assets can keep domain names such as `Unit_`, `Skill_`, or `State_`.

Examples:
- `S_Bootstrap`
- `S_MainMenu`
- `P_TileBase`
- `P_TileFeedbackMovement`
- `UI_Popup_CustomMatch`
- `UI_Popup_TeamSelection`
- `UI_Screen_Loading`

## Scene And Prefab Hierarchy

Use hierarchy prefixes to describe the role of a GameObject, not its implementation type.

- `Scene_` for scene-only structural objects.
- `Runtime_` for persistent runtime services or runtime context groups.
- `Screen_` for an exclusive UI screen inside a menu shell.
- `Popup_` for modal or overlay UI surfaces.
- `Panel_` for internal sections inside a screen or popup.
- `View_` for a scoped visual surface driven by a view script.

UI child names use readable Pascal-style prefixes:
- `Btn_`
- `Txt_`
- `Img_`
- `Icon_`
- `Input_`
- `Group_`
- `Content_`
- `Viewport_`
- `Scroll_`

Examples:
- `Scene_TransitionRoot`
- `Runtime_Context`
- `Screen_MainMenu`
- `Popup_CustomMatch`
- `Panel_QuickMatchStatus`
- `View_QuickMatch`
- `Btn_CustomMatch`
- `Txt_MatchmakingStatus`

## Script Suffixes

- `View`: presentation component, visual state, no gameplay decision ownership.
- `Controller`: orchestrates local interaction, UI actions, or player-facing flow.
- `Router`: routes between UI surfaces or explicit destinations.
- `Bootstrap`: initializes a scene or runtime composition.
- `Binder`: connects an external context to an existing scene/system.
- `Service`: gameplay service or external integration implementation.
- `Runtime`: runtime state/model object.
- `Definition`: ScriptableObject data definition.
- `Importer`: converts external payloads into project runtime models.
- `Resolver`: computes a deterministic answer from inputs.
- `Evaluator`: scores or evaluates a result.
- `Validator`: validates rules and returns pass/fail intent.
- `Factory`: creates runtime objects from definitions/configuration.

Avoid `Manager` for new scripts unless the existing API or Unity workflow makes it clearly more readable.

## Code Naming

Use the current project notation consistently across C# scripts.

- Private fields: `_MyField`.
- Private serialized fields: `[SerializeField] private Type _MyField`.
- Local variables: `lMyLocal`.
- Method parameters: `pMyParam`.
- Constants: `MY_CONST`.
- Methods, properties, and types: `PascalCase`.
- Unity callbacks keep Unity's standard names: `Awake`, `Start`, `Update`, `OnValidate`, `OnDestroy`, etc.
- Event or callback variables use lower camel case with an `on` prefix: `onMyEvent`.
- Event or callback methods use PascalCase with an `On` prefix: `OnMyCallback`.
- Keep public lower camel case fields in DTOs when they map to external serialized payloads, especially JSON, UGS, Cloud Code, Relay, Lobby, Matchmaker, or EdgeGap responses. Compatibility takes priority over project readability for fields such as `ipAddress`, `allocationId`, `deploymentId`, and `requestId`.

## Method Naming

Name methods by intent. The prefix should tell whether the method queries, builds, creates, binds, mutates, or handles a callback.

- `TryX(...)`: attempts an operation and returns `bool`. Use `out pValue` when the result needs to be returned.
- `ResolveX(...)`: computes or selects a value without meaningful side effects.
- `BuildX(...)`: assembles data or request objects without instantiating Unity objects.
- `CreateX(...)`: creates or instantiates new objects.
- `ConfigureX(...)`: injects dependencies or applies configuration.
- `BindX(...)`: connects a view, service, or runtime object to a source.
- `RefreshX(...)`: reads current state and updates presentation or cached view state.
- `CacheMissingReferences()`: reserved for Unity auto-assignment of missing scene/prefab references.
- `ValidateX(...)`: validates and returns the result; avoid mutating state unless the method name makes that explicit.
- `HandleX(...)`: handles UI, input, UnityEvent, network callback, or event subscription callbacks.

Avoid `Try` on `void` methods. Prefer `XIfNeeded`, `XIfReady`, or `XIfAvailable` when the method conditionally performs an action but does not return a result.

## Script Folders

Organize scripts by product/runtime area first, then by responsibility when the folder starts to grow.

- `Scripts/App`: application-level startup, persistent runtime services, scene transition flow.
- `Scripts/Combat/Bootstrap`: combat scene composition and combat-only startup helpers.
- `Scripts/Combat`: combat commands, controllers, and gameplay-facing runtime components.
- `Scripts/Core`: pure gameplay systems, rules, services, interfaces, and runtime models.
- `Scripts/Matchmaking/Runtime`: match context, flow controllers, handoff, and debug runners.
- `Scripts/Matchmaking/Services`: UGS, local, EdgeGap, Relay, Lobby, and service contracts.
- `Scripts/Networking`: PurrNet bridge, replicated packets, runtime network helpers.
- `Scripts/UI/Menu`: main menu shell, menu screen controllers, popups, panels, loading view.
- `Scripts/UI/Combat`: combat HUD, feedback, selection, timeline UI.
- `Scripts/View`: board, camera, and unit presentation components.
- `Scripts/Editor`: Unity editor-only tools, inspectors, creators, windows, utilities.

Namespaces do not need to mirror folders during migration. Prefer preserving existing namespaces until a dedicated namespace cleanup pass.

## Menu UI Naming

The menu scene is treated as a UI shell: it hosts screens and popups without changing scene for every menu action.

- `MenuScreenRouter` owns UI shell navigation.
- `MainMenuScreenController` owns the main menu screen interactions.
- `QuickMatchPanelView` owns quick match status and buttons.
- `CustomMatchPopupView` owns custom match host/join UI.
- `TeamSelectionController` remains valid while team selection still supports both standalone and embedded popup modes.

## Migration Rules

- Preserve Unity `.meta` files when renaming assets or scripts.
- Use `FormerlySerializedAs` when renaming serialized fields.
- Do not bulk rename serialized fields without checking affected scenes and prefabs first.
- Add `[FormerlySerializedAs("OldFieldName")]` on every renamed serialized field, then keep the attribute until all affected assets have been opened, saved, and validated in Unity.
- Do not rename DTO fields that are part of external payload compatibility, even if they do not follow the project field convention.
- Keep short compatibility wrappers when public methods may be referenced by UnityEvents.
- Validate renames with C# compilation and, when possible, by opening affected scenes/prefabs in Unity.
