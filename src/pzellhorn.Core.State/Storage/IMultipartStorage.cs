namespace pzellhorn.Core.State.Storage
{ 
    public interface IMultipartStorage
    { 
        int MinChunkSize { get; }

        Task<string> BeginMultipart(string path, CancellationToken cancellationToken = default);
        Task UploadPart(string path, string multipartUploadId, int partNumber, Stream content, CancellationToken cancellationToken = default);
        Task CompleteMultipart(string path, string multipartUploadId, CancellationToken cancellationToken = default);
        Task AbortMultipart(string path, string multipartUploadId, CancellationToken cancellationToken = default);
    }
}
