#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Data
#endregion

using System;
using UnityEngine;

namespace TacticalPort.Data
{
    [Serializable]
    public sealed class SceneScenarioDefinition
    {
        #region _____________________________/ VALUES

        [Header("Identity")]
        [SerializeField] private string _ScenarioId = "sample_scene";
        [SerializeField] private string _DisplayName = "Sample Combat";

        [Header("Board")]
        [SerializeField, Min(1)] private int _DefaultMovementCost = 1;

        #endregion

        #region _____________________________/ ACCESSORS

        public string ScenarioId => string.IsNullOrWhiteSpace(_ScenarioId) ? "scene_board" : _ScenarioId;
        public string DisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? "Scene Board" : _DisplayName;
        public int DefaultMovementCost => Mathf.Max(1, _DefaultMovementCost);

        #endregion

    }
}
