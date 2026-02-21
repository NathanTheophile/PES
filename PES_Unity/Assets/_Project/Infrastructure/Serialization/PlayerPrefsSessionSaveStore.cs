using UnityEngine;

namespace PES.Infrastructure.Serialization
{
    /// <summary>
    /// Store persistant local basé sur PlayerPrefs (prototype vertical-slice).
    /// </summary>
    public sealed class PlayerPrefsSessionSaveStore : ISessionSaveStore
    {
        private readonly string _storageKey;

        public PlayerPrefsSessionSaveStore(string storageKey)
        {
            _storageKey = string.IsNullOrWhiteSpace(storageKey) ? "PES.SessionSave" : storageKey;
        }

        public bool TryLoad(out SessionSaveData data)
        {
            var raw = PlayerPrefs.GetString(_storageKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                data = null;
                return false;
            }

            data = SessionSaveSerializer.DeserializeOrDefault(raw);
            return true;
        }

        public void Save(SessionSaveData data)
        {
            var raw = SessionSaveSerializer.Serialize(data);
            PlayerPrefs.SetString(_storageKey, raw);
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(_storageKey);
            PlayerPrefs.Save();
        }
    }
}
