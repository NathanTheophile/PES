#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback
//  Data
#endregion

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalPort.Data
{
    public sealed class PlayableRosterDefinition : ScriptableObject
    {
        #region _____________________________/ VALUES

        [SerializeField] private List<UnitDefinition> _Units = new List<UnitDefinition>();

        #endregion

        #region _____________________________/ ACCESSORS

        public IReadOnlyList<UnitDefinition> Units =>
            (IReadOnlyList<UnitDefinition>)_Units ?? Array.Empty<UnitDefinition>();

        #endregion

        #region _____________________________| VALIDATION

        public bool TryGetValidationError(out string pError)
        {
            if (_Units == null || _Units.Count == 0)
            {
                pError = "The playable roster must contain at least one character.";
                return true;
            }

            for (int lIndex = 0; lIndex < _Units.Count; lIndex++)
            {
                UnitDefinition lUnit = _Units[lIndex];
                if (lUnit == null)
                {
                    pError = $"Entry {lIndex + 1} is empty.";
                    return true;
                }

                for (int lPreviousIndex = 0; lPreviousIndex < lIndex; lPreviousIndex++)
                {
                    UnitDefinition lPrevious = _Units[lPreviousIndex];
                    if (lPrevious == lUnit)
                    {
                        pError = $"'{lUnit.DisplayName}' is assigned more than once.";
                        return true;
                    }

                    if (lPrevious != null && !string.IsNullOrWhiteSpace(lUnit.Id) &&
                        string.Equals(lPrevious.Id, lUnit.Id, StringComparison.Ordinal))
                    {
                        pError = $"Character ID '{lUnit.Id}' is used by more than one entry.";
                        return true;
                    }
                }
            }

            pError = string.Empty;
            return false;
        }

        #endregion
    }
}
