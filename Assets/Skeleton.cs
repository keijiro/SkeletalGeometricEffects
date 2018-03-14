using UnityEngine;
using System.Collections.Generic;

public class Skeleton : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] Transform _root;
    [SerializeField] float _minJointDistance = 0.12f;

    [Space]
    [SerializeField] Color _color = Color.white;
    [SerializeField] float _metallic = 0;
    [SerializeField] float _smoothness = 0.5f;

    [SerializeField, HideInInspector] Shader _shader;

    #endregion

    #region Private field memebers

    List<Transform> _joints; // should be multiple of two
    List<Vector3> _vertices;
    List<Vector3> _normals;

    Mesh _mesh;
    Material _material;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _joints = new List<Transform>();
        EnumerateJointsRecursively(_root, _root);

        _vertices = new List<Vector3>(_joints.Count);
        _normals = new List<Vector3>(_joints.Count);
        var indices = new int[_joints.Count];

        for (var i = 0; i < _joints.Count; i++)
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
        for (var i = 0; i < _joints.Count; i += 2)
        {
            var j1 = _joints[i];
            var j2 = _joints[i + 1];

            _vertices[i] = j1.position;
            _vertices[i + 1] = j2.position;

            _normals[i] = _normals[i + 1] = CalculateBoneNormal(j1, j2);
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

    #region Local methods

    void EnumerateJointsRecursively(Transform origin, Transform parent)
    {
        foreach (Transform node in parent)
        {
            var d = Vector3.Distance(origin.position, node.position);

            if (d < _minJointDistance)
            {
                EnumerateJointsRecursively(origin, node);
            }
            else
            {
                _joints.Add(origin);
                _joints.Add(node);
                EnumerateJointsRecursively(node, node);
            }
        }
    }

    Vector3 CalculateBoneNormal(Transform joint1, Transform joint2)
    {
        return joint1.up;
        /*
        var v = joint2.position - joint1.position;
        var dx = Mathf.Abs(Vector3.Dot(v, joint1.right));
        var dy = Mathf.Abs(Vector3.Dot(v, joint1.up));
        var dz = Mathf.Abs(Vector3.Dot(v, joint1.forward));
        // Use local Y axis iff local X axis is aligned to the bone.
        return (dx > dy && dx > dz) ? joint1.up : joint1.right;
        */
    }

    #endregion
}
