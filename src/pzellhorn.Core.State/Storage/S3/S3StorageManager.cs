using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace pzellhorn.Core.State.Storage.S3
{
    public class S3StorageManager : IStorageManager, ISignedUrlProvider, IDisposable
    {
        private readonly AmazonS3Client _client;
        private readonly string _bucketName;
        private readonly bool _useHttp;

        public S3StorageManager(IOptions<S3Options> options)
        {
            S3Options config = options.Value;
            _bucketName = config.BucketName;
            _useHttp = config.ServiceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);

            AmazonS3Config clientConfig = new()
            {
                ServiceURL = config.ServiceUrl,
                ForcePathStyle = config.ForcePathStyle,
                AuthenticationRegion = config.Region,
                UseHttp = _useHttp,
            };

            _client = new AmazonS3Client(config.AccessKey, config.SecretKey, clientConfig);
        }

        public async Task<Stream> Get(string path, CancellationToken cancellationToken = default)
        {
            try
            {
                GetObjectResponse response = await _client.GetObjectAsync(_bucketName, GetKey(path), cancellationToken);
                return response.ResponseStream;
            }
            catch (AmazonS3Exception ex)
            {
                throw new FileNotFoundException($"Object not found: {_bucketName}/{GetKey(path)}", ex);
            }
        }

        public async Task<bool> Upsert(string path, Stream content, CancellationToken cancellationToken = default)
        {
            if (content.CanSeek && content.Position != 0)
                content.Position = 0;

            PutObjectRequest request = new()
            {
                BucketName = _bucketName,
                Key = GetKey(path),
                InputStream = content,
                ContentType = "application/octet-stream",
                AutoCloseStream = false,
            };

            await _client.PutObjectAsync(request, cancellationToken);
            return true;
        }

        public async Task<bool> Delete(string path, CancellationToken cancellationToken = default)
        {
            await _client.DeleteObjectAsync(_bucketName, GetKey(path), cancellationToken);
            return true;
        }

        public async Task<bool> Exists(string path, CancellationToken cancellationToken = default)
        {
            try
            {
                await _client.GetObjectMetadataAsync(_bucketName, GetKey(path), cancellationToken);
                return true;
            }
            catch (AmazonS3Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> Copy(string source, string destination, CancellationToken cancellationToken = default)
        {
            CopyObjectRequest request = new()
            {
                SourceBucket = _bucketName,
                SourceKey = GetKey(source),
                DestinationBucket = _bucketName,
                DestinationKey = GetKey(destination),
            };

            await _client.CopyObjectAsync(request, cancellationToken);
            return true;
        }

        public async Task<bool> Move(string source, string destination, CancellationToken cancellationToken = default)
        {
            if (!await Copy(source, destination, cancellationToken))
                return false;

            return await Delete(source, cancellationToken);
        }

        public Task<Uri> GetDownloadUrl(string path, TimeSpan ttl, CancellationToken cancellationToken = default) => PreSign(path, HttpVerb.GET, ttl);

        public Task<Uri> GetUploadUrl(string path, TimeSpan ttl, CancellationToken cancellationToken = default) => PreSign(path, HttpVerb.PUT, ttl);

        private Task<Uri> PreSign(string path, HttpVerb verb, TimeSpan ttl)
        {
            GetPreSignedUrlRequest request = new()
            {
                BucketName = _bucketName,
                Key = GetKey(path),
                Verb = verb,
                Expires = DateTime.UtcNow.Add(ttl),
                Protocol = _useHttp ? Protocol.HTTP : Protocol.HTTPS,
            };

            string url = _client.GetPreSignedURL(request);
            return Task.FromResult(new Uri(url));
        }

        private static string GetKey(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must be provided.", nameof(path));

            return path.TrimStart('/');
        }

        public void Dispose() => _client.Dispose();
    }
}
