using System.Reflection;
using UnityEditor;
using UnityEngine.Assertions;

namespace VRWorldToolkit
{
    /// <summary>
    /// Utility for setting and getting internal model importer values
    /// </summary>
    public static class ModelImporterUtil
    {
        private static readonly System.Type systemType;
        private static PropertyInfo mProperty_LegacyBlendShapeNormals;

        static ModelImporterUtil()
        {
            systemType = Assembly.Load("UnityEditor.dll").GetType("UnityEditor.ModelImporter");
            Assert.IsNotNull(systemType);
        }

        public static bool GetLegacyBlendShapeNormals(ModelImporter importer)
        {
            if (mProperty_LegacyBlendShapeNormals == null)
                mProperty_LegacyBlendShapeNormals = systemType.GetProperty("legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(mProperty_LegacyBlendShapeNormals);
            return (bool)mProperty_LegacyBlendShapeNormals.GetValue(importer);
        }

        public static void SetLegacyBlendShapeNormals(ModelImporter importer, bool value)
        {
            if (mProperty_LegacyBlendShapeNormals == null)
                mProperty_LegacyBlendShapeNormals = systemType.GetProperty("legacyComputeAllNormalsFromSmoothingGroupsWhenMeshHasBlendShapes", BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.IsNotNull(mProperty_LegacyBlendShapeNormals);
            mProperty_LegacyBlendShapeNormals.SetValue(importer, value, null);
        }
    }
}