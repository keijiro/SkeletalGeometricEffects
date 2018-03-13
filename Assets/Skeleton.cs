using System.Collections.Generic;
using UnityEngine;

public class Skeleton : MonoBehaviour
{
    [SerializeField] Transform _root;
    [SerializeField] float _segmentLength = 0.2f;
    [SerializeField] Shader _shader;

    List<Transform> _segments;
    List<Vector3> _vertices;

    Mesh _mesh;
    Material _material;

    void TraverseHierarchy(Transform head, Transform parent)
    {
        foreach (Transform node in parent)
        {
            if (Vector3.Distance(head.position, node.position) < _segmentLength)
            {
                TraverseHierarchy(head, node);
            }
            else
            {
                _segments.Add(head);
                _segments.Add(node);
                TraverseHierarchy(node, node);
            }
        }
    }

    void Start()
    {
        _segments = new List<Transform>();
        TraverseHierarchy(_root, _root);

        _vertices = new List<Vector3>(_segments.Count);
        foreach (var t in _segments) _vertices.Add(t.position);

        var uvs = new Vector2[_segments.Count];
        for (var i = 0; i < uvs.Length; i++) uvs[i] = new Vector2(i, i);

        var indices = new int[_segments.Count];
        for (var i = 0; i < indices.Length; i++) indices[i] = i;

        _mesh = new Mesh();
        _mesh.SetVertices(_vertices);
        _mesh.uv = uvs;
        _mesh.SetIndices(indices, MeshTopology.Lines, 0);
        _material = new Material(_shader);
    }

    void OnDestroy()
    {
        Destroy(_mesh);
        Destroy(_material);
    }

    void Update()
    {
        for (var i = 0; i < _vertices.Count; i++) _vertices[i] = _segments[i].position;
        _mesh.SetVertices(_vertices);

        Graphics.DrawMesh(_mesh, transform.localToWorldMatrix, _material, gameObject.layer);
    }
}
