using System;
using UnityEngine;

namespace Work.Core.Utils.RendererProximityFade
{
    [Serializable]
    public struct RendererSettings
    {
        [Tooltip("알파 값을 조절할 대상 렌더러입니다.")]
        public Renderer TargetRenderer;

        [Tooltip("렌더러 Transform Position을 기준으로 한 월드 방향 중심점 오프셋입니다.")]
        public Vector3 BodyOffset;

        [Tooltip("캡슐 판정에 사용되는 반지름입니다.")]
        [Min(0f)]
        public float BodyRadius;

        [Tooltip("캡슐 판정에 사용되는 전체 높이입니다.")]
        [Min(0f)]
        public float BodyHeight;

        public static RendererSettings CreateDefault(Renderer renderer)
        {
            return new RendererSettings
            {
                TargetRenderer = renderer,
                BodyOffset = new Vector3(0f, 0.9f, 0f),
                BodyRadius = 0.45f,
                BodyHeight = 1.8f
            };
        }
    }
}
