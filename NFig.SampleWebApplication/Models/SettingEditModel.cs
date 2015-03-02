using System.Collections.Generic;

namespace NFig.SampleWebApplication.Models
{
    public class SettingEditModel
    {
        public SettingInfo<Tier, DataCenter> SettingInfo { get; set; }
        public string Value { get; set; }
        public string DescriptionHtml { get; set; }
        public bool IsInvalid { get; set; }
        public Tier EditingTier { get; set; }
        public DataCenter EditingDataCenter { get; set; }
        public bool RequiresRestart { get; set; }

        public IList<DataCenter> AvailableDataCenters { get; set; }
    }
}