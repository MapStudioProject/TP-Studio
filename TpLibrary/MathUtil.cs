using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TpLibrary
{
    public static class MathUtil
    {
        public static Vector3 ToDegrees(this Vector3 v)
        {
            Vector3 rot = new Vector3();
            rot.X = (v.X / 32768f) * 180f;
            rot.Y = (v.Y / 32768f) * 180f;
            rot.Z = (v.Z / 32768f) * 180f;

            return rot;
        }

        public static Vector3 FromDegrees(this Vector3 v)
        {
            Vector3 rot = new Vector3();
            rot.X = (short)(v.X * 32768f * 180f);
            rot.Y = (short)(v.Y * 32768f * 180f);
            rot.Z = (short)(v.Z * 32768f * 180f);
            return rot;
        }
    }
}
