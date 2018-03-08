/* Copyright (c) 1996-2016, OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
   version 2 of the License are accompanied with this source code. See http://opcfoundation.org/License/GPLv2
   This source code is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
*/

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
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class SessionMgmtPage : Page
    {
        private ObservableCollection<SessionStatusInfo> listSessionStatus;
        private SessionConfig sessionConfig = null;
        public SessionMgmtPage()
        {
            this.InitializeComponent();
            listSessionStatus = new ObservableCollection<SessionStatusInfo>();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            SiteProfileManager siteManager = SiteProfileManager.DefaultSiteManager;
            if (siteManager != null)
            {
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            SiteProfileManager siteManager = SiteProfileManager.DefaultSiteManager;
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
                    SessionStatusInfo sessionInfoItem = new SessionStatusInfo();
                    sessionInfoItem.Name = session.sessionName;
                    sessionInfoItem.SourceUrl = String.Format("{0}", session.sourceType.ToString());
                    sessionInfoItem.Description = "";
                    sessionInfoItem.Status = 0;
                    sessionInfoItem.IsActive = false;
                    sessionInfoItem.Session = session;
                    listSessionStatus.Add(sessionInfoItem);

                    listviewSessionStatus.Items.Add(sessionInfoItem);
                }
            }

            //listviewSessionStatus.ItemsSource = listSessionStatus;
        }

        // function code copied from https://stackoverflow.com/questions/12608641/textbox-inside-listview-getting-listviewitem-on-textbox-textchanged
        private T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element; while (parent != null)
            {
                T correctlyTyped = parent as T; if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }
                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            } return null;
        }
        private T FindVisualChild<T>(UIElement element, object dataContext) where T : UIElement
        {
            UIElement parent = element; while (parent != null)
            {
                T correctlyTyped = parent as T; if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }
                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            } return null;
        }

        private void ViewSessionDetail_Click(object sender,RoutedEventArgs e)
        {
            MenuFlyoutItem itemViewDetail = sender as MenuFlyoutItem;
            SessionStatusInfo sessionStatusInfo = itemViewDetail.DataContext as SessionStatusInfo;
            //if (sessionStatusInfo?.Session.sourceType == SourceSession.SupportedSourceType.OPCUAServer)
            //{
                Frame.Navigate(typeof(SessionMgmt_OpcuaPage), sessionStatusInfo.Session);
            //}
        }

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
                await SiteProfileManager.DefaultSiteManager?.SaveSessionConfig();
                isSuccess = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("DeleteSession Exception! " + ex.Message);
            }
            return isSuccess;
        }

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

        private void RenameSession_Click(object sender,RoutedEventArgs e)
        {
            SessionStatusInfo sessionStatusInfo = (sender as MenuFlyoutItem).DataContext as SessionStatusInfo;
            RenameSessionEnable(true, sessionStatusInfo);
        }

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

        private async Task<bool> RenameSession(string targetName, SessionInfo session)
        {
            bool isSuccess = false;
            try
            {
                StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                string filepath = Path.Combine(localFolder.Path, SiteProfileManager.GetFullPath(App.SiteProfileId, session.profilePath));
                
                StorageFile targetFile = await StorageFile.GetFileFromPathAsync(filepath);
                string targetFilename = String.Format("session_{0}.config.json");

                if (targetFile != null)
                {
                    await targetFile.RenameAsync(targetName, NameCollisionOption.FailIfExists);
                }
                session.sessionName = targetName;
                session.profilePath = targetFilename;

                await SiteProfileManager.DefaultSiteManager?.SaveSessionConfig();
                isSuccess = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RenameSession Exception! " + ex.Message);
            }
            return isSuccess;
        }

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
            //TextBox tbSessionName = sender as TextBox;
            //if (!tbSessionName.IsReadOnly)
            //{
            //    tbSessionName.LostFocus += tbSessionName_LostFocus;
            //}
        }

        private void listviewSessionStatus_SelectionChanged(object sender,SelectionChangedEventArgs e)
        {

        }

        private void btnNewSession_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SessionMgmt_OpcuaPage), "NEW");
        }

        private async void btnExportPublisherNodes_Click(object sender, RoutedEventArgs e)
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
            picker.SuggestedFileName = SiteProfileManager.DEFAULT_PUBLISHERNODESPATH;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            picker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });

            StorageFile destfile = await picker.PickSaveFileAsync();
            if (destfile != null)
            {
                try
                {
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    bool isSuccess = await SiteProfileManager.DefaultSiteManager?.SavePublisherNodes();
                    if (isSuccess)
                    {
                        var srcuri = new Uri("ms-appdata:///local/" + SiteProfileManager.GetFullPath(App.SiteProfileId, SiteProfileManager.DEFAULT_PUBLISHERNODESPATH));
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
                    Debug.WriteLine("btnExportPublisherNodes_Click Exception! " + ex.Message);
                    MessageDialog showDialog = new MessageDialog("Export PublisherNodes Failed!\nError: " + ex.Message);
                    showDialog.Commands.Add(new UICommand("Close") { Id = 1 });
                    showDialog.DefaultCommandIndex = 1;
                    showDialog.CancelCommandIndex = 1;
                    await showDialog.ShowAsync();
                }
            }
        }
    }
}
