#if BAKERY_INCLUDED
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace VRWorldToolkit.Editor
{
    public class BakeryCompat
    {
        public static IEnumerable<GameObject> GetBakeryLights()
        {
            var names = new[] { "BakeryDirectLight", "BakeryPointLight", "BakerySkyLight" };
            foreach (var name in names)
            {
                var type = Helper.GetTypeFromName(name);
                if (type == null) continue;

                var objects = UnityEngine.Object.FindObjectsOfType(type);
                foreach (var obj in objects)
                {
                    if (obj is Component component && component != null && component.gameObject != null) yield return component.gameObject;
                }
            }
        }

        public static (object renderSettingsStorage, Type ftRenderLightmap) TryGetSettings()
        {
            var ftRenderLightmap = Helper.GetTypeFromName("ftRenderLightmap");
            if (ftRenderLightmap == null) return (null, null);

            var method = ftRenderLightmap.GetMethod("FindRenderSettingsStorage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            var renderSettingsStorage = method?.Invoke(null, new object[method?.GetParameters()?.Length ?? 0]);
            return (renderSettingsStorage, ftRenderLightmap);
        }

        public static bool IsRenderDirRNMOrSH(object renderSettingsStorage, Type ftRenderLightmap)
        {
            if (renderSettingsStorage == null || ftRenderLightmap == null) return false;

            var field = renderSettingsStorage.GetType().GetField("renderSettingsRenderDirMode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var value = field?.GetValue(renderSettingsStorage);
            if (value == null) return false;

            var enumType = ftRenderLightmap.GetNestedType("RenderDirMode", BindingFlags.Public | BindingFlags.NonPublic);
            if (enumType == null) return false;

            var name = Enum.GetName(enumType, value);
            return name is "RNM" or "SH";
        }

        public static bool UsesXatlas(object renderSettingsStorage)
        {
            if (renderSettingsStorage == null) return false;
            var field = renderSettingsStorage.GetType().GetField("renderSettingsUnwrapper", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var value = field?.GetValue(renderSettingsStorage);
            return value is int i && i == 1;
        }
    }
}
#endif