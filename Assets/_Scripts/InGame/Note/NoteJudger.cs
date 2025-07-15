using UnityEngine;
using Define; // Define 네임스페이스가 필요합니다 (JudgementType, NoteDirection, SaberNoteType 등).
// using System.ComponentModel.Design.Serialization; // 이 using은 현재 코드에서 사용되지 않으므로 제거 가능

public class NoteJudger
{
    private float _autoMissTimingWindow;
    private float _perfectTimingWindow;
    private float _excellentTimingWindow;
    private float _goodTimingWindow;

    /// <summary>
    /// NoteJudger 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    /// <param name="autoMiss">자동 미스 판정 시간 임계값입니다.</param>
    /// <param name="perfect">퍼펙트 판정 시간 임계값입니다.</param>
    /// <param name="excellent">엑설런트 판정 시간 임계값입니다.</param>
    /// <param name="good">굿 판정 시간 임계값입니다.</param>
    public NoteJudger(float autoMiss, float perfect, float excellent, float good)
    {
        _autoMissTimingWindow = autoMiss;
        _perfectTimingWindow = perfect;
        _excellentTimingWindow = excellent;
        _goodTimingWindow = good;
    }

    /// <summary>
    /// 주어진 매개변수를 기반으로 노트의 최종 판정 타입을 계산합니다.
    /// </summary>
    private JudgementType GetJudgementInternal(
        Vector3 saberSwingDirection,
        float currentMusicTime, // float으로 변경 (MusicSynchronizer의 currentTimeDSP와 일치)
        float targetMusicTime,  // float으로 변경 (NoteMovement의 TargetMusicTime과 일치)
        NoteDirection requiredDirection,
        SaberNoteType requiredNoteType,
        SaberNoteType saberType,
        Vector3 noteForward)
    {
        bool isDirectionCorrect = CheckDirection(saberSwingDirection, requiredDirection, noteForward);
        bool isNoteTypeCorrect = CheckNoteType(requiredNoteType, saberType);
        float timeDifference = Mathf.Abs(currentMusicTime - targetMusicTime);

        // 노트 타입이 틀리면 BadCut (가장 먼저 검사하여 불필요한 계산 방지)
        if (!isNoteTypeCorrect)
        {
            return JudgementType.BadCut;
        }

        // 타이밍 윈도우를 벗어나면 Miss (노트 타입은 맞으나 너무 늦거나 빠름)
        if (timeDifference > _autoMissTimingWindow)
        {
            return JudgementType.Miss;
        }

        // 방향이 틀리면 BadCut (노트 타입과 타이밍은 맞으나 스윙 방향이 틀림)
        if (!isDirectionCorrect)
        {
            return JudgementType.BadCut;
        }

        // 타이밍에 따른 점수 판정
        if (timeDifference <= _perfectTimingWindow)
        {
            return JudgementType.Perfect;
        }
        else if (timeDifference <= _excellentTimingWindow)
        {
            return JudgementType.Excellent;
        }
        else if (timeDifference <= _goodTimingWindow)
        {
            return JudgementType.Good;
        }
        else
        {
            // 이 경우 일반적으로 위의 _autoMissTimingWindow에서 걸러지나,
            // 방어 코드 또는 예상치 못한 시나리오에 대비.
            return JudgementType.BadCut;
        }
    }

    /// <summary>
    /// 세이버 스윙 방향과 노트의 요구되는 방향이 일치하는지 확인합니다.
    /// </summary>
    private bool CheckDirection(Vector3 saberSwingDirection, NoteDirection requiredDirection, Vector3 noteForward)
    {
        //if (requiredDirection == NoteDirection.Any)
        //{
        //    return true; // Any 방향은 항상 맞음
        //}

        // 노트를 기준으로 상대적인 방향 벡터를 계산 (노트의 forward를 기준으로)
        // NoteMovement에서 노트가 항상 Z축으로 스폰되므로 noteForward는 보통 Vector3.forward 또는 Vector3.back일 것.
        // GetRelativeDirectionVector가 반환하는 Vector3.up, Vector3.down 등을 노트의 로컬 공간으로 변환.
        Vector3 noteLocalUp = noteForward.y > 0 ? Vector3.up : Vector3.down; // 노트의 위쪽 방향
        Vector3 noteLocalRight = Vector3.Cross(noteForward, noteLocalUp).normalized; // 노트의 오른쪽 방향
        Vector3 noteLocalForward = noteForward.normalized; // 노트의 앞쪽 방향 (스윙 방향과 관련 없음)

        Vector3 targetDirection = Vector3.zero;
        switch (requiredDirection)
        {
            case NoteDirection.Up: targetDirection = noteLocalUp; break;
            case NoteDirection.Down: targetDirection = -noteLocalUp; break; // 아래 방향
            case NoteDirection.Left: targetDirection = -noteLocalRight; break; // 왼쪽 방향
            case NoteDirection.Right: targetDirection = noteLocalRight; break; // 오른쪽 방향
                                                                               // NoteDirection.Any는 위에서 이미 처리됨
        }

        float angle = Vector3.Angle(saberSwingDirection.normalized, targetDirection.normalized);

        return angle < 150f; // 45도 이내면 올바른 방향으로 간주 (조정 가능)
    }

    /// <summary>
    /// 주어진 노트 방향에 해당하는 상대적인 3D 벡터를 반환합니다.
    /// 이 함수는 이제 CheckDirection 내부에서 직접 사용되지 않고, 로컬 공간 변환에 대한 이해를 돕기 위해 남겨둡니다.
    /// </summary>
    private Vector3 GetRelativeDirectionVector(NoteDirection direction)
    {
        // 이 함수는 더 이상 직접적으로 사용되지 않습니다.
        // 대신 CheckDirection에서 noteForward를 기준으로 직접 방향을 계산합니다.
        // 이전 코드의 Vector3.up, down, left, right는 월드 좌표계 기준이므로 노트의 회전이 있다면 잘못된 방향을 유발합니다.
        // 아래는 참고용으로 유지.
        switch (direction)
        {
            case NoteDirection.Up: return Vector3.up;
            case NoteDirection.Down: return Vector3.down;
            case NoteDirection.Left: return Vector3.left;
            case NoteDirection.Right: return Vector3.right;
            //case NoteDirection.Any: return Vector3.zero;
            default: return Vector3.zero;
        }
    }

    /// <summary>
    /// 노트의 요구되는 타입과 세이버의 타입이 일치하는지 확인합니다.
    /// </summary>
    private bool CheckNoteType(SaberNoteType requiredNoteType, SaberNoteType saberType)
    {
        //// Bomb 타입 처리를 위한 추가 로직
        //if (requiredNoteType == SaberNoteType.Bomb)
        //{
        //    // 폭탄을 치면 항상 BadCut (true를 반환하여 JudgementType.BadCut으로 유도)
        //    return false; // 폭탄은 어떤 세이버로도 칠 수 없으므로 항상 false
        //}

        //// Any 타입은 어떤 세이버로든 칠 수 있음
        //if (requiredNoteType == SaberNoteType.None) // Define.SaberNoteType.None을 Any로 가정
        //{
        //    return true;
        //}

        // 그 외에는 requiredNoteType과 saberType이 정확히 일치해야 함
        return requiredNoteType == saberType;
    }

    /// <summary>
    /// 비트 세이버 스타일의 컷 점수(스윙 각도 및 정확도)를 계산합니다.
    /// </summary>
    public CutScores CalculateCutScores(
        Vector3 saberSwingDirection,
        Vector3 noteForward,
        Vector3 hitPoint,
        Vector3 noteCenter,
        Vector3 noteScale)
    {
        // 스윙 각도 계산 개선: 노트의 로컬 Up/Right/Forward를 기준으로 각도 계산
        // NoteForward는 노트가 움직이는 방향 (Z축)
        // 스윙은 노트의 Z축 이동 방향에 수직인 평면에서 이루어진다고 가정.

        // 예시: 노트가 플레이어를 향해 오는 경우 (NoteForward = Vector3.back)
        // 노트의 Up = Vector3.up, Right = Vector3.right
        // 세이버 스윙 방향 (saberSwingDirection)이 이 평면 내에서 이루어지는 것을 가정.

        // Beat Saber의 경우, 이전 스윙 각도(Pre-Swing)와 이후 스윙 각도(Post-Swing)가 중요.
        // 여기서는 노트의 "진행 방향"과 "세이버 스윙 방향"의 각도를 Before Cut Angle로 사용.
        // 이는 비트세이버와는 약간 다른 방식일 수 있으므로 유의.

        // Before Cut Angle: 스윙 방향과 노트의 '정방향' 또는 '필요 방향'과의 각도.
        // 여기서는 noteForward 대신 requiredDirection을 사용하는 것이 더 의미 있을 수 있습니다.
        // 현재는 noteForward (노트의 Z축 진행 방향)과 스윙 방향의 각도를 사용.
        // Note: 스윙 방향이 노트의 Z축과 수직에 가까울수록 좋은 점수를 얻음.
        // 0~90도까지의 각도로 정규화. 0도에 가까울수록 이상적.
        float rawAngleBeforeCut = Vector3.Angle(noteForward, saberSwingDirection); // 현재 Z축 방향과 스윙 방향 각도
        // 이 각도를 점수에 매핑하는 방식은 게임 디자인에 따라 달라질 수 있음.
        // 비트세이버는 주로 노트 컷 방향에 대해 스윙의 궤적을 평가.

        // 임시로, 노트의 Z축 진행 방향에 수직으로 스윙할 때 (90도) 좋은 점수를 받도록 조정
        // 0도가 90점, 90도가 0점이라면:
        // float swingAngleBeforeCutScore = 90 - Mathf.Clamp(rawAngleBeforeCut, 0, 90);

        // 비트 세이버 방식 (프리 스윙 각도):
        // 노트의 requiredDirection (예: Vector3.up)과 스윙 시작 방향 벡터의 각도.
        // 이를 위해 Saber 클래스에서 스윙 시작점과 끝점, 그리고 이전 프레임의 위치 정보를 가지고 있어야 함.
        // 현재는 단순화하여 'saberSwingDirection' (충돌 순간의 최종 스윙 방향)만 사용.
        // 여기서는 'noteForward' 대신 'requiredDirection'의 월드 벡터를 사용하는 것이 더 정확할 수 있음.
        // float angleToCheck = Vector3.Angle(GetRelativeDirectionVector(requiredDirection), saberSwingDirection);
        // 이 예시에서는 NoteMovement의 _preSpawnBeats 값과 같이 70점 만점 기준으로 70-degree swing을 70점으로 계산.
        // (100 - angle)/100 * 70
        float swingAngleBeforeCutScore = Mathf.Clamp(70f - rawAngleBeforeCut, 0f, 70f); // 0도에서 70점, 70도 이상에서 0점

        float swingAngleAfterCut = 0f; // 현재는 계산을 위한 추가 정보 필요. 임시 0

        // 노트 중심으로부터의 히트 지점 거리 계산
        // hitPoint는 충돌 지점, noteCenter는 노트의 월드 중심
        // noteScale은 노트의 로컬 스케일. 노트는 일반적으로 정육면체이므로 X/Y/Z 스케일 중 가장 큰 값을 사용
        // 또는 노트의 실제 충돌 영역 반경을 고려해야 함. 여기서는 단순화를 위해 가장 큰 스케일 값을 사용
        float noteRadius = Mathf.Max(noteScale.x, noteScale.y, noteScale.z) / 2f;
        float distanceToCenter = Vector3.Distance(hitPoint, noteCenter);

        // cutAccuracy: 1에 가까울수록 정확 (중심에 가까움)
        // distanceToCenter가 noteRadius를 넘어가면 0 이하의 값으로 클램프
        float cutAccuracy = 1f - Mathf.Clamp01(distanceToCenter / noteRadius);

        return new CutScores
        {
            swingAngleBeforeCut = swingAngleBeforeCutScore, // 이제 점수화된 값
            swingAngleAfterCut = swingAngleAfterCut,
            cutAccuracy = cutAccuracy
        };
    }

    /// <summary>
    /// 노트 충돌 정보를 받아 판정을 계산하고, 그 결과에 따라 게임 상태를 업데이트합니다.
    /// 최종적으로 NoteManager에게 노트 컷 연출을 요청합니다.
    /// </summary>
    public void JudgeAndProcessNote(GameObject hitNoteObject, Saber saber, Collider noteCollider, MusicSynchronizer timeChecker)
    {
        // 필수 컴포넌트 유효성 검사
        if (saber == null)
        {
            Debug.LogWarning($"NoteJudger: 처리 종료 - Saber 컴포넌트를 찾을 수 없습니다.");
            NoteManager.Instance?.ReturnPooledNote(hitNoteObject);
            return;
        }

        Note noteComponent = hitNoteObject.GetComponent<Note>();
        if (noteComponent == null)
        {
            Debug.LogWarning($"NoteJudger: 충돌한 오브젝트({hitNoteObject.name})에 Note 컴포넌트가 없습니다.", hitNoteObject);
            NoteManager.Instance?.ReturnPooledNote(hitNoteObject);
            return;
        }

        // NoteMovement 대신 NoteMovement 사용
        NoteMovement noteMovement = hitNoteObject.GetComponent<NoteMovement>();
        if (noteMovement == null)
        {
            Debug.LogWarning($"NoteJudger: 충돌한 오브젝트({hitNoteObject.name})에 NoteMovement 컴포넌트가 없습니다. 판정이 어려울 수 있습니다.", hitNoteObject);
            NoteManager.Instance?.ReturnPooledNote(hitNoteObject);
            return;
        }

        // NoteHitContext 데이터 구성
        NoteHitContext context = new NoteHitContext
        {
            HitNoteObject = hitNoteObject,
            NoteCollider = noteCollider,
            NoteMovement = noteMovement,

            Saber = saber,
            SaberSwingDirection = saber.GetSwingDirection(), // Saber 스크립트에 GetSwingDirection() 메서드가 있어야 함

            RequiredDirection = noteComponent.requiredDirection,
            RequiredNoteType = noteComponent.requiredNoteType,
            NoteForward = noteComponent.transform.forward, // 노트가 바라보는 방향 (회전이 없다면 Vector3.forward)
            NoteCenter = noteComponent.transform.position,
            NoteScale = noteComponent.transform.localScale,

            HitPoint = noteCollider.ClosestPoint(saber.transform.position), // 충돌 지점 계산 개선: 세이버 위치에서 가장 가까운 콜라이더 점
            CurrentMusicTime = timeChecker.currentTimeDSP, // MusicSynchronizer의 currentTimeDSP 사용
            TargetMusicTime = noteMovement.TargetMusicTime
        };

        // 최종 판정 계산
        JudgementType finalJudgement = GetJudgementInternal(
            context.SaberSwingDirection,
            context.CurrentMusicTime,
            context.TargetMusicTime,
            context.RequiredDirection,
            context.RequiredNoteType,
            context.Saber.saberNoteType,
            context.NoteForward
        );

        // 컷 점수 계산
        CutScores cutScores = CalculateCutScores(
            context.SaberSwingDirection, context.NoteForward, context.HitPoint, context.NoteCenter, context.NoteScale);

        // 판정 결과 적용 (점수, 콤보, 데미지, 파티클)
        ApplyJudgementResult(finalJudgement, cutScores.swingAngleBeforeCut, cutScores.swingAngleAfterCut, cutScores.cutAccuracy, context);

        // 노트 시각 연출 및 풀 반환 요청
        if (NoteManager.Instance != null)
        {
            // NoteManager.HandleNoteCutVisuals는 hitNoteObject, hitPoint, saberSwingDirection을 받는다고 가정
            NoteManager.Instance.HandleNoteCutVisuals(context.HitNoteObject, context.HitPoint, context.SaberSwingDirection);
        }
        else
        {
            Debug.LogError("NoteJudger: NoteManager.Instance가 초기화되지 않았습니다. 노트 컷 연출을 할 수 없습니다.");
            NoteManager.Instance?.ReturnPooledNote(context.HitNoteObject); // 오류 발생 시에도 풀로 반환
        }
    }

    /// <summary>
    /// 판정 결과에 따라 게임 점수, 콤보, 데미지를 처리하고 파티클을 스폰합니다.
    /// </summary>
    private void ApplyJudgementResult(
        JudgementType result,
        float swingAngleBeforeCut,
        float swingAngleAfterCut,
        float cutAccuracy,
        NoteHitContext context)
    {
        int score = 0;
        bool comboIncreased = false;
        

        if (InGameManager.Instance == null || ParticlePoolManager.Instance == null)
        {
            Debug.LogError("NoteJudger: InGameManager.Instance 또는 ParticlePoolManager.Instance가 초기화되지 않았습니다. 점수/파티클 처리를 건너뜁니다.");
            return;
        }

         Debug.Log($"NoteJudger: 판정 결과 - {result}, 스윙 각도: {swingAngleBeforeCut}, 정확도: {cutAccuracy}");
        // 판정 타입에 따른 점수 및 게임 상태 업데이트
        if (result == JudgementType.Perfect || result == JudgementType.Excellent || result == JudgementType.Good)
        {
            // CalculateBeatSaberScore는 이미 swingAngleBeforeCut을 점수화하여 반환
            // cutAccuracy는 0~1 값 (정확도) -> 점수 계산 시 10점 만점으로 곱하기
            score = Mathf.RoundToInt(swingAngleBeforeCut + (cutAccuracy * 10f)); // Max 70 + 10 = 80점

            float timingMultiplier;
            switch (result)
            {
                case JudgementType.Perfect: timingMultiplier = 1.0f; break;
                case JudgementType.Excellent: timingMultiplier = 0.9f; break;
                case JudgementType.Good: timingMultiplier = 0.8f; break;
                default: timingMultiplier = 0.6f; break; // 예상치 못한 경우 (방어 코드)
            }

            score = Mathf.RoundToInt(score * timingMultiplier); // 타이밍에 따른 최종 점수 조정

            InGameManager.Instance.AddScore(score);
            InGameManager.Instance.AddCombo();
            comboIncreased = true;
        }
        else if (result == JudgementType.BadCut)
        {
            InGameManager.Instance.ResetCombo();
            InGameManager.Instance.TakeDamage(10); // BadCut 시 데미지
            score = 0;
        }
        else // JudgementType.Miss
        {
            InGameManager.Instance.ResetCombo();
            InGameManager.Instance.TakeDamage(20); // Miss 시 데미지 (더 큼)
            score = 0;
            InGameManager.Instance.MissUpdate();
           
        }

       
        

        ParticlePoolManager.Instance.SpawnParticle(result.ToString(), context.HitNoteObject.transform.position);
    }

    /// <summary>
    /// 비트 세이버 점수 계산 방식에 따라 최종 점수를 계산합니다.
    /// 이 함수는 이제 'swingAngleBeforeCut'을 이미 점수화된 값으로 받고,
    /// 'cutAccuracy'에 10을 곱하여 최종 점수를 반환하도록 수정됩니다.
    /// </summary>
    public int CalculateBeatSaberScore(float swingAngleBeforeCut, float swingAngleAfterCut, float cutAccuracy)
    {
        // swingAngleBeforeCut: 0~70점 (CalculateCutScores에서 이미 점수화됨)
        // swingAngleAfterCut: 현재 0으로 가정했으므로 점수 기여 없음
        // cutAccuracy: 0~1 값 (정확도) -> 점수 계산 시 10점 만점으로 곱함

        // 총점은 최대 80점 (70 + 10)
        return Mathf.RoundToInt(swingAngleBeforeCut + (cutAccuracy * 10f));
    }
}

// 다음 구조체와 Enum은 Define.cs 또는 별도의 파일에 정의되어 있어야 합니다.
// 누락되었을 경우를 대비하여 여기에 예시를 포함합니다.

/*
// Define.cs (또는 별도 파일)
namespace Define
{
    public enum JudgementType
    {
        Perfect,
        Excellent,
        Good,
        BadCut,
        Miss
    }

    public enum SaberNoteType
    {
        None, // Any Note (흰색 노트)
        Left, // 왼쪽 세이버용 (빨간색)
        Right, // 오른쪽 세이버용 (파란색)
        Bomb // 폭탄 (치면 안 되는 노트)
    }

    public enum NoteDirection
    {
        Up,
        Down,
        Left,
        Right,
        Any // 아무 방향이나 상관없는 노트
    }

    // NoteHitContext 구조체 (필요한 정보들을 담아 전달)
    public struct NoteHitContext
    {
        public GameObject HitNoteObject;
        public Collider NoteCollider;
        public NoteMovement NoteMovement; // NoteMovement 참조

        public Saber Saber; // Saber 컴포넌트 참조
        public Vector3 SaberSwingDirection; // 세이버의 스윙 방향 벡터

        public NoteDirection RequiredDirection; // 노트가 요구하는 컷 방향
        public SaberNoteType RequiredNoteType; // 노트가 요구하는 세이버 타입
        public Vector3 NoteForward; // 노트의 Z축 진행 방향
        public Vector3 NoteCenter; // 노트의 월드 중심 위치
        public Vector3 NoteScale; // 노트의 스케일 (정확도 계산용)

        public Vector3 HitPoint; // 충돌 지점의 월드 위치

        public float CurrentMusicTime; // 현재 음악 시간 (MusicSynchronizer.currentTimeDSP)
        public float TargetMusicTime; // 노트의 목표 도달 시간 (NoteMovement.TargetMusicTime)
    }

    // CutScores 구조체 (컷 점수 정보를 담아 반환)
    public struct CutScores
    {
        public float swingAngleBeforeCut; // 이전 스윙 각도 점수 (0-70)
        public float swingAngleAfterCut;  // 이후 스윙 각도 점수 (0-30) - 현재 0으로 고정
        public float cutAccuracy;         // 중앙에서 얼마나 벗어났는지 (0-1, 1이 완벽)
    }
}
*/