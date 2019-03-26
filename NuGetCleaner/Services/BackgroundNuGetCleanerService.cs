using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources;
using Windows.UI.Notifications;

namespace NuGetCleaner.Services
{
    public class BackgroundNuGetCleanerService
    {
        public const string BackgroundTaskName = "BackgroundCleaner";
        private string toastTag;
        private const string ToastGroup = "background-cleaner";
        private readonly NuGetCleanerService cleaner;
        private readonly SettingsService settings;

        public BackgroundNuGetCleanerService(NuGetCleanerService cleaner, SettingsService settings)
        {
            this.cleaner = cleaner ?? throw new ArgumentNullException(nameof(cleaner));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Random random = new Random();
            toastTag = random.NextDouble().ToString();
        }

        public async Task EnableAsync()
        {
            BackgroundExecutionManager.RemoveAccess();
            var status = await BackgroundExecutionManager.RequestAccessAsync();

            Disable();

            var builder = new BackgroundTaskBuilder();
            builder.Name = BackgroundTaskName;
            builder.AddCondition(new SystemCondition(SystemConditionType.BackgroundWorkCostNotHigh));
            builder.SetTrigger(new TimeTrigger(1440, false));

            BackgroundTaskRegistration task = builder.Register();
        }

        public void Disable()
        {
            foreach (var cur in BackgroundTaskRegistration.AllTasks)
            {
                if (cur.Value.Name == BackgroundTaskName)
                {
                    cur.Value.Unregister(false);
                    break;
                }
            }
        }

        public bool IsTaskEnabled
        {
            get
            {
                return BackgroundTaskRegistration.AllTasks.Any(p => p.Value.Name == BackgroundTaskName);
            }
        }

        public async Task RunAsync()
        {
            var loader = new ResourceLoader();
            if ((cleaner.NugetFolder = await settings.GetNuGetFolderAsync()) == null)
            {
                ShowToast(loader.GetString("BackgroundNuGetCleaner_FolderNotSet"));
            }

            cleaner.MaxTimeAlive = TimeSpan.FromDays(settings.DaysOfPackageToKeep);
            ShowToastWithProgress(
                0,
                loader.GetString("BackgroundNuGetCleaner_CleaningStatus"),
                loader.GetString("BackgroundNuGetCleaner_CleaningMessage"));
            await cleaner.CleanAsync(new Progress<double>(progress =>
            {
                UpdateToastProgress(progress);
            }));
            ShowToast(loader.GetString("BackgroundNuGetCleaner_Cleaned"));
        }

        private void ShowToast(string message)
        {
            var loader = new ResourceLoader();

            ToastVisual visual = new ToastVisual
            {
                BindingGeneric = new ToastBindingGeneric
                {
                    Children =
                    {
                        new AdaptiveText
                        {
                            Text = loader.GetString("BackgroundNuGetCleaner_ToastTitle")
                        },
                        new AdaptiveText
                        {
                            Text = message
                        }
                    }
                }
            };

            ToastContent toastContent = new ToastContent()
            {
                Visual = visual,
            };

            var toast = new ToastNotification(toastContent.GetXml());
            toast.SuppressPopup = true;
            toast.Tag = toastTag;
            toast.Group = ToastGroup;

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private void ShowToastWithProgress(double progress, string status, string message)
        {
            var loader = new ResourceLoader();

            ToastVisual visual = new ToastVisual
            {
                BindingGeneric = new ToastBindingGeneric
                {
                    Children =
                    {
                        new AdaptiveText
                        {
                            Text = loader.GetString("BackgroundNuGetCleaner_ToastTitle")
                        },
                        new AdaptiveText
                        {
                            Text = message
                        },
                        new AdaptiveProgressBar
                        {
                            Value = new BindableProgressBarValue("progress"),
                            Status = new BindableString("status")
                        }
                    }
                }
            };

            ToastContent toastContent = new ToastContent()
            {
                Visual = visual
            };

            var toast = new ToastNotification(toastContent.GetXml());
            toast.Data = new NotificationData();
            toast.Data.Values["progress"] = progress.ToString();
            toast.Data.Values["status"] = status;
            toast.Data.SequenceNumber = 0;
            toast.SuppressPopup = true;
            toast.Tag = toastTag;
            toast.Group = ToastGroup;

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private void UpdateToastProgress(double progress)
        {
            var data = new NotificationData();
            data.Values["progress"] = progress.ToString();
            data.SequenceNumber = 0;
            ToastNotificationManager.CreateToastNotifier().Update(data, toastTag, ToastGroup);
        }
    }
}
