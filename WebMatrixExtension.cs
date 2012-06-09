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
    [Export(typeof(Extension))]
    public class GitWebMatrix : Extension
    {
        private IWebMatrixHost webMatrixHost;

        public GitWebMatrix()
            : base(Resources.Name)
        {
            GitBash.GitExePath = new string[] {
			    @"C:\Program Files\Git\bin\sh.exe",
				@"C:\Program Files (x86)\Git\bin\sh.exe",
			}
            .Where(p => File.Exists(p))
            .FirstOrDefault();

            this.gitBashCommand = new DelegateCommand((object param) => GitBash.Exists, delegate(object param)
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
            list.Add(new RibbonButton("Git Bash", this.gitBashCommand, null, Resources.git_16, Resources.git_32));
            var button = new RibbonSplitButton("Git", this.gitBashCommand, null, list, Resources.git_16, Resources.git_32);
            initData.RibbonItems.Add(button);
        }

        void host_WebSiteChanged(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}
