using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;

namespace TpLibrary
{
    /// <summary>
    /// Represents a plugin used to activate this library and load the editor contents.
    /// </summary>
    public class Plugin : IPlugin
    {
        /// <summary>
        /// The name of the plugin.
        /// </summary>
        public string Name => "Twilight Princess Test Editor";

        public Plugin()
        {
                  }
    }
}
