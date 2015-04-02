# NFig Sample Web Application

This project is an example of how to use [NFig](https://github.com/NFig/NFig) and includes a sample web interface for updating settings.

## Requirements

### Redis

This sample uses [NFig.Redis](https://github.com/NFig/NFig.Redis), so you'll need [Redis](http://redis.io/) running. Redis doesn't officially support Windows, but there is a [Microsoft port](https://github.com/MSOpenTech/redis/releases) you can use for local development (just run the `redis-server.exe`).

## Walkthrough

### Project Setup

The project is based on the standard ASP.NET MVC 5 template which comes pre-installed with things like Bootstrap, jQuery, MVC, Razor, etc. Additionally, the [NFig.Redis package](https://www.nuget.org/packages/NFig.Redis/) has been installed via nuget (currently only available via a pre-release version), and [CommonMark.NET](https://www.nuget.org/packages/CommonMark.NET/) is installed to enable markdown rendering in setting descriptions.

### Tier and Data Center

NFig was designed to be Tier and Data Center aware; therefore, you need to define enums for both in your application. This is done in [Config.cs](https://github.com/NFig/SampleWebApplication/blob/master/NFig.SampleWebApplication/Config.cs#L8):

```csharp
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
```

You should **ALWAYS** define an `Any = 0` element for both enums. When setting defaults or overrides, NFig assumes a zero value for tier or data center means "this setting is valid for any [tier|datacenter]." The zero value **should not** represent an _actual_ tier or data center.

### Settings Class

All of the example settings are defined in [NFig.SampleWebApplication/Settings.cs](https://github.com/NFig/SampleWebApplication/blob/master/NFig.SampleWebApplication/Settings.cs).

This class must implement the [INFigSettings<TTier, TDataCenter>](https://github.com/NFig/NFig/blob/master/NFig/INFigSettings.cs) interface. The type arguments are the names of your tier and data center enums.

```csharp
public class Settings : INFigSettings<Tier, DataCenter>
{
}
```

The first four properties are required by the interface.

```csharp
public string ApplicationName { get; set; }
public string Commit { get; set; }
public Tier Tier { get; set; }
public DataCenter DataCenter { get; set; }
```

You could define top level settings as properties on the `Settings` class itself, but most likely you'll want to break them up into groups. The best way to do this is to create a nested-class inside the Settings class, then make a property of that type, and mark it with the `SettingsGroup` attribute. Here's an example:

```csharp
[SettingsGroup]
public ContactSettings Contact { get; private set; }

public class ContactSettings
{
	[Setting(true)]
	[Description("True to include the Contact page in the top nav bar.")]
	public bool IncludeInNav { get; private set; }
}
```

Each individual setting must be marked with the [Setting](https://github.com/NFig/NFig/blob/master/NFig/SettingAttribute.cs) attribute. It takes a single argument which represents the default value for the setting. 

Additionally, it is best practice to use a [Description](https://msdn.microsoft.com/en-us/library/system.componentmodel.descriptionattribute) attribute (from `System.ComponentModel`) to give the other humans on your team a meaningful description of what the setting controls.

### Loading Settings

The Settings class defines the settings themselves, but you still need to setup NFig in your application. In our example, this is done in the static [Config](https://github.com/NFig/SampleWebApplication/blob/master/NFig.SampleWebApplication/Config.cs#L25) class. You could also make a version which is dependency-injectable instead.

`Config` has a field for [NFigRedisStore](https://github.com/NFig/NFig.Redis/blob/master/NFig.Redis/NFigRedisStore.cs) (which inherits from [NFigAsyncStore](https://github.com/NFig/NFig/blob/master/NFig/NFigStore.cs#L41)), and a `Settings` property which we'll use to access the current settings object throughout the app.

```csharp
private static readonly NFigRedisStore<Settings, Tier, DataCenter> s_store;
public static Settings Settings { get; private set; }
```

Config's static constructor first loads a few values from Web.config's AppSettngs section. It reads `ApplicationName`, `Tier`, `DataCenter`, and `RedisConnectionString`. These are values we need in order to setup NFig.Redis.

Next, the constructor creates a settings store. The second argument (`0` in this example) is the Redis database you want to use.

```csharp
s_store = new NFigRedisStore<Settings, Tier, DataCenter>(RedisConnectionString, 0);
```

Then, we subscribe to live updates (implemented in NFig.Redis via a [Redis pub/sub](http://redis.io/topics/pubsub)).

```csharp
s_store.SubscribeToAppSettings(ApplicationName, Tier, DataCenter, OnSettingsUpdate);
```

Now the [OnSettingsUpdate](https://github.com/NFig/SampleWebApplication/blob/master/NFig.SampleWebApplication/Config.cs#L90) method will be called every time a change is made to settings (even if that change is made on a different machine). That method will receive a new instance of the `Settings` object, which it will assign to the `Config.Settings` property.

Next, we load the settings for the first time by calling `ReloadSettings()` which is implemented as:

```csharp
private static void ReloadSettings()
{
	OnSettingsUpdate(null, s_store.GetApplicationSettings(ApplicationName, Tier, DataCenter), s_store);
}
```

This works because we're calling `s_store.GetApplicationSettings()` with the application name, tier, and data center that we loaded from Web.config, and it returns a `Settings` object, which we pass into the same `OnSettingsUpdate` method that we're using as the subscribe callback.

The last thing that that the constructor does is setup a fallback in case the Redis pub/sub fails. We setup a timer which polls for settings changes, and reloads the settings if it finds any.

```csharp
var interval = TimeSpan.FromSeconds(Settings.NFig.PollingInterval);
s_settingsPollTimer = new Timer(o => CheckForSettingUpdates(), null, interval, interval);
```

```csharp
public static void CheckForSettingUpdates()
{
	if (!s_store.IsCurrent(Settings))
		ReloadSettings();
}
```

> Eventually this polling behavior will likely be implemented in NFig.Redis, but for now you have to build it outside the library.

### Setting Converters

All settings, regardless of type, are stored as strings. Therefore, every type needs to have a conversion to and from strings. NFig has built-in support for `bool`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `string`, and `char` types. If you want to use another type for a setting, then you need to build a converter for it which implements [ISettingConverter<T>](https://github.com/NFig/NFig/blob/master/NFig/SettingConverterAttribute.cs#L27).

An example of this is in [SettingsHelpers.cs](https://github.com/NFig/SampleWebApplication/blob/master/NFig.SampleWebApplication/SettingsHelpers.cs). This example converter converts a multi-line string to a `List<string>` and back.

```csharp
public class StringsByLineConverter : ISettingConverter<List<string>>
{
	public List<string> GetValue(string str)
	{
		return str.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
	}

	public string GetString(List<string> list)
	{
		return String.Join("\n", list);
	}
}
```

We also need to create an attribute which inherits from [SettingConverterAttribute](https://github.com/NFig/NFig/blob/master/NFig/SettingConverterAttribute.cs#L6) in order to use it.

```csharp
public class StringsByLineAttribute : SettingConverterAttribute
{
	public StringsByLineAttribute() : base(typeof(StringsByLineConverter)) { }
}
```

Now, we can create a setting which uses this converter by marking the setting with the attribute we just created. There is an example in [Settings.cs](https://github.com/NFig/SampleWebApplication/blob/master/NFig.SampleWebApplication/Settings.cs#L25):

```csharp
[Setting(@"
http://github.com
http://stackoverflow.com
http://google.com
")]
[StringsByLine]
[Description("Useful links to show on the home page (one per line).")]
public List<string> UsefulLinks { get; private set; }
```

If we wanted this to be the default converter for any setting of type `List<string>` then we can use the optional `additionalDefaultConverters` parameter of the `NFigRedisStore` constructor.

```csharp
var converters = new Dictionary<Type, SettingConverterAttribute>
{
	{typeof(List<string>), new StringsByLineAttribute()}
};

store = new NFigRedisStore<Settings, Tier, DataCenter>(RedisConnectionString, 0, converters);
```

Then we wouldn't need to use the `[StringsByLine]` attribute on `List<string>` settings.

### Specific Defaults

Sometimes you may want to specify a default value which is specific to a data center or tier (or both). This can be accomplished using attributes which inherit from [DefaultValueAttribute](https://github.com/NFig/NFig/blob/master/NFig/DefaultValueAttribute.cs). See [NFig.SampleWebApplication/NfigAttributes.cs](https://github.com/NFig/SampleWebApplication/blob/master/NFig.SampleWebApplication/NfigAttributes.cs) (which is generated from a [T4 template](https://github.com/NFig/SampleWebApplication/blob/master/NFig.SampleWebApplication/NfigAttributes.tt) for examples of such attributes (`DataCenterDefaultValueAttribute`, `TieredDefaultValueAttribute`, and `TieredDataCenterDefaultValueAttribute`).

Then, simply apply the attribute to a setting:

```csharp
[Setting(42)]
[TieredDefaultValue(Tier.Prod, 23)]
public int FavoriteNumber { get; private set; }
```

### Web Interface

This sample application includes a mobile-friendly admin panel for viewing and editing settings. Clicking on the "Settings" tab shows a list of all the settings and their current active values. Settings which have an override are highlighted in blue.  

![Settings List](https://github.com/NFig/SampleWebApplication/blob/master/screenshots/SettingsList.png)

When you click on a setting, it takes you to a details page which has information about the active values in the various data centers, and allows you to set or clear overrides.

![Setting Page](https://github.com/NFig/SampleWebApplication/blob/master/screenshots/SettingPage.png)

Notice that you can set an override for any data center, but only for the current tier. Generally speaking, it wouldn't make sense to try to edit another tier's overrides, so it's best for the admin panel to enforce this behavior.

The relevant code for the admin panel is in the [SettingsController](https://github.com/NFig/SampleWebApplication/blob/master/NFig.SampleWebApplication/Controllers/SettingsController.cs), [Settings Views](https://github.com/NFig/SampleWebApplication/tree/master/NFig.SampleWebApplication/Views/Settings), and a little bit of CSS in [Site.css](https://github.com/NFig/SampleWebApplication/blob/master/NFig.SampleWebApplication/Content/Site.css). If your application already uses MVC and Bootstrap, it should take little more than than copy-pasting those files to get started.
