using System.Collections.Generic;
using PES.Core.Simulation;
using PES.Grid.Grid3D;
using UnityEngine;

namespace PES.Presentation.Scene
{
    public sealed partial class VerticalSliceBootstrap
    {
        private void BuildSteppedMap()
        {
            var topHeightByCell = new Dictionary<(int X, int Y), int>(MapWidth * MapDepth);

            for (var x = 0; x < MapWidth; x++)
            {
                for (var y = 0; y < MapDepth; y++)
                {
                    topHeightByCell[(x, y)] = 0;
                }
            }

            for (var x = 4; x <= 7; x++)
            {
                for (var y = 4; y <= 7; y++)
                {
                    topHeightByCell[(x, y)] = 1;
                }
            }

            foreach (var pair in topHeightByCell)
            {
                var x = pair.Key.X;
                var y = pair.Key.Y;
                var topHeight = pair.Value;
                var checker = (x + y) % 2 == 0;
                var groundColor = checker ? new Color(0.27f, 0.27f, 0.27f) : new Color(0.33f, 0.33f, 0.33f);
                var topColor = topHeight > 0 ? new Color(0.42f, 0.42f, 0.42f) : groundColor;

                for (var z = 0; z <= topHeight; z++)
                {
                    CreateTileFromGrid(x, y, z, z == topHeight ? topColor : groundColor);
                }

                var topTile = new Position3(x, y, topHeight);
                _mapTiles.Add(topTile);
                _battleLoop.State.SetWalkablePosition(topTile, true);
                _battleLoop.State.SetMovementCost(topTile, 1);
            }

            AddBlockingColumn(6, 2, 1, new Color(0.2f, 0.2f, 0.2f));
            AddBlockingColumn(6, 3, 1, new Color(0.2f, 0.2f, 0.2f));
            AddBlockingColumn(6, 4, 1, new Color(0.2f, 0.2f, 0.2f));
            AddBlockingColumn(5, 6, 2, new Color(0.18f, 0.18f, 0.18f));
            AddBlockingColumn(7, 6, 2, new Color(0.18f, 0.18f, 0.18f));
        }

        private void EnsureAnkamaLikeCamera()
        {
            if (!_autoSetupIsometricCamera)
            {
                return;
            }

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            var centerX = (MapWidth - 1) * 0.5f;
            var centerY = (MapDepth - 1) * 0.5f;
            var mapCenter = new Vector3(centerX, 0f, centerY);

            var rotation = Quaternion.Euler(_cameraTiltX, _cameraYawY, 0f);
            var backward = rotation * Vector3.back;
            var cameraPosition = mapCenter + (backward * _cameraDistance) + Vector3.up * _cameraHeightOffset;

            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.rotation = rotation;
            mainCamera.orthographic = false;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 250f;

            if (!mainCamera.TryGetComponent<AudioListener>(out _))
            {
                mainCamera.gameObject.AddComponent<AudioListener>();
            }
        }

        private void AddBlockingColumn(int x, int y, int height, Color color)
        {
            if (height < 1)
            {
                height = 1;
            }

            for (var z = 1; z <= height; z++)
            {
                CreateTileFromGrid(x, y, z, color);
            }

            var topWalkable = FindTopWalkableTileAt(x, y);
            if (!topWalkable.HasValue)
            {
                return;
            }

            _battleLoop.State.SetBlockedPosition(topWalkable.Value, blocked: true);
        }

        private Position3? FindTopWalkableTileAt(int x, int y)
        {
            foreach (var tile in _mapTiles)
            {
                if (tile.X == x && tile.Y == y)
                {
                    return tile;
                }
            }

            return null;
        }

        private static GameObject CreateTileFromGrid(int x, int y, int z, Color color)
        {
            return CreateTile(new Vector3(x, z, y), color);
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

        private static Vector3 ToWorld(Position3 position)
        {
            return new Vector3(position.X, position.Z + 1.5f, position.Y);
        }

        private static GridCoord3 ToGrid(Vector3 world)
        {
            var gridZ = Mathf.FloorToInt(world.y + 0.001f);
            return new GridCoord3(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z), gridZ);
        }
    }
}
