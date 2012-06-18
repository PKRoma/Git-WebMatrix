using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using Microsoft.WebMatrix.Extensibility;
using GitScc;

namespace GitWebMatrix
{
	[Export(typeof(Extension))]
	public class GitWebMatrix : Extension
	{
		FileStatusTracker tracker = new FileStatusTracker();

		[Import(typeof(ISiteFileWatcherService))]
		private ISiteFileWatcherService siteFileWatcherService { get; set; }

		private IWebMatrixHost webMatrixHost;
		private readonly DelegateCommand gitBashCommand;
		private readonly DelegateCommand gitInitCommand;
		private readonly DelegateCommand gitCloneCommand;
		private readonly DelegateCommand gitCommitCommand;
		private readonly DelegateCommand gitLogCommand;
		private readonly DelegateCommand gitRefreshCommand;
		private readonly DelegateCommand gitCompareFileCommand;
		private readonly DelegateCommand gitUndoFileChangeCommand;

		Dictionary<string, ISiteFileSystemItem> siteFiles = new Dictionary<string, ISiteFileSystemItem>();

		private DispatcherTimer timer;

		public GitWebMatrix()
			: base(Resources.Name)
		{
			GitBash.GitExePath = GitSccOptions.Current.GitBashPath;
			GitBash.UseUTF8FileNames = GitSccOptions.Current.UseUTF8FileNames;

			if (!GitBash.Exists)
			{

				GitBash.GitExePath = new string[] {
					@"C:\Program Files\Git\bin\sh.exe",
					@"C:\Program Files (x86)\Git\bin\sh.exe",
				}
				.Where(p => File.Exists(p))
				.FirstOrDefault();
			}

			#region commands
			
			this.gitInitCommand = new DelegateCommand((object param) => !tracker.HasGitRepository, delegate(object param)
			{
				if (this.webMatrixHost != null && this.webMatrixHost.WebSite != null)
					GitFileStatusTracker.Init(this.webMatrixHost.WebSite.Path);
			});

			this.gitCloneCommand = new DelegateCommand((object param) => !tracker.HasGitRepository, delegate(object param)
			{

			});

			this.gitCommitCommand = new DelegateCommand((object param) => tracker.HasGitRepository, delegate(object param)
			{
				ShowDragonTool("-c");
			});

			this.gitLogCommand = new DelegateCommand((object param) => tracker.HasGitRepository, delegate(object param)
			{
				ShowDragonTool();
			});

			this.gitBashCommand = new DelegateCommand((object param) => GitBash.Exists, delegate(object param)
			{
				GitBash.OpenGitBash(webMatrixHost.WebSite.Path);
			});

			this.gitRefreshCommand = new DelegateCommand((object param) => true, delegate(object param)
			{
				Refresh();
			});

			this.gitCompareFileCommand = new DelegateCommand((object param) => tracker.HasGitRepository, delegate(object param)
			{

			});

			this.gitUndoFileChangeCommand = new DelegateCommand((object param) => tracker.HasGitRepository, delegate(object param)
			{
				UndoFileChanges(param as string);
			});

			#endregion
			
			timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromMilliseconds(500);
			timer.Tick += (o, _) =>
			{
				timer.Stop();
				Refresh();
			};
		}

		protected override void Initialize(IWebMatrixHost host, ExtensionInitData initData)
		{
			this.webMatrixHost = host;
			this.webMatrixHost.WebSiteChanged += new EventHandler<EventArgs>(host_WebSiteChanged);
			this.webMatrixHost.TreeItemCreated += new EventHandler<TreeItemEventArgs>(webMatrixHost_TreeItemCreated);
			this.webMatrixHost.TreeItemRemoved += new EventHandler<TreeItemEventArgs>(webMatrixHost_TreeItemRemoved);
			this.webMatrixHost.ContextMenuOpening += new EventHandler<ContextMenuOpeningEventArgs>(webMatrixHost_ContextMenuOpening);

			var list = new List<RibbonButton>{
				new RibbonButton("Initialize", this.gitInitCommand, null, Resources.git_init, Resources.git_32),
				//new RibbonButton("Clone", this.gitInitCommand, null, Resources.git_init, Resources.git_32),
				new RibbonButton("Commit Changes", this.gitCommitCommand, null, Resources.git_16, Resources.git_32),
				new RibbonButton("View Log/History", this.gitLogCommand, null, Resources.git_16, Resources.git_32),
				new RibbonButton("Refresh", this.gitRefreshCommand, null, Resources.git_16, Resources.git_32),
				new RibbonButton("Run Git Bash", this.gitBashCommand, null, Resources.git_16, Resources.git_32),
			};
			var button = new RibbonSplitButton("Git", this.gitBashCommand, null, list, Resources.git_16, Resources.git_32);
			initData.RibbonItems.Add(button);

		}

		#region refresh related

		private void Refresh()
		{
			tracker.Refresh();
			RefreshFileStatus();
		}

		void webMatrixHost_TreeItemRemoved(object sender, TreeItemEventArgs e)
		{
			GetSiteFileSystemItem(e, (item) => siteFiles.Remove(item.Path));
		}

		void webMatrixHost_TreeItemCreated(object sender, TreeItemEventArgs e)
		{
			GetSiteFileSystemItem(e, (item) => {
				if (!siteFiles.ContainsKey(item.Path) && !item.Path.Contains("\\.git\\"))
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
				tracker = new FileStatusTracker(this.webMatrixHost.WebSite.Path);
				this.siteFileWatcherService.RegisterForSiteNotifications(WatcherChangeTypes.All,
					new FileSystemEventHandler(this.FileChanged), new RenamedEventHandler(FileRenamed));
			}
			else
			{
				tracker.Close();
				this.siteFileWatcherService.DeregisterForSiteNotifications(WatcherChangeTypes.All,
					new FileSystemEventHandler(this.FileChanged), new RenamedEventHandler(FileRenamed));
			}
		}

		protected void FileChanged(object source, FileSystemEventArgs e)
		{
			if ((e.Name.Equals(".git") && e.ChangeType == WatcherChangeTypes.Deleted) ||
				(e.Name.Equals(".git\\objects") && e.ChangeType == WatcherChangeTypes.Changed) )
			{
				timer.Start();
			}
			else
			{
				Action act = () =>
				{
					tracker.RefreshFileStatus(e.FullPath);
					SetFileStatusIcon(e.FullPath);
				};
				Dispatcher.CurrentDispatcher.Invoke(act, DispatcherPriority.Normal);
			}
		}

		protected void FileRenamed(object source, RenamedEventArgs e)
		{
		}

		private void SetFileStatusIcon(string path)
		{
			if (!siteFiles.ContainsKey(path)) return;

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

		private void RefreshFileStatus()
		{
			siteFiles.Keys.ToList().ForEach(path =>
			{
				Action act = () => SetFileStatusIcon(path);
				Dispatcher.CurrentDispatcher.Invoke(act, DispatcherPriority.Normal);
			});
		}

		#endregion

		#region Deragon tool

		private void ShowDragonTool(string options = "")
		{
			if (this.webMatrixHost == null || string.IsNullOrEmpty(this.webMatrixHost.WebSite.Path)) return;

			var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			path = Path.Combine(path, "GitUI.dll");
			var tmpPath = Path.Combine(Path.GetTempPath(), "Dragon.exe");

			var needCopy = !File.Exists(tmpPath);
			if (!needCopy)
			{
				var date1 = File.GetLastWriteTimeUtc(path);
				var date2 = File.GetLastWriteTimeUtc(tmpPath);
				needCopy = (date1 > date2);
			}

			if (needCopy)
			{
				try
				{
					File.Copy(path, tmpPath, true);
				}
				catch // try copy file silently
				{
				}
			}

			if (File.Exists(tmpPath))
			{
				Process.Start(tmpPath, "\"" + this.webMatrixHost.WebSite.Path + "\" " + options);
			}
		}

		#endregion

		#region context menu related

		void webMatrixHost_ContextMenuOpening(object sender, ContextMenuOpeningEventArgs e)
		{
			ISiteItem siteItem = e.Items.FirstOrDefault<ISiteItem>();
			if (siteItem != null)
			{
				ISiteFile siteFile = siteItem as ISiteFile;

				if (tracker.HasGitRepository)
				{
					if (siteFile != null)
					{
						var status = tracker.GetFileStatus(siteFile.Path);

						if (status == GitFileStatus.Modified || status == GitFileStatus.Staged)
						{
							//e.AddMenuItem(new ContextMenuItem("Compare ...", Utility.ConvertBitmapToImageSource(Resources.git_16), this.gitCompareFileCommand, siteFile.Path));
							e.AddMenuItem(new ContextMenuItem("Undo File Changes ", Utility.ConvertBitmapToImageSource(Resources.git_16), this.gitUndoFileChangeCommand, siteFile.Path));
						}
					}

					//e.AddMenuItem(new ContextMenuItem("Commit Changes", Utility.ConvertBitmapToImageSource(Resources.git_16), this.gitCommitCommand, null));
					//e.AddMenuItem(new ContextMenuItem("View Log/History", Utility.ConvertBitmapToImageSource(Resources.git_16), this.gitLogCommand, null));
				}
				else
				{
					e.AddMenuItem(new ContextMenuItem("Create Git Repository", Utility.ConvertBitmapToImageSource(Resources.git_16), this.gitInitCommand, null));
				}
			}
		}

		private void UndoFileChanges(string fileName)
		{
			GitFileStatus status = tracker.GetFileStatus(fileName);
			if (status == GitFileStatus.Modified || status == GitFileStatus.Staged ||
				status == GitFileStatus.Deleted || status == GitFileStatus.Removed)
			{
				if (MessageBox.Show("Are you sure you want to undo changes for " + Path.GetFileName(fileName) +
					" and restore a version from the last commit? ",
					"Undo Changes", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
				{
					tracker.CheckOutFile(fileName);
				}
			}
		}

		#endregion
	}
}
