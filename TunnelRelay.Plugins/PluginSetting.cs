using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TunnelRelay.PluginEngine
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class PluginSetting : Attribute
    {
        public PluginSetting(string displayName, string helpText)
        {
        }

        public string DisplayName { get; private set; }

        public string HelpText { get; private set; }
    }
}
