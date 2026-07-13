namespace pzellhorn.Core.State.Storage.Disk
{ 
    public class DiskStorage(string baseStoragePath) : IStorageManager
    {
        public Task<Stream> Get(string path, CancellationToken cancellationToken = default)
        {
            string full = ResolvePath(path);

            if (!File.Exists(full))
                throw new FileNotFoundException($"Object not found: {path}", full);
             
            Stream stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 81920, useAsync: true);

            return Task.FromResult(stream);
        }

        public async Task<bool> Upsert(string path, Stream content, CancellationToken cancellationToken = default)
        {
            string full = ResolvePath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);

            if (content.CanSeek && content.Position != 0)
                content.Position = 0;

            await using FileStream file = new(full, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);

            await content.CopyToAsync(file, cancellationToken);
            return true;
        }

        public Task<bool> Delete(string path, CancellationToken cancellationToken = default)
        {
            string full = ResolvePath(path);

            if (!File.Exists(full))
                return Task.FromResult(false);

            File.Delete(full);
            return Task.FromResult(true);
        }

        public Task<int> DeleteByPrefix(string prefix, CancellationToken cancellationToken = default)
        {
            string full = ResolvePath(prefix);
            string directory = Path.GetDirectoryName(full)!;

            if (!Directory.Exists(directory))
                return Task.FromResult(0);

            string leaf = Path.GetFileName(full);
            string[] matches = Directory.GetFiles(directory, $"{leaf}*", SearchOption.AllDirectories);
            foreach (string match in matches)
                File.Delete(match);

            return Task.FromResult(matches.Length);
        }

        public Task<bool> Exists(string path, CancellationToken cancellationToken = default)
            => Task.FromResult(File.Exists(ResolvePath(path)));

        public Task<bool> Copy(string source, string destination, CancellationToken cancellationToken = default)
        {
            string src = ResolvePath(source);
            string dst = ResolvePath(destination);

            if (!File.Exists(src))
                return Task.FromResult(false);

            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            File.Copy(src, dst, overwrite: true);
            return Task.FromResult(true);
        }

        public Task<bool> Move(string source, string destination, CancellationToken cancellationToken = default)
        {
            string src = ResolvePath(source);
            string dst = ResolvePath(destination);

            if (!File.Exists(src))
                return Task.FromResult(false);

            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            File.Move(src, dst, overwrite: true);
            return Task.FromResult(true);
        }
         
        private string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must be provided.", nameof(path));

            string root = Path.GetFullPath(baseStoragePath);
            string full = Path.GetFullPath(Path.Combine(root, path.TrimStart('/', '\\')));

            string rootWithSep = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;

            if (!string.Equals(full, root, StringComparison.Ordinal) &&
                !full.StartsWith(rootWithSep, StringComparison.Ordinal))
                throw new UnauthorizedAccessException($"Resolved path escapes storage root: {path}");

            return full;
        }
    }
}
