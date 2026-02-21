namespace PES.Infrastructure.Serialization
{
    public interface ISessionSaveStore
    {
        bool TryLoad(out SessionSaveData data);

        void Save(SessionSaveData data);

        void Clear();
    }

    /// <summary>
    /// Store in-memory pour tests et bootstrap local.
    /// </summary>
    public sealed class InMemorySessionSaveStore : ISessionSaveStore
    {
        private string _rawJson;

        public bool TryLoad(out SessionSaveData data)
        {
            if (string.IsNullOrWhiteSpace(_rawJson))
            {
                data = null;
                return false;
            }

            data = SessionSaveSerializer.DeserializeOrDefault(_rawJson);
            return true;
        }

        public void Save(SessionSaveData data)
        {
            _rawJson = SessionSaveSerializer.Serialize(data);
        }

        public void Clear()
        {
            _rawJson = null;
        }
    }
}
