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
using UnityEditor.SceneManagement;
using System.Text.RegularExpressions;
using System;
using VRWorldToolkit.DataStructures;
using Object = UnityEngine.Object;

#if (VRC_SDK_VRCSDK2 || VRC_SDK_VRCSDK3) && !VRWT_DISABLE_EDITORS
namespace VRWorldToolkit
{
    /// <summary>
    /// Custom editor addition for drawing avatar pedestal bounds
    /// </summary>
    [CustomEditor(typeof(VRC_AvatarPedestal), true, isFallback = false)]
    [CanEditMultipleObjects]
    public class CustomAvatarPedestalEditor : Editor
    {
        private const float INNER_BOUND = 1.5f;
        private const float OUTER_BOUND = 2f;

        private const string AVATAR_ID_REGEX = "avtr_[A-Za-z0-9]{8}-[A-Za-z0-9]{4}-[A-Za-z0-9]{4}-[A-Za-z0-9]{4}-[A-Za-z0-9]{12}";

        private bool setIDsFoldout;
        private string avatarIDArea = "";

        private string[] avatarIDs;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("VRWorld Toolkit Additions", EditorStyles.boldLabel);

            var pedestals = serializedObject.targetObjects.Select(x => x as VRC_AvatarPedestal).OrderBy(x => x.transform.GetSiblingIndex()).ToArray();

            setIDsFoldout = EditorGUILayout.Foldout(setIDsFoldout, "Mass set avatar IDs");
            if (setIDsFoldout)
            {
                if (Selection.activeTransform)
                {
                    avatarIDArea = EditorGUILayout.TextArea(avatarIDArea, GUILayout.ExpandWidth(true));

                    avatarIDs = Regex.Matches(avatarIDArea, AVATAR_ID_REGEX).Cast<Match>().Select(m => m.Value).ToArray();

                    EditorGUILayout.LabelField("IDs found/Pedestals selected: ", avatarIDs.Length + "/" + serializedObject.targetObjects.Length, avatarIDs.Length > serializedObject.targetObjects.Length ? Styles.RedLabel : GUIStyle.none);

                    if (GUILayout.Button("Set IDs"))
                    {
                        var count = Math.Min(serializedObject.targetObjects.Length, avatarIDs.Length);

                        Undo.RegisterCompleteObjectUndo(pedestals.ToArray<Object>(), "Avatar ID Change");

                        for (var i = 0; i < count; i++)
                        {
                            pedestals[i].blueprintId = avatarIDs[i];
                            PrefabUtility.RecordPrefabInstancePropertyModifications(pedestals[i]);
                        }

                        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    }

                    EditorGUILayout.Space();
                }
            }

            GUILayout.Label("Selected IDs (Ordered by hierarchy):");

            for (var i = 0; i < pedestals.Length; i++)
            {
                EditorGUI.BeginChangeCheck();

                pedestals[i].blueprintId = EditorGUILayout.DelayedTextField(pedestals[i].name + " ID: ", pedestals[i].blueprintId);

                if (EditorGUI.EndChangeCheck())
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pedestals[i]);
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

            // Get transform from the pedestal placement value otherwise get transform of the pedestal itself
            var pedestalTransform = pedestal.Placement != null ? pedestal.Placement : pedestal.transform;

            // Set gizmo matrix to match the pedestal for proper placement and rotation
            Gizmos.matrix = pedestalTransform.localToWorldMatrix;
            Gizmos.color = Color.green;

            // Draw the outer bound of the pedestal
            Gizmos.DrawWireCube(Vector3.up * 1.2f, new Vector3(1f * OUTER_BOUND, 1f * OUTER_BOUND));

            // Change color to red if showing the front is active and active camera is behind the pedestal
            var cameraDirection = pedestalTransform.position - Camera.current.transform.position;

            var angle = Vector3.Angle(pedestalTransform.forward, cameraDirection);

            if (Mathf.Abs(angle) < 90)
            {
                Gizmos.color = Color.red;
            }

            // Draw the inner bound of the pedestal
            Gizmos.DrawWireCube(Vector3.up * 1.2f, new Vector3(1f * INNER_BOUND, 1f * INNER_BOUND));
        }
    }
}
#endif