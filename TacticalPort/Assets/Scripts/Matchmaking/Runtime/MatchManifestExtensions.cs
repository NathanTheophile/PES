#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System.Collections.Generic;
using TacticalPort.Data;

namespace TacticalPort.Matchmaking
{
    public static class MatchManifestExtensions
    {
        public static void SetUnitIds(this MatchManifest pManifest, string pPlayerId, IReadOnlyList<UnitDefinition> pUnits)
        {
            if (pManifest == null || !pManifest.TryGetAssignment(pPlayerId, out MatchPlayerAssignment lAssignment))
                return;

            lAssignment.UnitIds ??= new List<string>();
            lAssignment.UnitIds.Clear();
            if (pUnits == null)
                return;

            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = pUnits[lIndex];
                if (lUnit != null && !string.IsNullOrWhiteSpace(lUnit.Id))
                    lAssignment.UnitIds.Add(lUnit.Id);
            }
        }
    }
}
