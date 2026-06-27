namespace pzellhorn.Core.State.Storage
{ 
    public interface ISignedUrlProvider
    {
        public Task<Uri> GetDownloadUrl(string path, TimeSpan ttl, CancellationToken cancellationToken = default);
        public Task<Uri> GetUploadUrl(string path, TimeSpan ttl, CancellationToken cancellationToken = default);
    }
}
