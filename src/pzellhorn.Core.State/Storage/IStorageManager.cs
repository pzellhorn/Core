namespace pzellhorn.Core.State.Storage
{
    public interface IStorageManager
    {
        public Task<Stream> Get(string path, CancellationToken cancellationToken = default);
        public Task<bool> Upsert(string path, Stream content, CancellationToken cancellationToken = default);
        public Task<bool> Delete(string path, CancellationToken cancellationToken = default);

        public Task<bool> Exists(string path, CancellationToken cancellationToken = default);
        public Task<bool> Copy(string source, string destination, CancellationToken cancellationToken = default);
        public Task<bool> Move(string source, string destination, CancellationToken cancellationToken = default);
    }
}