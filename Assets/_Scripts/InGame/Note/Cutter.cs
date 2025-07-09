using System.Collections.Generic;
using UnityEngine;

public class Cutter : MonoBehaviour
{
    private static bool isBusy;
    private static Mesh originalMesh;

    public static void Cut(GameObject originalGameObject, Vector3 contactPoint, Vector3 cutNormal)
    {
        if(isBusy)
            return;

        isBusy = true;
        
        // --- 변경 시작: 절단 평면의 법선 계산 로직 수정 ---
        // cutNormal은 사벨의 월드 스윙 방향 (NoteJudger에서 saberSwingDirection으로 전달됨)
        
        // 노트를 자를 평면의 법선을 노트의 로컬 공간으로 변환하여 계산합니다.
        // 이렇게 하면 노트가 회전되어 있어도 항상 노트의 기준에 맞춰 잘립니다.
        Vector3 localSaberSwingDirection = originalGameObject.transform.InverseTransformDirection(cutNormal);
        Vector3 planeNormal;

        // 사벨의 로컬 스윙 방향(localSaberSwingDirection)과 노트의 로컬 Z축(Vector3.forward)의 외적을 사용합니다.
        // 이 외적은 localSaberSwingDirection에 대해 수직이며, 동시에 노트의 앞/뒤 방향(Z축)에도 수직인 벡터를 생성합니다.
        // 이는 사용자가 원하는 '스윙 평면'과 같은 방향의 절단면을 만들어줍니다.
        planeNormal = Vector3.Cross(localSaberSwingDirection, Vector3.forward);

        // 특수 케이스 처리: 사벨 스윙 방향이 노트의 로컬 Z축과 거의 평행할 때 (즉, 노트를 찌르거나 뽑는 움직임)
        // 이 경우 외적 결과가 0에 가까워지므로, 기본적으로 노트의 로컬 '위쪽' 방향을 법선으로 사용하여 수평 절단을 강제합니다.
        if (planeNormal.magnitude < 0.0001f) // 아주 작은 값으로 비교하여 부동 소수점 오차 처리
        {
            planeNormal = Vector3.up; // 노트의 로컬 위쪽 방향 (수평 절단을 위한 법선)
        }

        // 계산된 평면 법선과 충돌 지점을 사용하여 절단 평면을 생성합니다.
        Plane cutPlane = new Plane(planeNormal, originalGameObject.transform.InverseTransformPoint(contactPoint));
        // --- 변경 끝 ---

        originalMesh = originalGameObject.GetComponent<MeshFilter>().mesh;

        if (originalMesh == null)
        {
            Debug.LogError("Need mesh to cut");
            return;
        }
        
        List<Vector3> addedVertices = new List<Vector3>();
        GeneratedMesh leftMesh = new GeneratedMesh();
        GeneratedMesh rightMesh = new GeneratedMesh();
        
        SeparateMeshes(leftMesh,rightMesh,cutPlane,addedVertices);
        FillCut(addedVertices, cutPlane, leftMesh, rightMesh);

        Mesh finishedLeftMesh = leftMesh.GetGeneratedMesh();
        Mesh finishedRightMesh = rightMesh.GetGeneratedMesh();

        //Getting and destroying all original colliders to prevent having multiple colliders
        //of different kinds on one object
        var originalCols = originalGameObject.GetComponents<Collider>();
        foreach (var col in originalCols)
            Destroy(col);

        originalGameObject.GetComponent<MeshFilter>().mesh = finishedLeftMesh;
        var collider = originalGameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = finishedLeftMesh;
        collider.convex = true;
        collider.isTrigger = true; 
        
        Material[] mats = new Material[finishedLeftMesh.subMeshCount];
        for (int i = 0; i < finishedLeftMesh.subMeshCount; i++)
       {
            mats[i] = originalGameObject.GetComponent<MeshRenderer>().material;
        }
        originalGameObject.GetComponent<MeshRenderer>().materials = mats;

        GameObject right = new GameObject();
        right.transform.position = originalGameObject.transform.position + (Vector3.up * .05f);
        right.transform.rotation = originalGameObject.transform.rotation;
        right.transform.localScale = originalGameObject.transform.localScale;
        right.AddComponent<MeshRenderer>();
        
        mats = new Material[finishedRightMesh.subMeshCount];
        for (int i = 0; i < finishedRightMesh.subMeshCount; i++)
       {
            mats[i] = originalGameObject.GetComponent<MeshRenderer>().material;
        }
        right.GetComponent<MeshRenderer>().materials = mats;
        right.AddComponent<MeshFilter>().mesh = finishedRightMesh;
        
        right.AddComponent<MeshCollider>().sharedMesh = finishedRightMesh;
        var cols = right.GetComponents<MeshCollider>();
        foreach (var col in cols)
        {
            col.convex = true;
            col.isTrigger = false; 
        }
        
        var rightRigidbody = right.AddComponent<Rigidbody>();
        rightRigidbody.AddRelativeForce(-cutPlane.normal * 250f);
        rightRigidbody.useGravity = true; 
        
        DestroyAfterDelay destroyScript = right.AddComponent<DestroyAfterDelay>();
        destroyScript.delay = 1.0f; 

        isBusy = false;
        
    }
    

    /// <summary>
    /// Iterates over all the triangles of all the submeshes of the original mesh to separate the left
    /// and right side of the plane into individual meshes.
    /// </summary>
    /// <param name="leftMesh"></param>
    /// <param name="rightMesh"></param>
    /// <param name="plane"></param>
    /// <param name="addedVertices"></param>
    private static void SeparateMeshes(GeneratedMesh leftMesh,GeneratedMesh rightMesh, Plane plane, List<Vector3> addedVertices)
    {
        for (int i = 0; i < originalMesh.subMeshCount; i++)
        {
            var subMeshIndices = originalMesh.GetTriangles(i);

            for (int j = 0; j < subMeshIndices.Length; j+=3)
            {
                var triangleIndexA = subMeshIndices[j];
                var triangleIndexB = subMeshIndices[j + 1];
                var triangleIndexC = subMeshIndices[j + 2];

                MeshTriangle currentTriangle = GetTriangle(triangleIndexA,triangleIndexB,triangleIndexC,i);

                bool triangleALeftSide = plane.GetSide(originalMesh.vertices[triangleIndexA]);
                bool triangleBLeftSide = plane.GetSide(originalMesh.vertices[triangleIndexB]);
                bool triangleCLeftSide = plane.GetSide(originalMesh.vertices[triangleIndexC]);

                switch (triangleALeftSide)
                {
                    case true when triangleBLeftSide && triangleCLeftSide:
                        leftMesh.AddTriangle(currentTriangle);
                        break;
                    case false when !triangleBLeftSide && !triangleCLeftSide:
                        rightMesh.AddTriangle(currentTriangle);
                        break;
                    default:
                        CutTriangle(plane,currentTriangle, triangleALeftSide, triangleBLeftSide, triangleCLeftSide,leftMesh,rightMesh,addedVertices);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Returns the tree vertices of a triangle as one MeshTriangle to keep code more readable
    /// </summary>
    /// <param name="_triangleIndexA"></param>
    /// <param name="_triangleIndexB"></param>
    /// <param name="_triangleIndexC"></param>
    /// <param name="_submeshIndex"></param>
    /// <returns></returns>
    private static MeshTriangle GetTriangle(int _triangleIndexA, int _triangleIndexB, int _triangleIndexC, int _submeshIndex)
    {
        Vector3[] verticesToAdd = {
            originalMesh.vertices[_triangleIndexA],
            originalMesh.vertices[_triangleIndexB],
            originalMesh.vertices[_triangleIndexC]
        };

        Vector3[] normalsToAdd = {
            originalMesh.normals[_triangleIndexA],
            originalMesh.normals[_triangleIndexB],
            originalMesh.normals[_triangleIndexC]
        };

        Vector2[] uvsToAdd = {
            originalMesh.uv[_triangleIndexA],
            originalMesh.uv[_triangleIndexB],
            originalMesh.uv[_triangleIndexC]
        };

        return new MeshTriangle(verticesToAdd, normalsToAdd, uvsToAdd, _submeshIndex);
    }

    /// <summary>
    /// Cuts a triangle that exists between both sides of the cut apart adding additional vertices
    /// where needed to create intact triangles on both sides.
    /// </summary>
    /// <param name="plane"></param>
    /// <param name="triangle"></param>
    /// <param name="triangleALeftSide"></param>
    /// <param name="triangleBLeftSide"></param>
    /// <param name="triangleCLeftSide"></param>
    /// <param name="leftMesh"></param>
    /// <param name="rightMesh"></param>
    /// <param name="addedVertices"></param>
    private static void CutTriangle(Plane plane,MeshTriangle triangle, bool triangleALeftSide, bool triangleBLeftSide, bool triangleCLeftSide,
    GeneratedMesh leftMesh, GeneratedMesh rightMesh, List<Vector3> addedVertices)
    {
        List<bool> leftSide = new List<bool>();
        leftSide.Add(triangleALeftSide);
        leftSide.Add(triangleBLeftSide);
        leftSide.Add(triangleCLeftSide);

        MeshTriangle leftMeshTriangle = new MeshTriangle(new Vector3[2],new Vector3[2],new Vector2[2],triangle.SubmeshIndex);
        MeshTriangle rightMeshTriangle = new MeshTriangle(new Vector3[2], new Vector3[2], new Vector2[2], triangle.SubmeshIndex);

        bool left = false;
        bool right = false;

        for (int i = 0; i < 3; i++)
        {
            if(leftSide[i])
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
                if(!right)
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

        if(updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if(Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0],updatedVertices[2] - updatedVertices[0]),updatedNormals[0]) < 0) 
            {
                FlipTriangel(currentTriangle);
            }
            leftMesh.AddTriangle(currentTriangle);
        }

        updatedVertices = new Vector3[] { leftMeshTriangle.Vertices[0], leftMeshTriangle.Vertices[1], vertRight };
        updatedNormals = new Vector3[] { leftMeshTriangle.Normals[0], leftMeshTriangle.Normals[1], normalRight };
        updatedUVs = new Vector2[] { leftMeshTriangle.UVs[0],leftMeshTriangle.UVs[1], uvRight };


        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, triangle.SubmeshIndex);
        if(updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if(Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0],updatedVertices[2] - updatedVertices[0]),updatedNormals[0]) < 0) 
            {
                FlipTriangel(currentTriangle);
            }
            leftMesh.AddTriangle(currentTriangle);
        }

        updatedVertices = new Vector3[] { rightMeshTriangle.Vertices[0], vertLeft, vertRight };
        updatedNormals = new Vector3[] { rightMeshTriangle.Normals[0], normalLeft, normalRight };
        updatedUVs = new Vector2[] { rightMeshTriangle.UVs[0],uvLeft, uvRight };

        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, triangle.SubmeshIndex);
        if(updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if(Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0],updatedVertices[2] - updatedVertices[0]),updatedNormals[0]) < 0) 
            {
                FlipTriangel(currentTriangle);
            }
            rightMesh.AddTriangle(currentTriangle);
        }

        updatedVertices = new Vector3[] { rightMeshTriangle.Vertices[0], rightMeshTriangle.Vertices[1], vertRight };
        updatedNormals = new Vector3[] { rightMeshTriangle.Normals[0], rightMeshTriangle.Normals[1], normalRight };
        updatedUVs = new Vector2[] { rightMeshTriangle.UVs[0],rightMeshTriangle.UVs[1], uvRight };

        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, triangle.SubmeshIndex);
        if(updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if(Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0],updatedVertices[2] - updatedVertices[0]),updatedNormals[0]) < 0) 
            {
                FlipTriangel(currentTriangle);
            }
            rightMesh.AddTriangle(currentTriangle);
        }
    }

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

    public static void FillCut(List<Vector3> _addedVertices, Plane _plane, GeneratedMesh _leftMesh, GeneratedMesh _rightMesh)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> polygon = new List<Vector3>();

        for (int i = 0; i < _addedVertices.Count; i++)
        {
            if(!vertices.Contains(_addedVertices[i]))
            {
                polygon.Clear();
                polygon.Add(_addedVertices[i]);
                polygon.Add(_addedVertices[i + 1]);

                vertices.Add(_addedVertices[i]);
                vertices.Add(_addedVertices[i + 1]);

                EvaluatePairs(_addedVertices, vertices, polygon);
                Fill(polygon, _plane, _leftMesh, _rightMesh);
            }
        }
    }

    public static void EvaluatePairs(List<Vector3> _addedVertices,List<Vector3> _vertices, List<Vector3> _polygone)
    {
        bool isDone = false;
        while(!isDone)
        {
            isDone = true;
            for (int i = 0; i < _addedVertices.Count; i+=2)
            {
                if(_addedVertices[i] == _polygone[_polygone.Count - 1] && !_vertices.Contains(_addedVertices[i + 1]))
                {
                    isDone = false;
                    _polygone.Add(_addedVertices[i + 1]);
                    _vertices.Add(_addedVertices[i + 1]);
                } 
                else if (_addedVertices[i + 1] == _polygone[_polygone.Count - 1] && !_vertices.Contains(_addedVertices[i]))
                {
                    isDone = false;
                    _polygone.Add(_addedVertices[i]);
                    _vertices.Add(_addedVertices[i]);
                }
            }
        }
    }

    private static void Fill(List<Vector3> _vertices, Plane _plane, GeneratedMesh _leftMesh, GeneratedMesh _rightMesh)
    {
        Vector3 centerPosition = Vector3.zero;
        for (int i = 0; i < _vertices.Count; i++)
        {
            centerPosition += _vertices[i];
        }
        centerPosition /= _vertices.Count;

        Vector3 up = new Vector3()
        {
            x = _plane.normal.x,
            y = _plane.normal.y,
            z = _plane.normal.z
        };

        Vector3 left = Vector3.Cross(_plane.normal, up);

        Vector3 displacement = Vector3.zero;
        Vector2 uv1 = Vector2.zero;
        Vector2 uv2 = Vector2.zero;

        for (int i = 0; i < _vertices.Count; i++)
        {
            displacement = _vertices[i] - centerPosition;
            uv1 = new Vector2()
            {
                x = .5f + Vector3.Dot(displacement, left),
                y = .5f + Vector3.Dot(displacement, up)
            };

            displacement = _vertices[(i + 1) % _vertices.Count] - centerPosition;
            uv2 = new Vector2()
            { 
                x = .5f + Vector3.Dot(displacement, left),
                y = .5f + Vector3.Dot(displacement, up)
            };

            Vector3[] vertices = {_vertices[i], _vertices[(i+1) % _vertices.Count], centerPosition};
          Vector3[] normals = {-_plane.normal, -_plane.normal, -_plane.normal};
          Vector2[] uvs   = {uv1, uv2, new(0.5f, 0.5f)};

            MeshTriangle currentTriangle = new MeshTriangle(vertices, normals, uvs, originalMesh.subMeshCount + 1);

            if(Vector3.Dot(Vector3.Cross(vertices[1] - vertices[0],vertices[2] - vertices[0]),normals[0]) < 0)
            {
                FlipTriangel(currentTriangle);
            }
            _leftMesh.AddTriangle(currentTriangle);

            normals = new[] { _plane.normal, _plane.normal, _plane.normal };
            currentTriangle = new MeshTriangle(vertices, normals, uvs, originalMesh.subMeshCount + 1);

            if(Vector3.Dot(Vector3.Cross(vertices[1] - vertices[0],vertices[2] - vertices[0]),normals[0]) < 0)
            {
                FlipTriangel(currentTriangle);
            }
            _rightMesh.AddTriangle(currentTriangle);
        
        } 
    }
}