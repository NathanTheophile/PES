#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Data
#endregion

using System.Collections.Generic;
using UnityEngine;

namespace TacticalPort.Data
{
    [CreateAssetMenu(fileName = "TeamPresetCatalog", menuName = "Project/Data/Combat Team Preset Catalog")]
    public sealed class CombatTeamPresetCatalog : ScriptableObject
    {
        #region _____________________________/ VALUES

        [SerializeField] private List<CombatTeamPresetDefinition> _Presets = new List<CombatTeamPresetDefinition>();
        [SerializeField] private List<UnitDefinition> _Units = new List<UnitDefinition>();

        #endregion

        #region _____________________________| RESOLVE

        public bool TryGetUnits(string pPresetId, out IReadOnlyList<UnitDefinition> pUnits)
        {
            if (!string.IsNullOrWhiteSpace(pPresetId))
            {
                for (int lIndex = 0; lIndex < _Presets.Count; lIndex++)
                {
                    CombatTeamPresetDefinition lPreset = _Presets[lIndex];
                    if (lPreset == null || lPreset.PresetId != pPresetId)
                        continue;

                    if (lPreset.TryValidate(out _))
                    {
                        pUnits = lPreset.Units;
                        return true;
                    }

                    break;
                }
            }

            pUnits = null;
            return false;
        }

        public bool TryGetUnits(IReadOnlyList<string> pUnitIds, out IReadOnlyList<UnitDefinition> pUnits)
        {
            List<UnitDefinition> lUnits = new List<UnitDefinition>();
            if (pUnitIds != null)
            {
                for (int lIndex = 0; lIndex < pUnitIds.Count; lIndex++)
                {
                    if (TryGetUnit(pUnitIds[lIndex], out UnitDefinition lUnit))
                        lUnits.Add(lUnit);
                }
            }

            pUnits = lUnits;
            return lUnits.Count > 0;
        }

        private bool TryGetUnit(string pUnitId, out UnitDefinition pUnit)
        {
            if (!string.IsNullOrWhiteSpace(pUnitId))
            {
                for (int lIndex = 0; lIndex < _Units.Count; lIndex++)
                {
                    UnitDefinition lUnit = _Units[lIndex];
                    if (lUnit != null && lUnit.Id == pUnitId)
                    {
                        pUnit = lUnit;
                        return true;
                    }
                }

                for (int lPresetIndex = 0; lPresetIndex < _Presets.Count; lPresetIndex++)
                {
                    CombatTeamPresetDefinition lPreset = _Presets[lPresetIndex];
                    IReadOnlyList<UnitDefinition> lPresetUnits = lPreset != null ? lPreset.Units : null;
                    if (TryGetUnitFromList(pUnitId, lPresetUnits, out pUnit))
                        return true;
                }
            }

            pUnit = null;
            return false;
        }

        private static bool TryGetUnitFromList(string pUnitId, IReadOnlyList<UnitDefinition> pUnits, out UnitDefinition pUnit)
        {
            if (!string.IsNullOrWhiteSpace(pUnitId) && pUnits != null)
            {
                for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
                {
                    UnitDefinition lUnit = pUnits[lIndex];
                    if (lUnit != null && lUnit.Id == pUnitId)
                    {
                        pUnit = lUnit;
                        return true;
                    }
                }
            }

            pUnit = null;
            return false;
        }

        #endregion
    }
}
