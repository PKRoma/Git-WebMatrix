using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.WebMatrix.Extensibility;
using GitScc;

namespace GitWebMatrix
{
	[Export(typeof(Extension))]
	public class GitWebMatrix : Extension
	{
		FileStatusTracker tracker;

		[Import(typeof(ISiteFileWatcherService))]
		private ISiteFileWatcherService SiteFileWatcherService { get; set; }

		private IWebMatrixHost webMatrixHost;
		private readonly DelegateCommand gitBashCommand;
		private readonly DelegateCommand gitInitCommand;
		private readonly DelegateCommand gitCommitCommand;
		private readonly DelegateCommand gitLogCommand;
		private readonly DelegateCommand gitRefreshCommand;

		Dictionary<string, ISiteFileSystemItem> siteFiles = new Dictionary<string, ISiteFileSystemItem>();

		public GitWebMatrix()
			: base(Resources.Name)
		{
			GitBash.GitExePath = new string[] {
				@"C:\Program Files\Git\bin\sh.exe",
				@"C:\Program Files (x86)\Git\bin\sh.exe",
			}
			.Where(p => File.Exists(p))
			.FirstOrDefault();

			tracker = new FileStatusTracker();
			tracker.OnRefresh += (o, e) => Refresh();

			this.gitInitCommand = new DelegateCommand((object param) => !tracker.HasGitRepository, delegate(object param)
			{
				
			});

			this.gitCommitCommand = new DelegateCommand((object param) => tracker.HasGitRepository, delegate(object param)
			{

			});
			this.gitLogCommand = new DelegateCommand((object param) => tracker.HasGitRepository, delegate(object param)
			{

			});

			this.gitBashCommand = new DelegateCommand((object param) => GitBash.Exists, delegate(object param)
			{
				GitBash.OpenGitBash(webMatrixHost.WebSite.Path);
			});

			this.gitRefreshCommand = new DelegateCommand((object param) => true, delegate(object param)
			{
                tracker.Reload();
				this.Refresh();
			});
		}

		protected override void Initialize(IWebMatrixHost host, ExtensionInitData initData)
		{
			this.webMatrixHost = host;
			this.webMatrixHost.WebSiteChanged += new EventHandler<EventArgs>(host_WebSiteChanged);
			this.webMatrixHost.TreeItemCreated += new EventHandler<TreeItemEventArgs>(webMatrixHost_TreeItemCreated);
			this.webMatrixHost.TreeItemRemoved += new EventHandler<TreeItemEventArgs>(webMatrixHost_TreeItemRemoved);

			var list = new List<RibbonButton>{
				new RibbonButton("Init Git Repository", this.gitInitCommand, null, Resources.git_init, Resources.git_32),
				//new RibbonButton("Commit Changes", this.gitCommitCommand, null, Resources.git_16, Resources.git_32),
				//new RibbonButton("View Log/History", this.gitLogCommand, null, Resources.git_16, Resources.git_32),
				new RibbonButton("Refresh", this.gitRefreshCommand, null, Resources.git_16, Resources.git_32),
				new RibbonButton("Run Git Bash", this.gitBashCommand, null, Resources.git_bash, Resources.git_32),
			};
			var button = new RibbonSplitButton("Git", this.gitBashCommand, null, list, Resources.git_16, Resources.git_32);
			initData.RibbonItems.Add(button);

		}

		void webMatrixHost_TreeItemRemoved(object sender, TreeItemEventArgs e)
		{
			GetSiteFileSystemItem(e, (item) => siteFiles.Remove(item.Path));
		}

		void webMatrixHost_TreeItemCreated(object sender, TreeItemEventArgs e)
		{
			GetSiteFileSystemItem(e, (item) => {
				if (!siteFiles.ContainsKey(item.Path))
				{
					siteFiles.Add(item.Path, item);
					Action act = () => SetFileStatusIcon(item.Path);
					Dispatcher.CurrentDispatcher.BeginInvoke(act, DispatcherPriority.Background);
				}
			});
		}

		private void GetSiteFileSystemItem(TreeItemEventArgs e, Action<ISiteFileSystemItem> action)
		{
			var id = e.HierarchyId;
			var item = this.webMatrixHost.GetSiteItem(id) as ISiteFileSystemItem;
			if (item != null && item is ISiteFile) action(item);
		}

		void host_WebSiteChanged(object sender, EventArgs e)
		{
			siteFiles.Clear();
			if (this.webMatrixHost != null && !string.IsNullOrEmpty(this.webMatrixHost.WebSite.Path))
			{
				tracker.Open(this.webMatrixHost.WebSite.Path);
			}
			else
			{
				tracker.Close();
			}
		}

		private void SetFileStatusIcon(string path)
		{
			var status = tracker.GetFileStatus(path);
			if (string.Compare(siteFiles[path].SecondaryIconToolTip, status.ToString()) != 0)
			{
				var bitmap = Resources.ResourceManager.GetObject("status_" + status.ToString().ToLower()) as System.Drawing.Bitmap;
                if (bitmap != null)
                {
                    var imageSource = Utility.ConvertBitmapToImageSource(bitmap);
                    imageSource.Freeze();
                    siteFiles[path].SecondaryIcon = imageSource;
                }
                else
                {
                    siteFiles[path].SecondaryIcon = null;
                }
				siteFiles[path].SecondaryIconToolTip = status.ToString();
			}
		}

		private void Refresh()
		{
			siteFiles.Keys.ToList().ForEach(path =>
			{
				Action act = () => SetFileStatusIcon(path);
				Dispatcher.CurrentDispatcher.Invoke(act, DispatcherPriority.Normal);
			});
		}
	}
}
