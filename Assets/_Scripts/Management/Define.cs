using UnityEngine;

namespace Define
{
    public enum eScenes
    {
        Title,
        Lobby,
        InGame
    }
    public enum SaberNoteType
    {
        Left,
        Right
    }
    public enum NoteDirection
    {
        Up,
        Down,
        Left,
        Right,
        Any
    }
    public enum JudgementType
    {
        Perfect,
        Excellent,
        Good,
        Normal,
        BadCut,
        Miss,
    }
    /// <summary>
    /// 노트 충돌 및 판정에 필요한 모든 관련 정보를 담는 구조체입니다.
    /// </summary>
    public struct NoteHitContext
    {
        public GameObject HitNoteObject;
        public Collider NoteCollider;
        public NoteMovement NoteMovement;

        public Saber Saber;
        public Vector3 SaberSwingDirection;

        public NoteDirection RequiredDirection;
        public SaberNoteType RequiredNoteType;
        public Vector3 NoteForward;
        public Vector3 NoteCenter;
        public Vector3 NoteScale;

        public Vector3 HitPoint;
        public float CurrentMusicTime;
        public float TargetMusicTime;
    }
    /// <summary>
    /// 노트 절단 시 계산되는 점수 구성 요소를 나타냅니다.
    /// </summary>
    public struct CutScores
    {
        public float swingAngleBeforeCut; // 노트를 치기 전 스윙 각도 (비트 세이버 기준 0~100)
        public float swingAngleAfterCut;  // 노트를 친 후 스윙 각도 (비트 세이버 기준 0~100)
        public float cutAccuracy;         // 노트 중앙을 얼마나 정확히 맞췄는지 (0~1 사이)
    }
}