using System;
using System.Threading.Tasks;
using AppFletesMueve.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;

namespace AppFletesMueve
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            RegisterGlobalExceptionHandlers();
        }

        private void RegisterGlobalExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                System.Diagnostics.Debug.WriteLine($"Unhandled exception: {ex}");
                // intentar mostrar alerta en UI thread
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try { await Current.MainPage.DisplayAlertAsync("Error", "Ocurrió un error inesperado.", "Aceptar"); } catch { }
                });
            }
            catch { }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Unobserved task exception: {e.Exception}");
                e.SetObserved();
            }
            catch { }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new NavigationPage(new LoginPage()));
        }
    }
}