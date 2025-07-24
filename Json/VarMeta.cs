using System.Collections.Generic;
using Newtonsoft.Json;

namespace Vrm.Json
{
    public class VarMeta
    {
        [JsonProperty("licenseType")]
        public string LicenseType { get; set; }

        [JsonProperty("creatorName")]
        public string CreatorName { get; set; }

        [JsonProperty("packageName")]
        public string PackageName { get; set; }

        [JsonProperty("standardReferenceVersionOption")]
        public string StandardReferenceVersionOption { get; set; }

        [JsonProperty("scriptReferenceVersionOption")]
        public string ScriptReferenceVersionOption { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("credits")]
        public string Credits { get; set; }

        [JsonProperty("instructions")]
        public string Instructions { get; set; }

        [JsonProperty("promotionalLink")]
        public string PromotionalLink { get; set; }

        [JsonProperty("programVersion")]
        public string ProgramVersion { get; set; }

        [JsonProperty("contentList")]
        public List<string> ContentList { get; set; }

        [JsonProperty("dependencies")]
        public Dictionary<string, DependencyNode> Dependencies { get; set; }

        [JsonProperty("customOptions")]
        public Dictionary<string, string> CustomOptions { get; set; }

        [JsonProperty("hadReferenceIssues")]
        public bool HadReferenceIssues { get; set; }

        [JsonProperty("referenceIssues")]
        public List<ReferenceIssue> ReferenceIssues { get; set; }
    }

    public class DependencyNode
    {
        [JsonProperty("licenseType")]
        public string LicenseType { get; set; }

        [JsonProperty("missing")]
        public string Missing { get; set; }

        [JsonProperty("dependencies")]
        public Dictionary<string, DependencyNode> Dependencies { get; set; }
    }

    public class ReferenceIssue
    {
        [JsonProperty("reference")]
        public string Reference { get; set; }

        [JsonProperty("issue")]
        public string Issue { get; set; }
    }
}
