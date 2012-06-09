using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Microsoft.WebMatrix.Extensibility;
using GitScc;

namespace GitWebMatrix
{
    /// <summary>
    /// A sample WebMatrix extension.
    /// </summary>
    [Export(typeof(Extension))]
    public class GitWebMatrix : Extension
    {
        private IWebMatrixHost webMatrixHost;
        /// <summary>
        /// Stores a reference to the small star image.
        /// </summary>
        private readonly BitmapImage _starImageSmall = new BitmapImage(new Uri("pack://application:,,,/GitWebMatrix;component/git-16.png", UriKind.Absolute));

        /// <summary>
        /// Stores a reference to the large star image.
        /// </summary>
        private readonly BitmapImage _starImageLarge = new BitmapImage(new Uri("pack://application:,,,/GitWebMatrix;component/git-32.png", UriKind.Absolute));

        /// <summary>
        /// Initializes a new instance of the GitWebMatrix class.
        /// </summary>
        public GitWebMatrix()
            : base("Git")
        {
            GitBash.GitExePath = new string[] {
			    @"C:\Program Files\Git\bin\sh.exe",
				@"C:\Program Files (x86)\Git\bin\sh.exe",
			}
            .Where(p => File.Exists(p))
            .FirstOrDefault();

            this.gitBashCommand = new DelegateCommand((object param) => true, delegate(object param)
            {
                GitBash.OpenGitBash(webMatrixHost.WebSite.Path);
            });
        }

        private readonly DelegateCommand gitBashCommand;

        protected override void Initialize(IWebMatrixHost host, ExtensionInitData initData)
        {
            this.webMatrixHost = host;
            this.webMatrixHost.WebSiteChanged += new EventHandler<EventArgs>(host_WebSiteChanged);

            var list = new List<RibbonButton>();
            list.Add(new RibbonButton("Git Bash", this.gitBashCommand, null, _starImageSmall, _starImageLarge));
            var button = new RibbonSplitButton("Git", this.gitBashCommand, null, list, _starImageSmall, _starImageLarge); ;
            initData.RibbonItems.Add(button);
        }

        void host_WebSiteChanged(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}
