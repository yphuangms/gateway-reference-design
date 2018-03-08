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

using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.Controls;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace PublisherDesignerApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public partial class SessionMgmt_OpcuaPage : Page
    {
        #region Private Fields
        private Session m_design_session;
        private ApplicationInstance m_application;
        //private Opc.Ua.Server.StandardServer m_server;
        private ConfiguredEndpointCollection m_endpoints;
        private ApplicationConfiguration m_configuration;
        private ServiceMessageContext m_context;
        private OpcuaSessionConfig m_sessionConfig;
        private static string m_cert_sub_path = @"OPC Foundation\CertificateStores\UA Applications\certs";
        private string m_local = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        private string m_cert_full_path = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, m_cert_sub_path);
        private static string m_sessionConfig_file = "Opc.Ua.SampleClient.json";
        private string m_sessionConfig_full_path = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, m_sessionConfig_file);
        #endregion

        public SessionMgmt_OpcuaPage()
        {
            this.InitializeComponent();
        }

        ~SessionMgmt_OpcuaPage()
        {
            CloseSessionView_OpcuaClient(false);
        }

        public bool IsSessionAlive()
        {
            return (m_design_session != null);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            CloseSessionView_OpcuaClient(false);
            //Window.Current.VisibilityChanged -= Current_VisibilityChanged;
            App.unclosedSession = null;
            base.OnNavigatedFrom(e);
        }

        enum SESSIONMGMT_ACTION
        {
            NEW,
            EDIT,
            UNKNOWN
        };

        private SESSIONMGMT_ACTION sessionMgmtAction = SESSIONMGMT_ACTION.EDIT; // 0: new, 1: edit, 2:...
        private SessionInfo sessionInfo;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            App.unclosedSession = this;
            //Window.Current.VisibilityChanged += Current_VisibilityChanged;

            //m_sessionConfig_full_path = e.Parameter as string;

            sessionInfo = e.Parameter as SessionInfo;        
            
            if (sessionInfo == null)
            {
                sessionMgmtAction = SESSIONMGMT_ACTION.NEW;
                sessionInfo = new SessionInfo();
                sessionInfo.sessionName = "";
                sessionInfo.profilePath = "";
                sessionInfo.sourceType = "";
                m_sessionConfig_full_path = "";
                m_sessionConfig = null;
            }
            else
            {
                m_sessionConfig_full_path = Path.Combine(m_local, SiteProfileManager.GetFullPath(App.SiteProfileId, sessionInfo.profilePath));
                // json configuration
                m_sessionConfig = OpcuaSessionConfig.LoadFromJsonFile(m_sessionConfig_full_path);
            }

                 
            ApplicationInstance application = OpcuaSessionConfig.OpcuaApplication;
            ApplicationConfiguration configuration = application.ApplicationConfiguration;
            ServiceMessageContext context = configuration.CreateMessageContext();

            if (!configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                // disable auto accept
                // need to import certificate
                configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
            }
        
            m_context = context;
            m_application = application;
            m_configuration = configuration;
             
            SessionsCTRL.Configuration = configuration;
            SessionsCTRL.MessageContext = context;
            SessionsCTRL.AddressSpaceCtrl = BrowseCTRL;
            SessionsCTRL.NodeSelected += SessionCtrl_NodeSelected;

            // get list of cached endpoints.
            // disable cached endpoints from Opc.Ua.SampleClient.Config.xml
            //m_endpoints = m_configuration.LoadCachedEndpoints(true);
            m_endpoints = new ConfiguredEndpointCollection();
            m_endpoints.DiscoveryUrls = configuration.ClientConfiguration.WellKnownDiscoveryUrls;

            // work around to fill Configuration and DiscoveryUrls
            if (m_sessionConfig != null)
            {
                m_sessionConfig.endpoint.Configuration = EndpointConfiguration.Create(m_configuration);
                m_sessionConfig.endpoint.Description.Server.DiscoveryUrls.Add(m_sessionConfig.endpoint.EndpointUrl.AbsoluteUri.ToString());
                m_endpoints.Add(m_sessionConfig.endpoint);
            }

            // hook up endpoint selector
            EndpointSelectorCTRL.Initialize(m_endpoints, m_configuration);
            EndpointSelectorCTRL.ConnectEndpoint += EndpointSelectorCTRL_ConnectEndpoint;
            EndpointSelectorCTRL.EndpointsChanged += EndpointSelectorCTRL_OnChange;

            BrowseCTRL.SessionTreeCtrl = SessionsCTRL;
            BrowseCTRL.NodeSelected += BrowseCTRL_NodeSelected;

            btnDelSubscription.IsEnabled = false;
            btnAddSubscription.IsEnabled = false;

            btnDelSubscription.Click += ContextMenu_OnDelete;
            btnAddSubscription.Click += ContextMenu_OnReport;

            // exception dialog
            GuiUtils.ExceptionMessageDlg += ExceptionMessageDlg;

            //m_task = EndpointConnect(true);   
            EndpointSelectorCTRL.IsEnabled = false;
            BrowseCTRL.IsEnabled = false;
            SessionsCTRL.IsEnabled = false;

            txtSessionName.Text = sessionInfo.sessionName;
            //txtSessionType.Text = "(" + sessionInfo.sourceType.ToString() + ")";

            if (sessionMgmtAction == SESSIONMGMT_ACTION.NEW)
            {
                txtSessionName.IsReadOnly = false;
                btnReload.IsEnabled = false;
                btnReload.Visibility = Visibility.Collapsed;
                EndpointSelectorCTRL.IsEnabled = true;
            }
            else
            {
                if (m_sessionConfig == null)
                {
                    txtSessionName.IsReadOnly = true;
                    EndpointSelectorCTRL.IsEnabled = true;
                    btnReload.IsEnabled = false;
                }
                else
                {
                    txtSessionName.IsReadOnly = true;
                    btnReload.IsEnabled = false;
                    var ignored = Task.Run(OpenSessionView_OpcuaClient);
                }
            }
        }

        //private void Current_VisibilityChanged(object sender, VisibilityChangedEventArgs e)
        //{
        //    if (!e.Visible)
        //    {
        //        //if (m_design_session != null)
        //            //CloseSessionView_OpcuaClient(false);
        //    }
        //}

        void RemoveAllClickEventsFromButton()
        {
            //CommandBTN.Click -= ContextMenu_OnDelete;
            //CommandBTN.Click -= ContextMenu_OnCancelSubscription;
            //CommandBTN.Click -= ContextMenu_OnDisconnect;
            //CommandBTN.Click -= ContextMenu_OnReport;
        }

        private void SessionCtrl_NodeSelected(object sender, TreeNodeActionEventArgs e)
        {
            if (e.Node != null)
            {
                MonitoredItem item = e.Node as MonitoredItem;
                if (e.Node is MonitoredItem)
                {
                    //CommandBTN.Visibility = Visibility.Visible;
                    //CommandBTN.Content = "Delete";
                    //RemoveAllClickEventsFromButton();
                    //CommandBTN.Click += ContextMenu_OnDelete;
                    //CommandBTN.Tag = e.Node;

                    btnDelSubscription.IsEnabled = true;
                    btnDelSubscription.Tag = e.Node;
                }
                else
                {
                    btnDelSubscription.IsEnabled = false;
                    btnDelSubscription.Tag = null;
                }
                //else if (e.Node is Subscription)
                //{
                //    CommandBTN.Visibility = Visibility.Visible;
                //    CommandBTN.Content = "Cancel";
                //    RemoveAllClickEventsFromButton();
                //    CommandBTN.Click += ContextMenu_OnCancelSubscription;
                //    CommandBTN.Tag = e.Node;
                //}
                //else if (e.Node is Session)
                //{
                //    CommandBTN.Visibility = Visibility.Visible;
                //    CommandBTN.Content = "Disconnect";
                //    RemoveAllClickEventsFromButton();
                //    CommandBTN.Click += ContextMenu_OnDisconnect;
                //    CommandBTN.Tag = e.Node;

                //    // Update current session object
                //    m_design_session = (Session)e.Node;
                //}
                //else
                //{
                //    RemoveAllClickEventsFromButton();
                //    CommandBTN.Visibility = Visibility.Collapsed;
                //    CommandBTN.Tag = null;
                //}
            }
        }

        private void BrowseCTRL_NodeSelected(object sender, TreeNodeActionEventArgs e)
        {
            if (e.Node != null)
            {
                ReferenceDescription reference = e.Node as ReferenceDescription;
                if (reference != null && reference.NodeClass == NodeClass.Variable)
                {
                    //CommandBTN.Visibility = Visibility.Visible;
                    //CommandBTN.Content = "Add";
                    //RemoveAllClickEventsFromButton();
                    //CommandBTN.Click += ContextMenu_OnReport;
                    //CommandBTN.Tag = e.Node;

                    btnAddSubscription.IsEnabled = true;
                    btnAddSubscription.Tag = e.Node;
                }
                else
                {
                    //RemoveAllClickEventsFromButton();
                    //CommandBTN.Visibility = Visibility.Collapsed;
                    //CommandBTN.Tag = null;

                    btnAddSubscription.IsEnabled = true;
                    btnAddSubscription.Tag = e.Node;
                }
            }
        }

        private void ContextMenu_OnDisconnect(object sender, RoutedEventArgs e)
        {
            try
            {
                CloseSessionView_OpcuaClient();
                //SessionsCTRL.Delete(CommandBTN.Tag as Session);
                //ServerUrlTB.Text = "None";
                //ServerStatusTB.Text = "";
            }
            catch (Exception exception)
            {
                GuiUtils.HandleException(String.Empty, GuiUtils.CallerName(), exception);
            }
        }

        private void ContextMenu_OnCancelSubscription(object sender, RoutedEventArgs e)
        {
            Button CommandBTN = sender as Button;
            try
            {
                foreach (MonitoredItem x in (CommandBTN.Tag as Subscription).MonitoredItems)
                {
                    SessionsCTRL.Delete(x);
                }
                SessionsCTRL.Delete(CommandBTN.Tag as Subscription);
            }
            catch (Exception exception)
            {
                GuiUtils.HandleException(String.Empty, GuiUtils.CallerName(), exception);
            }
        }

        private void ContextMenu_OnDelete(object sender, RoutedEventArgs e)
        {
            Button CommandBTN = sender as Button;
            try
            {
                var monitoredItem = CommandBTN.Tag as MonitoredItem;
                if (monitoredItem == null)
                    return;
                var subscription = monitoredItem.Subscription;
                SessionsCTRL.Delete(monitoredItem);
                if (subscription.MonitoredItemCount == 0)
                {
                    // Remove subscription if no more items
                    CommandBTN.Tag = subscription;
                    ContextMenu_OnCancelSubscription(sender, e);
                }
            }
            catch (Exception exception)
            {
                GuiUtils.HandleException(String.Empty, GuiUtils.CallerName(), exception);
            }
        }

        private async void ContextMenu_OnReport(object sender, RoutedEventArgs e)
        {
            Button CommandBTN = sender as Button;

            try
            {
                // can only subscribe to local variables. 
                ReferenceDescription reference = CommandBTN.Tag as ReferenceDescription;

                string userInputDisplayName = await GetUserInputDisplayName(reference.DisplayName.ToString());
                if (String.IsNullOrEmpty(userInputDisplayName))
                    userInputDisplayName = reference.DisplayName.ToString();

                if (m_design_session != null && reference != null)
                {
                    CreateMonitoredItem(userInputDisplayName,
                        m_design_session, null, reference.NodeId, reference.DisplayName.ToString(), MonitoringMode.Reporting);
                }
            }
            catch (Exception exception)
            {
                GuiUtils.HandleException(String.Empty, GuiUtils.CallerName(), exception);
            }
        }

        private async Task<string> GetUserInputDisplayName(string defaultDisplayName = "")
        {
            TextBox txtNameInput = new TextBox();
            txtNameInput.AcceptsReturn = false;
            txtNameInput.Height = 26;
            txtNameInput.Text = defaultDisplayName;
            ContentDialog inputDialog = new ContentDialog();
            inputDialog.Content = txtNameInput;
            inputDialog.Title = "Subscribed Item DisplayName";
            inputDialog.IsSecondaryButtonEnabled = true;
            inputDialog.PrimaryButtonText = "Cancel";
            inputDialog.SecondaryButtonText = "OK";
            if (await inputDialog.ShowAsync() == ContentDialogResult.Secondary)
                return txtNameInput.Text;
            else
                return "";
        }

        void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            ManualResetEvent ev = new ManualResetEvent(false);
            Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                async () =>
                {
                    await GuiUtils.HandleCertificateValidationError(this, validator, e);
                    if (e.Accept)
                    {
                        MessageDialog showDialog = new MessageDialog("Would you like to save this certificate?");
                        showDialog.Commands.Add(new UICommand("Save") { Id = 0 });
                        showDialog.Commands.Add(new UICommand("Cancel") { Id = 1 });
                        showDialog.DefaultCommandIndex = 1;
                        showDialog.CancelCommandIndex = 1;
                        var result = await showDialog.ShowAsync();

                        if ((int)result.Id == 0)
                        {
                            try
                            {
                                byte[] cert = e.Certificate.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Cert);
                                string issuerName = e.Certificate.GetNameInfo(System.Security.Cryptography.X509Certificates.X509NameType.SimpleName, true);
                                string filePath = Path.Combine(m_cert_full_path, String.Format("{0} [{1}].der", issuerName, e.Certificate.Thumbprint));
                                File.WriteAllBytes(filePath, cert);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                    ev.Set();
                }
                ).AsTask().Wait();
            ev.WaitOne();
        }

        async Task EndpointSelectorCTRL_ConnectEndpoint(object sender, ConnectEndpointEventArgs e)
        {
            try
            {
                // disable Connect while connecting button
                EndpointSelectorCTRL.IsEnabled = false;
                // Connect
                CloseSessionView_OpcuaClient();
                e.UpdateControl = await Connect(e.Endpoint);

                if (e.UpdateControl)
                {
                    //if (m_design_session != null)
                    //{
                    //    SessionsCTRL.AddNode(m_design_session);
                    //}

                    gridEndpointSelector.Visibility = Visibility.Visible;
                    txtEndpointSelector.Text = m_design_session.Endpoint.EndpointUrl.ToString();
                    EndpointSelectorCTRL.Visibility = Visibility.Collapsed;

                    BrowseCTRL.IsEnabled = true;
                    SessionsCTRL.IsEnabled = true;
                    EnableSessionOpButtons(true);
                }                
            }
            catch (Exception exception)
            {
                GuiUtils.HandleException(String.Empty, GuiUtils.CallerName(), exception);
                e.UpdateControl = false;
            }
            finally
            {
                // enable Connect button
                EndpointSelectorCTRL.IsEnabled = !e.UpdateControl;
            }
        }

        private void EndpointSelectorCTRL_OnChange(object sender, EventArgs e)
        {
            return;
            //try
            //{
            //    m_endpoints.Save();
            //}
            //catch (Exception exception)
            //{
            //    GuiUtils.HandleException(String.Empty, GuiUtils.CallerName(), exception);
            //}
        }

        /// <summary>
        /// Connects to a server.
        /// </summary>
        public async Task<bool> Connect(ConfiguredEndpoint endpoint)
        {
            bool result = false;
            if (endpoint == null)
            {
                return false;
            }
            // connect dialogs
            Session session = await SessionsCTRL.Connect(endpoint, m_sessionConfig?.sessionname);

            if (session != null)
            {
                if (m_sessionConfig != null)
                {
                    NamespaceTable namespaceTable = new NamespaceTable();
                    DataValue namespaceArrayNodeValue = session.ReadValue(VariableIds.Server_NamespaceArray);
                    namespaceTable.Update(namespaceArrayNodeValue.GetValue<string[]>(null));

                    m_sessionConfig.endpoint = session.ConfiguredEndpoint;
                    foreach (MonitoredNode x in m_sessionConfig.monitoredlist)
                    {
                        CreateMonitoredItem(x.description,
                                session, null, ExpandedNodeId.Parse(x.nodeid, namespaceTable), x.displayname, MonitoringMode.Reporting);
                    }
                }
                m_design_session = session;
                // BrowseCTRL.SetView(session, BrowseViewType.Objects, null);

                result = true;
            }
            else
            {
                //try
                //{
                //    await EndpointConnect(false);
                //    if (m_design_session != null)
                //        SessionsCTRL.AddNode(m_design_session);
                //}
                //catch (Exception exception)
                //{
                //    GuiUtils.HandleException(String.Empty, GuiUtils.CallerName(), exception);
                //}
            }

            return result;
        }

        async void ExceptionMessageDlg(string message)
        {
            await Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
            {
                //MessageDlg dialog = new MessageDlg(message);
                //await dialog.ShowAsync();
                gridMessageDlg.Visibility = Visibility.Visible;
                txtMessage.Text = message;
            });
        }

        public void CreateMonitoredItem(string userInputName,
           Session session, Subscription subscription, ExpandedNodeId exNodeId, string displayName, MonitoringMode mode)
        {
            if (subscription == null)
            {
                subscription = session.DefaultSubscription;
                if (session.AddSubscription(subscription))
                {
                    subscription.Create();
                    subscription.PublishingEnabled = true;
                    if (m_sessionConfig != null)
                        subscription.PublishingInterval = m_sessionConfig.publishinterval;
                    else
                        subscription.PublishingInterval = 1000;
                }
            }
            else
            {
                session.AddSubscription(subscription);
            }

            //-- concat selected node displayname with parent node displayname
            //Browser browser = new Browser(session);
            //browser.BrowseDirection = BrowseDirection.Inverse;
            //ReferenceDescriptionCollection inversecollection = browser.Browse(nodeId);
            //string parentDisplayName = (inversecollection.Count > 0)? inversecollection[0].DisplayName.ToString() : "";

            // add the new monitored item.
            MonitoredItem monitoredItem = new MonitoredItem(subscription.DefaultItem);

            monitoredItem.StartNodeId = (NodeId)exNodeId;
            monitoredItem.AttributeId = Attributes.Value;
            //monitoredItem.DisplayName = String.Format("{0} ({1}.{2} - {3})", userInputName, parentDisplayName,  displayName, nodeId.ToString());
            monitoredItem.DisplayName = String.Format("{0} ({1} - {2})", userInputName, displayName, exNodeId.ToString());
            monitoredItem.MonitoringMode = mode;
            monitoredItem.SamplingInterval = mode == MonitoringMode.Sampling ? 1000 : 0;
            monitoredItem.QueueSize = 0;
            monitoredItem.DiscardOldest = true;
            
            subscription.AddItem(monitoredItem);
            subscription.ApplyChanges();
        }

        private Session Opcua_EndpointDisconnect()
        {
            Session deletedSession = m_design_session;
            if (m_design_session != null)
            {
                m_design_session.Close();
                //SessionsCTRL.Delete(m_design_session);
                m_design_session = null;
            }
            return deletedSession;
        }

        private async Task<Session> Opcua_EndpointConnect(string sessionname, Uri endpointUrl, int preferredSecurityLevel = -1)
        {
            Session opcua_session = null;
            if (m_sessionConfig != null)
            {
                EndpointDescription selectedEndpoint = SelectUaTcpEndpoint(DiscoverEndpoints(m_configuration, endpointUrl, 600), preferredSecurityLevel);
                ConfiguredEndpoint configuredEndpoint = new ConfiguredEndpoint(selectedEndpoint.Server, EndpointConfiguration.Create(m_configuration));
                configuredEndpoint.Update(selectedEndpoint);
                Session session = await Opcua_EndpointConnect(sessionname, configuredEndpoint);
                return session;
            }
            return null;
        }

        private async Task<Session> Opcua_EndpointConnect(string sessionname, ConfiguredEndpoint configuredEndpoint)
        {
            Session opcua_session = null;
            if (m_sessionConfig != null)
            {
                try
                {
                    opcua_session = await Session.Create(
                        m_configuration,
                        configuredEndpoint,
                        true,
                        false,
                        sessionname,
                        60000,
                        new UserIdentity(new AnonymousIdentityToken()),
                        null);

                    if (opcua_session != null)
                    {
                        NamespaceTable namespaceTable = new NamespaceTable();
                        DataValue namespaceArrayNodeValue = opcua_session.ReadValue(VariableIds.Server_NamespaceArray);
                        namespaceTable.Update(namespaceArrayNodeValue.GetValue<string[]>(null));

                        foreach (MonitoredNode x in m_sessionConfig.monitoredlist)
                        {
                            CreateMonitoredItem(x.description,
                                  opcua_session, null, ExpandedNodeId.Parse(x.nodeid, namespaceTable), x.displayname, MonitoringMode.Reporting);
                        }
                    }
                }
                catch (Exception exception)
                {
                    GuiUtils.HandleException(String.Empty, GuiUtils.CallerName(), exception);
                }
            }
            return opcua_session;
        }

        private EndpointDescriptionCollection DiscoverEndpoints(ApplicationConfiguration config, Uri discoveryUrl, int timeout)
        {
            EndpointConfiguration configuration = EndpointConfiguration.Create(config);
            configuration.OperationTimeout = timeout;

            using (DiscoveryClient client = DiscoveryClient.Create(
                discoveryUrl,
                EndpointConfiguration.Create(config)))
            {
                try
                {
                    EndpointDescriptionCollection endpoints = client.GetEndpoints(null);
                    ReplaceLocalHostWithRemoteHost(endpoints, discoveryUrl);
                    return endpoints;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Opc.Ua.Client.SampleModule: Could not fetch endpoints from url: {0}", discoveryUrl);
                    Console.WriteLine("Opc.Ua.Client.SampleModule: Reason = {0}", e.Message);
                    throw e;
                }
            }
        }

        private void ReplaceLocalHostWithRemoteHost(EndpointDescriptionCollection endpoints, Uri discoveryUrl)
        {
            foreach (EndpointDescription endpoint in endpoints)
            {
                endpoint.EndpointUrl = Utils.ReplaceLocalhost(endpoint.EndpointUrl, discoveryUrl.DnsSafeHost);
                StringCollection updatedDiscoveryUrls = new StringCollection();

                foreach (string url in endpoint.Server.DiscoveryUrls)
                {
                    updatedDiscoveryUrls.Add(Utils.ReplaceLocalhost(url, discoveryUrl.DnsSafeHost));
                }

                endpoint.Server.DiscoveryUrls = updatedDiscoveryUrls;
            }
        }

        private EndpointDescription SelectUaTcpEndpoint(EndpointDescriptionCollection endpointCollection, int prefferedSecurityLevel = -1)
        {
            EndpointDescription bestEndpoint = null;
            foreach (EndpointDescription endpoint in endpointCollection)
            {
                if (endpoint.TransportProfileUri == Profiles.UaTcpTransport)
                {
                    if (prefferedSecurityLevel >= 0 && endpoint.SecurityLevel == prefferedSecurityLevel)
                    {
                        bestEndpoint = endpoint;
                        break;
                    }

                    if ((bestEndpoint == null) ||
                        (endpoint.SecurityLevel > bestEndpoint.SecurityLevel))
                    {
                        bestEndpoint = endpoint;
                    }
                }
            }

            return bestEndpoint;
        }

        private async Task<OpcuaSessionConfig> SaveSessionConfig(string targetfile, string sesssionName, Session session)
        {
            OpcuaSessionConfig sessionConfig = null;
            if (session != null)
            {
                try
                {
                    sessionConfig = new OpcuaSessionConfig();

                    sessionConfig.timestamp = DateTime.Now;
                    sessionConfig.endpoint = session.ConfiguredEndpoint;
                    sessionConfig.sessionname = sesssionName;
                    sessionConfig.monitoredlist = new List<MonitoredNode>();
                    sessionConfig.publishinterval = session.DefaultSubscription.PublishingInterval;
                    
                    NamespaceTable namespaceTable = new NamespaceTable();
                    DataValue namespaceArrayNodeValue = session.ReadValue(VariableIds.Server_NamespaceArray);
                    namespaceTable.Update(namespaceArrayNodeValue.GetValue<string[]>(null));

                    //collect monitored list
                    foreach (MonitoredItem x in session.DefaultSubscription.MonitoredItems)
                    {
                        int trimStartIndex = x.DisplayName.IndexOf(" (");
                        var displayName = x.DisplayName.Substring(trimStartIndex + 2);
                        displayName = displayName.Remove(displayName.IndexOf(" -"));
                        var description = (trimStartIndex >= 0) ? x.DisplayName.Remove(trimStartIndex) : x.DisplayName;

                        ExpandedNodeId expendedId = NodeId.ToExpandedNodeId(x.StartNodeId, namespaceTable);
                        MonitoredNode monnode = new MonitoredNode(expendedId.ToString(), displayName, description);
                        sessionConfig.monitoredlist.Add(monnode);
                    }

                    sessionConfig.SaveToJsonFile(targetfile);
                }
                catch (Exception exception)
                {
                    GuiUtils.HandleException(String.Empty, GuiUtils.CallerName(), exception);
                    sessionConfig = null;
                }
            }
            return sessionConfig;
        }

        private object Read_Node(NodeId nodeid)
        {
            DataValueCollection results;
            DiagnosticInfoCollection diagnosticInfos;
            ReadValueIdCollection valuesToRead = new ReadValueIdCollection();

            ReadValueId valueToRead = new ReadValueId();

            valueToRead.NodeId = nodeid;
            //RUN_LED
            //valueToRead.NodeId = new NodeId("ns=2;i=99");
            valueToRead.AttributeId = Attributes.Value;
            //valueToRead.Handle = item;

            valuesToRead.Add(valueToRead);

            m_design_session.Read(
                null,
                0,
                TimestampsToReturn.Neither,
                valuesToRead,
                out results,
                out diagnosticInfos);

            object val = results.Last().Value;
            string message_value = String.Format("Read from node: '{0}'({1}) = '{2}'", "", nodeid.ToString(), val);
            //UtilLog.DefaultLog.Log(message_value);

            if (!StatusCode.IsGood(results.Last().StatusCode))
            {
                string message_status = String.Format("Error! Read from node ({0}) status code: {1}", nodeid.ToString(), results.Last().StatusCode);
                //UtilLog.DefaultLog.Log(message_status);
            }
            return val;
        }

        // manage single session only
        private async Task OpenSessionView_OpcuaClient()
        {
            try
            {
                var sessionname = "NewSession";
                if (sessionMgmtAction == SESSIONMGMT_ACTION.EDIT)
                {
                    sessionname = m_sessionConfig.sessionname;
                }
                //var session = await Opcua_EndpointConnect(sessionname, m_sessionConfig.endpoint.EndpointUrl, m_sessionConfig.endpoint.Description.SecurityLevel);
                var session = await Opcua_EndpointConnect(sessionname, m_sessionConfig.endpoint);

                if (session != null)
                {
                    m_design_session = session;

                    var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, ()=>
                    {
                        SessionsCTRL.AddNode(session);

                        gridEndpointSelector.Visibility = Visibility.Visible;
                        txtEndpointSelector.Text = m_design_session.Endpoint.EndpointUrl.ToString();
                        EndpointSelectorCTRL.Visibility = Visibility.Collapsed;

                        BrowseCTRL.IsEnabled = true;
                        SessionsCTRL.IsEnabled = true;
                        EnableSessionOpButtons(true);
                    });
                }
            }
            catch (Exception exception)
            {
                GuiUtils.HandleException(String.Empty, GuiUtils.CallerName(), exception);
            }
        }

        public void CloseSessionView_OpcuaClient(bool resetUIButton = true)
        {
            Opcua_EndpointDisconnect();

            if (resetUIButton)
            {
                var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    BrowseCTRL.Clear();
                    SessionsCTRL.Clear();
                    gridEndpointSelector.Visibility = Visibility.Collapsed;
                    EndpointSelectorCTRL.Visibility = Visibility.Visible;
                    EndpointSelectorCTRL.IsEnabled = true;
                    EnableSessionOpButtons(true);
                });
            }
        }

        private void EnableSessionOpButtons(bool enable)
        {
            btnImportCert.IsEnabled = enable;
            btnReload.IsEnabled = enable;
            btnSave.IsEnabled = enable;
            btnCancel.IsEnabled = enable;
        }

        private void btnCancel_Button_Click(object sender, RoutedEventArgs e)
        {
            EnableSessionOpButtons(false);
            CloseSessionView_OpcuaClient();
            EnableSessionOpButtons(true);
            Frame.Navigate(typeof(SessionMgmtPage));
        }

        private async void btnSave_Button_Click(object sender, RoutedEventArgs e)
        {
            EnableSessionOpButtons(false);
            OpcuaSessionConfig config = null;
            var sessionname = m_design_session.SessionName;
            const string PROFILENAME_TEMPLATE = "session_{0}.json";
            if (sessionMgmtAction == SESSIONMGMT_ACTION.NEW)
            {
                sessionname = txtSessionName.Text;

                if (String.IsNullOrEmpty(sessionname))
                {
                    MessageDialog showDialog = new MessageDialog("Session Name must not be empty or duplicated.\nPlease enter a unique session name.");
                    showDialog.Commands.Add(new UICommand("OK") { Id = 1 });
                    showDialog.DefaultCommandIndex = 1;
                    showDialog.CancelCommandIndex = 1;
                    var result = await showDialog.ShowAsync();
                    EnableSessionOpButtons(true);
                    return;
                }

                m_sessionConfig_full_path = Path.Combine(m_local, SiteProfileManager.GetFullPath(App.SiteProfileId, String.Format(PROFILENAME_TEMPLATE, sessionname)));
                while (File.Exists(m_sessionConfig_full_path))
                {
                    var timestamp = DateTime.Now.ToString("_yyMMddHHmmss");
                    sessionname = txtSessionName.Text + timestamp;
                    m_sessionConfig_full_path = Path.Combine(m_local, SiteProfileManager.GetFullPath(App.SiteProfileId, String.Format(PROFILENAME_TEMPLATE, sessionname)));
                }
            }
            config = await SaveSessionConfig(m_sessionConfig_full_path, sessionname, m_design_session);
           
            if (config != null)
            {
                try
                {
                    if (sessionMgmtAction == SESSIONMGMT_ACTION.NEW)
                    {
                        //SessionInfo sessioninfo = new SessionInfo();
                        sessionInfo.profilePath = String.Format(PROFILENAME_TEMPLATE, sessionname);
                        sessionInfo.sessionName = sessionname;
                        sessionInfo.sourceType = m_design_session.Endpoint.Server.ApplicationName.ToString();
                        SiteProfileManager.DefaultSiteManager.sessionConfig.sessions.Add(sessionInfo);
                        await SiteProfileManager.DefaultSiteManager.SaveSessionConfig();
                    }
                    CloseSessionView_OpcuaClient();
                    Frame.Navigate(typeof(SessionMgmtPage), "RELOAD");
                }
                catch (Exception ex)
                {
                }
            }
            EnableSessionOpButtons(true);
        }


        private void btnReload_Button_Click(object sender, RoutedEventArgs e)
        {
            EnableSessionOpButtons(false);
            CloseSessionView_OpcuaClient();
            // json configuration
            m_sessionConfig = OpcuaSessionConfig.LoadFromJsonFile(m_sessionConfig_full_path);

            EndpointSelectorCTRL.IsEnabled = false;
            BrowseCTRL.IsEnabled = false;
            SessionsCTRL.IsEnabled = false;

            var ignored = Task.Run(OpenSessionView_OpcuaClient);
        }

        // import Opcua server ceritifcate and save to local storage
        private async void btnImportCert_Button_Click(object sender, RoutedEventArgs e)
        {
            EnableSessionOpButtons(false);
            try
            {
                // import certificate
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                picker.FileTypeFilter.Add(".der");
                Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
                Windows.Storage.StorageFolder folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(m_cert_full_path);
                if (file != null)
                {
                    Windows.Storage.StorageFile copiedFile = await file.CopyAsync(folder, file.Name, Windows.Storage.NameCollisionOption.ReplaceExisting);
                }
            }
            catch (Exception exception)
            {
                GuiUtils.HandleException(String.Empty, GuiUtils.CallerName(), exception);
            }
            EnableSessionOpButtons(true);
        }

        private void btnAddSubscription_Click(object sender,RoutedEventArgs e)
        {

        }

        private void btnDelSubscription_Click(object sender,RoutedEventArgs e)
        {

        }

        private void btnEndpointDisconnect_Click(object sender,RoutedEventArgs e)
        {
            CloseSessionView_OpcuaClient();
        }

        private void btnMessageDlg_Click(object sender,RoutedEventArgs e)
        {
            gridMessageDlg.Visibility = Visibility.Collapsed;
        }
    }
}
