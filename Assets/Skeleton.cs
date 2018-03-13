using UnityEngine;

public class Skeleton : MonoBehaviour
{
    [SerializeField] Shader _shader;

    Mesh _mesh;
    Material _material;

    void Start()
    {
        _mesh = new Mesh();

        _mesh.vertices = new [] {
            new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1.5f, 0)
        };

        _mesh.SetIndices(new [] { 0, 1, 1, 2 }, MeshTopology.Lines, 0);

        _material = new Material(_shader);
    }

    void OnDestroy()
    {
        Destroy(_mesh);
        Destroy(_material);
    }

    void Update()
    {
        Graphics.DrawMesh(_mesh, transform.localToWorldMatrix, _material, gameObject.layer);
    }
}
