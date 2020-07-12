#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif
#if VRC_SDK_VRCSDK2
using VRCSDK2;
#endif
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System;
using UnityEditor.SceneManagement;

#if VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3
namespace VRWorldToolkit
{
    /// <summary>
    /// Custom editor addition for drawing avatar pedestal bounds
    /// </summary>
    [CustomEditor(typeof(VRC_AvatarPedestal), true, isFallback = false)]
    [CanEditMultipleObjects]
    public class CustomAvatarPedestalEditor : Editor
    {
        private const float InnerBound = 1.5f;
        private const float OuterBound = 2f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("VRWorld Toolkit Additions", EditorStyles.boldLabel);

            GUILayout.Label("Selected IDs (Ordered by hierarchy):");

            var pedestals = serializedObject.targetObjects.Select(x => x as VRC_AvatarPedestal).OrderBy(x => x.transform.GetSiblingIndex());

            foreach (VRC_AvatarPedestal pedestal in pedestals)
            {
                EditorGUI.BeginChangeCheck();

                pedestal.blueprintId = EditorGUILayout.DelayedTextField(pedestal.name + " ID: ", pedestal.blueprintId);

                if (EditorGUI.EndChangeCheck())
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pedestal);
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
            }

            if (GUILayout.Button("Copy selected IDs"))
            {
                EditorGUIUtility.systemCopyBuffer = string.Join("\n", pedestals.Select(x => x.blueprintId));
            }
        }

        /// <summary>
        /// Draw bounds for selected avatar pedestals
        /// </summary>
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawAvatarPedestalGizmos(VRC_AvatarPedestal pedestal, GizmoType gizmoType)
        {
            if (Vector3.Distance(pedestal.transform.position, Camera.current.transform.position) > 25f) return;

            //Get transform from the pedestal placement value otherwise get transform of the pedestal itself
            var pedestalTransform = pedestal.Placement != null ? pedestal.Placement : pedestal.transform;

            //Set gizmo matrix to match the pedestal for proper placement and rotation
            Gizmos.matrix = pedestalTransform.localToWorldMatrix;
            Gizmos.color = Color.green;

            //Draw the outer bound of the pedestal
            Gizmos.DrawWireCube(Vector3.up * 1.2f, new Vector3(1f * OuterBound, 1f * OuterBound));

            //Change color to red if showing the front is active and active camera is behind the pedestal
            var cameraDirection = pedestalTransform.position - Camera.current.transform.position;

            var angle = Vector3.Angle(pedestalTransform.forward, cameraDirection);

            if (Mathf.Abs(angle) < 90)
            {
                Gizmos.color = Color.red;
            }

            //Draw the inner bound of the pedestal
            Gizmos.DrawWireCube(Vector3.up * 1.2f, new Vector3(1f * InnerBound, 1f * InnerBound));
        }
    }
}
#endif