using Prism.Unity.Windows;
using System;
using Windows.ApplicationModel.Activation;
using Microsoft.Practices.Unity;
using System.Threading.Tasks;
using NuGetCleaner.Services;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;

namespace NuGetCleaner
{
    /// <summary>
    /// 提供特定于应用程序的行为，以补充默认的应用程序类。
    /// </summary>
    sealed partial class App : PrismUnityApplication
    {
        /// <summary>
        /// 初始化单一实例应用程序对象。这是执行的创作代码的第一行，
        /// 已执行，逻辑上等同于 main() 或 WinMain()。
        /// </summary>
        public App()
        {
            InitializeComponent();
            UnhandledException += App_UnhandledException;
            AppCenter.Start("9ed17939-873a-4ce1-909f-706aa87e9953", typeof(Analytics));
        }

        private void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Container.Resolve<MetricsService>().TrackException(e.Exception);
            e.Handled = true;
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();
            Container.RegisterType<NuGetCleanerService>();
            Container.RegisterType<SettingsService>(new PerThreadLifetimeManager());
        }

        protected override Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
        {
            NavigationService.Navigate("Main", null);
            return Task.CompletedTask;
        }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);
            if (Container == null)
            {
                CreateAndConfigureContainer();
            }

            var deferral = args.TaskInstance.GetDeferral();
            if (args.TaskInstance.Task.Name == BackgroundNuGetCleanerService.BackgroundTaskName)
            {
                try
                {
                    await Container.Resolve<BackgroundNuGetCleanerService>().RunAsync();
                }
                catch (Exception ex)
                {
                    Container.Resolve<MetricsService>().TrackException(ex);
                }
            }

            deferral.Complete();
        }

        protected override void CreateAndConfigureContainer()
        {
            if (Container == null)
            {
                base.CreateAndConfigureContainer();
            }
        }
    }
}
