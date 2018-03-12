using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace PublisherDesignerApp
{
    /// <summary>
    /// Session list management page
    /// <summary>
    public partial class SessionMgmtPage : Page
    {
        private ObservableCollection<SessionStatusInfo> listSessionStatus;
        private SessionConfig sessionConfig = null;

        public SessionMgmtPage()
        {
            this.InitializeComponent();
            listSessionStatus = new ObservableCollection<SessionStatusInfo>();
        }
        
        /// <summary>
        /// On page unloaded
        /// <summary>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// On page loaded
        /// <summary>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            SiteProfileManager siteManager = SiteProfileManager.DefaultSiteProfileManager;
            if (siteManager == null)
            {
                siteManager = await SiteProfileManager.Open(App.SiteProfileId);
                SiteProfileManager.SetDefault(siteManager);
            }
            else
            {
                var action = e.Parameter as string;
                if (!String.IsNullOrEmpty(action) && action == "RELOAD")
                {
                    await siteManager.LoadSessionConfig();
                }
            }

            listviewSessionStatus.Items.Clear();

            if (siteManager?.sessionConfig?.sessions != null)
            {
                sessionConfig = siteManager.sessionConfig;
                foreach (var session in sessionConfig.sessions)
                {
                    SessionStatusInfo sessionInfoItem = new SessionStatusInfo()
                    { 
                        Name = session.sessionName,
                        SourceUrl = String.Format("{0}", session.sourceType.ToString()),
                        Description = String.Empty,
                        Status = 0,
                        IsActive = false,
                        Session = session,
                    };

                    listSessionStatus.Add(sessionInfoItem);
                    listviewSessionStatus.Items.Add(sessionInfoItem);
                }
            }
        }

        //------------------------------------
        // Session List Management Functions
        //------------------------------------

        /// <summary>
        /// Delete session
        /// <summary>
        private async Task<bool> DeleteSession(SessionInfo session)
        {
            bool isSuccess = false;
            try
            {
                StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                string fullpath = Path.Combine(localFolder.Path, SiteProfileManager.GetFullPath(App.SiteProfileId, session.profilePath));
                
                StorageFile targetFile = await StorageFile.GetFileFromPathAsync(fullpath);
                if (targetFile != null)
                {
                    await targetFile.DeleteAsync();
                }
                sessionConfig.sessions.Remove(session);
                await SiteProfileManager.DefaultSiteProfileManager?.SaveSessionConfig();
                isSuccess = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DeleteSession Exception! " + ex.Message);
            }
            return isSuccess;
        }

        /// <summary>
        /// Rename session
        /// <summary>
        private async Task<bool> RenameSession(string targetName, SessionInfo session)
        {
            bool isSuccess = false;
            try
            {
                StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                string filepath = Path.Combine(localFolder.Path, SiteProfileManager.GetFullPath(App.SiteProfileId, session.profilePath));
                
                StorageFile targetFile = await StorageFile.GetFileFromPathAsync(filepath);
                string targetFilename = String.Format("session_{0}.config.json", targetName);

                if (targetFile != null)
                {
                    await targetFile.RenameAsync(targetFilename, NameCollisionOption.FailIfExists);
                }
                session.sessionName = targetName;
                session.profilePath = targetFilename;

                await SiteProfileManager.DefaultSiteProfileManager?.SaveSessionConfig();
                isSuccess = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RenameSession Exception! " + ex.Message);
            }
            return isSuccess;
        }
        
        //----------------------
        // UI Helper Functions
        //----------------------

        /// <summary>
        /// Rename session
        /// <summary>
        private async Task<bool> RenameSessionSave(string targetName, SessionStatusInfo sessionStatusInfo)
        {
            bool hasNameConflict = false;
            foreach (var item in listSessionStatus)
            {
                if (item.Name == targetName && item.Name != sessionStatusInfo.Name)
                {
                    hasNameConflict = true;
                }
            }

            bool isRenamed = false;

            if (hasNameConflict)
            {
                MessageDialog showDialog = new MessageDialog("Session name conflicts with other session!");
                showDialog.Commands.Add(new UICommand("Close") { Id = 1 });
                showDialog.DefaultCommandIndex = 1;
                showDialog.CancelCommandIndex = 1;
                await showDialog.ShowAsync();
            }
            else
            {
                // confirm rename
                MessageDialog showDialog = new MessageDialog("This will rename this session, and SiteProfile content could be inconsistent.\nAre you sure to rename session from '" + sessionStatusInfo.Name + "' to '" + targetName + "'?");
                showDialog.Commands.Add(new UICommand("Rename") { Id = 0 });
                showDialog.Commands.Add(new UICommand("Cancel") { Id = 1 });
                showDialog.DefaultCommandIndex = 1;
                showDialog.CancelCommandIndex = 1;
                var result = await showDialog.ShowAsync();

                if ((int)result.Id == 0)
                {
                    isRenamed = await RenameSession(targetName, sessionStatusInfo.Session);
                    if (isRenamed)
                    {
                        sessionStatusInfo.UpdateSessionName(targetName);
                    }
                }
            }
            return isRenamed;
        }

        /// <summary>
        /// Enable selected session on UI to allow session rename
        /// <summary>
        private TextBox RenameSessionEnable(bool enable, SessionStatusInfo sessionStatusInfo)
        {
            ListViewItem curItem = null;
            foreach (ListViewItem item in listviewSessionStatus.ItemsPanelRoot.Children)
            {
                if (item.Content.Equals(sessionStatusInfo))
                {
                    curItem = item;
                    break;
                }
            }

            if (curItem != null)
            {
                TextBox tbSessionName = null;
                Button btnFunc = null;
                foreach (UIElement uielem in (curItem.ContentTemplateRoot as Grid).Children)
                {
                    if ((uielem as TextBox)?.Name == "tbSessionName")
                    {
                        tbSessionName = uielem as TextBox;
                    }
                    else if ((uielem as Button)?.Name == "btnFunc")
                    {
                        btnFunc = uielem as Button;
                    }
                }
                if (enable)
                {
                    if (btnFunc != null)
                    {
                        btnFunc.Tag = "Save";
                        ToolTipService.SetToolTip(btnFunc, "Save");
                        (btnFunc.Content as FontIcon).Glyph = "\uE28F"; // save
                    }
                    if (tbSessionName != null)
                    {
                        tbSessionName.IsReadOnly = false;
                        tbSessionName.Focus(FocusState.Pointer);
                    }
                }
                else
                {
                    if (btnFunc != null)
                    {
                        btnFunc.Tag = "Menu";
                        ToolTipService.SetToolTip(btnFunc, "Function Menu");
                        (btnFunc.Content as FontIcon).Glyph = "\uE10C"; // menu 
                    }
                    if (tbSessionName != null)
                    {
                        tbSessionName.IsReadOnly = true;
                    }
                }
                return tbSessionName;
            }
            return null;
        }


        //----------------------
        // UI Event Handlers
        //----------------------   
        
        /// <summary>
        /// On view-session-detail/edit-session menu item clicked
        /// -- navigate to Opcua session page for detail viewing & editing
        /// <summary>
        private void ViewSessionDetail_Click(object sender,RoutedEventArgs e)
        {
            MenuFlyoutItem itemViewDetail = sender as MenuFlyoutItem;
            SessionStatusInfo sessionStatusInfo = itemViewDetail.DataContext as SessionStatusInfo;
            Frame.Navigate(typeof(SessionMgmt_OpcuaPage), sessionStatusInfo.Session);
        }

        /// <summary>
        /// On delete-session menu item clicked
        /// -- confirm to delete target session
        /// <summary>
        private async void DeleteSession_Click(object sender,RoutedEventArgs e)
        {
            MenuFlyoutItem itemViewDetail = sender as MenuFlyoutItem;
            SessionStatusInfo sessionStatusInfo = itemViewDetail.DataContext as SessionStatusInfo;

            MessageDialog showDialog = new MessageDialog("This will delete settings in this session.\nAre you sure to delete session - '" + sessionStatusInfo.Name + "' ?");
            showDialog.Commands.Add(new UICommand("Delete") { Id = 0 });
            showDialog.Commands.Add(new UICommand("Cancel") { Id = 1 });
            showDialog.DefaultCommandIndex = 1;
            showDialog.CancelCommandIndex = 1;
            var result = await showDialog.ShowAsync();

            if ((int)result.Id == 0)
            {
                bool isDeleted = await DeleteSession(sessionStatusInfo.Session);

                if (isDeleted)
                {
                    int delIndex = listviewSessionStatus.SelectedIndex;
                    listviewSessionStatus.SelectedIndex = delIndex - 1;
                    listviewSessionStatus.Items.RemoveAt(delIndex);
                    listviewSessionStatus.UpdateLayout();

                    listSessionStatus.Remove(sessionStatusInfo);
                }
            }
        }

        /// <summary>
        /// On rename-session menu item clicked
        /// -- allow edit session name on UI (require explicit "save" confirm by user to commit name change)
        /// <summary>
        private void RenameSession_Click(object sender,RoutedEventArgs e)
        {
            SessionStatusInfo sessionStatusInfo = (sender as MenuFlyoutItem).DataContext as SessionStatusInfo;
            RenameSessionEnable(true, sessionStatusInfo);
        }

        /// <summary>
        /// On losing focus at name textbox
        /// -- require user confirmation to save this change or discard the change
        /// <summary>
        private async void tbSessionName_LostFocus(object sender,RoutedEventArgs e)
        {
            TextBox tbSessionName = sender as TextBox;
            SessionStatusInfo sessionStatusInfo = tbSessionName.DataContext as SessionStatusInfo;

            Debug.WriteLine("[" + sessionStatusInfo.Name  + "] Lost Focus " + tbSessionName.IsReadOnly + ", " + tbSessionName.FocusState);
            if (tbSessionName.IsReadOnly) return;

            RenameSessionEnable(false, sessionStatusInfo);
            tbSessionName.LostFocus -= tbSessionName_LostFocus;
            tbSessionName.Tag = null;

            string targetName = tbSessionName.Text;

            if (sessionStatusInfo.Name == targetName)
            {
                return;
            }
            else
            {
                // confirm rename
                MessageDialog showDialog = new MessageDialog("Would you like to save the change?");
                showDialog.Commands.Add(new UICommand("Yes") { Id = 0 });
                showDialog.Commands.Add(new UICommand("No") { Id = 1 });
                showDialog.DefaultCommandIndex = 1;
                showDialog.CancelCommandIndex = 1;
                var result = await showDialog.ShowAsync();
                if ((int)result.Id == 1)
                {
                    tbSessionName.Text = sessionStatusInfo.Name;
                    return;
                }
            }
            await RenameSessionSave(targetName, sessionStatusInfo);
        }

        /// <summary>
        /// On menu button clicked
        /// -- show flyout menu function
        /// <summary>
        private async void Menu_Click(object sender,RoutedEventArgs e)
        {
            Button btnMenu = sender as Button;
            //ListViewItem thisItem = FindVisualParent<ListViewItem>(sender as Button);
            listviewSessionStatus.SelectedItem = btnMenu.DataContext as SessionStatusInfo;

            if ((btnMenu.Tag as string) == "Save")
            {
                btnMenu.Flyout.Hide();

                SessionStatusInfo sessionStatusInfo = btnMenu.DataContext as SessionStatusInfo;
                TextBox txtSessionName = RenameSessionEnable(false, sessionStatusInfo);

                string targetName = txtSessionName?.Text;
                if (targetName == sessionStatusInfo.Name) return;

                await RenameSessionSave(targetName, sessionStatusInfo);
            }
            else
            {
                btnMenu.Flyout.ShowAt(btnMenu);
            }
        }

        /// <summary>
        /// On session name textbox got focus
        /// <summary>
        private void tbSessionName_GotFocus(object sender,RoutedEventArgs e)
        {
            TextBox tbSessionName = sender as TextBox;
            SessionStatusInfo sessionStatusInfo = tbSessionName.DataContext as SessionStatusInfo;
            Debug.WriteLine("[" + sessionStatusInfo.Name  + "] Got Focus " + tbSessionName.IsReadOnly + ", " + tbSessionName.FocusState);

            if (!tbSessionName.IsReadOnly && tbSessionName.FocusState != FocusState.Unfocused && String.IsNullOrEmpty(tbSessionName.Tag as string))
            {
                tbSessionName.LostFocus += tbSessionName_LostFocus;
                tbSessionName.Tag = "Editing";
                Debug.WriteLine("[" + sessionStatusInfo.Name  + "] Enable LostFocus Handler");
            }
        }

        private void tbSessionName_TextChanged(object sender,TextChangedEventArgs e)
        {
        }

        private void listviewSessionStatus_SelectionChanged(object sender,SelectionChangedEventArgs e)
        {
        }


        /// <summary>
        /// On new-session button clicked
        //  -- navigate to Opcua session page for session configuration
        /// <summary>
        private void btnNewSession_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SessionMgmt_OpcuaPage), "NEW");
        }

        /// <summary>
        /// On export-publishednodes button clicked
        //  -- generate "publishednodes.json" file to target file path
        /// <summary>
        private async void btnExportPublishedNodes_Click(object sender, RoutedEventArgs e)
        {
            if (sessionConfig?.sessions?.Count > 0)
            {
            }
            else
            {
                MessageDialog showDialog = new MessageDialog("No Node for Publishing!");
                showDialog.Commands.Add(new UICommand("Close") { Id = 1 });
                showDialog.DefaultCommandIndex = 1;
                showDialog.CancelCommandIndex = 1;
                await showDialog.ShowAsync();
                return;
            }

            var picker = new Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedFileName = SiteProfileManager.DEFAULT_PUBLISHEDNODESPATH;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            picker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });

            StorageFile destfile = await picker.PickSaveFileAsync();
            if (destfile != null)
            {
                try
                {
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    bool isSuccess = await SiteProfileManager.DefaultSiteProfileManager?.SavePublishedNodes();
                    if (isSuccess)
                    {
                        var srcuri = new Uri("ms-appdata:///local/" + SiteProfileManager.GetFullPath(App.SiteProfileId, SiteProfileManager.DEFAULT_PUBLISHEDNODESPATH));
                        StorageFile srcfile = await StorageFile.GetFileFromApplicationUriAsync(srcuri);
                        await srcfile.CopyAndReplaceAsync(destfile);

                        MessageDialog showDialog = new MessageDialog("Export PublisherNodes Done!\n" + destfile.Path);
                        showDialog.Commands.Add(new UICommand("Close") { Id = 1 });
                        showDialog.DefaultCommandIndex = 1;
                        showDialog.CancelCommandIndex = 1;
                        await showDialog.ShowAsync();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("btnExportPublishedNodes_Click Exception! " + ex.Message);
                    MessageDialog showDialog = new MessageDialog("Export PublishedNodes Failed!\nError: " + ex.Message);
                    showDialog.Commands.Add(new UICommand("Close") { Id = 1 });
                    showDialog.DefaultCommandIndex = 1;
                    showDialog.CancelCommandIndex = 1;
                    await showDialog.ShowAsync();
                }
            }
        }
    }
}
