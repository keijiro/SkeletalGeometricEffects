using UnityEngine;
using System.Collections.Generic;

public class Skeleton : MonoBehaviour
{
    #region Editable variables

    [SerializeField] Animator _animator;
    [SerializeField] Color _color = Color.white;
    [SerializeField] float _metallic = 0;
    [SerializeField] float _smoothness = 0.5f;

    [SerializeField, HideInInspector] Shader _shader;

    #endregion

    #region Private field memebers

    static readonly HumanBodyBones[] BonePairs = new [] {
        HumanBodyBones.Hips,            HumanBodyBones.LeftUpperLeg,
        HumanBodyBones.LeftUpperLeg,    HumanBodyBones.LeftLowerLeg,
        HumanBodyBones.LeftLowerLeg,    HumanBodyBones.LeftFoot,
        HumanBodyBones.LeftFoot,        HumanBodyBones.LeftToes,

        HumanBodyBones.Hips,            HumanBodyBones.RightUpperLeg,
        HumanBodyBones.RightUpperLeg,   HumanBodyBones.RightLowerLeg,
        HumanBodyBones.RightLowerLeg,   HumanBodyBones.RightFoot,
        HumanBodyBones.RightFoot,       HumanBodyBones.RightToes,

        HumanBodyBones.Hips,            HumanBodyBones.Chest,
        HumanBodyBones.Chest,           HumanBodyBones.Neck,
        HumanBodyBones.Neck,            HumanBodyBones.Head,

        HumanBodyBones.Neck,            HumanBodyBones.LeftUpperArm,
        HumanBodyBones.LeftUpperArm,    HumanBodyBones.LeftLowerArm,
        HumanBodyBones.LeftLowerArm,    HumanBodyBones.LeftHand,

        HumanBodyBones.Neck,            HumanBodyBones.RightUpperArm,
        HumanBodyBones.RightUpperArm,   HumanBodyBones.RightLowerArm,
        HumanBodyBones.RightLowerArm,   HumanBodyBones.RightHand
    };

    List<Vector3> _vertices;
    List<Vector3> _normals;

    Mesh _mesh;
    Material _material;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        var boneCount = BonePairs.Length;

        _vertices = new List<Vector3>(boneCount);
        _normals = new List<Vector3>(boneCount);
        var indices = new int[boneCount];

        for (var i = 0; i < boneCount; i++)
        {
            _vertices.Add(Vector3.zero);
            _normals.Add(Vector3.up);
            indices[i] = i;
        }

        _mesh = new Mesh();
        _mesh.SetVertices(_vertices);
        _mesh.SetNormals(_normals);
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
        for (var i = 0; i < BonePairs.Length; i += 2)
        {
            var bone1 = _animator.GetBoneTransform(BonePairs[i    ]);
            var bone2 = _animator.GetBoneTransform(BonePairs[i + 1]);

            _vertices[i    ] = bone1.position;
            _vertices[i + 1] = bone2.position;

            _normals[i] = _normals[i + 1] = bone1.up;
        }

        _mesh.SetVertices(_vertices);
        _mesh.SetNormals(_normals);

        _material.SetColor("_Color", _color);
        _material.SetFloat("_Metallic", _metallic);
        _material.SetFloat("_Glossiness", _smoothness);

        Graphics.DrawMesh(
            _mesh, transform.localToWorldMatrix,
            _material, gameObject.layer
        );
    }

    #endregion
}
