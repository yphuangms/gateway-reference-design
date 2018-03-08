using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace PublisherDesignerApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public static ApplicationExecutionState previousExecState { get; private set; }
        public static string appActivationDesc { get; private set; }

        public static int DebugLevel = 0;
        public static string SiteProfileId = "";

        public static SessionMgmt_OpcuaPage unclosedSession = null;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            LoadAppSettings();

            // initialize all available session types
            await OpcuaSessionConfig.Init();
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            previousExecState = e.PreviousExecutionState;
            Debug.WriteLine("App OnLaunched: previous state = " + e.PreviousExecutionState.ToString());

            appActivationDesc = "App OnLaunched: previous state = " + e.PreviousExecutionState.ToString() + "\nkind=" + e.Kind.ToString();

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }

            Windows.ApplicationModel.Core.CoreApplication.Exiting += CoreApplication_Exiting;
        }

        private void CoreApplication_Exiting(object sender, object e)
        {
            Debug.WriteLine("Application Is Closing!!!!");
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            Task.Run(() =>
            {
                if (unclosedSession != null && unclosedSession.IsSessionAlive())
                {
                    unclosedSession.CloseSessionView_OpcuaClient(false);
                }
                Application.Current.Exit();
                deferral.Complete();
            });
        }

        public static void LoadAppSettings()
        {
            SiteProfileId = ApplicationData.Current.LocalSettings.Values["SiteProfileId"] as string;
            if (String.IsNullOrEmpty(SiteProfileId))
                SiteProfileId = "";
        }

        public static void SaveAppSettings()
        {
             ApplicationData.Current.LocalSettings.Values["SiteProfileId"] = SiteProfileId;
        }

        public static async Task CleanAppData()
        {
            await ApplicationData.Current.ClearAsync(ApplicationDataLocality.Local);
            ApplicationData.Current.LocalSettings.Values.Clear();
        }
    }

}