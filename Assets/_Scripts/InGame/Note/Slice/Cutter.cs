using System.Collections.Generic;
using UnityEngine;

public class Cutter : MonoBehaviour
{
    private static bool isBusy;
    private static Mesh originalMesh;
    private static Vector3[] originalVertices;
    private static Vector3[] originalNormals;
    private static Vector2[] originalUVs;

    /// <summary>
    /// 지정된 게임 오브젝트를 주어진 절단 평면을 기준으로 두 부분으로 자릅니다.
    /// 잘린 부분 중 한 쪽은 원본 오브젝트에 유지되고, 다른 한 쪽은 새 오브젝트로 생성되어 물리 효과가 적용된 후 자동으로 제거됩니다.
    /// </summary>
    /// <param name="originalGameObject">절단할 원본 게임 오브젝트입니다.</param>
    /// <param name="contactPoint">절단이 시작된 월드 공간의 지점입니다.</param>
    /// <param name="cutNormal">절단에 사용될 세이버 스윙의 월드 공간 방향입니다. 이 방향을 기준으로 절단면의 법선이 계산됩니다.</param>
    public static void Cut(GameObject originalGameObject, Vector3 contactPoint, Vector3 cutNormal)
    {
        if (isBusy)
            return;

        isBusy = true;

        MeshFilter meshFilter = originalGameObject.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = originalGameObject.GetComponent<MeshRenderer>();

        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError("Need mesh to cut or MeshFilter missing.");
            isBusy = false;
            return;
        }

        originalMesh = meshFilter.mesh;
        originalVertices = originalMesh.vertices;
        originalNormals = originalMesh.normals;
        originalUVs = originalMesh.uv;

        Vector3 localSaberSwingDirection = originalGameObject.transform.InverseTransformDirection(cutNormal);
        Vector3 planeNormal = Vector3.Cross(localSaberSwingDirection, Vector3.forward);

        if (planeNormal.magnitude < 0.0001f)
        {
            planeNormal = Vector3.up;
        }

        Plane cutPlane = new Plane(planeNormal, originalGameObject.transform.InverseTransformPoint(contactPoint));

        List<Vector3> addedVertices = new List<Vector3>();
        GeneratedMesh leftMesh = new GeneratedMesh();
        GeneratedMesh rightMesh = new GeneratedMesh();

        SeparateMeshes(leftMesh, rightMesh, cutPlane, addedVertices);
        FillCut(addedVertices, cutPlane, leftMesh, rightMesh);

        Mesh finishedLeftMesh = leftMesh.GetGeneratedMesh();
        Mesh finishedRightMesh = rightMesh.GetGeneratedMesh();

        var originalCols = originalGameObject.GetComponents<Collider>();
        foreach (var col in originalCols)
            Destroy(col);

        meshFilter.mesh = finishedLeftMesh;
        var collider = originalGameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = finishedLeftMesh;
        collider.convex = true;
        collider.isTrigger = true;

        Material[] mats = new Material[finishedLeftMesh.subMeshCount];
        for (int i = 0; i < finishedLeftMesh.subMeshCount; i++)
        {
            mats[i] = meshRenderer.material;
        }
        meshRenderer.materials = mats;

        GameObject right = new GameObject("CutPiece_Right");
        right.transform.position = originalGameObject.transform.position + (Vector3.up * .05f);
        right.transform.rotation = originalGameObject.transform.rotation;
        right.transform.localScale = originalGameObject.transform.localScale;

        MeshRenderer rightMeshRenderer = right.AddComponent<MeshRenderer>();
        mats = new Material[finishedRightMesh.subMeshCount];
        for (int i = 0; i < finishedRightMesh.subMeshCount; i++)
        {
            mats[i] = meshRenderer.material; // 원본 메쉬의 머티리얼 재사용
        }
        rightMeshRenderer.materials = mats;
        right.AddComponent<MeshFilter>().mesh = finishedRightMesh;

        MeshCollider rightCollider = right.AddComponent<MeshCollider>();
        rightCollider.sharedMesh = finishedRightMesh;
        rightCollider.convex = true;
        rightCollider.isTrigger = false;

        Rigidbody rightRigidbody = right.AddComponent<Rigidbody>();
        rightRigidbody.AddRelativeForce(-cutPlane.normal * 250f);
        rightRigidbody.useGravity = true;

        Destroy(right, 1.0f);

        isBusy = false;
    }

    /// <summary>
    /// 원본 메시의 모든 서브메시의 모든 삼각형을 순회하여,
    /// 절단 평면의 왼쪽에 있는 메시와 오른쪽에 있는 메시를 분리하여 개별 메시로 생성합니다.
    /// </summary>
    /// <param name="leftMesh">절단 평면의 왼쪽에 해당하는 메시 데이터를 담을 GeneratedMesh 객체입니다.</param>
    /// <param name="rightMesh">절단 평면의 오른쪽에 해당하는 메시 데이터를 담을 GeneratedMesh 객체입니다.</param>
    /// <param name="plane">메시를 분리하는 데 사용될 절단 평면입니다.</param>
    /// <param name="addedVertices">절단 과정에서 새로 추가된 정점들을 저장하는 리스트입니다.</param>
    private static void SeparateMeshes(GeneratedMesh leftMesh, GeneratedMesh rightMesh, Plane plane, List<Vector3> addedVertices)
    {
        for (int i = 0; i < originalMesh.subMeshCount; i++)
        {
            var subMeshIndices = originalMesh.GetTriangles(i);

            for (int j = 0; j < subMeshIndices.Length; j += 3)
            {
                var triangleIndexA = subMeshIndices[j];
                var triangleIndexB = subMeshIndices[j + 1];
                var triangleIndexC = subMeshIndices[j + 2];

                MeshTriangle currentTriangle = GetTriangle(triangleIndexA, triangleIndexB, triangleIndexC, i);

                bool triangleALeftSide = plane.GetSide(originalVertices[triangleIndexA]);
                bool triangleBLeftSide = plane.GetSide(originalVertices[triangleIndexB]);
                bool triangleCLeftSide = plane.GetSide(originalVertices[triangleIndexC]);

                switch (triangleALeftSide)
                {
                    case true when triangleBLeftSide && triangleCLeftSide:
                        leftMesh.AddTriangle(currentTriangle);
                        break;
                    case false when !triangleBLeftSide && !triangleCLeftSide:
                        rightMesh.AddTriangle(currentTriangle);
                        break;
                    default:
                        CutTriangle(plane, currentTriangle, triangleALeftSide, triangleBLeftSide, triangleCLeftSide, leftMesh, rightMesh, addedVertices);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// 삼각형의 세 정점을 MeshTriangle 객체로 반환하여 코드 가독성을 높입니다.
    /// </summary>
    /// <param name="_triangleIndexA">첫 번째 정점의 인덱스입니다.</param>
    /// <param name="_triangleIndexB">두 번째 정점의 인덱스입니다.</param>
    /// <param name="_triangleIndexC">세 번째 정점의 인덱스입니다.</param>
    /// <param name="_submeshIndex">해당 삼각형이 속한 서브메시의 인덱스입니다.</param>
    /// <returns>생성된 MeshTriangle 객체입니다.</returns>
    private static MeshTriangle GetTriangle(int _triangleIndexA, int _triangleIndexB, int _triangleIndexC, int _submeshIndex)
    {
        Vector3[] verticesToAdd = {
            originalVertices[_triangleIndexA],
            originalVertices[_triangleIndexB],
            originalVertices[_triangleIndexC]
        };

        Vector3[] normalsToAdd = {
            originalNormals[_triangleIndexA],
            originalNormals[_triangleIndexB],
            originalNormals[_triangleIndexC]
        };

        Vector2[] uvsToAdd = {
            originalUVs[_triangleIndexA],
            originalUVs[_triangleIndexB],
            originalUVs[_triangleIndexC]
        };

        return new MeshTriangle(verticesToAdd, normalsToAdd, uvsToAdd, _submeshIndex);
    }

    /// <summary>
    /// 절단 평면의 양쪽에 걸쳐 있는 삼각형을 잘라내어,
    /// 양쪽에 온전한 삼각형을 만들기 위해 필요한 추가 정점들을 생성하고 각 메시(좌/우)에 추가합니다.
    /// </summary>
    /// <param name="plane">절단 평면입니다.</param>
    /// <param name="triangle">절단할 메시 삼각형입니다.</param>
    /// <param name="triangleALeftSide">삼각형 첫 번째 정점이 평면의 왼쪽에 있는지 여부입니다.</param>
    /// <param name="triangleBLeftSide">삼각형 두 번째 정점이 평면의 왼쪽에 있는지 여부입니다.</param>
    /// <param name="triangleCLeftSide">삼각형 세 번째 정점이 평면의 왼쪽에 있는지 여부입니다.</param>
    /// <param name="leftMesh">평면의 왼쪽에 생성될 메시를 담을 GeneratedMesh 객체입니다.</param>
    /// <param name="rightMesh">평면의 오른쪽에 생성될 메시를 담을 GeneratedMesh 객체입니다.</param>
    /// <param name="addedVertices">절단 과정에서 새로 추가된 정점들을 저장하는 리스트입니다.</param>
    private static void CutTriangle(Plane plane, MeshTriangle triangle, bool triangleALeftSide, bool triangleBLeftSide, bool triangleCLeftSide,
    GeneratedMesh leftMesh, GeneratedMesh rightMesh, List<Vector3> addedVertices)
    {
        List<bool> leftSide = new List<bool>();
        leftSide.Add(triangleALeftSide);
        leftSide.Add(triangleBLeftSide);
        leftSide.Add(triangleCLeftSide);

        MeshTriangle leftMeshTriangle = new MeshTriangle(new Vector3[2], new Vector3[2], new Vector2[2], triangle.SubmeshIndex);
        MeshTriangle rightMeshTriangle = new MeshTriangle(new Vector3[2], new Vector3[2], new Vector2[2], triangle.SubmeshIndex);

        bool left = false;
        bool right = false;

        for (int i = 0; i < 3; i++)
        {
            if (leftSide[i])
            {
                if (!left)
                {
                    left = true;

                    leftMeshTriangle.Vertices[0] = triangle.Vertices[i];
                    leftMeshTriangle.Vertices[1] = leftMeshTriangle.Vertices[0];

                    leftMeshTriangle.UVs[0] = triangle.UVs[i];
                    leftMeshTriangle.UVs[1] = leftMeshTriangle.UVs[0];

                    leftMeshTriangle.Normals[0] = triangle.Normals[i];
                    leftMeshTriangle.Normals[1] = leftMeshTriangle.Normals[0];
                }
                else
                {
                    leftMeshTriangle.Vertices[1] = triangle.Vertices[i];
                    leftMeshTriangle.Normals[1] = triangle.Normals[i];
                    leftMeshTriangle.UVs[1] = triangle.UVs[i];
                }
            }
            else
            {
                if (!right)
                {
                    right = true;

                    rightMeshTriangle.Vertices[0] = triangle.Vertices[i];
                    rightMeshTriangle.Vertices[1] = rightMeshTriangle.Vertices[0];

                    rightMeshTriangle.UVs[0] = triangle.UVs[i];
                    rightMeshTriangle.UVs[1] = rightMeshTriangle.UVs[0];

                    rightMeshTriangle.Normals[0] = triangle.Normals[i];
                    rightMeshTriangle.Normals[1] = rightMeshTriangle.Normals[0];

                }
                else
                {
                    rightMeshTriangle.Vertices[1] = triangle.Vertices[i];
                    rightMeshTriangle.Normals[1] = triangle.Normals[i];
                    rightMeshTriangle.UVs[1] = triangle.UVs[i];
                }
            }
        }

        float normalizedDistance;
        float distance;
        plane.Raycast(new Ray(leftMeshTriangle.Vertices[0], (rightMeshTriangle.Vertices[0] - leftMeshTriangle.Vertices[0]).normalized), out distance);

        normalizedDistance = distance / (rightMeshTriangle.Vertices[0] - leftMeshTriangle.Vertices[0]).magnitude;
        Vector3 vertLeft = Vector3.Lerp(leftMeshTriangle.Vertices[0], rightMeshTriangle.Vertices[0], normalizedDistance);
        addedVertices.Add(vertLeft);

        Vector3 normalLeft = Vector3.Lerp(leftMeshTriangle.Normals[0], rightMeshTriangle.Normals[0], normalizedDistance);
        Vector2 uvLeft = Vector2.Lerp(leftMeshTriangle.UVs[0], rightMeshTriangle.UVs[0], normalizedDistance);

        plane.Raycast(new Ray(leftMeshTriangle.Vertices[1], (rightMeshTriangle.Vertices[1] - leftMeshTriangle.Vertices[1]).normalized), out distance);

        normalizedDistance = distance / (rightMeshTriangle.Vertices[1] - leftMeshTriangle.Vertices[1]).magnitude;
        Vector3 vertRight = Vector3.Lerp(leftMeshTriangle.Vertices[1], rightMeshTriangle.Vertices[1], normalizedDistance);
        addedVertices.Add(vertRight);

        Vector3 normalRight = Vector3.Lerp(leftMeshTriangle.Normals[1], rightMeshTriangle.Normals[1], normalizedDistance);
        Vector2 uvRight = Vector2.Lerp(leftMeshTriangle.UVs[1], rightMeshTriangle.UVs[1], normalizedDistance);

        MeshTriangle currentTriangle;
        Vector3[] updatedVertices = { leftMeshTriangle.Vertices[0], vertLeft, vertRight };
        Vector3[] updatedNormals = { leftMeshTriangle.Normals[0], normalLeft, normalRight };
        Vector2[] updatedUVs = { leftMeshTriangle.UVs[0], uvLeft, uvRight };

        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, triangle.SubmeshIndex);

        if (updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if (Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0], updatedVertices[2] - updatedVertices[0]), updatedNormals[0]) < 0)
            {
                FlipTriangel(currentTriangle);
            }
            leftMesh.AddTriangle(currentTriangle);
        }

        updatedVertices = new Vector3[] { leftMeshTriangle.Vertices[0], leftMeshTriangle.Vertices[1], vertRight };
        updatedNormals = new Vector3[] { leftMeshTriangle.Normals[0], leftMeshTriangle.Normals[1], normalRight };
        updatedUVs = new Vector2[] { leftMeshTriangle.UVs[0], leftMeshTriangle.UVs[1], uvRight };


        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, triangle.SubmeshIndex);
        if (updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if (Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0], updatedVertices[2] - updatedVertices[0]), updatedNormals[0]) < 0)
            {
                FlipTriangel(currentTriangle);
            }
            leftMesh.AddTriangle(currentTriangle);
        }

        updatedVertices = new Vector3[] { rightMeshTriangle.Vertices[0], vertLeft, vertRight };
        updatedNormals = new Vector3[] { rightMeshTriangle.Normals[0], normalLeft, normalRight };
        updatedUVs = new Vector2[] { rightMeshTriangle.UVs[0], uvLeft, uvRight };

        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, triangle.SubmeshIndex);
        if (updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if (Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0], updatedVertices[2] - updatedVertices[0]), updatedNormals[0]) < 0)
            {
                FlipTriangel(currentTriangle);
            }
            rightMesh.AddTriangle(currentTriangle);
        }

        updatedVertices = new Vector3[] { rightMeshTriangle.Vertices[0], rightMeshTriangle.Vertices[1], vertRight };
        updatedNormals = new Vector3[] { rightMeshTriangle.Normals[0], rightMeshTriangle.Normals[1], normalRight };
        updatedUVs = new Vector2[] { rightMeshTriangle.UVs[0], rightMeshTriangle.UVs[1], uvRight };

        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, triangle.SubmeshIndex);
        if (updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if (Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0], updatedVertices[2] - updatedVertices[0]), updatedNormals[0]) < 0)
            {
                FlipTriangel(currentTriangle);
            }
            rightMesh.AddTriangle(currentTriangle);
        }
    }

    /// <summary>
    /// 주어진 메시 삼각형의 정점 순서를 뒤집어 면의 법선 방향을 반전시킵니다.
    /// 이는 메시가 잘못된 방향을 향할 때 면이 올바르게 렌더링되도록 합니다.
    /// </summary>
    /// <param name="_triangle">뒤집을 메시 삼각형입니다.</param>
    private static void FlipTriangel(MeshTriangle _triangle)
    {
        Vector3 temp = _triangle.Vertices[2];
        _triangle.Vertices[2] = _triangle.Vertices[0];
        _triangle.Vertices[0] = temp;

        temp = _triangle.Normals[2];
        _triangle.Normals[2] = _triangle.Normals[0];
        _triangle.Normals[0] = temp;

        (_triangle.UVs[2], _triangle.UVs[0]) = (_triangle.UVs[0], _triangle.UVs[2]);
    }

    /// <summary>
    /// 절단 과정에서 새로 추가된 정점들을 이용하여 절단면을 채웁니다.
    /// 이 과정은 잘린 메시의 내부를 닫아 시각적으로 온전하게 보이도록 합니다.
    /// </summary>
    /// <param name="_addedVertices">절단 과정에서 생성된 새로운 정점들의 리스트입니다.</param>
    /// <param name="_plane">절단에 사용된 평면입니다.</param>
    /// <param name="_leftMesh">절단된 메시의 왼쪽 부분을 나타내는 GeneratedMesh 객체입니다.</param>
    /// <param name="_rightMesh">절단된 메시의 오른쪽 부분을 나타내는 GeneratedMesh 객체입니다.</param>
    public static void FillCut(List<Vector3> _addedVertices, Plane _plane, GeneratedMesh _leftMesh, GeneratedMesh _rightMesh)
    {
        // HashSet을 사용하여 정점 중복 검사를 O(1)에 가깝게 수행
        HashSet<Vector3> processedVertices = new HashSet<Vector3>();
        List<Vector3> polygon = new List<Vector3>();

        for (int i = 0; i < _addedVertices.Count; i += 2) // 정점은 항상 쌍으로 추가되므로 i+=2로 건너뛰어도 됨
        {
            // 이미 처리된 정점 쌍의 시작점을 건너뜁니다.
            if (!processedVertices.Contains(_addedVertices[i]))
            {
                polygon.Clear();
                polygon.Add(_addedVertices[i]);
                polygon.Add(_addedVertices[i + 1]);

                processedVertices.Add(_addedVertices[i]);
                processedVertices.Add(_addedVertices[i + 1]);

                EvaluatePairs(_addedVertices, processedVertices, polygon); // HashSet으로 변경
                Fill(polygon, _plane, _leftMesh, _rightMesh);
            }
        }
    }

    /// <summary>
    /// 주어진 정점 쌍들을 평가하여 폴리곤을 완성합니다.
    /// 이는 절단면에 생긴 복잡한 다각형을 구성하는 데 사용됩니다.
    /// </summary>
    /// <param name="_addedVertices">절단 과정에서 추가된 모든 정점 쌍의 리스트입니다.</param>
    /// <param name="processedVertices">이미 처리된 정점들을 추적하는 HashSet입니다.</param>
    /// <param name="_polygone">현재 구성 중인 폴리곤의 정점 리스트입니다.</param>
    public static void EvaluatePairs(List<Vector3> _addedVertices, HashSet<Vector3> processedVertices, List<Vector3> _polygone)
    {
        bool isDone = false;
        while (!isDone)
        {
            isDone = true;
            for (int i = 0; i < _addedVertices.Count; i += 2)
            {
                // 현재 폴리곤의 마지막 정점과 _addedVertices[i]가 일치하고, _addedVertices[i+1]이 아직 처리되지 않았다면
                if (_addedVertices[i] == _polygone[_polygone.Count - 1] && !processedVertices.Contains(_addedVertices[i + 1]))
                {
                    isDone = false;
                    _polygone.Add(_addedVertices[i + 1]);
                    processedVertices.Add(_addedVertices[i + 1]);
                }
                // 현재 폴리곤의 마지막 정점과 _addedVertices[i+1]이 일치하고, _addedVertices[i]가 아직 처리되지 않았다면
                else if (_addedVertices[i + 1] == _polygone[_polygone.Count - 1] && !processedVertices.Contains(_addedVertices[i]))
                {
                    isDone = false;
                    _polygone.Add(_addedVertices[i]);
                    processedVertices.Add(_addedVertices[i]);
                }
            }
        }
    }

    /// <summary>
    /// 완성된 폴리곤과 절단 평면을 사용하여 잘린 메시의 절단면을 삼각형으로 채웁니다.
    /// 이는 잘린 면에 텍스처 좌표와 법선을 부여하여 시각적으로 올바르게 렌더링되도록 합니다.
    /// </summary>
    /// <param name="_vertices">절단면을 구성하는 폴리곤의 정점 리스트입니다.</param>
    /// <param name="_plane">절단에 사용된 평면입니다.</param>
    /// <param name="_leftMesh">왼쪽 메시를 나타내는 GeneratedMesh 객체입니다.</param>
    /// <param name="_rightMesh">오른쪽 메시를 나타내는 GeneratedMesh 객체입니다.</param>
    private static void Fill(List<Vector3> _vertices, Plane _plane, GeneratedMesh _leftMesh, GeneratedMesh _rightMesh)
    {
        Vector3 centerPosition = Vector3.zero;
        for (int i = 0; i < _vertices.Count; i++)
        {
            centerPosition += _vertices[i];
        }
        centerPosition /= _vertices.Count;

        Vector3 planeNormal = _plane.normal;
        Vector3 up = new Vector3(planeNormal.x, planeNormal.y, planeNormal.z);
        Vector3 left = Vector3.Cross(planeNormal, up);

        // UV 계산을 위한 평면상의 정규화된 축 사용 (필요하다면 더 최적화된 UV 매핑 방식 고려)
        if (left.sqrMagnitude < 0.0001f) // left가 0벡터에 가까우면 수직 평면이므로 다른 축 사용
        {
            left = Vector3.Cross(planeNormal, Vector3.up);
            if (left.sqrMagnitude < 0.0001f) // 여전히 0벡터에 가까우면 다른 축 사용 (Z축)
            {
                left = Vector3.Cross(planeNormal, Vector3.forward);
            }
        }
        left.Normalize();
        up = Vector3.Cross(left, planeNormal).normalized;


        for (int i = 0; i < _vertices.Count; i++)
        {
            Vector3 currentVertex = _vertices[i];
            Vector3 nextVertex = _vertices[(i + 1) % _vertices.Count];

            Vector3 displacement1 = currentVertex - centerPosition;
            Vector2 uv1 = new Vector2(0.5f + Vector3.Dot(displacement1, left), 0.5f + Vector3.Dot(displacement1, up));

            Vector3 displacement2 = nextVertex - centerPosition;
            Vector2 uv2 = new Vector2(0.5f + Vector3.Dot(displacement2, left), 0.5f + Vector3.Dot(displacement2, up));

            Vector3[] vertices = { currentVertex, nextVertex, centerPosition };
            Vector3[] normals = { -planeNormal, -planeNormal, -planeNormal };
            Vector2[] uvs = { uv1, uv2, new(0.5f, 0.5f) };

            MeshTriangle currentTriangle = new MeshTriangle(vertices, normals, uvs, originalMesh.subMeshCount + 1);

            if (Vector3.Dot(Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]), normals[0]) < 0)
            {
                FlipTriangel(currentTriangle);
            }
            _leftMesh.AddTriangle(currentTriangle);

            normals = new[] { planeNormal, planeNormal, planeNormal };
            currentTriangle = new MeshTriangle(vertices, normals, uvs, originalMesh.subMeshCount + 1);

            if (Vector3.Dot(Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]), normals[0]) < 0)
            {
                FlipTriangel(currentTriangle);
            }
            _rightMesh.AddTriangle(currentTriangle);
        }
    }
}