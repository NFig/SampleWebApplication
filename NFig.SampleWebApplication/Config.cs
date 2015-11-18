using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

using NFig.Redis;
using System.Threading.Tasks;

using NFig.UI;


namespace NFig.SampleWebApplication
{
    public enum Tier
    {
        Any = 0,
        Local = 1,
        Dev = 2,
        Prod = 3,
    }

    public enum DataCenter
    {
        Any = 0,
        Local = 1,
        UsEastCoast = 2,
        UsWestCoast = 3,
        Europe = 4,
    }

    public static class Config
    {
        private static readonly NFigRedisStore<Settings, Tier, DataCenter> s_store;

        public static readonly string ApplicationName;
        public static readonly Tier Tier;
        public static readonly DataCenter DataCenter;
        public static readonly string RedisConnectionString;

        public static Settings Settings { get; private set; }
        public static NFigAsyncStore<Settings, Tier, DataCenter> NFigAsyncStore { get { return s_store; } }

        public static event Action<Settings> SettingsUpdated;

        static Config()
        {
            // load fixed data from Web.config
            ApplicationName = ConfigurationManager.AppSettings["ApplicationName"];
            if (string.IsNullOrEmpty(ApplicationName))
            {
                throw new Exception("ApplicationName must be provided in Web.Config AppSettings");
            }

            if (!Enum.TryParse(ConfigurationManager.AppSettings["Tier"], out Tier))
            {
                throw new Exception("Tier must be provided in Web.Config AppSettings");
            }

            if (!Enum.TryParse(ConfigurationManager.AppSettings["DataCenter"], out DataCenter))
            {
                throw new Exception("DataCenter must be provided in Web.Config AppSettings");
            }

            RedisConnectionString = ConfigurationManager.AppSettings["RedisConnectionString"];
            if (string.IsNullOrEmpty(RedisConnectionString))
            {
                throw new Exception("RedisConnectionString must be provided in Web.Config AppSettings");
            }

            // create settings store
            s_store = new NFigRedisStore<Settings, Tier, DataCenter>(RedisConnectionString, 0);

            // subscribe to updates
            // also loads initial settings as OnSettingsUpdate is called during subscribe
            s_store.SubscribeToAppSettings(ApplicationName, Tier, DataCenter, OnSettingsUpdate);
        }

        private static void OnSettingsUpdate(Exception ex, Settings settings, NFigRedisStore<Settings, Tier, DataCenter> store)
        {
            if (ex != null)
            {
                // todo: log this exception
            }
            else
            {
                Settings = settings;

                // call an updated event so any part of our application can subscribe
                SettingsUpdated?.Invoke(settings);
            }
        }

        public static IList<DataCenter> GetAvailableDataCenters()
        {
            IEnumerable<DataCenter> dcs = (DataCenter[])Enum.GetValues(typeof(DataCenter));
            dcs = Tier == Tier.Local
                ? dcs.Where(d => d == DataCenter.Any || d == DataCenter.Local)
                : dcs.Where(d => d != DataCenter.Local);

            return dcs.ToList();
        }


        public static Task<string> GetSettingsJsonAsync()
        {
            return NFigAsyncStore.GetSettingsJsonAsync(
                ApplicationName,
                Tier,
                DataCenter,
                GetAvailableDataCenters());
        }

        public static async Task<bool> AllowsOverrideFor(string settingName, DataCenter dataCenter)
        {
            var info = await NFigAsyncStore.GetSettingInfoAsync(ApplicationName, settingName);
            return info.CanSetOverrideFor(Tier, dataCenter);
        }


        public static async Task<string> GetSettingJsonAsync(string settingName)
        {
            return await NFigAsyncStore.GetSettingJsonAsync(
                ApplicationName,
                settingName,
                Tier,
                DataCenter,
                GetAvailableDataCenters());
        }
    }
}