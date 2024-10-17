#nullable enable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

namespace Es.Unity.Addins.CustomInspectors
{
    /// <summary>
    /// 
    /// </summary>
    [AddComponentMenu("Gizmos/Collider Vision")]
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class ColliderVision : MonoBehaviour
    {

        static ColliderVision() {
            TryAttach();
        }

        [InitializeOnLoadMethod]
        private static void _Init() {
            TryAttach();
        }

        private static bool TryAttach() {
            try {
                var mainCameraGO = Camera.main.gameObject;
                var co = mainCameraGO.GetComponent<ColliderVision>();
                if(co == null) {
                    mainCameraGO.AddComponent<ColliderVision>();
                    return true;
                }
            }
            catch(Exception ex) {
                Debug.LogWarning(ex);
                return false;
            }
            return false;
        }

        [SerializeField] private ColliderGizmosMode _Mode = ColliderGizmosMode.None;
        public ColliderGizmosMode Mode {
            get => _Mode;
            set {
                if(_Mode != value) {
                    _Mode = value;
                }
            }
        }

        [field: SerializeField] private Color GizmosColor { get; set; } = System.Drawing.Color.Lime.ToUnityColor();

        [field: SerializeField] private bool ExcludePlaneMeshCollider { get; set; } = true;

        [Flags]
        public enum ColliderGizmosMode 
        {
            None = 0,
            Wire = 0x01,
            Solid = 0x10,
            WireAndSolid = Wire | Solid,
        }

        private void OnDrawGizmos() {

            if(this.Mode == ColliderGizmosMode.None) return;

            var beforeColor = UnityEngine.Gizmos.color;
            var beforeMatrix = UnityEngine.Gizmos.matrix;
            UnityEngine.Gizmos.color = this.GizmosColor;

            foreach(var collider in GameObject.FindObjectsByType<UnityEngine.Collider>(FindObjectsSortMode.None)) {
                var transform = collider.transform;
                if(collider is BoxCollider boxCollider) {
                    UnityEngine.Gizmos.matrix = transform.localToWorldMatrix;
                    if(this.Mode.HasFlag(ColliderGizmosMode.Wire)) {
                        UnityEngine.Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                    }
                    if(this.Mode.HasFlag(ColliderGizmosMode.Solid)) {
                        UnityEngine.Gizmos.DrawCube(boxCollider.center, boxCollider.size);
                    }
                }
                else if(collider is SphereCollider sphereCollider) {
                    var c = transform.TransformVector(sphereCollider.center) + transform.position;
                    if(this.Mode.HasFlag(ColliderGizmosMode.Wire)) {
                        UnityEngine.Gizmos.DrawWireSphere(c, sphereCollider.radius);
                    }
                    if(this.Mode.HasFlag(ColliderGizmosMode.Solid)) {
                        UnityEngine.Gizmos.DrawSphere(c, sphereCollider.radius);
                    }
                }
                else if(collider is CapsuleCollider capsuleCollider) {
                    if(this.Mode.HasFlag(ColliderGizmosMode.Wire)) {
                        DrawWireCapsule(transform, capsuleCollider.center, capsuleCollider.height, capsuleCollider.radius, capsuleCollider.direction, Gizmos.color);
                    }
                }
                else if(collider is MeshCollider meshCollider) {
                    var mf = collider.gameObject.GetComponent<MeshFilter>();
                    if(mf != null && mf.sharedMesh is Mesh mesh && mesh != null) {

                        if(this.ExcludePlaneMeshCollider) {
                            if(mf.sharedMesh.name == "Plane") continue;
                        }

                        UnityEngine.Gizmos.matrix = transform.localToWorldMatrix;
                        if(this.Mode.HasFlag(ColliderGizmosMode.Wire)) {
                            UnityEngine.Gizmos.DrawWireMesh(mesh);
                        }
                        if(this.Mode.HasFlag(ColliderGizmosMode.Solid)) {
                            UnityEngine.Gizmos.DrawMesh(mesh);
                        }
                    }
                }
            }

            UnityEngine.Gizmos.matrix = beforeMatrix;
            UnityEngine.Gizmos.color = beforeColor;
        }


        public static void DrawWireCapsule(Transform transform, Vector3 center, float height, float radius, int directionIndex, Color color = default) {
            var s_matrix = Gizmos.matrix;

            Vector3 directionVector = directionIndex switch {
                0 => Vector3.right,
                1 => Vector3.up,
                2 => Vector3.forward,
                _ => throw new InvalidProgramException()
            };

            var sphereCenterOffset = ((height / 2) - radius) * directionVector;

            var p0 = center - sphereCenterOffset;
            var p1 = center + sphereCenterOffset;

            var gp0 = p0 + (Vector3)transform.position;
            var gp1 = p1 + (Vector3)transform.position;

            var r = transform.TransformVector(radius, radius, radius);
            var _radius = Enumerable.Range(0, 3).Select(xyz => xyz == directionIndex ? 0 : r[xyz]).Select(Mathf.Abs).Max();

            DrawWireCapsule(gp0, gp1, _radius, color);

            Gizmos.matrix = s_matrix;
        }

        public static void DrawWireCapsule(Vector3 _pos, Vector3 _pos2, float _radius, Color _color = default) {
            if(_color != default) UnityEditor.Handles.color = _color;

            var forward = _pos2 - _pos;
            var _rot = Quaternion.identity;
            if(forward.magnitude > 0) {
                _rot = Quaternion.LookRotation(forward);
            }
            var pointOffset = _radius/2f;
            var length = forward.magnitude;
            var center2 = new Vector3(0f,0,length);

            Matrix4x4 angleMatrix = Matrix4x4.TRS(_pos, _rot, UnityEditor.Handles.matrix.lossyScale);

            using(new UnityEditor.Handles.DrawingScope(angleMatrix)) {
                UnityEditor.Handles.DrawWireDisc(Vector3.zero, Vector3.forward, _radius);
                UnityEditor.Handles.DrawWireArc(Vector3.zero, Vector3.up, Vector3.left * pointOffset, -180f, _radius);
                UnityEditor.Handles.DrawWireArc(Vector3.zero, Vector3.left, Vector3.down * pointOffset, -180f, _radius);
                UnityEditor.Handles.DrawWireDisc(center2, Vector3.forward, _radius);
                UnityEditor.Handles.DrawWireArc(center2, Vector3.up, Vector3.right * pointOffset, -180f, _radius);
                UnityEditor.Handles.DrawWireArc(center2, Vector3.left, Vector3.up * pointOffset, -180f, _radius);

                void DrawLine(float arg1, float arg2, float forward) {
                    UnityEngine.Gizmos.matrix = angleMatrix;
                    UnityEngine.Gizmos.DrawLine(new Vector3(arg1, arg2, 0f), new Vector3(arg1, arg2, forward));
                }

                DrawLine(_radius, 0f, length);
                DrawLine(-_radius, 0f, length);
                DrawLine(0f, _radius, length);
                DrawLine(0f, -_radius, length);
            }
        }
    }
}
