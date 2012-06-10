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
        DispatcherTimer timer = new DispatcherTimer();
        FileSystemWatcher fileSystemWatcher;
        public event EventHandler OnRefresh = delegate { };

        public void Open(string directory)
        {
            tracker = new GitFileStatusTracker(directory);
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();

            fileSystemWatcher = new FileSystemWatcher(directory);
            fileSystemWatcher.IncludeSubdirectories = true;
            fileSystemWatcher.Changed += new FileSystemEventHandler(fileSystemWatcher_Changed);
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void Close()
        {
            tracker = null;
            timer.Stop();
        }

        public bool HasGitRepository
        {
            get { return (tracker != null && tracker.HasGitRepository); }
        }

        internal void Reload()
        {
            if(HasGitRepository) tracker.Refresh();
        }

        internal GitFileStatus GetFileStatus(string path)
        {
            return HasGitRepository ? tracker.GetFileStatus(path) : GitFileStatus.NotControlled;
        }

        #region Refresh

        internal DateTime lastTimeRefresh = DateTime.Now.AddDays(-1);
        internal DateTime nextTimeRefresh = DateTime.Now.AddDays(-1);

        private void fileSystemWatcher_Changed(object source, FileSystemEventArgs e)
        {
            if (!NoRefresh && tracker != null)
            {
                double delta = DateTime.Now.Subtract(lastTimeRefresh).TotalMilliseconds;
                if (delta > 500)
                {
                    NeedRefresh = true;
                    lastTimeRefresh = DateTime.Now;
                    nextTimeRefresh = DateTime.Now;
                }
            }
        }

        private bool NoRefresh;
        private bool NeedRefresh;

        private void timer_Tick(Object sender, EventArgs args)
        {
            if (NeedRefresh && !NoRefresh)
            {
                double delta = DateTime.Now.Subtract(nextTimeRefresh).TotalMilliseconds;
                if (delta > 200)
                {
                    System.Diagnostics.Debug.WriteLine("$$$$ Refresh");
                    DisableAutoRefresh();
                    tracker.Refresh();
                    OnRefresh(this, null);
                    EnableAutoRefresh();
                }
            }
        }

        internal void EnableAutoRefresh()
        {
            timer.Start();
            NoRefresh = false;
            NeedRefresh = false;
            lastTimeRefresh = DateTime.Now;
        }

        internal void DisableAutoRefresh()
        {
            timer.Stop();
            NoRefresh = true;
            NeedRefresh = false;
            lastTimeRefresh = DateTime.Now.AddSeconds(2);
        }

        #endregion

    }
}
