using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using GitScc;
using Microsoft.WebMatrix.Extensibility;

namespace GitWebMatrix
{
    /// <summary>
    /// A sample WebMatrix extension.
    /// </summary>
    [Export(typeof(IExtension))]
    public class GitWebMatrix : ExtensionBase
    {
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
            : base("GitWebMatrix", "1.0")
        {
            // Add a simple button to the Ribbon
            RibbonItemsCollection.Add(
                new RibbonGroup(
                    "Git",
                    new IRibbonItem[]
                    {
                        new RibbonButton(
                            "Git Bash",
                            new DelegateCommand(HandleRibbonButtonInvoke),
                            null,
                            _starImageSmall,
                            _starImageLarge)
                    }));


            GitBash.GitExePath = new string[] {
			    @"C:\Program Files\Git\bin\sh.exe",
				@"C:\Program Files (x86)\Git\bin\sh.exe",
			}
            .Where(p => File.Exists(p))
            .FirstOrDefault();

        }

        /// <summary>
        /// Called when the Ribbon button is invoked.
        /// </summary>
        /// <param name="parameter">Unused.</param>
        private void HandleRibbonButtonInvoke(object parameter)
        {
            //if (WebMatrixHost.ShowDialog(this.Name, "Open a browser window for the site?").GetValueOrDefault())
            //{
            //    Process.Start(WebMatrixHost.WebSite.Uri.AbsoluteUri);
            //}

            GitBash.OpenGitBash(WebMatrixHost.WebSite.Path);
        }
    }
}
