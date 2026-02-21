using UnityEngine;

namespace PES.Infrastructure.Serialization
{
    public static class SessionSaveSerializer
    {
        public static string Serialize(SessionSaveData data)
        {
            var dto = SessionSaveDto.FromData(data);
            return JsonUtility.ToJson(dto);
        }

        public static SessionSaveData DeserializeOrDefault(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new SessionSaveData(ProductFlowSnapshot.Menu());
            }

            SessionSaveDto dto;
            try
            {
                dto = JsonUtility.FromJson<SessionSaveDto>(json);
            }
            catch
            {
                return new SessionSaveData(ProductFlowSnapshot.Menu());
            }

            return dto != null ? dto.ToData() : new SessionSaveData(ProductFlowSnapshot.Menu());
        }

        [System.Serializable]
        private sealed class SessionSaveDto
        {
            public int ContractVersion;
            public string LastKnownState;
            public int LastBattleSeed;
            public bool HasBattleToResume;

            public static SessionSaveDto FromData(SessionSaveData data)
            {
                var snapshot = data?.FlowSnapshot ?? ProductFlowSnapshot.Menu();
                return new SessionSaveDto
                {
                    ContractVersion = data?.ContractVersion ?? SessionSaveData.CurrentContractVersion,
                    LastKnownState = snapshot.LastKnownState,
                    LastBattleSeed = snapshot.LastBattleSeed,
                    HasBattleToResume = snapshot.HasBattleToResume
                };
            }

            public SessionSaveData ToData()
            {
                var snapshot = HasBattleToResume
                    ? ProductFlowSnapshot.Battle(LastBattleSeed)
                    : ProductFlowSnapshot.Menu();

                return new SessionSaveData(snapshot, ContractVersion);
            }
        }
    }
}
