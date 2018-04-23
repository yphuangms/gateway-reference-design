using Newtonsoft.Json;
using Opc.Ua;
using System.Collections.Generic;



namespace PublisherDesignerApp
{
    using System;
    using System.ComponentModel;

    #region legacy publishing node format
    public class PublisherNodeId
    {
        public string Identifier { get; set; }

    }

    public class PublisherNode
    {
        public string EndpointUrl { get; set; }
        public bool UseSecurity { get; set; }
        public PublisherNodeId NodeId { get; set; }

        // extra
        public string Tag { get; set; }
        public string Name { get; set; }
    }
    #endregion

    /// <summary>
    /// Class describing a list of nodes in the ExpandedNodeId format
    /// </summary>
    public class OpcNodeOnEndpointUrl
    {
        public string ExpandedNodeId;

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public string Name;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
        public int? OpcSamplingInterval;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
        public int? OpcPublishingInterval;

        [DefaultValue(null)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
        public string Tag;
    }

    /// <summary>
    /// Class describing the nodes which should be published. It supports three formats:
    /// - NodeId syntax using the namespace index (ns) syntax
    /// - ExpandedNodeId syntax, using the namespace URI (nsu) syntax
    /// - List of ExpandedNodeId syntax, to allow putting nodes with similar publishing and/or sampling intervals in one object
    /// </summary>
    public partial class PublisherConfigurationFileEntry
    {
        public PublisherConfigurationFileEntry()
        {
        }

        [JsonProperty("EndpointUrl")]
        public Uri EndpointUri { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? UseSecurity { get; set; }


        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<OpcNodeOnEndpointUrl> OpcNodes { get; set; }
    }
}