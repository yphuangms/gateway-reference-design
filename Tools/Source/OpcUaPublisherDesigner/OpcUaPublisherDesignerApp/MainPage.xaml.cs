using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace PublisherDesignerApp
{
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;
        public const string APP_FULLNAME = "Sample OpcUa Publisher DesignerApp";
        public const string APP_SHORTNAME = "PublisherDesignerApp";

        private class SiteProfileFolder
        {
            public string Path = String.Empty;
            public string Name = String.Empty;
            public string Title { get; set; }
        };

        public MainPage()
        {
            this.InitializeComponent();

            // This is a static public property that allows downstream pages to get a handle to the MainPage instance
            // in order to call methods that are in this class.
            Current = this;
            Header.Text = APP_FULLNAME + ((App.SiteProfileId == null)? "" : (": " + App.SiteProfileId));
        }

        /// <summary>
        /// On leaving MainPage
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// On MainPage is loaded
        /// </summary>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string url = e.Parameter as string;
            bool forceReload = false;

            if (url != null && url == "RELOAD_MAIN")
            {
                forceReload = true;
            }

            panelFunctionMenu.Visibility = Visibility.Visible;
            Splitter.IsPaneOpen = false;

            var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                string infoStr = GetNetworkInfo();
                txtNetworkInfo.Text = "IP: " + infoStr;
            });
            
            await OpenSiteProfile(forceReload);

            UpdateTitle();

            if (SiteProfileManager.DefaultSiteProfileManager == null)
            {
                Splitter.IsPaneOpen = true;
                btnAppSetup_Click(null, null);
            }

            ContentFrame.Navigate(typeof(SessionMgmtPage));
        }

        /// <summary>
        /// Reload MainPage
        /// </summary>
        private void Reload()
        {
            Frame.Navigate(typeof(MainPage), "RELOAD_MAIN");
        }

        /// <summary>
        /// Function to collect network interface IP information
        /// </summary>
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

        /// <summary>
        /// Load profile information from app storage
        /// </summary>
        private async Task OpenSiteProfile(bool forceRelad = false, bool createIfNotExist = false)
        {
            string newSiteProfileId = App.SiteProfileId;
            SiteProfileManager curSiteProfile = SiteProfileManager.DefaultSiteProfileManager;
            if (curSiteProfile != null && (forceRelad || newSiteProfileId != curSiteProfile.SiteProfileId))
            {
                SiteProfileManager.SetDefault(null);
                curSiteProfile = null;
            }
            if (curSiteProfile == null)
            {
                curSiteProfile = await SiteProfileManager.Open(newSiteProfileId, createIfNotExist);
                SiteProfileManager.SetDefault(curSiteProfile);
            }
        }

        /// <summary>
        /// Delete profile
        /// </summary>
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

        /// <summary>
        /// Enumerate profiles in app storage
        /// </summary>
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

        /// <summary>
        /// Update app title with profile name
        /// </summary>
        private void UpdateTitle()
        {
            var ignored = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                SessionConfig siteInfo = (SiteProfileManager.DefaultSiteProfileManager == null)? null : SiteProfileManager.DefaultSiteProfileManager.sessionConfig;
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

        //-------------------
        // UI event handlers
        //-------------------

        /// <summary>
        /// On toggle-panel button clicked
        /// </summary>
        private void btnTogglePanel_Click(object sender, RoutedEventArgs e)
        {
            if (panelAppSetup.Visibility == Visibility.Visible)
            {
                Splitter.IsPaneOpen = true;
                return; // when AppSetup panel is onscreen, only "save" or "cancel" may dismiss the panel
            }
            Splitter.IsPaneOpen = !Splitter.IsPaneOpen;
        }

        /// <summary>
        /// On show-device-info button clicked
        /// -- toggle to show/hide device info
        /// -- device info: host IP address list
        /// </summary>
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

        /// <summary>
        /// On app-setup button clicked
        /// -- expand panel with app-setup UI items
        /// </summary>
        private async void btnAppSetup_Click(object sender, RoutedEventArgs e)
        {
            if (panelAppSetup.Visibility == Visibility.Collapsed)
            {
                panelAppSetup.Visibility = Visibility.Visible;
                btnAppSetup.Visibility = Visibility.Collapsed;
                panelFunctionMenu.Visibility = Visibility.Collapsed;

                txtSourceId.Text = "";

                var siteFolderList = await GetSiteProfileFolderList();
                SiteProfileFolder newProfile = new SiteProfileFolder()
                {
                    Name = "",
                    Path = "",
                    Title = "-- New Profile --"
                };
                siteFolderList.Insert(0, newProfile);
                listSiteProfileIds.Items.Clear();

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
                            break;
                        }
                    }
                }
                else
                {
                    listSiteProfileIds.SelectedIndex = 0;
                    txtSourceId.Text = "";
                }
            }
        }

        /// <summary>
        /// On app-setup-save button clicked
        /// -- save changes made in app-setup to app storage
        /// -- load new app settings accordingly
        /// </summary>
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
            await OpenSiteProfile(forceReload, createIfNotExist);

            UpdateTitle();

            if (SiteProfileManager.DefaultSiteProfileManager != null)
            {
                btnAppSetup.Visibility = Visibility.Visible;
                panelAppSetup.Visibility = Visibility.Collapsed;
                panelFunctionMenu.Visibility = Visibility.Visible;

                ContentFrame.Navigate(typeof(SessionMgmtPage));
            } 

            btnAppSetupCancel.IsEnabled = true;
        }

        /// <summary>
        /// On app-setup-cancel button clicked
        /// -- discard changes made in app-setup
        /// </summary>
        private void btnAppSetupCancel_Click(object sender, RoutedEventArgs e)
        {
            txtDeviceId.Text = "Source: " + txtSourceId.Text;

            if (SiteProfileManager.DefaultSiteProfileManager != null)
            {
                btnAppSetup.Visibility = Visibility.Visible;
                panelAppSetup.Visibility = Visibility.Collapsed;
                panelFunctionMenu.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// On app-data-clean button clicked
        /// -- clean all data/settings saved in app storage
        /// </summary>
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

        /// <summary>
        /// On profile-delete button clicked
        /// -- delete selected profile from app storage
        /// </summary>
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
                bool isDeleted = await DeleteProfile(siteProfileId);
                if (isDeleted)
                {
                    listSiteProfileIds.SelectedIndex = -1;
                    listSiteProfileIds.Items.RemoveAt(delIndex);
                    listSiteProfileIds.UpdateLayout();
                }
                btnAppSetupCancel.IsEnabled = false;         
            }
        }

        /// <summary>
        /// On profile selection changed
        /// -- prompt for new profile if "--New--" is selected
        /// </summary>
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

        /// <summary>
        /// On reload button clicked
        /// -- reload MainPage
        /// </summary>
        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        #region advanced profile functions
        #if ENABLE_ADVANCED_PROFILE_FUNCTIONS
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
        #endif
        #endregion advanced profile operations
    }
}
