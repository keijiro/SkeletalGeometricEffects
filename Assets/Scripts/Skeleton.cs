// Geometry shader instancing with skeletal animations
// https://github.com/keijiro/SkeletalGeometricEffects

using UnityEngine;
using System.Collections.Generic;

public class Skeleton : MonoBehaviour
{
    #region Editable variables

    [SerializeField] Animator _sourceAnimator;
    [SerializeField, ColorUsage(false)] Color _baseColor = Color.white;
    [SerializeField, Range(0, 1)] float _metallic = 0;
    [SerializeField, Range(0, 1)] float _smoothness = 0.5f;

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

    #region Private field memebers

    List<Vector3> _vertices;
    List<Vector3> _normals;
    List<Vector4> _tangents;
    List<Vector2> _texcoords;

    Mesh _mesh;
    Material _material;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        var vcount = _boneList.Length * 2;

        _vertices = new List<Vector3>(vcount);
        _normals = new List<Vector3>(vcount);
        _tangents = new List<Vector4>(vcount);
        _texcoords = new List<Vector2>(vcount);
        var indices = new int[vcount];

        for (var i = 0; i < vcount; i++)
        {
            _vertices.Add(Vector3.zero);
            _normals.Add(Vector3.up);
            _tangents.Add(Vector4.one);
            _texcoords.Add(Vector2.zero);
            indices[i] = i;
        }

        _mesh = new Mesh();
        _mesh.SetVertices(_vertices);
        _mesh.SetNormals(_normals);
        _mesh.SetTangents(_tangents);
        _mesh.SetUVs(0, _texcoords);
        _mesh.SetIndices(indices, MeshTopology.Lines, 0);
        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

        _material = new Material(_shader);
    }

    void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
        if (_material != null) Destroy(_material);
    }

    void Update()
    {
        var i = 0;

        foreach (var bone in _boneList)
        {
            var joint1 = _sourceAnimator.GetBoneTransform(bone.JointFrom);
            var joint2 = _sourceAnimator.GetBoneTransform(bone.JointTo);

            _vertices[i    ] = joint1.position;
            _vertices[i + 1] = joint2.position;

            _normals[i    ] = joint1.up;
            _normals[i + 1] = joint2.up;

            _tangents[i    ] = MakeTangent(joint1);
            _tangents[i + 1] = MakeTangent(joint2);

            _texcoords[i    ] = new Vector2(bone.Radius, 0);
            _texcoords[i + 1] = new Vector2(bone.Radius, 0);

            i += 2;
        }

        _mesh.SetVertices(_vertices);
        _mesh.SetNormals(_normals);
        _mesh.SetTangents(_tangents);
        _mesh.SetUVs(0, _texcoords);

        _material.SetColor("_Color", _baseColor);
        _material.SetFloat("_Metallic", _metallic);
        _material.SetFloat("_Glossiness", _smoothness);

        Graphics.DrawMesh(
            _mesh, transform.localToWorldMatrix,
            _material, gameObject.layer
        );
    }

    #endregion

    #region Private methods

    static Vector4 MakeTangent(Transform t)
    {
        var v = t.right;
        return new Vector4(v.x, v.y, v.z, 1);
    }

    #endregion
}
