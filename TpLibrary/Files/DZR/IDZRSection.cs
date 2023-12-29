using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TpLibrary
{
    public interface IDZRSection
    {
        void Write(BinaryDataWriter writer, DZR Header);
    }
}
