// Geometry shader instancing with skeletal animations
// https://github.com/keijiro/SkeletalGeometricEffects

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using System.Collections.Generic;

namespace GeoFx
{
    [ExecuteInEditMode]
    public class Skeleton : MonoBehaviour, ITimeControl, IPropertyPreview
    {
        #region Editable variables

        [SerializeField] Animator _sourceAnimator;

        [SerializeField, Range(0, 0.5f)] float _baseRadius = 0.25f;
        [SerializeField, Range(0, 0.025f)] float _stripWidth = 0.015f;
        [SerializeField, Range(0, 10)] float _stripSpeed = 4;
        [SerializeField, Range(0, 5)] float _stripLength = 2;

        [SerializeField, Range(0, 10)] float _waveWidth = 2;
        [SerializeField, Range(0, 10)] float _waveSpeed = 2;
        [SerializeField, Range(0, 1)] float _distortion = 0.1f;

        [SerializeField, ColorUsage(false)] Color _baseColor = Color.white;
        [SerializeField, Range(0, 1)] float _hueShift = 0.1f;
        [SerializeField, Range(0, 1)] float _metallic = 0;
        [SerializeField, Range(0, 1)] float _smoothness = 0.5f;
        [SerializeField, ColorUsage(false, true)] Color _emissiveColor = Color.white;
        [SerializeField, Range(0, 1)] float _hilight = 0.1f;

        [SerializeField] bool _debug;

        [SerializeField, HideInInspector] Shader _shader;

        #endregion

        #region Bone definitions

        [System.Serializable] struct Bone
        {
            public HumanBodyBones JointFrom;
            public HumanBodyBones JointTo;
            public float Radius;

            public Bone(HumanBodyBones from, HumanBodyBones to, float radius)
            {
                JointFrom = from;
                JointTo = to;
                Radius = radius;
            }
        }

        [SerializeField] Bone[] _boneList = new []
        {
            new Bone(HumanBodyBones.Hips,          HumanBodyBones.LeftUpperLeg,  1),
            new Bone(HumanBodyBones.LeftUpperLeg,  HumanBodyBones.LeftLowerLeg,  1),
            new Bone(HumanBodyBones.LeftLowerLeg,  HumanBodyBones.LeftFoot,      1),
            new Bone(HumanBodyBones.LeftFoot,      HumanBodyBones.LeftToes,      1),

            new Bone(HumanBodyBones.Hips,          HumanBodyBones.RightUpperLeg, 1),
            new Bone(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, 1),
            new Bone(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,     1),
            new Bone(HumanBodyBones.RightFoot,     HumanBodyBones.RightToes,     1),

            new Bone(HumanBodyBones.Hips,          HumanBodyBones.Chest,         1),
            new Bone(HumanBodyBones.Chest,         HumanBodyBones.Neck,          1),
            new Bone(HumanBodyBones.Neck,          HumanBodyBones.Head,          1),

            new Bone(HumanBodyBones.Neck,          HumanBodyBones.LeftUpperArm,  1),
            new Bone(HumanBodyBones.LeftUpperArm,  HumanBodyBones.LeftLowerArm,  1),
            new Bone(HumanBodyBones.LeftLowerArm,  HumanBodyBones.LeftHand,      1),

            new Bone(HumanBodyBones.Neck,          HumanBodyBones.RightUpperArm, 1),
            new Bone(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, 1),
            new Bone(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,     1)
        };

        #endregion

        #region Shader property IDs

        static class ShaderID
        {
            public static readonly int GeoParams = Shader.PropertyToID("_GeoParams");
            public static readonly int AnimParams = Shader.PropertyToID("_AnimParams");
            public static readonly int MatParams = Shader.PropertyToID("_MatParams");
            public static readonly int BaseHSVM = Shader.PropertyToID("_BaseHSVM");
            public static readonly int AddHSVM = Shader.PropertyToID("_AddHSVM");
        }

        #endregion

        #region Private members

        Material _material;

        Vector4 ColorToHsvm(Color color)
        {
            var max = Mathf.Max(color.maxColorComponent, 1e-5f);
            float h, s, v;
            Color.RGBToHSV(color / max, out h, out s, out v);
            return new Vector4(h, s, v, max);
        }

        #endregion

        #region ITimeControl implementation

        float _controlTime = -1;

        public void OnControlTimeStart()
        {
        }

        public void OnControlTimeStop()
        {
            _controlTime = -1;
        }

        public void SetTime(double time)
        {
            _controlTime = (float)time;
        }

        float LocalTime
        {
            get
            {
                if (_controlTime < 0)
                    return Application.isPlaying ? Time.time : 0;
                else
                    return _controlTime;
            }
        }

        #endregion

        #region IPropertyPreview implementation

        public void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            // There is nothing controllable.
        }

        #endregion

        #region Mesh operations

        List<Vector3> _vertices  = new List<Vector3>();
        List<Vector3> _normals   = new List<Vector3>();
        List<Vector2> _texcoords = new List<Vector2>();
        Mesh _mesh;

        void InitializeIndices()
        {
            var vcount = _boneList.Length * 2;
            var indices = new int[vcount];
            for (var i = 0; i < vcount; i++) indices[i] = i;
            _mesh.SetIndices(indices, MeshTopology.Lines, 0);
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
        }

        void UpdateMesh()
        {
            _vertices .Clear();
            _normals  .Clear();
            _texcoords.Clear();

            foreach (var bone in _boneList)
            {
                var joint1 = _sourceAnimator.GetBoneTransform(bone.JointFrom);
                var joint2 = _sourceAnimator.GetBoneTransform(bone.JointTo);

                _vertices.Add(joint1.position);
                _vertices.Add(joint2.position);

                _normals.Add(joint1.up);
                _normals.Add(joint2.up);

                _texcoords.Add(Vector2.one * bone.Radius);
                _texcoords.Add(Vector2.one * bone.Radius);
            }

            _mesh.SetVertices(_vertices);
            _mesh.SetNormals(_normals);
            _mesh.SetUVs(0, _texcoords);

            if (_mesh.GetIndexCount(0) == 0) InitializeIndices();
        }

        void DrawMesh()
        {
            var gparams = new Vector4(
                _baseRadius, _stripWidth,
                _stripSpeed * Mathf.PI * 2,
                _stripLength * Mathf.PI * 2
            );

            var aparams = new Vector4(
                LocalTime + 100,
                _waveWidth, _waveSpeed, _distortion
            );

            var mparams = new Vector4(
                _metallic, _smoothness,
                _hueShift, _hilight
            );

            _material.SetVector(ShaderID.GeoParams, gparams);
            _material.SetVector(ShaderID.AnimParams, aparams);
            _material.SetVector(ShaderID.MatParams, mparams);
            _material.SetVector(ShaderID.BaseHSVM, ColorToHsvm(_baseColor));
            _material.SetVector(ShaderID.AddHSVM, ColorToHsvm(_emissiveColor));

            if (_debug)
                _material.EnableKeyword("GEOFX_DEBUG");
            else
                _material.DisableKeyword("GEOFX_DEBUG");

            Graphics.DrawMesh(
                _mesh, transform.localToWorldMatrix,
                _material, gameObject.layer
            );
        }

        #endregion

        #region MonoBehaviour implementation

        void OnValidate()
        {
            _baseRadius = Mathf.Max(0, _baseRadius);
            _stripLength = Mathf.Max(0, _stripLength);
            _stripWidth = Mathf.Max(0, _stripWidth);

            // Dispose the current mesh if the vertex count doesn't match.
            if (_mesh != null && _mesh.GetIndexCount(0) != _boneList.Length * 2)
                _mesh.Clear();
        }

        void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (_mesh != null) Destroy(_mesh);
                if (_material != null) Destroy(_material);
            }
            else
            {
                if (_mesh != null) DestroyImmediate(_mesh);
                if (_material != null) DestroyImmediate(_material);
            }

            _mesh = null;
            _material = null;
        }

        void LateUpdate()
        {
            // Lazy initialization
            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.hideFlags = HideFlags.DontSave;
            }

            if (_material == null)
            {
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.DontSave;
            }

            UpdateMesh();
            DrawMesh();
        }

        #endregion
    }
}
