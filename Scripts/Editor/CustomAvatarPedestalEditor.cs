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
        const float innerBound = 1.5f;
        const float outerBound = 2f;

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
                EditorGUIUtility.systemCopyBuffer = String.Join("\n", pedestals.Select(x => x.blueprintId));
            }
        }

        /// <summary>
        /// Draw bounds for selected avatar pedestals
        /// </summary>
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        static void DrawAvatarPedestalGizmos(VRC_AvatarPedestal pedestal, GizmoType gizmoType)
        {
            Transform pedestalTransform;

            //Get transform from the pedestal placement value otherwise get transform of the pedestal itself
            if (pedestal.Placement != null)
            {
                pedestalTransform = pedestal.Placement;
            }
            else
            {
                pedestalTransform = pedestal.transform;
            }

            //Set gizmo matrix to match the pedestal for proper placement and rotation
            Gizmos.matrix = pedestalTransform.localToWorldMatrix;

            //Draw the inner and outer bounds
            DrawBound(pedestalTransform, innerBound, Color.green, true);
            DrawBound(pedestalTransform, outerBound, Color.red, false);
        }

        /// <summary>
        /// Helper function for drawing gizmo bounds
        /// </summary>
        /// <param name="placement">Center of the bounds</param>
        /// <param name="size">Size of the bounds</param>
        /// <param name="color">Color of the bounds</param>
        /// <param name="showFront">Whether to change the color depending on which side is being looked at</param>
        private static void DrawBound(Transform placement, float size, Color color, bool showFront)
        {
            if (Vector3.Distance(placement.position, Camera.current.transform.position) < 25f)
            {
                //Set gizmo color to the passed variable
                Gizmos.color = color;

                //Change color to red if showing the front is active and active camera is behind the pedestal
                if (showFront)
                {
                    Vector3 cameraDirection = placement.position - Camera.current.transform.position;

                    float angle = Vector3.Angle(placement.forward, cameraDirection);

                    if (Mathf.Abs(angle) < 90)
                    {
                        Gizmos.color = Color.red;
                    }
                }

                //Draw the bounds
                Gizmos.DrawWireCube(Vector3.up * 1.2f, new Vector3(1f * size, 1f * size));
            }
        }
    }
}
#endif