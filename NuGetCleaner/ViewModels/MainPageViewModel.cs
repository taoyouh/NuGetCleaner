using NuGetCleaner.Services;
using Prism.Commands;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace NuGetCleaner.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private readonly NuGetCleanerService cleaner;
        private readonly SettingsService settings;
        private readonly MetricsService metrics;
        private readonly BackgroundNuGetCleanerService backgroundCleaner;
        private bool _isFolderInitialized = false;
        private StorageFolder _nuGetFolder;
        private int _daysOfPackagesToKeep = 7;
        private bool _useRecycleBinIfPossible;
        private readonly ObservableCollection<string> _messages = new ObservableCollection<string>();
        private bool _inProgress = false;
        private double _progress = 0;
        private bool _backgroundCleanSwitchEnabled = true;

        public MainPageViewModel(NuGetCleanerService cleaner, SettingsService settings, MetricsService metrics, BackgroundNuGetCleanerService backgroundCleaner)
        {
            BrowseCommand = new DelegateCommand(Browse, CanBrowse);
            CleanCommand = new DelegateCommand(Clean, CanClean);
            this.cleaner = cleaner ?? throw new ArgumentNullException(nameof(cleaner));
            this.cleaner.Error += Cleaner_Error;
            this.cleaner.PackageCleaned += Cleaner_PackageCleaned;

            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            this.backgroundCleaner = backgroundCleaner ?? throw new ArgumentNullException(nameof(backgroundCleaner));
            DaysOfPackagesToKeep = settings.DaysOfPackageToKeep;
            UseRecycleBinIfPossible = settings.UseRecycleBinIfPossible;
            InitializeNuGetFolder();
        }

        private async void InitializeNuGetFolder()
        {
            NuGetFolder = await settings.GetNuGetFolderAsync();
            IsFolderInitialized = true;
        }

        private StorageFolder NuGetFolder
        {
            get => _nuGetFolder;
            set
            {
                if (_nuGetFolder != value)
                {
                    _nuGetFolder = value;
                    settings.SetNuGetFolder(value);
                    RaisePropertyChanged(nameof(NuGetFolderPath));
                    CleanCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsFolderInitialized
        {
            get => _isFolderInitialized;
            private set
            {
                if (_isFolderInitialized != value)
                {
                    _isFolderInitialized = value;
                    RaisePropertyChanged();
                    BrowseCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string NuGetFolderPath
        {
            get => NuGetFolder?.Path ?? string.Empty;
        }

        public DelegateCommand BrowseCommand { get; }

        public int DaysOfPackagesToKeep
        {
            get => _daysOfPackagesToKeep;
            set => SetProperty(ref _daysOfPackagesToKeep, value, () =>
            {
                settings.DaysOfPackageToKeep = value;
            });
        }

        public bool UseRecycleBinIfPossible
        {
            get => _useRecycleBinIfPossible;
            set => SetProperty(ref _useRecycleBinIfPossible, value, () =>
            {
                settings.UseRecycleBinIfPossible = value;
            });
        }

        public bool BackgroundCleanSwitchEnabled
        {
            get => _backgroundCleanSwitchEnabled;
            private set => SetProperty(ref _backgroundCleanSwitchEnabled, value);
        }

        public bool BackgroundCleanEnabled
        {
            get => backgroundCleaner.IsTaskEnabled;
            set => SetBackgroundCleanEnabled(value);
        }

        public DelegateCommand CleanCommand { get; }

        public bool InProgress
        {
            get => _inProgress;
            private set
            {
                if (_inProgress != value)
                {
                    _inProgress = value;
                    RaisePropertyChanged();
                    CleanCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public IReadOnlyCollection<string> Messages => _messages;

        public double Progress
        {
            get => _progress;
            private set => SetProperty(ref _progress, value);
        }

        private bool CanBrowse()
        {
            return IsFolderInitialized;
        }

        private async void Browse()
        {
            try
            {
                FolderPicker picker = new FolderPicker();
                picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                picker.FileTypeFilter.Add("*");
                NuGetFolder = await picker.PickSingleFolderAsync();
            }
            catch (Exception ex)
            {
                metrics.TrackException(ex);
            }
        }

        private bool CanClean()
        {
            return NuGetFolder != null && !InProgress;
        }

        private async void Clean()
        {
            try
            {
                if (InProgress)
                {
                    throw new InvalidOperationException("Another clean is in progress.");
                }

                Progress = 0;
                InProgress = true;
                cleaner.NugetFolder = NuGetFolder;
                cleaner.MaxTimeAlive = TimeSpan.FromDays(DaysOfPackagesToKeep);
                cleaner.UseRecycleBinIfPossible = UseRecycleBinIfPossible;
                await cleaner.CleanAsync(new Progress<double>(Cleaner_ProgressChanged));
            }
            catch (Exception ex)
            {
                metrics.TrackException(ex);
            }
            finally
            {
                InProgress = false;
            }
        }

        private void Cleaner_ProgressChanged(double progress)
        {
            Progress = progress;
        }

        private void Cleaner_PackageCleaned(object sender, NuGetCleanerPackageCleanedEventArgs e)
        {
            var loader = new ResourceLoader();
            _messages.Add(string.Format(loader.GetString("MainPageVM_PackageCleaned"), e.PackageName, e.VersionName));
        }

        private void Cleaner_Error(object sender, NuGetCleanerErrorEventArgs e)
        {
            var loader = new ResourceLoader();
            _messages.Add(string.Format(loader.GetString("MainPageVM_Error"), e.Path, e.Exception.Message));
        }

        private async void SetBackgroundCleanEnabled(bool value)
        {
            try
            {
                BackgroundCleanSwitchEnabled = false;
                if (value)
                {
                    await backgroundCleaner.EnableAsync();
                }
                else
                {
                    backgroundCleaner.Disable();
                }
            }
            catch (Exception ex)
            {
                metrics.TrackException(ex);
                RaisePropertyChanged(nameof(BackgroundCleanEnabled));
            }
            finally
            {
                BackgroundCleanSwitchEnabled = true;
            }
        }
    }
}
