using System.Collections.Generic;
using UnityEngine;

public class Cutter : MonoBehaviour
{
    private static bool isBusy;
    private static Mesh originalMesh;
    private static Vector3[] originalVertices;
    private static Vector3[] originalNormals;
    private static Vector2[] originalUVs;

    // 절단면에 사용될 머티리얼을 외부에서 설정할 수 있도록 추가
    [Tooltip("The material to be applied to the cut surface. If null, the original mesh's first material will be used.")]
    public static Material cutPlaneMaterial; // 인스펙터에서 설정하거나 코드에서 할당

    // 부동 소수점 비교를 위한 작은 임계값
    private const float Epsilon = 0.0001f; // 매우 작은 값, 필요에 따라 조정 가능

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
        {
            Debug.LogWarning("Cutter is busy, skipping cut operation for " + originalGameObject.name);
            return;
        }

        isBusy = true; // 작업 시작 플래그 설정

        MeshFilter meshFilter = originalGameObject.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = originalGameObject.GetComponent<MeshRenderer>();

        if (meshFilter == null || meshFilter.mesh == null)
        {
            Debug.LogError("Cutter: Need mesh to cut or MeshFilter missing on " + originalGameObject.name);
            isBusy = false; // 에러 시 즉시 해제
            return;
        }

        // try-finally 블록을 사용하여 isBusy 상태를 항상 올바르게 관리
        try
        {
            originalMesh = meshFilter.mesh;
            originalVertices = originalMesh.vertices;
            originalNormals = originalMesh.normals;
            originalUVs = originalMesh.uv;

            // === 변경된 부분: 절단 평면의 법선을 cutNormal (세이버 스윙 방향)으로 직접 사용 ===
            // cutNormal은 이미 월드 공간 방향이며 정규화되어 들어온다고 가정합니다.
            Vector3 worldPlaneNormal = cutNormal.normalized; 

            // 절단 평면의 법선과 시작점은 오브젝트의 로컬 공간으로 변환되어야 합니다.
            Vector3 localPlaneNormal = originalGameObject.transform.InverseTransformDirection(worldPlaneNormal);
            Vector3 localContactPoint = originalGameObject.transform.InverseTransformPoint(contactPoint);
            
            // Plane 생성: 법선은 로컬 공간의 planeNormal, 점은 로컬 공간의 contactPoint
            Plane cutPlane = new Plane(localPlaneNormal, localContactPoint);
            // =======================================================================

            List<Vector3> addedVertices = new List<Vector3>();
            GeneratedMesh leftMesh = new GeneratedMesh();
            GeneratedMesh rightMesh = new GeneratedMesh();

            SeparateMeshes(leftMesh, rightMesh, cutPlane, addedVertices);
            FillCut(addedVertices, cutPlane, leftMesh, rightMesh);

            Mesh finishedLeftMesh = leftMesh.GetGeneratedMesh();
            Mesh finishedRightMesh = rightMesh.GetGeneratedMesh();

            // 메시가 유효한지 확인 (삼각형이 하나라도 있는지)
            if (finishedLeftMesh == null || finishedLeftMesh.vertexCount == 0 || finishedRightMesh == null || finishedRightMesh.vertexCount == 0)
            {
                Debug.LogWarning("Cutter: One or both cut meshes are empty or null. Skipping cut operation for " + originalGameObject.name);
                // null 체크를 추가하여 혹시 모를 GetGeneratedMesh 실패 상황에 대비
                if (finishedLeftMesh != null) Destroy(finishedLeftMesh);
                if (finishedRightMesh != null) Destroy(finishedRightMesh);
                return;
            }

            // 기존 콜라이더 제거
            var originalCols = originalGameObject.GetComponents<Collider>();
            foreach (var col in originalCols)
                Destroy(col);

            // 원본 오브젝트 (왼쪽) 설정
            meshFilter.mesh = finishedLeftMesh;
            MeshCollider leftCollider = originalGameObject.AddComponent<MeshCollider>();
            leftCollider.sharedMesh = finishedLeftMesh;
            leftCollider.convex = true;
            leftCollider.isTrigger = true; // 물리적 상호작용 의도에 따라 조정 필요 (예: 검이 통과해야 하므로 Trigger)

            // 머티리얼 할당 개선: 기존 서브메시 + 새로운 서브메시 (절단면)
            Material[] originalMaterials = meshRenderer.materials;
            int totalSubMeshesLeft = finishedLeftMesh.subMeshCount;
            Material[] newLeftMaterials = new Material[totalSubMeshesLeft];

            for (int i = 0; i < totalSubMeshesLeft; i++)
            {
                if (i < originalMaterials.Length)
                {
                    newLeftMaterials[i] = originalMaterials[i];
                }
                else if (i == originalMesh.subMeshCount) // 새로운 절단면 서브메시 인덱스
                {
                    newLeftMaterials[i] = cutPlaneMaterial != null ? cutPlaneMaterial : (originalMaterials.Length > 0 ? originalMaterials[0] : null);
                }
                else // 혹시 모를 경우를 대비하여 기본 머티리얼 할당
                {
                    newLeftMaterials[i] = originalMaterials.Length > 0 ? originalMaterials[0] : null;
                }
                if (newLeftMaterials[i] == null) Debug.LogError("Cutter: Missing material for submesh " + i + " on left part of " + originalGameObject.name);
            }
            meshRenderer.materials = newLeftMaterials;


            // 오른쪽 조각 오브젝트 생성
            GameObject right = new GameObject("CutPiece_Right_" + originalGameObject.name);
            right.transform.position = originalGameObject.transform.position;
            right.transform.rotation = originalGameObject.transform.rotation;
            right.transform.localScale = originalGameObject.transform.localScale;

            MeshRenderer rightMeshRenderer = right.AddComponent<MeshRenderer>();
            int totalSubMeshesRight = finishedRightMesh.subMeshCount;
            Material[] newRightMaterials = new Material[totalSubMeshesRight];
            for (int i = 0; i < totalSubMeshesRight; i++)
            {
                if (i < originalMaterials.Length)
                {
                    newRightMaterials[i] = originalMaterials[i];
                }
                else if (i == originalMesh.subMeshCount) // 새로운 절단면 서브메시 인덱스
                {
                    newRightMaterials[i] = cutPlaneMaterial != null ? cutPlaneMaterial : (originalMaterials.Length > 0 ? originalMaterials[0] : null);
                }
                else
                {
                    newRightMaterials[i] = originalMaterials.Length > 0 ? originalMaterials[0] : null;
                }
                if (newRightMaterials[i] == null) Debug.LogError("Cutter: Missing material for submesh " + i + " on right part of " + originalGameObject.name);
            }
            rightMeshRenderer.materials = newRightMaterials;
            right.AddComponent<MeshFilter>().mesh = finishedRightMesh;

            MeshCollider rightCollider = right.AddComponent<MeshCollider>();
            rightCollider.sharedMesh = finishedRightMesh;
            rightCollider.convex = true;
            rightCollider.isTrigger = false; // 잘린 조각은 물리적 상호작용 (바닥에 떨어지도록)

            Rigidbody rightRigidbody = right.AddComponent<Rigidbody>();
            // 절단면에 수직 방향으로 힘을 가해 분리
            // 힘의 방향은 localPlaneNormal의 월드 공간 변환 방향에 250f의 힘을 가합니다.
            // CutNormal 방향으로 노트를 쳐서 분리하는 것이 일반적이므로 -localPlaneNormal 대신 worldPlaneNormal에 힘을 주는 것이 더 자연스러울 수 있습니다.
            // 여기서는 잘린 면이 튀어나가는 방향을 기준으로 (즉, localPlaneNormal의 반대 방향으로) 힘을 가합니다.
            // 이는 세이버가 지나간 방향으로 조각이 밀려나가는 것처럼 보일 것입니다.
            rightRigidbody.AddForceAtPosition(originalGameObject.transform.TransformDirection(-localPlaneNormal) * 250f, contactPoint, ForceMode.Impulse);
            rightRigidbody.useGravity = true;

            Destroy(right, 2.0f); // 2초 후 제거 (물리 시뮬레이션을 충분히 볼 수 있도록)
        }
        catch (System.Exception e)
        {
            Debug.LogError("Cutter: An error occurred during cutting on " + originalGameObject.name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            isBusy = false; // 작업 완료 또는 오류 발생 시 항상 해제
        }
    }

    /// <summary>
    /// 원본 메시의 모든 서브메시의 모든 삼각형을 순회하여,
    /// 절단 평면의 왼쪽에 있는 메시와 오른쪽에 있는 메시를 분리하여 개별 메시로 생성합니다.
    /// </summary>
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

                // Plane.GetSide는 평면의 양쪽 중 어느 쪽에 점이 있는지 확인합니다.
                // 부동 소수점 오차를 고려하여 Plane.GetSide 결과를 직접 사용합니다.
                bool triangleALeftSide = plane.GetSide(currentTriangle.Vertices[0]);
                bool triangleBLeftSide = plane.GetSide(currentTriangle.Vertices[1]);
                bool triangleCLeftSide = plane.GetSide(currentTriangle.Vertices[2]);

                // 모든 정점이 한 쪽에 있는 경우
                if (triangleALeftSide && triangleBLeftSide && triangleCLeftSide)
                {
                    leftMesh.AddTriangle(currentTriangle);
                }
                else if (!triangleALeftSide && !triangleBLeftSide && !triangleCLeftSide)
                {
                    rightMesh.AddTriangle(currentTriangle);
                }
                else // 삼각형이 평면을 가로지르는 경우
                {
                    CutTriangle(plane, currentTriangle, triangleALeftSide, triangleBLeftSide, triangleCLeftSide, leftMesh, rightMesh, addedVertices);
                }
            }
        }
    }

    /// <summary>
    /// 삼각형의 세 정점을 MeshTriangle 객체로 반환하여 코드 가독성을 높입니다.
    /// </summary>
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
    private static void CutTriangle(Plane plane, MeshTriangle triangle, bool triangleALeftSide, bool triangleBLeftSide, bool triangleCLeftSide,
                                    GeneratedMesh leftMesh, GeneratedMesh rightMesh, List<Vector3> addedVertices)
    {
        List<bool> leftSideFlags = new List<bool> { triangleALeftSide, triangleBLeftSide, triangleCLeftSide };

        List<int> leftIndices = new List<int>();
        List<int> rightIndices = new List<int>();

        for (int i = 0; i < 3; i++)
        {
            if (leftSideFlags[i])
                leftIndices.Add(i);
            else
                rightIndices.Add(i);
        }

        // 교차점 계산을 위한 도우미 함수
        (Vector3 vert, Vector3 normal, Vector2 uv) GetIntersection(Vector3 v1, Vector3 v2, Vector3 n1, Vector3 n2, Vector2 uv1, Vector2 uv2, Plane p)
        {
            Vector3 lineDir = v2 - v1;
            float dotNormalLine = Vector3.Dot(plane.normal, lineDir);

            if (Mathf.Abs(dotNormalLine) < Epsilon)
            {
                // 선분이 평면과 거의 평행한 경우 (이전처럼 에러를 내지 않고, 중간 지점을 반환)
                // 이 상황은 매우 드물어야 하지만, 만약을 대비하여 Lerp로 안전하게 처리
                Debug.LogWarning("CutTriangle: Line is almost parallel to plane. Using mid-point as intersection.");
                return (Vector3.Lerp(v1, v2, 0.5f), Vector3.Lerp(n1, n2, 0.5f), Vector2.Lerp(uv1, uv2, 0.5f));
            }

            float t = Vector3.Dot(plane.normal, plane.normal * plane.distance - v1) / dotNormalLine; 
            t = Mathf.Clamp01(t); 

            Vector3 newVert = Vector3.Lerp(v1, v2, t);
            Vector3 newNormal = Vector3.Lerp(n1, n2, t);
            Vector2 newUV = Vector2.Lerp(uv1, uv2, t);

            return (newVert, newNormal, newUV);
        }

        // 교차점 1: 왼쪽에 있는 정점 중 하나와 오른쪽에 있는 정점 중 하나를 연결하는 선분
        var (vert1, normal1, uv1) = GetIntersection(
            triangle.Vertices[leftIndices[0]], triangle.Vertices[rightIndices[0]],
            triangle.Normals[leftIndices[0]], triangle.Normals[rightIndices[0]],
            triangle.UVs[leftIndices[0]], triangle.UVs[rightIndices[0]], plane);

        // 교차점 2: 나머지 한 쌍의 정점을 연결하는 선분
        var (vert2, normal2, uv2) = GetIntersection(
            triangle.Vertices[leftIndices[leftIndices.Count - 1]], triangle.Vertices[rightIndices[rightIndices.Count - 1]],
            triangle.Normals[leftIndices[leftIndices.Count - 1]], triangle.Normals[rightIndices[rightIndices.Count - 1]],
            triangle.UVs[leftIndices[leftIndices.Count - 1]], triangle.UVs[rightIndices[rightIndices.Count - 1]], plane);

        // 새로운 정점을 addedVertices에 추가 (절단면 생성을 위함)
        // 두 교차점 순서가 일관되도록 정렬 (예: X좌표 기준)
        // 이 정렬은 FillCut에서 폴리곤을 구성할 때 일관성을 유지하는 데 도움이 됩니다.
        if (vert1.x > vert2.x) // 간단한 정렬 방식, 복잡한 경우 Vector3.ProjectOnPlane 등 활용
        {
            (vert1, vert2) = (vert2, vert1);
            (normal1, normal2) = (normal2, normal1);
            (uv1, uv2) = (uv2, uv1);
        }

        addedVertices.Add(vert1);
        addedVertices.Add(vert2);

        // --- 왼쪽 메쉬를 구성하는 삼각형 ---
        if (leftIndices.Count == 2) // 2개의 정점이 왼쪽에, 1개의 정점이 오른쪽에 있는 경우 (Left: A, B; Right: C)
        {
            // 왼쪽 1: (Left[0], Vert1, Vert2)
            leftMesh.AddTriangle(new MeshTriangle(
                new Vector3[] { triangle.Vertices[leftIndices[0]], vert1, vert2 },
                new Vector3[] { triangle.Normals[leftIndices[0]], normal1, normal2 },
                new Vector2[] { triangle.UVs[leftIndices[0]], uv1, uv2 },
                triangle.SubmeshIndex));

            // 왼쪽 2: (Left[0], Left[1], Vert2)
            leftMesh.AddTriangle(new MeshTriangle(
                new Vector3[] { triangle.Vertices[leftIndices[0]], triangle.Vertices[leftIndices[1]], vert2 },
                new Vector3[] { triangle.Normals[leftIndices[0]], triangle.Normals[leftIndices[1]], normal2 },
                new Vector2[] { triangle.UVs[leftIndices[0]], triangle.UVs[leftIndices[1]], uv2 },
                triangle.SubmeshIndex));

            // --- 오른쪽 메쉬를 구성하는 삼각형 ---
            // 오른쪽 1: (Right[0], Vert2, Vert1) - 법선 방향 유지를 위해 순서 중요
            rightMesh.AddTriangle(new MeshTriangle(
                new Vector3[] { triangle.Vertices[rightIndices[0]], vert2, vert1 },
                new Vector3[] { triangle.Normals[rightIndices[0]], normal2, normal1 },
                new Vector2[] { triangle.UVs[rightIndices[0]], uv2, uv1 },
                triangle.SubmeshIndex));
        }
        else // leftIndices.Count == 1 // 1개의 정점이 왼쪽에, 2개의 정점이 오른쪽에 있는 경우 (Left: A; Right: B, C)
        {
            // --- 왼쪽 메쉬를 구성하는 삼각형 ---
            // 왼쪽 1: (Left[0], Vert1, Vert2)
            leftMesh.AddTriangle(new MeshTriangle(
                new Vector3[] { triangle.Vertices[leftIndices[0]], vert1, vert2 },
                new Vector3[] { triangle.Normals[leftIndices[0]], normal1, normal2 },
                new Vector2[] { triangle.UVs[leftIndices[0]], uv1, uv2 },
                triangle.SubmeshIndex));

            // --- 오른쪽 메쉬를 구성하는 삼각형 ---
            // 오른쪽 1: (Right[0], Vert1, Right[1])
            rightMesh.AddTriangle(new MeshTriangle(
                new Vector3[] { triangle.Vertices[rightIndices[0]], vert1, triangle.Vertices[rightIndices[1]] },
                new Vector3[] { triangle.Normals[rightIndices[0]], normal1, triangle.Normals[rightIndices[1]] },
                new Vector2[] { triangle.UVs[rightIndices[0]], uv1, triangle.UVs[rightIndices[1]] },
                triangle.SubmeshIndex));

            // 오른쪽 2: (Right[0], Vert2, Right[1]) - 법선 방향 유지를 위해 순서 중요
            rightMesh.AddTriangle(new MeshTriangle(
                new Vector3[] { triangle.Vertices[rightIndices[0]], vert2, triangle.Vertices[rightIndices[1]] },
                new Vector3[] { triangle.Normals[rightIndices[0]], normal2, triangle.Normals[rightIndices[1]] },
                new Vector2[] { triangle.UVs[rightIndices[0]], uv2, triangle.UVs[rightIndices[1]] },
                triangle.SubmeshIndex));
        }
    }

    /// <summary>
    /// 주어진 메시 삼각형의 정점 순서를 뒤집어 면의 법선 방향을 반전시킵니다.
    /// 이는 메시가 잘못된 방향을 향할 때 면이 올바르게 렌더링되도록 합니다.
    /// </summary>
    private static void FlipTriangel(MeshTriangle _triangle)
    {
        // 정점 순서 뒤집기 (0과 2 교환)
        Vector3 tempV = _triangle.Vertices[2];
        _triangle.Vertices[2] = _triangle.Vertices[0];
        _triangle.Vertices[0] = tempV;

        // 법선 순서 뒤집기 (0과 2 교환)
        Vector3 tempN = _triangle.Normals[2];
        _triangle.Normals[2] = _triangle.Normals[0];
        _triangle.Normals[0] = tempN;

        // UV 순서 뒤집기 (0과 2 교환)
        Vector2 tempU = _triangle.UVs[2];
        _triangle.UVs[2] = _triangle.UVs[0];
        _triangle.UVs[0] = tempU;
    }

    /// <summary>
    /// 절단 과정에서 새로 추가된 정점들을 이용하여 절단면을 채웁니다.
    /// 이 과정은 잘린 메시의 내부를 닫아 시각적으로 온전하게 보이도록 합니다.
    /// </summary>
    public static void FillCut(List<Vector3> _addedVertices, Plane _plane, GeneratedMesh _leftMesh, GeneratedMesh _rightMesh)
    {
        if (_addedVertices.Count < 2) return; // 절단선이 없으면 채울 필요 없음

        HashSet<Vector3> processedVertices = new HashSet<Vector3>();
        List<Vector3> currentPolygon = new List<Vector3>();

        // _addedVertices는 (P1, P2), (P2, P3), (P3, P1)과 같은 쌍으로 구성되어야 함
        // 각 쌍은 절단면에 새로 생성된 두 정점
        for (int i = 0; i < _addedVertices.Count; i += 2)
        {
            // 이미 처리된 정점 쌍의 시작점을 건너뜁니다.
            if (ContainsApprox(processedVertices, _addedVertices[i]) && ContainsApprox(processedVertices, _addedVertices[i + 1]))
            {
                continue;
            }

            currentPolygon.Clear();
            currentPolygon.Add(_addedVertices[i]);
            currentPolygon.Add(_addedVertices[i + 1]);

            processedVertices.Add(_addedVertices[i]); // 정확한 값으로 추가
            processedVertices.Add(_addedVertices[i + 1]);

            // 이 쌍에 연결되는 다음 쌍을 찾아서 폴리곤을 완성합니다.
            EvaluatePairs(_addedVertices, processedVertices, currentPolygon);

            // 완성된 폴리곤으로 절단면을 채웁니다.
            Fill(currentPolygon, _plane, _leftMesh, _rightMesh);
        }
    }

    /// <summary>
    /// 주어진 정점 쌍들을 평가하여 폴리곤을 완성합니다.
    /// 이는 절단면에 생긴 복잡한 다각형을 구성하는 데 사용됩니다.
    /// </summary>
    public static void EvaluatePairs(List<Vector3> _addedVertices, HashSet<Vector3> processedVertices, List<Vector3> _polygon)
    {
        bool isDone = false;
        int maxIterations = _addedVertices.Count * 2; // 무한 루프 방지용 최대 반복 횟수
        int currentIteration = 0;

        while (!isDone && currentIteration < maxIterations)
        {
            isDone = true; // 이번 순회에서 더이상 추가되지 않으면 종료
            Vector3 lastVertexInPolygon = _polygon[_polygon.Count - 1];

            for (int i = 0; i < _addedVertices.Count; i += 2)
            {
                Vector3 vertA = _addedVertices[i];
                Vector3 vertB = _addedVertices[i + 1];

                // 현재 폴리곤의 마지막 정점과 _addedVertices[i]가 일치하고, _addedVertices[i+1]이 아직 폴리곤에 추가되지 않았다면
                if (Vector3.Distance(vertA, lastVertexInPolygon) < Epsilon && !ContainsApprox(_polygon, vertB))
                {
                    isDone = false;
                    _polygon.Add(vertB);
                    processedVertices.Add(vertB);
                    break; // 다음 탐색은 새로 추가된 정점에서 시작해야 하므로 break
                }
                // 현재 폴리곤의 마지막 정점과 _addedVertices[i+1]이 일치하고, _addedVertices[i]가 아직 폴리곤에 추가되지 않았다면
                else if (Vector3.Distance(vertB, lastVertexInPolygon) < Epsilon && !ContainsApprox(_polygon, vertA))
                {
                    isDone = false;
                    _polygon.Add(vertA);
                    processedVertices.Add(vertA);
                    break; // 다음 탐색은 새로 추가된 정점에서 시작해야 하므로 break
                }
            }
            currentIteration++;
        }

        if (currentIteration >= maxIterations && Vector3.Distance(_polygon[0], _polygon[_polygon.Count - 1]) >= Epsilon)
        {
            Debug.LogWarning("EvaluatePairs: Exceeded max iterations. Polygon might not be perfectly closed. Number of points: " + _polygon.Count);
        }
    }

    // List<Vector3>에 특정 Vector3가 근사적으로 포함되어 있는지 확인하는 도우미 함수
    // HashSet이 아니라 List<Vector3>에 대한 ContainsApprox를 사용하여 _polygon 내의 중복 확인
    private static bool ContainsApprox(List<Vector3> list, Vector3 target)
    {
        foreach (Vector3 v in list)
        {
            if (Vector3.Distance(v, target) < Epsilon)
            {
                return true;
            }
        }
        return false;
    }

    // HashSet<Vector3>에 특정 Vector3가 근사적으로 포함되어 있는지 확인하는 도우미 함수
    private static bool ContainsApprox(HashSet<Vector3> hashSet, Vector3 target)
    {
        foreach (Vector3 v in hashSet)
        {
            if (Vector3.Distance(v, target) < Epsilon)
            {
                return true;
            }
        }
        return false;
    }


    /// <summary>
    /// 완성된 폴리곤과 절단 평면을 사용하여 잘린 메시의 절단면을 삼각형으로 채웁니다.
    /// 이는 잘린 면에 텍스처 좌표와 법선을 부여하여 시각적으로 올바르게 렌더링되도록 합니다.
    /// </summary>
    private static void Fill(List<Vector3> _vertices, Plane _plane, GeneratedMesh _leftMesh, GeneratedMesh _rightMesh)
    {
        if (_vertices.Count < 3)
        {
            Debug.LogWarning("Fill: Polygon has less than 3 vertices, cannot form triangles. Vertices count: " + _vertices.Count);
            return; // 삼각형을 만들 수 없는 경우
        }

        // 폴리곤의 중심점 계산
        Vector3 centerPosition = Vector3.zero;
        foreach (Vector3 v in _vertices)
        {
            centerPosition += v;
        }
        centerPosition /= _vertices.Count;

        // UV 계산을 위한 로컬 축 생성 (평면상에서 UV를 매핑)
        Vector3 planeNormal = _plane.normal;
        Vector3 uAxis = Vector3.Cross(planeNormal, Vector3.up);
        if (uAxis.sqrMagnitude < Epsilon * Epsilon) // uAxis가 0에 가까우면 (planeNormal이 거의 Vector3.up 또는 Vector3.down인 경우)
        {
            uAxis = Vector3.Cross(planeNormal, Vector3.forward); // 다른 축을 사용
        }
        uAxis.Normalize();
        Vector3 vAxis = Vector3.Cross(planeNormal, uAxis).normalized; // vAxis는 uAxis와 planeNormal에 수직


        // 삼각형 팬(triangle fan) 방식으로 폴리곤 채우기 (중심점 + 두 정점)
        int cutSubmeshIndex = originalMesh.subMeshCount; // 새로운 서브메시 인덱스

        for (int i = 0; i < _vertices.Count; i++)
        {
            Vector3 currentVertex = _vertices[i];
            Vector3 nextVertex = _vertices[(i + 1) % _vertices.Count]; // 다음 정점 (마지막은 첫 번째와 연결)

            // UV 매핑: 중심을 (0,0)으로 하는 로컬 좌표를 생성하고, 이를 UV 공간의 스케일로 사용
            // 이 UV는 쉐이더에서 _MainTex_ST 속성을 사용하여 타일링을 조절해야 합니다.
            Vector2 uvCurrent = new Vector2(Vector3.Dot(currentVertex - centerPosition, uAxis),
                                            Vector3.Dot(currentVertex - centerPosition, vAxis));
            Vector2 uvNext = new Vector2(Vector3.Dot(nextVertex - centerPosition, uAxis),
                                         Vector3.Dot(nextVertex - centerPosition, vAxis));
            Vector2 uvCenter = Vector2.zero; // 중심점 UV (0,0)

            // 왼쪽 메시 (법선이 -planeNormal, 즉 절단 평면의 '안쪽'을 향함)
            Vector3[] verticesLeft = { currentVertex, nextVertex, centerPosition };
            Vector3[] normalsLeft = { -planeNormal, -planeNormal, -planeNormal };
            Vector2[] uvsLeft = { uvCurrent, uvNext, uvCenter };

            MeshTriangle triangleLeft = new MeshTriangle(verticesLeft, normalsLeft, uvsLeft, cutSubmeshIndex);
            // 법선 방향이 올바른지 확인 (시계 반대 방향 - Counter-Clockwise)
            // 즉, (V1-V0) x (V2-V0) . Normal > 0 이 되도록
            if (Vector3.Dot(Vector3.Cross(verticesLeft[1] - verticesLeft[0], verticesLeft[2] - verticesLeft[0]), normalsLeft[0]) < 0)
            {
                FlipTriangel(triangleLeft); // 법선 방향이 틀리면 뒤집기
            }
            _leftMesh.AddTriangle(triangleLeft);

            // 오른쪽 메시 (법선이 planeNormal, 즉 절단 평면의 '바깥쪽'을 향함)
            // 정점 순서를 바꿔서 법선 방향이 올바르게 외부로 향하도록
            Vector3[] verticesRight = { currentVertex, centerPosition, nextVertex };
            Vector3[] normalsRight = { planeNormal, planeNormal, planeNormal };
            Vector2[] uvsRight = { uvCurrent, uvCenter, uvNext }; // UV 순서도 정점 순서에 맞춰야 함

            MeshTriangle triangleRight = new MeshTriangle(verticesRight, normalsRight, uvsRight, cutSubmeshIndex);
            // 법선 방향이 올바른지 확인 (시계 반대 방향 - Counter-Clockwise)
            if (Vector3.Dot(Vector3.Cross(verticesRight[1] - verticesRight[0], verticesRight[2] - verticesRight[0]), normalsRight[0]) < 0)
            {
                FlipTriangel(triangleRight); // 법선 방향이 틀리면 뒤집기
            }
            _rightMesh.AddTriangle(triangleRight);
        }
    }
}