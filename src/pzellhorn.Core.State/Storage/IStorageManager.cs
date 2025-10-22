using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pzellhorn.Core.State.Storage
{
    public interface IStorageManager
    {
        public Stream Get(string path, CancellationToken cancellationToken = default);
        public bool Upsert(string path, Stream content, CancellationToken cancellationToken = default);
        public bool Delete(string path, CancellationToken cancellationToken = default);

        public bool Exists(string path, CancellationToken cancellationToken = default);
        public bool Copy(string path, CancellationToken cancellationToken = default);
        public bool Move(string path, CancellationToken cancellationToken = default);
    }
}