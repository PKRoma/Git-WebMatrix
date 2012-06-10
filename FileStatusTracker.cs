using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using System.Windows.Threading;
using GitScc;

namespace GitWebMatrix
{
    class FileStatusTracker
    {
        GitFileStatusTracker tracker;
        Dictionary<string, GitFileStatus> cache;

        public FileStatusTracker(string directory)
        {
            tracker = new GitFileStatusTracker(directory);
            cache = new Dictionary<string, GitFileStatus>();
        }

        public void Close()
        {
            tracker.Dispose();
            tracker = null;
        }

        public bool HasGitRepository
        {
            get { return (tracker != null && tracker.HasGitRepository); }
        }

        internal void Refresh()
        {
            if (tracker != null) tracker.Refresh();
            cache.Clear();
        }

        internal GitFileStatus GetFileStatus(string path)
        {
            if(!HasGitRepository) return GitFileStatus.NotControlled;

            if (!cache.ContainsKey(path))
            {
                cache[path] = tracker.GetFileStatusNoCacheOld(path);
            }
            return cache[path];
        }

        internal void RefreshFileStatus(string path)
        {
            if (cache.ContainsKey(path))
            {
                cache[path] = tracker.GetFileStatusNoCacheOld(path);
            }
        }

    }
}
