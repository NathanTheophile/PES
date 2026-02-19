using UnityEngine;

namespace PES.Presentation.Scene
{
    /// <summary>
    /// Bootstrap MonoBehaviour pour visualiser une mini boucle tactique 3D.
    /// Appuyer sur Espace pour exécuter l'action suivante.
    /// </summary>
    public sealed class VerticalSliceBootstrap : MonoBehaviour
    {
        private VerticalSliceBattleLoop _battleLoop;
        private GameObject _unitAView;
        private GameObject _unitBView;

        private void Start()
        {
            _battleLoop = new VerticalSliceBattleLoop();

            BuildSteppedMap();
            _unitAView = CreateUnitVisual("UnitA", Color.cyan);
            _unitBView = CreateUnitVisual("UnitB", Color.red);
            SyncUnitViews();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var result = _battleLoop.ExecuteNextStep();
                SyncUnitViews();
                Debug.Log($"[VerticalSlice] {result.Description}");
            }
        }

        private void BuildSteppedMap()
        {
            CreateTile(new Vector3(0f, 0f, 0f), new Color(0.25f, 0.25f, 0.25f));
            CreateTile(new Vector3(1f, 0f, 0.5f), new Color(0.35f, 0.35f, 0.35f));
            CreateTile(new Vector3(2f, 0f, 0.5f), new Color(0.45f, 0.45f, 0.45f));
            CreateTile(new Vector3(2f, 1f, 1f), new Color(0.55f, 0.55f, 0.55f));
        }

        private static GameObject CreateTile(Vector3 center, Color color)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = $"Tile_{center.x}_{center.y}_{center.z}";
            tile.transform.position = center;
            tile.transform.localScale = new Vector3(1f, 1f, 1f);

            var renderer = tile.GetComponent<Renderer>();
            renderer.material.color = color;
            return tile;
        }

        private static GameObject CreateUnitVisual(string objectName, Color color)
        {
            var unit = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unit.name = objectName;

            var renderer = unit.GetComponent<Renderer>();
            renderer.material.color = color;
            return unit;
        }

        private void SyncUnitViews()
        {
            if (_battleLoop.State.TryGetEntityPosition(VerticalSliceBattleLoop.UnitA, out var unitAPosition))
            {
                _unitAView.transform.position = ToWorld(unitAPosition);
            }

            if (_battleLoop.State.TryGetEntityPosition(VerticalSliceBattleLoop.UnitB, out var unitBPosition))
            {
                _unitBView.transform.position = ToWorld(unitBPosition);
            }
        }

        private static Vector3 ToWorld(Core.Simulation.Position3 position)
        {
            return new Vector3(position.X, position.Z + 0.5f, position.Y);
        }
    }
}
