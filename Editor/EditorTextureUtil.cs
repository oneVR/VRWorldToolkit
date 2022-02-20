using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// <see cref="UnityEditor.TextureUtil"/> Accessor
/// </summary>
/// <author>Seibe TAKAHASHI</author>
/// <remarks>
/// (c) 2017 Seibe TAKAHASHI.
/// This code is released under the MIT License.
/// http://opensource.org/licenses/mit-license.php
/// </remarks>

namespace VRWorldToolkit
{
    public static class EditorTextureUtil
    {
        private static readonly System.Type cType;
        private static MethodInfo mMethod_GetMipmapCount;
        private static MethodInfo mMethod_GetTextureFormat;
        private static MethodInfo mMethod_GetRuntimeMemorySizeLong;
        private static MethodInfo mMethod_GetStorageMemorySizeLong;
        private static MethodInfo mMethod_IsNonPowerOfTwo;

        static EditorTextureUtil()
        {
            cType = Assembly.Load("UnityEditor.dll").GetType("UnityEditor.TextureUtil");
            Assert.IsNotNull(cType);
        }

        public static int GetMipmapCount(Texture texture)
        {
            if (mMethod_GetMipmapCount == null)
                mMethod_GetMipmapCount = cType.GetMethod("GetMipmapCount", BindingFlags.Static | BindingFlags.Public);

            Assert.IsNotNull(mMethod_GetMipmapCount);
            return (int) mMethod_GetMipmapCount.Invoke(null, new[] {texture});
        }

        public static TextureFormat GetTextureFormat(Texture texture)
        {
            if (mMethod_GetTextureFormat == null)
                mMethod_GetTextureFormat = cType.GetMethod("GetTextureFormat", BindingFlags.Static | BindingFlags.Public);

            Assert.IsNotNull(mMethod_GetTextureFormat);
            return (TextureFormat) mMethod_GetTextureFormat.Invoke(null, new[] {texture});
        }

        public static long GetRuntimeMemorySize(Texture texture)
        {
            if (mMethod_GetRuntimeMemorySizeLong == null)
                mMethod_GetRuntimeMemorySizeLong = cType.GetMethod("GetRuntimeMemorySizeLong", BindingFlags.Static | BindingFlags.Public);

            Assert.IsNotNull(mMethod_GetRuntimeMemorySizeLong);
            return (long) mMethod_GetRuntimeMemorySizeLong.Invoke(null, new[] {texture});
        }

        public static long GetStorageMemorySize(Texture texture)
        {
            if (mMethod_GetStorageMemorySizeLong == null)
                mMethod_GetStorageMemorySizeLong = cType.GetMethod("GetStorageMemorySizeLong", BindingFlags.Static | BindingFlags.Public);

            Assert.IsNotNull(mMethod_GetStorageMemorySizeLong);
            return (long) mMethod_GetStorageMemorySizeLong.Invoke(null, new[] {texture});
        }

        public static bool IsNonPowerOfTwo(Texture2D texture)
        {
            if (mMethod_IsNonPowerOfTwo == null)
                mMethod_IsNonPowerOfTwo = cType.GetMethod("IsNonPowerOfTwo", BindingFlags.Static | BindingFlags.Public);

            Assert.IsNotNull(mMethod_IsNonPowerOfTwo);
            return (bool) mMethod_IsNonPowerOfTwo.Invoke(null, new[] {texture});
        }
    }
}