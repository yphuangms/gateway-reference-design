using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace PublisherDesignerApp
{
    /// <summary>
    /// Monitored opcua node description
    /// </summary>
    public class MonitoredNode
    {
        public string nodeid { get; set; }
        public string displayname { get; set; }
        public string description { get; set; }
        public MonitoredNode(string id, string disname, string desc)
        {
            nodeid = id;
            displayname = disname;
            description = desc;
        }
    }

    /// <summary>
    /// Opcua session settings
    /// </summary>
    public class OpcuaSessionConfig
    {
        public static ApplicationInstance OpcuaApplication;

        public static async Task<bool> Init()
        {
            bool isSuccess = false;

            if (OpcuaApplication != null) return true;

            // helper to let Opc.Ua Utils find the localFolder in the environment
            Utils.DefaultLocalFolder = ApplicationData.Current.LocalFolder.Path;
            //ApplicationInstance.MessageDlg = new ApplicationMessageDlg();

            ApplicationInstance application = new ApplicationInstance();
            application.ApplicationName = "Sample.PublisherDesignerApp";
            application.ApplicationType = ApplicationType.Client;
            application.ConfigSectionName = "PublisherDesignerApp";

            try
            {
                // load the application configuration.
                await application.LoadApplicationConfiguration(false);

                // check the application certificate.
                await application.CheckApplicationInstanceCertificate(false, 0);

                OpcuaApplication = application;

                isSuccess = true;
            }
            catch (Exception ex)
            {
                Utils.Trace(ex, "OpcuaSession.Init() Exception: " + ex.Message);
            }
            return isSuccess;
        }

        public DateTime timestamp { get; set; }
        public ConfiguredEndpoint endpoint { get; set; }
        public string sessionname { get; set; }
        public List<MonitoredNode> monitoredlist { get; set; }
        public int publishinterval { get; set; }

        public void SaveToJsonFile(string path)
        {
            JsonConverter[] ConvList = new JsonConverter[]
            {
                new SessionConfigConverter()
                //new StringEnumConverter { CamelCaseText = true }
            };
            string output = JsonConvert.SerializeObject(this, ConvList);
            File.WriteAllText(path, output);
        }
        public static OpcuaSessionConfig LoadFromJsonFile(string path)
        {
            OpcuaSessionConfig result = null;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                if (json != null)
                {
                    JsonConvert.DefaultSettings = (() =>
                    {
                        var settings = new JsonSerializerSettings();
                        settings.Converters.Add(new StringEnumConverter { CamelCaseText = true });
                        return settings;
                    });
                    try
                    {
                        result = JsonConvert.DeserializeObject<OpcuaSessionConfig>(json);
                    }
                    catch (Exception ex)
                    {
                        Utils.Trace(ex, "OpcuaSessionConfig.LoadFromJsonFile() Exception: " + ex.Message);
                    }
                }
            }
            return result;
        }

        class SessionConfigConverter : JsonConverter
        {
            private List<string> m_export_properties = new List<string>()
            {
            "['timestamp']",
            "['endpoint'].['Endpoint'].['EndpointUrl']",
            "['endpoint'].['Endpoint'].['SecurityMode']",
            "['endpoint'].['Endpoint'].['SecurityPolicyUri']",
            "['endpoint'].['Endpoint'].['UserIdentityTokens'][*]",
            "['endpoint'].['Endpoint'].['TransportProfileUri']",
            "['endpoint'].['Endpoint'].['SecurityLevel']",
            "['endpoint'].['UpdateBeforeConnect']",
            "['endpoint'].['SelectedUserTokenPolicy']",
            "['sessionname']",
            "['publishinterval']",
            "['monitoredlist'].[*]"
            };
        
            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(OpcuaSessionConfig));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                // just a shell of JObject
                JObject jo = JObject.Parse(@"{""endpoint"":{""Endpoint"":{}}}");
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter());
                JToken root = JToken.FromObject(value, JsonSerializer.Create(settings));
                foreach (string prop in m_export_properties)
                {
                    JArray jarr = new JArray();
                    var tokens = root.SelectTokens(prop);
                    bool is_array = prop[prop.Length - 2] == '*';
                    string name;
                    JObject obj = getObjectbyPath(jo, prop, out name);
                    foreach (JToken t in tokens)
                    {
                        if (!is_array)
                        {
                            obj.Add(name, t);
                        }
                        else
                        {
                            jarr.Add(t);
                        }
                    }
                    if (is_array)
                        obj.Add(name, jarr);
                }
                jo.WriteTo(writer);
            }
            private JObject getObjectbyPath(JObject obj, string path, out string name)
            {
                JObject result = obj;
                string[] subpath = path.Split("[]'.*".ToCharArray());
                subpath = subpath.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                int i = 0;
                for (; i < subpath.Length - 1; i++)
                {
                    result = result[subpath[i]] as JObject;
                }
                name = subpath[i];
                return result;
            }
        }
    }
}
