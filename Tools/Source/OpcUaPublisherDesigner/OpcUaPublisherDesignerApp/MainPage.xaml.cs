using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PublisherDesignerApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;
        public const string APP_FULLNAME = "Sample OpcUa Publisher DesignerApp";
        public const string APP_SHORTNAME = "PublisherDesignerApp";

        private string GetNetworkInfo()
        {
            string infoStr = "";
            foreach (HostName networkName in NetworkInformation.GetHostNames())
            {
                if (networkName.IPInformation != null)
                {
                    if (networkName.Type == HostNameType.Ipv4)
                    {
                        infoStr += ((infoStr.Length == 0) ? "" : " / ") + networkName.ToString();
                        //break;
                    }
                }
            }
            return infoStr;
        }

        public MainPage()
        {
            this.InitializeComponent();

            // This is a static public property that allows downstream pages to get a handle to the MainPage instance
            // in order to call methods that are in this class.
            Current = this;
            Header.Text = APP_FULLNAME + ((App.SiteProfileId == null)? "" : (": " + App.SiteProfileId));

            //ContentFrame.Navigate(typeof(SiteMgmtPage));
        }

        //public void NavigateToPageWithParameter(string parameter)
        //{
        //    ContentPage p = ContentFrame.Content as ContentPage;
        //    if (p == null)
        //    {
        //        ContentFrame.Navigate(typeof(ContentPage));
        //    }
        //    UpdateDebugMessage(App.Current, App.appActivationDesc);
        //    dashboardManager.Load(dashboardSource.CanAck, dashboardSource.Url, dashboardSource.GuardianStatusCache, null);
        //}

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            //SiteMgmtPage p = ContentFrame.Content as SiteMgmtPage;

            string url = e.Parameter as string;
            bool forceReload = false;
            if (url == null || url != "RELOAD_MAIN")
            {
                // Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().SuppressSystemOverlays = true;
                // // open in fullscreen
                //Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().FullScreenSystemOverlayMode = Windows.UI.ViewManagement.FullScreenSystemOverlayMode.Standard;
                //Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
            }

            if (url != null && url == "RELOAD_MAIN")
            {
                forceReload = true;
            }

            if (Window.Current.Bounds.Width < 640)
            {
                Splitter.IsPaneOpen = false;
            }

            panelFunctionMenu.Visibility = Visibility.Visible;
            Splitter.IsPaneOpen = false;

            var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                string infoStr = GetNetworkInfo();
                txtNetworkInfo.Text = "IP: " + infoStr;
            });

            
            await OpenSite(forceReload);

            UpdateTitle();

            if (SiteProfileManager.DefaultSiteManager == null)
            {
                Splitter.IsPaneOpen = true;
                btnAppSetup_Click(null, null);
            }

            ContentFrame.Navigate(typeof(SessionMgmtPage));
        }

        public async Task OpenSite(bool forceRelad = false, bool createIfNotExist = false)
        {
            string newSiteProfileId = App.SiteProfileId;
            SiteProfileManager curSite = SiteProfileManager.DefaultSiteManager;
            if (curSite != null && (forceRelad || newSiteProfileId != curSite.SiteProfileId))
            {
                //App.StopPublish();
                SiteProfileManager.SetDefault(null);
                curSite = null;
            }
            if (curSite == null)
            {
                curSite = await SiteProfileManager.Open(newSiteProfileId, createIfNotExist);
                SiteProfileManager.SetDefault(curSite);
            }
        }

        public void UpdateTitle()
        {
            var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                SessionConfig siteInfo = (SiteProfileManager.DefaultSiteManager == null)? null : SiteProfileManager.DefaultSiteManager.sessionConfig;
                if (siteInfo == null)
                {
                    Header.Text = APP_FULLNAME + ((App.SiteProfileId == null)? "" : (" - " + App.SiteProfileId));
                    txtMainFunctionsTitle.Text = ((App.SiteProfileId == null)? "(No Profile)" : (App.SiteProfileId));
                }
                else
                {
                    Header.Text = APP_FULLNAME + " - " + App.SiteProfileId;
                    txtMainFunctionsTitle.Text = App.SiteProfileId;
                }
            });
        }

        public void UpdateDebugMessage(object sender, string message)
        {
            var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (sender.GetType() == typeof(App))
                {
                    txtPreviousExecState.Text = message;
                }
            });
        }

        private void OnDashboardCallback(object sender, string dataString)
        {
            //dashboardSource.CommandHandler(dataString);
        }

        private void OnDataUpdated(object sender, string dataString)
        {
            var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //if (dashboardManager == null) return;
                //await dashboardManager.UpdateData(dataString);
            });
        }

        /// <summary>
        /// Used to display messages to the user
        /// </summary>
        /// <param name="strMessage"></param>
        /// <param name="type"></param>
        public void NotifyUser(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }
            StatusBlock.Text = strMessage;

            //if (App.DebugLevel == 0) return;

            // Collapse the StatusBlock if it has no text to conserve real estate.
            StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            if (StatusBlock.Text != String.Empty)
            {
                StatusBorder.Visibility = Visibility.Visible;
                StatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;
                StatusPanel.Visibility = Visibility.Collapsed;
            }
        }

        async void Footer_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(((HyperlinkButton)sender).Tag.ToString()));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (panelAppSetup.Visibility == Visibility.Visible)
            {
                Splitter.IsPaneOpen = true;
                return; // when AppSetup panel is onscreen, only "save" or "cancel" may dismiss the panel
            }
            Splitter.IsPaneOpen = !Splitter.IsPaneOpen;
        }


        private void StatusBlock_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            NotifyUser("", NotifyType.StatusMessage);
        }

        class SiteProfileFolder
        {
            public string Path = String.Empty;
            public string Name = String.Empty;
            public string Title { get; set; }
            //public SiteSettings SiteSettings = new SiteSettings();
        };

        private async Task<List<SiteProfileFolder>> GetSiteProfileFolderList()
        {
            List<SiteProfileFolder> siteFolderList = new List<SiteProfileFolder>();
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFolder sitesFolder = await localFolder.CreateFolderAsync(SiteProfileManager.PROFILEROOT, CreationCollisionOption.OpenIfExists);
                var folders = await sitesFolder.GetFoldersAsync();

                foreach (var folder in folders)
                {
                    SiteProfileFolder siteFolderInfo = new SiteProfileFolder();
                    siteFolderInfo.Name = folder.Name;
                    siteFolderInfo.Path = SiteProfileManager.PROFILEROOT + "/" + siteFolderInfo.Name + "/";
                    //var filepath = siteFolderInfo.Path + SiteManager.DEFAULT_SITESETTINGSPATH;
                    //siteFolderInfo.SiteSettings = await SerializationUtil.LoadFromJsonFile<SiteSettings>(filepath) as SiteSettings;
                    siteFolderInfo.Title = String.Format("{0}", siteFolderInfo.Name);
                    siteFolderList.Add(siteFolderInfo);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetSiteProfileFolderList Exception! " + ex.Message);
            }
            return siteFolderList;
        }


        private async void btnAppSetup_Click(object sender, RoutedEventArgs e)
        {
            if (panelAppSetup.Visibility == Visibility.Collapsed)
            {
                panelAppSetup.Visibility = Visibility.Visible;
                btnAppSetup.Visibility = Visibility.Collapsed;
                panelFunctionMenu.Visibility = Visibility.Collapsed;

                txtSourceId.Text = "";

                var siteFolderList = await GetSiteProfileFolderList();
                SiteProfileFolder newProfile = new SiteProfileFolder();
                newProfile.Name = "";
                newProfile.Path = "";
                //newProfile.SiteSettings = null;
                newProfile.Title = "-- New Profile --";
                siteFolderList.Insert(0, newProfile);
                listSiteProfileIds.Items.Clear();
                //listSiteProfileIds.ItemsSource = null;
                //listSiteProfileIds.ItemsSource = siteFolderList;

                foreach (var item in siteFolderList)
                {
                    listSiteProfileIds.Items.Add(item);
                }

                listSiteProfileIds.SelectedIndex = -1;

                var searchName = App.SiteProfileId;
                if (!String.IsNullOrEmpty(App.SiteProfileId))
                {
                    for (int i = 1; i < siteFolderList.Count; i++)
                    {
                        if (App.SiteProfileId == siteFolderList[i].Name)
                        {
                            listSiteProfileIds.SelectedIndex = i;
                            //txtSourceId.Text = siteFolderList[i].SiteSettings.SiteName;
                            break;
                        }
                    }
                }
                else
                {
                    listSiteProfileIds.SelectedIndex = 0;
                    txtSourceId.Text = "";
                }
                //if (App.Config.ServiceResourceList.Count > 0)
                //{
                //    for (int i = 0; i < App.Config.ServiceResourceList.Count; i++)
                //    {
                //        if (App.Config.ServiceResourceList[i].ProfileName == App.ProfileName)
                //        {
                //            listSourceIds.SelectedIndex = i;
                //            txtSourceId.Text = App.Config.ServiceResourceList[i].SourceId;
                //            chkIsDataSource.IsChecked = (App.Config.ServiceResourceList[i].EmulatorIotHubConnString != null);
                //            break;
                //        }
                //    }
                //}
            }
        }

        private async Task<bool> DeleteProfile(string profileId)
        {
            bool isSuccess = false;
            try
            {
                StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                StorageFolder sitesFolder = await localFolder.GetFolderAsync(SiteProfileManager.PROFILEROOT);
                var folder = await sitesFolder.GetFolderAsync(profileId);
                if (folder != null)
                {
                    await folder.DeleteAsync();
                }
                isSuccess = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DeleteProfile Exception! " + ex.Message);
            }
            return isSuccess;
        }

        private async void btnProfileDelete_Click(object sender,RoutedEventArgs e)
        {
            var delIndex = listSiteProfileIds.SelectedIndex;
            SiteProfileFolder selectedItem = listSiteProfileIds.SelectedValue as SiteProfileFolder;
            var siteProfileId = (selectedItem == null) ? String.Empty : selectedItem.Name;

            if (listSiteProfileIds.SelectedIndex == 0 || String.IsNullOrEmpty(siteProfileId))
                // nothing to delete
                return;


            btnProfileDelete.IsEnabled = false;

            MessageDialog showDialog = new MessageDialog("This will delete all settings in this profile. Are you sure to delete profile - '" + siteProfileId + "' ?");
            showDialog.Commands.Add(new UICommand("Delete") { Id = 0 });
            showDialog.Commands.Add(new UICommand("Cancel") { Id = 1 });
            showDialog.DefaultCommandIndex = 1;
            showDialog.CancelCommandIndex = 1;
            var result = await showDialog.ShowAsync();

            if ((int)result.Id == 0)
            {
                //await App.CleanAppData();
                //await App.LoadAppSettings();

                //MessageDialog closeAppDialog = new MessageDialog("App data is cleaned. Applicationn will be closed!");
                //closeAppDialog.Commands.Add(new UICommand("OK") { Id = 0 });
                //closeAppDialog.DefaultCommandIndex = 0;
                //closeAppDialog.CancelCommandIndex = 0;
                //await closeAppDialog.ShowAsync();

                bool isDeleted = await DeleteProfile(siteProfileId);
                if (isDeleted)
                {
                    listSiteProfileIds.SelectedIndex = -1;
                    listSiteProfileIds.Items.RemoveAt(delIndex);
                    listSiteProfileIds.UpdateLayout();
                }
                btnAppSetupCancel.IsEnabled = false;         
                
                //if (listSiteProfileIds.SelectedIndex != 0)
                //    btnProfileDelete.IsEnabled = true;
                //else
                //    btnExportProfile.IsEnabled = false;
            }
        }

        /*
        private async void btnImportProfile_Click(object sender, RoutedEventArgs e)
        {
            if (listSiteProfileIds.SelectedValue == null) return;

            string siteProfileId = (listSiteProfileIds.SelectedValue as SiteProfileFolder).Name;

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".zip");

            StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                try
                {
                    StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                    //StorageFolder tempFolder = await localFolder.CreateFolderAsync("Temp", CreationCollisionOption.OpenIfExists);
                    //string tempFileName = siteProfileId + DateTime.Now.ToString(".yyMMddHHmmssfff") + ".zip";

                    MessageDialog showDialog = new MessageDialog("This will overwrite settings in selected profile folder.\nAre you sure to import settings to profile '" + siteProfileId + "'?");
                    showDialog.Commands.Add(new UICommand("OK") { Id = 0 });
                    showDialog.Commands.Add(new UICommand("Cancel") { Id = 1 });
                    showDialog.DefaultCommandIndex = 1;
                    showDialog.CancelCommandIndex = 1;
                    var result = await showDialog.ShowAsync();

                    var task = Task.Run(()=>
                    {
                        Exception ex = null;
                        ZipArchive ziparch = null;
                        try
                        {
                            ziparch = ZipFile.OpenRead(file.Path); //tempFile.Path);
                            Dictionary<string, ZipArchiveEntry> ziparchdic = new Dictionary<string, ZipArchiveEntry>(ziparch.Entries.Count);
                            foreach (var entry in ziparch.Entries)
                            {
                                ziparchdic.Add(entry.FullName, entry);
                            }
                            if (ziparchdic.ContainsKey(SiteManager.DEFAULT_SITESETTINGSPATH))
                            {
                                foreach (var entry in ziparch.Entries)
                                {
                                    entry.ExtractToFile(Path.Combine(localFolder.Path, @"Sites\" + siteProfileId + @"\" + entry.FullName), true);
                                }
                            }
                        }
                        catch (Exception ex1)
                        {
                            ex = ex1;
                        }
                        finally
                        {
                            ziparch?.Dispose();
                        }
                        if (ex != null)
                        {
                            throw(ex);
                        }
                    });
                    await task;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ImportProfile Exception! " + ex.Message);
                }
            }
            Splitter.IsPaneOpen = true;
        }

        private async void btnExportProfile_Click(object sender, RoutedEventArgs e)
        {
            if (listSiteProfileIds.SelectedValue == null) return;

            string siteProfileId = (listSiteProfileIds.SelectedValue as SiteProfileFolder).Name;

            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedFileName =  siteProfileId + ".zip";
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeChoices.Add("Zip", new List<string>() { ".zip" });

            StorageFile file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    
                    StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                    StorageFolder tempFolder = await localFolder.CreateFolderAsync("Temp", CreationCollisionOption.OpenIfExists);
                    string tempFileName = siteProfileId + DateTime.Now.ToString(".yyMMddHHmmssfff") + ".zip";
                    
                    var task = Task.Run(()=>
                    {
                        try
                        {
                            ZipFile.CreateFromDirectory(Path.Combine(localFolder.Path, SiteManager.PROFILEROOT + "\\" + siteProfileId), Path.Combine(tempFolder.Path, tempFileName));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("ZipFile.CreateFromDirectory Exception! " + ex.Message);
                        }
                    });
                    await task;
                    StorageFile tempFile = await tempFolder.GetFileAsync(tempFileName);
                    if (tempFile != null)
                    {
                        await tempFile.CopyAndReplaceAsync(file);
                        await tempFile.DeleteAsync();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ImportProfile Exception! " + ex.Message);
                }
            }
            Splitter.IsPaneOpen = true;
        }
        */

        private async void btnCleanAppData_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog showDialog = new MessageDialog("This will delete all app settings and then close application.\nAre you sure to clean app data?");
            showDialog.Commands.Add(new UICommand("Clean") { Id = 0 });
            showDialog.Commands.Add(new UICommand("Cancel") { Id = 1 });
            showDialog.DefaultCommandIndex = 1;
            showDialog.CancelCommandIndex = 1;
            var result = await showDialog.ShowAsync();

            if ((int)result.Id == 0)
            {
                await App.CleanAppData();
                App.SiteProfileId = "";
                Reload();
            }
        }

        private async void btnAppSetupSave_Click(object sender, RoutedEventArgs e)
        {
            SiteProfileFolder selectedItem = listSiteProfileIds.SelectedValue as SiteProfileFolder;
            App.SiteProfileId = (selectedItem == null) ? String.Empty : selectedItem.Name;

            if (selectedItem != null && String.IsNullOrEmpty(selectedItem.Name))
            {
                if (!String.IsNullOrEmpty(txtSourceId.Text))
                    // create new profile
                    App.SiteProfileId = txtSourceId.Text;
            }

            App.SaveAppSettings();
            bool forceReload = false;
            bool createIfNotExist = true;
            await OpenSite(forceReload, createIfNotExist);

            UpdateTitle();

            if (SiteProfileManager.DefaultSiteManager != null)
            {
                btnAppSetup.Visibility = Visibility.Visible;
                panelAppSetup.Visibility = Visibility.Collapsed;
                panelFunctionMenu.Visibility = Visibility.Visible;

                ContentFrame.Navigate(typeof(SessionMgmtPage));
            } 

            btnAppSetupCancel.IsEnabled = true;
            //MessageDialog closeAppDialog = new MessageDialog("App data changes are saved. Restart application for new settings!");
            //closeAppDialog.Commands.Add(new UICommand("OK") { Id = 0 });
            //closeAppDialog.DefaultCommandIndex = 0;
            //closeAppDialog.CancelCommandIndex = 0;
            //await closeAppDialog.ShowAsync();

            //Reload();
        }

        public void Reload()
        {
            //Application.Current.Exit();
            //Frame.Navigate(typeof(ContentPage), "RELOAD_MAIN");
            Frame.Navigate(typeof(MainPage), "RELOAD_MAIN");
        }

        private void btnAppSetupCancel_Click(object sender, RoutedEventArgs e)
        {
            //listSourceIds.ItemsSource = App.Config.ServiceResourceList;
            //txtSourceId.Text = App.Config.Current.SourceId;
            //chkIsDataSource.IsChecked = dashboardSource.IsDataSource;

            txtDeviceId.Text = "Source: " + txtSourceId.Text;

            if (SiteProfileManager.DefaultSiteManager == null)
            {
            }
            else
            {
                btnAppSetup.Visibility = Visibility.Visible;
                panelAppSetup.Visibility = Visibility.Collapsed;
                panelFunctionMenu.Visibility = Visibility.Visible;
            }
        }

        private void btnShowDeviceInfo_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceInfo.Visibility == Visibility.Visible)
            {
                DeviceInfo.Visibility = Visibility.Collapsed;
                iconShowDeviceInfo.Glyph = "\uE010"; // show
            }
            else
            {
                DeviceInfo.Visibility = Visibility.Visible;
                iconShowDeviceInfo.Glyph = "\uE011"; // hide
            }
        }

        private void listSiteProfileIds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SiteProfileFolder selectedItem = listSiteProfileIds.SelectedValue as SiteProfileFolder;

            if (selectedItem != null)
            {
                if (!String.IsNullOrEmpty(selectedItem.Name))
                {
                    txtSourceTitle.Text = "Current Profile:";
                    txtSourceId.IsReadOnly = true;
                    txtSourceId.Text = App.SiteProfileId;
                    if (selectedItem.Name != App.SiteProfileId)
                    {
                        btnProfileDelete.IsEnabled = true;
                        btnAppSetupSave.IsEnabled = true;
                    }
                    else
                    {
                        btnProfileDelete.IsEnabled = false;
                        btnAppSetupSave.IsEnabled = false;
                    }
                }
                else
                {
                    txtSourceTitle.Text = "New Profile Name:";
                    txtSourceId.IsReadOnly = false;
                    txtSourceId.Text = "";
                    btnProfileDelete.IsEnabled = false;
                    btnAppSetupSave.IsEnabled = true;
                }
            }
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private void btnTogglePanel_PointerPressed(object sender,PointerRoutedEventArgs e)
        {
            Button_Click(sender, e);
        }
    }

    public enum NotifyType
    {
        StatusMessage,
        ErrorMessage
    };
}
