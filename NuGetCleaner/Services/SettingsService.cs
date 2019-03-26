using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace NuGetCleaner.Services
{
    public class SettingsService
    {
        private const string NuGetFolderTokenSettingsName = "NuGetFolderToken";
        private const string DaysOfPackageToKeepSettingsName = "DaysOfPackageToKeep";

        private string NuGetFolderToken
        {
            get
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                if (settings.TryGetValue(NuGetFolderTokenSettingsName, out object value)
                    && value is string token)
                {
                    return token;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                settings[NuGetFolderTokenSettingsName] = value;
            }
        }

        public async Task<StorageFolder> GetNuGetFolderAsync()
        {
            if (NuGetFolderToken == null)
            {
                return null;
            }
            else
            {
                var list = StorageApplicationPermissions.FutureAccessList;
                return await list.GetFolderAsync(NuGetFolderToken);
            }
        }

        public void SetNuGetFolder(StorageFolder value)
        {
            var list = StorageApplicationPermissions.FutureAccessList;
            if (value == null)
            {
                if (NuGetFolderToken != null)
                {
                    list.Remove(NuGetFolderToken);
                }
            }
            else
            {
                if (NuGetFolderToken != null)
                {
                    list.AddOrReplace(NuGetFolderToken, value);
                }
                else
                {
                    NuGetFolderToken = list.Add(value);
                }
            }
        }

        public int DaysOfPackageToKeep
        {
            get
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                if (settings.TryGetValue(DaysOfPackageToKeepSettingsName, out object value)
                    && value is int days)
                {
                    return days;
                }
                else
                {
                    return 7;
                }
            }
            set
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                settings[DaysOfPackageToKeepSettingsName] = value;
            }
        }
    }
}
