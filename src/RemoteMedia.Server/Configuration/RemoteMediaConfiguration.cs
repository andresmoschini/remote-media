using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RemoteMedia.Server.Configuration
{
    public class RemoteMediaConfiguration
    {
        public MediaFilesConfiguration mediaFiles { get; set; }
    }

    public class MediaFilesConfiguration
    {
        public MediaFilesCollectionConfiguration[] collections { get; set; }
        public string[] videoFileExtensions { get; set; }
        public string[] audioFileExtensions { get; set; }
        public string[] imageFileExtensions { get; set; }
    }

    public class MediaFilesCollectionConfiguration
    {
        public string name { get; set; }
        public string path { get; set; } // Try using Path
        public MediaType[] mediaTypes { get; set; }
    }

    public enum MediaType
    {
        video,
        image,
        audio
    }
}
