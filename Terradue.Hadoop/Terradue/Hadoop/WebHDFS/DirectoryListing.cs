using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Terradue.Hadoop.WebHDFS
{
    // todo - make abstract
    public class DirectoryListing : Resource
    {
        IEnumerable<DirectoryEntry> directoryEntries;

        public DirectoryListing(JObject rootEntry)
        {
            directoryEntries = rootEntry.GetValue("FileStatuses").Values<JObject>("FileStatus").Select(fs => new DirectoryEntry(fs));
            Info = rootEntry;
        }

        public IEnumerable<DirectoryEntry> Entries { get { return directoryEntries; } }
        public IEnumerable<DirectoryEntry> Directories { get { return directoryEntries.Where(fs => fs.Type == "DIRECTORY"); } }
        public IEnumerable<DirectoryEntry> Files { get { return directoryEntries.Where(fs => fs.Type == "FILE"); } }
    }
}
