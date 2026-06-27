namespace pzellhorn.Core.State.Storage.S3
{
    public class S3Options
    { 
        public string ServiceUrl { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
         
        public bool ForcePathStyle { get; set; } = true;
         
        public string Region { get; set; } = "eu-central-2";
    }
}
