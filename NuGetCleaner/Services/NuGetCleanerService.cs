using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NuGetCleaner.Services
{
    public class NuGetCleanerService
    {
        private const string PropertyName = "System.DateAccessed";

        public StorageFolder NugetFolder { get; set; }

        public TimeSpan MaxTimeAlive { get; set; }

        public bool UseRecycleBinIfPossible { get; set; }

        public NuGetCleanerService()
        {
        }

        /// <summary>
        /// 根据所选设置进行清理
        /// </summary>
        /// <param name="progress">用于更新任务进度的对象</param>
        /// <exception cref="InvalidOperationException"><see cref="NugetFolder"/>为null</exception>
        /// <returns>可等待的任务</returns>
        public async Task CleanAsync(IProgress<double> progress)
        {
            var nugetFolder = NugetFolder ?? throw new InvalidOperationException("NuGetFolder is not set");
            var accessTimeThreshold = DateTimeOffset.Now - MaxTimeAlive;
            var deleteSettings = UseRecycleBinIfPossible ? StorageDeleteOption.Default : StorageDeleteOption.PermanentDelete;

            try
            {
                var packagesFolder = await NugetFolder.GetFolderAsync("packages");
                var packageFolders = await packagesFolder.GetFoldersAsync();
                var packageCount = packageFolders.Count;
                var packageIndex = 0;
                foreach (var folder in packageFolders)
                {
                    progress?.Report((double)packageIndex++ / packageCount);
                    try
                    {
                        var packageName = folder.Name;
                        var versionFolders = await folder.GetFoldersAsync();
                        foreach (var versionFolder in versionFolders)
                        {
                            var versionName = versionFolder.Name;
                            try
                            {
                                var packageFile = await versionFolder.GetFileAsync(packageName + "." + versionName + ".nupkg");
                                var properties = await packageFile.Properties.RetrievePropertiesAsync(new[] { PropertyName });
                                var dateAccessed = properties[PropertyName];

                                if (dateAccessed is DateTimeOffset dateAccessedDateTime)
                                {
                                    if (dateAccessedDateTime < accessTimeThreshold)
                                    {
                                        await versionFolder.DeleteAsync(deleteSettings);
                                        PackageCleaned?.Invoke(this, new NuGetCleanerPackageCleanedEventArgs
                                        {
                                            PackageName = packageName,
                                            VersionName = versionName
                                        });
                                    }
                                }
                            }
                            catch (FileNotFoundException) { }
                            catch (ArgumentException) { }
                            catch (Exception ex)
                            {
                                Error?.Invoke(this, new NuGetCleanerErrorEventArgs
                                {
                                    Path = versionFolder.Path,
                                    Exception = ex
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Error?.Invoke(this, new NuGetCleanerErrorEventArgs
                        {
                            Path = folder.Path,
                            Exception = ex
                        });
                    }
                }
            }
            catch (FileNotFoundException) { }
            catch (Exception ex)
            {
                Error?.Invoke(this, new NuGetCleanerErrorEventArgs
                {
                    Path = nugetFolder.Path,
                    Exception = ex
                });
            }
        }

        public event EventHandler<NuGetCleanerErrorEventArgs> Error;

        public event EventHandler<NuGetCleanerPackageCleanedEventArgs> PackageCleaned;
    }

    public class NuGetCleanerErrorEventArgs : EventArgs
    {
        public string Path { get; set; }

        public Exception Exception { get; set; }
    }

    public class NuGetCleanerPackageCleanedEventArgs : EventArgs
    {
        public string PackageName { get; set; }

        public string VersionName { get; set; }
    }
}
