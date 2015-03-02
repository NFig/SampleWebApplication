using System;
using System.Collections.Generic;
using System.Linq;

namespace NFig.SampleWebApplication
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RequiresRestartAttribute : Attribute { }

    public class StringsByLineAttribute : SettingConverterAttribute
    {
        public StringsByLineAttribute() : base(typeof(StringsByLineConverter)) { }
    }

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
}