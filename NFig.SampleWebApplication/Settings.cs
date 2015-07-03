// ReSharper disable UnusedAutoPropertyAccessor.Local

using System.Collections.Generic;
using System.ComponentModel;

namespace NFig.SampleWebApplication
{
    public class Settings : INFigSettings<Tier, DataCenter>
    {
        public string ApplicationName { get; set; }
        public string Commit { get; set; }
        public Tier Tier { get; set; }
        public DataCenter DataCenter { get; set; }

        [SettingsGroup]
        public HomeSettings Home { get; private set; }

        public class HomeSettings
        {
            [Setting(42)]
            [TieredDefaultValue(Tier.Prod, 23)]
            [Description("What is your favorite number? **Did you know you can use markdown in descriptions?**")]
            public int FavoriteNumber { get; private set; }

            [Setting(@"
http://github.com
http://stackoverflow.com
http://google.com
")]
            [StringsByLine]
            [Description("Useful links to show on the home page (one per line). Probably want to keep [Stack Overflow](http://stackoverflow.com) in this list.")]
            public List<string> UsefulLinks { get; private set; }

            [Setting("Editable Box")]
            [Description("Sets the title for the third small box on the home page.")]
            public string ThirdBoxTitle { get; private set; }

            [Setting("The entire content of this box is editable via the settings page.")]
            [Description("Sets the content for the third small box on the home page.")]
            public string ThirdBoxContent { get; private set; }
        }

        [SettingsGroup]
        public ContactSettings Contact { get; private set; }

        public class ContactSettings
        {
            [Setting(true)]
            [Description("True to include the Contact page in the top nav bar.")]
            public bool IncludeInNav { get; private set; }
        }

        [SettingsGroup]
        public NFigSettings NFig { get; private set; }

        public class NFigSettings
        {
            [Setting(60)]
            [RequiresRestart]
            [Description("How often (in seconds) to check for settings updates (useful in case Redis pub/sub fails).")]
            public int PollingInterval { get; private set; }
        }
    }
}