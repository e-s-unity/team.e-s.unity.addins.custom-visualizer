#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Es.Unity.Addins.CustomInspectors
{
    internal static class CustomInspectorUtls
    {
        internal static UnityEngine.Color ToUnityColor(this System.Drawing.Color drawingColor) {
            return new UnityEngine.Color() {
                a = drawingColor.A.ToScale(),
                r = drawingColor.R.ToScale(),
                g = drawingColor.G.ToScale(),
                b = drawingColor.B.ToScale(),
            };
        }

        internal static System.Drawing.Color ToSystemColor(this UnityEngine.Color unityColor) {
            return System.Drawing.Color.FromArgb(unityColor.a.ToByte(), unityColor.r.ToByte(), unityColor.g.ToByte(), unityColor.b.ToByte());
        }

        private static float ToScale(this byte b) => b / (float)byte.MaxValue;

        private static byte ToByte(this float sc) => (byte)(sc * byte.MaxValue);


    }
}
