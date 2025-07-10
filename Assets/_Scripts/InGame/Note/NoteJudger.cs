using UnityEngine;
using Define;

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
        float currentMusicTime,
        float targetMusicTime,
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
        if (requiredDirection == NoteDirection.Any)
        {
            return true; // Any 방향은 항상 맞음
        }

        // 노트를 기준으로 상대적인 방향 벡터를 계산 (노트의 forward를 기준으로)
        Vector3 targetDirection = Quaternion.LookRotation(noteForward) * GetRelativeDirectionVector(requiredDirection);
        float angle = Vector3.Angle(saberSwingDirection.normalized, targetDirection.normalized);

        return angle < 45f; // 45도 이내면 올바른 방향으로 간주
    }

    /// <summary>
    /// 주어진 노트 방향에 해당하는 상대적인 3D 벡터를 반환합니다.
    /// </summary>
    private Vector3 GetRelativeDirectionVector(NoteDirection direction)
    {
        switch (direction)
        {
            case NoteDirection.Up: return Vector3.up;
            case NoteDirection.Down: return Vector3.down;
            case NoteDirection.Left: return Vector3.left;
            case NoteDirection.Right: return Vector3.right;
            case NoteDirection.Any: return Vector3.zero; // Any 방향은 여기서 사용되지 않지만, 완전성을 위해 추가
            default: return Vector3.zero; // 기본값 (오류 방지)
        }
    }

    /// <summary>
    /// 노트의 요구되는 타입과 세이버의 타입이 일치하는지 확인합니다.
    /// </summary>
    private bool CheckNoteType(SaberNoteType requiredNoteType, SaberNoteType saberType)
    {
        // Bomb 타입은 모든 세이버로 칠 수 없으므로, Bomb이 아니면서 타입이 일치하는지 확인
        // 또는 requiredNoteType이 Bomb이면 무조건 false를 반환하도록 로직을 추가할 수 있습니다.
        // 현재는 Bomb 노트를 친 경우 BadCut으로 처리됩니다.
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
        float swingAngleBeforeCut = Vector3.Angle(noteForward, saberSwingDirection); // 노트 진행 방향과 세이버 스윙 방향 각도
        float swingAngleAfterCut = 0f; // 현재는 계산을 위한 추가 정보 필요. 임시 0
        float distanceToCenter = Vector3.Distance(hitPoint, noteCenter); // 노트 중심으로부터의 히트 지점 거리
        float maxDistance = noteScale.x / 2f; // 노트의 절반 크기 (최대 벗어날 수 있는 거리)
        float cutAccuracy = 1f - Mathf.Clamp01(distanceToCenter / maxDistance); // 0~1 사이의 정확도 (1에 가까울수록 정확)

        return new CutScores
        {
            swingAngleBeforeCut = swingAngleBeforeCut,
            swingAngleAfterCut = swingAngleAfterCut,
            cutAccuracy = cutAccuracy
        };
    }

    /// <summary>
    /// 노트 충돌 정보를 받아 판정을 계산하고, 그 결과에 따라 게임 상태를 업데이트합니다.
    /// 최종적으로 NoteManager에게 노트 컷 연출을 요청합니다.
    /// </summary>
    public void JudgeAndProcessNote(GameObject hitNoteObject, Saber saber, Collider noteCollider, MusicTimeChecker timeChecker)
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

        // NoteMover 대신 NoteMovement 사용
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
            SaberSwingDirection = saber.GetSwingDirection(),

            RequiredDirection = noteComponent.requiredDirection,
            RequiredNoteType = noteComponent.requiredNoteType,
            NoteForward = noteComponent.transform.forward,
            NoteCenter = noteComponent.transform.position,
            NoteScale = noteComponent.transform.localScale,

            HitPoint = noteCollider.ClosestPoint(hitNoteObject.transform.position),
            CurrentMusicTime = timeChecker.elapsedTime,
            TargetMusicTime = noteMovement.TargetMusicTime
        };

        // 최종 판정 계산
        JudgementType finalJudgement = GetJudgementInternal(
            context.SaberSwingDirection,
            (float)context.CurrentMusicTime, // double -> float 명시적 캐스팅
            (float)context.TargetMusicTime, // double -> float 명시적 캐스팅
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

        // 판정 타입에 따른 점수 및 게임 상태 업데이트
        if ((int)result < (int)JudgementType.BadCut) // Perfect, Excellent, Good
        {
            score = CalculateBeatSaberScore(swingAngleBeforeCut, swingAngleAfterCut, cutAccuracy);

            float timingMultiplier;
            switch (result)
            {
                case JudgementType.Perfect: timingMultiplier = 1.0f; break;
                case JudgementType.Excellent: timingMultiplier = 0.9f; break;
                case JudgementType.Good: timingMultiplier = 0.8f; break;
                default: timingMultiplier = 0.6f; break; // 예상치 못한 경우
            }

            score = Mathf.RoundToInt(score * timingMultiplier);

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
        }

        Debug.Log($"[{context.HitNoteObject.name}] 판정 결과: {result}, 점수: {score}, 콤보 증가: {comboIncreased}");

        ParticlePoolManager.Instance.SpawnParticle(result.ToString(), context.HitNoteObject.transform.position);
    }

    /// <summary>
    /// 비트 세이버 점수 계산 방식에 따라 최종 점수를 계산합니다.
    /// </summary>
    public int CalculateBeatSaberScore(float swingAngleBeforeCut, float swingAngleAfterCut, float cutAccuracy)
    {
        // 비트 세이버 점수 계산 공식: (Before Cut Angle Score) + (After Cut Angle Score) + (Cut Accuracy Score)
        // 비트 세이버는 Before Angle 70, After Angle 30, Accuracy 10 점으로 총 110점
        // 현재는 swingAngleBeforeCut (0~100) 과 cutAccuracy (0~1) 만 사용
        // 여기에 swingAngleAfterCut을 0f로 가정했으니 반영해서 계산.

        // Before Cut: 0~100점 (대부분 스윙 각도에 비례)
        int angleBeforeScore = Mathf.RoundToInt(Mathf.Clamp(swingAngleBeforeCut, 0, 100) * 0.7f); // 최대 70점

        // After Cut: 현재 0으로 가정했으므로 점수 기여 없음
        // int angleAfterScore = Mathf.RoundToInt(Mathf.Clamp(swingAngleAfterCut, 0, 100) * 0.3f); // 최대 30점

        // Accuracy: 0~10점
        int accuracyScore = Mathf.RoundToInt(cutAccuracy * 10f); // cutAccuracy가 1이면 10점

        return angleBeforeScore + accuracyScore; // 총점은 최대 80점 (70 + 10)
    }
}