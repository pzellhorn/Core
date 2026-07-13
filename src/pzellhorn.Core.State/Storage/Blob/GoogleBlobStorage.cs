using System.Net;
using Google;
using Google.Cloud.Storage.V1;

namespace pzellhorn.Core.State.Storage.Blob
{
    public class GoogleBlobStorage(StorageClient storageClient) : IStorageManager
    {
        private readonly string _bucketName = Environment.GetEnvironmentVariable("GCS_BUCKET") ?? throw new ArgumentNullException($"Can't find GCP bucket name in .env");
         
        public async Task<Stream> Get(string path, CancellationToken cancellationToken = default)
        {
            (string bucket, string objectName) = GetBucketValuesFromPath(path);
            MemoryStream stream = new();
            try
            {
                await storageClient.DownloadObjectAsync(bucket: bucket, objectName: objectName, destination: stream, cancellationToken: cancellationToken);
                stream.Position = 0;
                return stream;
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                stream.Dispose();
                throw new FileNotFoundException($"Object not found: {bucket}/{objectName}", ex);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        public async Task<bool> Upsert(string path, Stream contentToUpload, CancellationToken cancellationToken = default)
        {
            (string bucket, string objectName) = GetBucketValuesFromPath(path);

            if (contentToUpload.CanSeek && contentToUpload.Position != 0) contentToUpload.Position = 0;

            Google.Apis.Storage.v1.Data.Object? result = await storageClient.UploadObjectAsync(bucket: bucket, objectName: objectName, contentType: "application/octet-stream", source: contentToUpload, cancellationToken: cancellationToken);
            if (result != null) return true;
            else return false;
        }

        public async Task<bool> Delete(string path, CancellationToken cancellationToken = default)
        {
            (string bucket, string objectName) = GetBucketValuesFromPath(path);
            try
            {
                await storageClient.DeleteObjectAsync(bucket: bucket, objectName: objectName, cancellationToken: cancellationToken); 
                return true;
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
            catch 
            {
                return false; 
            }
        }

        public async Task<int> DeleteByPrefix(string prefix, CancellationToken cancellationToken = default)
        {
            (string bucket, string objectPrefix) = GetBucketValuesFromPath(prefix);
            int deleted = 0;

            await foreach (Google.Apis.Storage.v1.Data.Object storageObject in storageClient.ListObjectsAsync(bucket, objectPrefix).WithCancellation(cancellationToken))
            {
                try
                {
                    await storageClient.DeleteObjectAsync(bucket, storageObject.Name, cancellationToken: cancellationToken);
                    deleted++;
                }
                catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
                {
                }
            }

            return deleted;
        }

        public async Task<bool> Exists(string path, CancellationToken cancellationToken = default)
        {
            (string bucket, string objectName) = GetBucketValuesFromPath(path);
            try
            {
                Google.Apis.Storage.v1.Data.Object? storageObject = await storageClient.GetObjectAsync(bucket: bucket, objectName: objectName, cancellationToken: cancellationToken);
                if (storageObject != null) return true;
                else return false;
            }
            catch (GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                return false;
            } 
            catch
            {
                return false;
            }
        }

        public async Task<bool> Copy(string source, string destination, CancellationToken cancellationToken = default)
        {
            (string srcBucket, string srcObjName) = GetBucketValuesFromPath(source);
            (string dstBucket, string dstObjName) = GetBucketValuesFromPath(destination);

            Google.Apis.Storage.v1.Data.Object? copiedObject = await storageClient.CopyObjectAsync(sourceBucket: srcBucket, sourceObjectName: srcObjName, destinationBucket: dstBucket, destinationObjectName: dstObjName, cancellationToken: cancellationToken);
            if (copiedObject != null) return true; 
            else return false;
        }

        public async Task<bool> Move(string source, string destination, CancellationToken cancellationToken = default)
        {
            (string srcBucket, string srcObjName) = GetBucketValuesFromPath(source);
            (string dstBucket, string dstObjName) = GetBucketValuesFromPath(destination);
             
            if (string.Equals(srcBucket, dstBucket, StringComparison.Ordinal))
            {
                try
                {
                    Google.Apis.Storage.v1.Data.Object moved = await storageClient.MoveObjectAsync(sourceBucket: srcBucket, sourceObjectName: srcObjName, destinationObjectName: dstObjName, cancellationToken: cancellationToken);
                    if (moved != null) return true;
                }
                catch {}    //failed to move using MoveObjectAsync, probably due to bucket settings. Fallback to copy + delete
            }

            Google.Apis.Storage.v1.Data.Object? copied = await storageClient.CopyObjectAsync(sourceBucket: srcBucket, sourceObjectName: srcObjName, destinationBucket: dstBucket, destinationObjectName: dstObjName, cancellationToken: cancellationToken);

            if (copied == null) return false;

            await storageClient.DeleteObjectAsync(bucket: srcBucket, objectName: srcObjName, cancellationToken: cancellationToken);
            return true;
        }

        private (string bucket, string objectName) GetBucketValuesFromPath(string path)
        {
            if (path.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
            {
                string rest = path.Substring(5);
                int slash = rest.IndexOf('/');

                if (slash < 0) throw new ArgumentException("gs:// path must include an object name", nameof(path));

                string bucket = rest.Substring(0, slash);
                string objectName = rest[(slash + 1)..];

                return (bucket, objectName.TrimStart('/'));
            }
            return (_bucketName, path.TrimStart('/'));
        }
    }
}
