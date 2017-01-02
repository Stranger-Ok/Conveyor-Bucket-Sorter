using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentSorter
{
    public class FileBucket: IComparable<FileBucket>
    {
        public string FilePath { get; set; }
        public string PrefixString { get; set; }

        public int CompareTo(FileBucket other)
        {
            return this.PrefixString.CompareTo(other.PrefixString);
        }
    }

    public class MemoryBucket : IEquatable<MemoryBucket>
    {
        private Guid Id;
        private int IdHash;
        public string PrefixString { get; set; }
        public string Word { get; set; }

        public MemoryBucket(string prefix, string word)
        {
            Id = Guid.NewGuid();
            IdHash = Id.GetHashCode();
            PrefixString = prefix;
            Word = word;
        }

        public bool Equals(MemoryBucket other)
        {
            return other != null && Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MemoryBucket);
        }

        public override int GetHashCode()
        {
            return IdHash;
        }
    }
}
