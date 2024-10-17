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
            //TryAttach();
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


        private static IEnumerable<GameObject> GetChildren(GameObject go) {
            foreach(Transform childTransform in go.transform) {
                yield return childTransform.gameObject;
            }
        }

        private static IEnumerable<GameObject> GetDescendant(GameObject go) {
            foreach(GameObject child in GetChildren(go)) {
                yield return child;
                foreach(var desc in GetDescendant(child)) yield return desc;
            }
        }

        private void OnDrawGizmos() {

            if(this.Mode == ColliderGizmosMode.None) return;

            var selectedGOs = Selection.gameObjects.SelectMany(_ => GetDescendant(_));

            foreach(var collider in GameObject.FindObjectsByType<UnityEngine.Collider>(FindObjectsSortMode.None)) {

                bool maybeAlreadyDrown = selectedGOs.Contains(collider.gameObject);

                if(maybeAlreadyDrown) continue;

                var beforeColor = UnityEngine.Gizmos.color;
                var beforeMatrix = UnityEngine.Gizmos.matrix;
                UnityEngine.Gizmos.color = this.GizmosColor;

                {

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
                        var r = sphereCollider.radius * Enumerable.Range(0, 2).Select(_ => transform.lossyScale[_]).Max();

                        if(this.Mode.HasFlag(ColliderGizmosMode.Wire)) {
                            UnityEngine.Gizmos.DrawWireSphere(c, r);
                        }
                        if(this.Mode.HasFlag(ColliderGizmosMode.Solid)) {
                            UnityEngine.Gizmos.DrawSphere(c, r);
                        }
                    }
                    else if(collider is CapsuleCollider capsuleCollider) {
                        if(this.Mode.HasFlag(ColliderGizmosMode.Wire)) {
                            Gizmos.matrix = Matrix4x4.identity;
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


        }


        public static void DrawWireCapsule(Transform transform, Vector3 center, float height, float l_radius, int directionIndex, Color color = default) {
            Vector3 l_directionVector = directionIndex switch {
                0 => Vector3.right,
                1 => Vector3.up,
                2 => Vector3.forward,
                _ => throw new InvalidProgramException()
            };

            var l_height = Vector3.Dot(height * l_directionVector, transform.lossyScale);

            var l_sphereCenterOffset = ((l_height / 2) - l_radius) * l_directionVector;

            var p0 = center - transform.TransformDirection(l_sphereCenterOffset);
            var p1 = center + transform.TransformDirection(l_sphereCenterOffset);

            var lp0 = p0 + transform.position;
            var lp1 = p1 + transform.position;

            //var r = transform.TransformVector(radius, radius, radius);
            var r = new Vector3(l_radius * transform.lossyScale.x, l_radius * transform.lossyScale.y, l_radius * transform.lossyScale.z);
            var _radius = Enumerable.Range(0, 3).Select(xyz => xyz == directionIndex ? 0 : r[xyz]).Select(Mathf.Abs).Max();

            DrawWireCapsule(lp0, lp1, _radius, color);
        }

        public static void DrawWireCapsule(Vector3 _pos, Vector3 _pos2, float _radius, Color _color = default) {

            //if(!Gizmos.matrix.isIdentity) throw new InvalidOperationException();

            if(_color != default) UnityEditor.Handles.color = _color;

            var forward = _pos2 - _pos;
            var _rot = Quaternion.identity;
            if(forward.magnitude > 0) {
                _rot = Quaternion.LookRotation(forward);
            }
            var pointOffset = _radius/2f;
            var length = forward.magnitude;
            var center2 = new Vector3(0f,0,length);

            Matrix4x4 angleMatrix = Matrix4x4.TRS(_pos, _rot, Vector3.one);

            using(new UnityEditor.Handles.DrawingScope(angleMatrix)) {
                UnityEditor.Handles.DrawWireDisc(Vector3.zero, Vector3.forward, _radius);
                UnityEditor.Handles.DrawWireArc(Vector3.zero, Vector3.up, Vector3.left * pointOffset, -180f, _radius);
                UnityEditor.Handles.DrawWireArc(Vector3.zero, Vector3.left, Vector3.down * pointOffset, -180f, _radius);
                UnityEditor.Handles.DrawWireDisc(center2, Vector3.forward, _radius);
                UnityEditor.Handles.DrawWireArc(center2, Vector3.up, Vector3.right * pointOffset, -180f, _radius);
                UnityEditor.Handles.DrawWireArc(center2, Vector3.left, Vector3.up * pointOffset, -180f, _radius);

                void DrawLine(float arg1, float arg2, float forward) {
                    var m = UnityEngine.Gizmos.matrix;
                    UnityEngine.Gizmos.matrix = angleMatrix;
                    UnityEngine.Gizmos.DrawLine(new Vector3(arg1, arg2, 0f), new Vector3(arg1, arg2, forward));
                    UnityEngine.Gizmos.matrix = m;
                }

                DrawLine(_radius, 0f, length);
                DrawLine(-_radius, 0f, length);
                DrawLine(0f, _radius, length);
                DrawLine(0f, -_radius, length);
            }
        }
    }
}
