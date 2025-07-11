using System.Collections.Generic;
using UnityEngine;
using System.Linq; // For OptimizeReorderVertexBuffer() or other LINQ if used

public class GeneratedMesh
{
    List<Vector3> vertices = new List<Vector3>();
    List<Vector3> normals = new List<Vector3>();
    List<Vector2> uvs = new List<Vector2>();
    List<List<int>> submeshIndices = new List<List<int>>();

    public List<Vector3> Vertices { get { return vertices; } set { vertices = value; } }
    public List<Vector3> Normals { get { return normals; } set { normals = value; } }
    public List<Vector2> UVs { get { return uvs; } set { uvs = value; } }
    public List<List<int>> SubmeshIndices { get { return submeshIndices; } set { submeshIndices = value; } }

    /// <summary>
    /// MeshTriangle 객체를 기반으로 메시 데이터를 추가합니다.
    /// </summary>
    /// <param name="_triangle">추가할 MeshTriangle 객체</param>
    public void AddTriangle(MeshTriangle _triangle)
    {
        int currentVerticeCount = vertices.Count;

        vertices.AddRange(_triangle.Vertices);
        normals.AddRange(_triangle.Normals);
        uvs.AddRange(_triangle.UVs);

        // 서브메시 인덱스에 해당하는 리스트가 없으면 생성
        // 기존 코드의 문제점: List<List<int>>에 접근할 때 _triangle.SubmeshIndex + 1 크기만큼 보장해야 함
        while (submeshIndices.Count <= _triangle.SubmeshIndex)
        {
            submeshIndices.Add(new List<int>());
        }

        for (int i = 0; i < 3; i++)
        {
            submeshIndices[_triangle.SubmeshIndex].Add(currentVerticeCount + i);
        }
    }

    /// <summary>
    /// 직접 정점, 법선, UV 배열을 기반으로 메시 데이터를 추가합니다. (Cutter에서는 직접 사용하지 않음)
    /// </summary>
    public void AddTriangle(Vector3[] _vertices, Vector3[] _normals, Vector2[] _uvs, int _submeshIndex, Vector4[] _tangents = null)
    {
        int currentVerticeCount = vertices.Count;

        vertices.AddRange(_vertices);
        normals.AddRange(_normals);
        uvs.AddRange(_uvs);

        // 서브메시 인덱스에 해당하는 리스트가 없으면 생성
        while (submeshIndices.Count <= _submeshIndex)
        {
            submeshIndices.Add(new List<int>());
        }

        for (int i = 0; i < 3; i++)
        {
            submeshIndices[_submeshIndex].Add(currentVerticeCount + i);
        }
        // 이 오버로드에서는 탄젠트 처리가 누락되어 있습니다.
        // 만약 _tangents가 null이 아니면, `this.tangents.AddRange(_tangents);`와 같은 로직이 필요할 수 있습니다.
        // 하지만 Cutter 스크립트는 이 오버로드를 사용하지 않으므로 현재는 무시합니다.
    }


    /// <summary>
    /// 축적된 데이터를 기반으로 Unity Mesh 객체를 생성하여 반환합니다.
    /// </summary>
    /// <returns>생성된 Unity Mesh 객체입니다.</returns>
    public Mesh GetGeneratedMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "GeneratedCutMesh"; // 메시 이름 설정

        // 메시의 최대 정점 수 제한을 확인 (65535개) 및 IndexFormat 설정
        if (vertices.Count > 65535)
        {
            Debug.LogWarning("Generated mesh has more than 65535 vertices. Setting IndexFormat to UInt32.");
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        else
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetUVs(1, uvs); // 주신 코드에 SetUVs(1, uvs)가 있어서 그대로 유지

        mesh.subMeshCount = submeshIndices.Count;
        for (int i = 0; i < submeshIndices.Count; i++)
        {
            // 서브메시의 삼각형 인덱스가 비어있지 않은 경우에만 설정
            if (submeshIndices[i].Count > 0)
            {
                mesh.SetTriangles(submeshIndices[i], i);
            }
            else
            {
                Debug.LogWarning($"Submesh {i} has no triangles."); // 비어있는 서브메시가 있다면 경고
            }
        }

        mesh.RecalculateBounds(); // 메시의 바운드 다시 계산
        mesh.RecalculateNormals(); // 법선 다시 계산 (기존 노멀 사용하지만, 안전을 위해 호출)
        mesh.RecalculateTangents(); // 노멀맵 사용 시 필요한 탄젠트 다시 계산
        mesh.OptimizeReorderVertexBuffer(); // 렌더링 성능 최적화

        return mesh;
    }
}