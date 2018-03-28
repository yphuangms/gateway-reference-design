using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace PublisherDesignerApp
{
    public class SiteProfileManager
    {
        public const string PROFILEROOT = "Profiles";
        public const string DEFAULT_SESSIONCONFIGPATH = "SessionConfig.json";
        public const string DEFAULT_PUBLISHEDNODESPATH = "publishednodes.json";
        public static SiteProfileManager DefaultSiteProfileManager { private set; get; }
        public string SiteProfileId { private set; get; } = String.Empty;
        public SessionConfig sessionConfig { private set; get; } = new SessionConfig();

        SiteProfileManager(string profileId = null)
        {
            if (!String.IsNullOrEmpty(profileId))
                SiteProfileId = profileId;
        }

        public static void SetDefault(SiteProfileManager siteManager)
        {
            DefaultSiteProfileManager = siteManager;
        }

        public static async Task<SiteProfileManager> Open(string profileId, bool createIfNotExist = false)
        {
            if (String.IsNullOrEmpty(profileId))
                return null;

            bool isSuccess = await OpenSiteProfileFolder(profileId, createIfNotExist);
            if (isSuccess)
            {
                SiteProfileManager siteManager = new SiteProfileManager(profileId);
                isSuccess = await siteManager.LoadSessionConfig();
                if (isSuccess)
                {
                    Debug.WriteLine("InitSiteManager success!");
                }
                else
                {
                    Debug.WriteLine("InitSiteManager failed!");
                }
                return siteManager;
            }
            return null;
        }

        public static async Task<bool> OpenSiteProfileFolder(string profileid, bool createIfNotExist = false)
        {
            bool isExisted = false;
            StorageFolder baseFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            try
            {
                if (createIfNotExist)
                {
                    StorageFolder sitesFolder = await baseFolder.CreateFolderAsync(PROFILEROOT, CreationCollisionOption.OpenIfExists);
                    StorageFolder siteFolder = await sitesFolder.CreateFolderAsync(profileid, CreationCollisionOption.OpenIfExists);
                    if (siteFolder != null)
                    {
                        StorageFile settingsFile = null;
                        try
                        {
                            settingsFile = await siteFolder.GetFileAsync(DEFAULT_SESSIONCONFIGPATH);
                        }
                        catch (Exception e)
                        {
                            Utils.Trace(e, "SiteProfileManager.OpenSiteProfileFolder() Exception: " + e.Message);
                        }

                        if (settingsFile == null)
                        {
                            SiteProfileManager siteManager = new SiteProfileManager(App.SiteProfileId);
                            await siteManager.SaveSessionConfig(null);
                        }

                        isExisted = true;
                    }
                }
                else
                {
                    StorageFolder sitesFolder = await baseFolder.GetFolderAsync(PROFILEROOT);
                    StorageFolder siteFolder = await sitesFolder.GetFolderAsync(profileid);
                    if (siteFolder != null)
                    {
                        StorageFile settingsFile = null;
                        try
                        {
                            settingsFile = await siteFolder.GetFileAsync(DEFAULT_SESSIONCONFIGPATH);
                            if (settingsFile != null)
                                isExisted = true;
                        }
                        catch (Exception e)
                        {
                            Utils.Trace(e, "SiteProfileManager.OpenSiteProfileFolder() Exception: " + e.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.Trace(ex, "SiteProfileManager.OpenSiteProfileFolder() Exception: " + ex.Message);
            }
            return isExisted;
        }

        public static string GetFullPath(string profileid, string filename)
        {
            string path = String.Format(PROFILEROOT + @"\{0}\{1}", profileid, filename);
            return path;
        }

        public async Task<bool> LoadSessionConfig(string filePath = "")
        {
            bool isSuccess = false;
            try
            {
                string path = filePath;

                if (String.IsNullOrEmpty(path))
                    path = DEFAULT_SESSIONCONFIGPATH;

                SessionConfig sessionconfig = sessionConfig;

                if (!String.IsNullOrEmpty(path))
                    sessionconfig = await SerializationUtil.LoadFromJsonFile<SessionConfig>(GetFullPath(SiteProfileId, path)) as SessionConfig;

                if (sessionconfig != null)
                {
                    sessionConfig = sessionconfig;
                    isSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SiteMangager.LoadSessionConfig() Exception! " + ex.Message);
            }
            return isSuccess;
        }
                
        public async Task<bool> SaveSessionConfig(string filePath = null)
        {
            bool isSuccess = false;
            try
            {
                string path = filePath;

                if (String.IsNullOrEmpty(path))
                    path = DEFAULT_SESSIONCONFIGPATH;

                // save session config
                isSuccess = await SerializationUtil.SaveToJsonFile(GetFullPath(SiteProfileId, path), sessionConfig);
            }
            catch (Exception ex)
            {
                Utils.Trace(ex, "SiteProfileManager.SaveSessionConfig() Exception: " + ex.Message);
            }
            return isSuccess;
        }

        public async Task<bool> SavePublishedNodes(string filePath = null)
        {
            bool isSuccess = false;
            try
            {
                string path = filePath;

                if (String.IsNullOrEmpty(path))
                    path = DEFAULT_PUBLISHEDNODESPATH;

                List<PublisherConfigurationFileEntry> publisherConfig = new List<PublisherConfigurationFileEntry>(sessionConfig.sessions.Count);

                foreach (var session in sessionConfig.sessions)
                {
                    PublisherConfigurationFileEntry configEntry = new PublisherConfigurationFileEntry();
                    var settingpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, GetFullPath(SiteProfileId, session.profilePath));
                    var sessionSetting = OpcuaSessionConfig.LoadFromJsonFile(settingpath);

                    configEntry.EndpointUri = sessionSetting.endpoint.EndpointUrl;
                    configEntry.UseSecurity = (sessionSetting.endpoint.Description.SecurityMode != Opc.Ua.MessageSecurityMode.None);
                    configEntry.OpcNodes = new List<OpcNodeOnEndpointUrl>(sessionSetting.monitoredlist.Count);

                    foreach (var item in sessionSetting.monitoredlist)
                    {
                        OpcNodeOnEndpointUrl node = new OpcNodeOnEndpointUrl()
                        { 
                            ExpandedNodeId = item.nodeid,
                            Name = item.description,
                            Tag = item.displayname
                        };
                        configEntry.OpcNodes.Add(node);
                    }
                    publisherConfig.Add(configEntry);
                }
                // save session config
                isSuccess = await SerializationUtil.SaveToJsonFile(GetFullPath(SiteProfileId, path), publisherConfig);
            }
            catch (Exception ex)
            {
                Utils.Trace(ex, "SiteProfileManager.SavePublishedNodes() Exception: " + ex.Message);
            }
            return isSuccess;
        }

        // save to file in OpcPublisher legacy publishernodes format
        public async Task<bool> SavePublisherNodesLegacy(string filePath = null)
        {
            bool isSuccess = false;
            try
            {
                string path = filePath;

                if (String.IsNullOrEmpty(path))
                    path = DEFAULT_PUBLISHEDNODESPATH;

                List<PublishedNode> publisherNodes = new List<PublishedNode>(sessionConfig.sessions.Count);
                foreach (var session in sessionConfig.sessions)
                {
                    var settingpath = Path.Combine(ApplicationData.Current.LocalFolder.Path, GetFullPath(SiteProfileId, session.profilePath));
                    var sessionSetting = OpcuaSessionConfig.LoadFromJsonFile(settingpath);

                    foreach (var item in sessionSetting.monitoredlist)
                    {
                        PublishedNode node = new PublishedNode()
                        {
                            EndpointUrl = sessionSetting.endpoint.EndpointUrl.ToString(),
                            NodeId = new PublishedNodeId()
                            {
                                Identifier = item.nodeid
                            },
                            Name = item.description,
                            Tag = item.displayname,
                            UseSecurity = (sessionSetting.endpoint.Description.SecurityMode != Opc.Ua.MessageSecurityMode.None)
                        };
                        publisherNodes.Add(node);
                    }
                }
                // save session config
                isSuccess = await SerializationUtil.SaveToJsonFile(GetFullPath(SiteProfileId, path), publisherNodes);
            }
            catch (Exception ex)
            {
                Utils.Trace(ex, "SiteProfileManager.SavePublisherNodesLegacy() Exception: " + ex.Message);
            }
            return isSuccess;
        }
    }
}
