using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace pzellhorn.Core.State.Storage.S3
{
    public class S3StorageManager : IStorageManager, ISignedUrlProvider, IMultipartStorage, IDisposable
    {
        public int MinChunkSize => 5 * 1024 * 1024;

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

        public async Task<int> DeleteByPrefix(string prefix, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Prefix must be provided.", nameof(prefix));

            string normalizedPrefix = prefix.TrimStart('/');
            int deleted = 0;
            string? continuationToken = null;

            do
            {
                ListObjectsV2Response listing = await _client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = normalizedPrefix,
                    ContinuationToken = continuationToken,
                }, cancellationToken);

                if (listing.S3Objects is { Count: > 0 } objects)
                {
                    DeleteObjectsResponse deleteResponse = await _client.DeleteObjectsAsync(new DeleteObjectsRequest
                    {
                        BucketName = _bucketName,
                        Objects = objects.Select(o => new KeyVersion { Key = o.Key }).ToList(),
                    }, cancellationToken);

                    deleted += deleteResponse.DeletedObjects?.Count ?? 0;
                }

                continuationToken = listing.IsTruncated == true ? listing.NextContinuationToken : null;
            }
            while (continuationToken is not null);

            return deleted;
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

        public async Task<string> BeginMultipart(string path, CancellationToken cancellationToken = default)
        {
            InitiateMultipartUploadResponse response = await _client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
            {
                BucketName = _bucketName,
                Key = GetKey(path),
                ContentType = "application/octet-stream",
            }, cancellationToken);

            return response.UploadId;
        }

        public async Task UploadPart(string path, string multipartUploadId, int partNumber, Stream content, CancellationToken cancellationToken = default)
        {
            await _client.UploadPartAsync(new UploadPartRequest
            {
                BucketName = _bucketName,
                Key = GetKey(path),
                UploadId = multipartUploadId,
                PartNumber = partNumber,
                InputStream = content,
            }, cancellationToken);
        }

        public async Task CompleteMultipart(string path, string multipartUploadId, CancellationToken cancellationToken = default)
        {
            ListPartsResponse parts = await _client.ListPartsAsync(new ListPartsRequest
            {
                BucketName = _bucketName,
                Key = GetKey(path),
                UploadId = multipartUploadId,
            }, cancellationToken);

            List<PartETag> etags = new();
            foreach (PartDetail part in parts.Parts.OrderBy(p => p.PartNumber))
                etags.Add(new PartETag(part.PartNumber.GetValueOrDefault(), part.ETag));

            await _client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
            {
                BucketName = _bucketName,
                Key = GetKey(path),
                UploadId = multipartUploadId,
                PartETags = etags,
            }, cancellationToken);
        }

        public async Task AbortMultipart(string path, string multipartUploadId, CancellationToken cancellationToken = default)
        {
            await _client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
            {
                BucketName = _bucketName,
                Key = GetKey(path),
                UploadId = multipartUploadId,
            }, cancellationToken);
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
