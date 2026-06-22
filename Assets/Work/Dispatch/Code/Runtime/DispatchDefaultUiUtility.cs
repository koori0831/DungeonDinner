using UnityEngine;

namespace Work.Dispatch.Code.Runtime
{
    /// <summary>
    /// 파견 UI의 런타임 반복 항목 정리 유틸리티
    /// </summary>
    internal static class DispatchDefaultUiUtility
    {
        /// <summary>
        /// 자식 오브젝트 전체 제거
        /// </summary>
        /// <param name="root">자식 제거 대상</param>
        public static void ClearChildren(Transform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (Application.isPlaying == true)
                {
                    Object.Destroy(child.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
