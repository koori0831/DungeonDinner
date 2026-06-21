using Alchemy.Inspector;
using UnityEngine;

namespace Work.Players.Code
{
    [ExecuteAlways]
    public class FollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private GameObject target;
        [SerializeField] private Vector3 offset = new Vector3(0, 10f, -10f);
        [Header("Smoothing")]
        [SerializeField] private float smoothSpeed = 0.125f;
        [SerializeField] private Vector3 damping = Vector3.zero; 
        [Header("Editor Preview")]
        [SerializeField] private bool previewInEditor = true;

        [Header("Rotation")]
        [SerializeField] private bool useLookAt = true;
        private bool _useLookAt => !useLookAt;
        [ShowIf(nameof(_useLookAt))]
        [SerializeField] private Vector3 fixedRotation = new Vector3(45f, 0f, 0f);
        [SerializeField] private bool smoothRotation = true;
        [SerializeField] private float rotationSmoothSpeed = 5f;

        // internal velocity used by SmoothDamp
        private Vector3 _currentVelocity = Vector3.zero;

        private void OnValidate()
        {
            if (smoothSpeed < 0f) smoothSpeed = 0f;
            // 에디터에서 타겟이 비어있으면 이름/태그로 자동 할당 시도
            if (target == null)
            {
                FindTargetByTag("Player");
            }
        }

        private void LateUpdate()
        {
            // 에디터에서의 미리보기 제어
            if (!Application.isPlaying && !previewInEditor) return;
            if (target == null) return;

            Vector3 desiredPosition = target.transform.position + offset;

            Vector3 newPosition;
            // If damping is set (non-zero), use it as the smoothing time (use the largest component)
            float smoothTime = smoothSpeed;
            if (damping != Vector3.zero)
            {
                smoothTime = Mathf.Max(damping.x, Mathf.Max(damping.y, damping.z));
                if (smoothTime <= 0f) smoothTime = smoothSpeed;
            }

            if (smoothTime > 0f)
            {
                newPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, smoothTime);
            }
            else
            {
                newPosition = Vector3.Lerp(transform.position, desiredPosition, Mathf.Clamp01(smoothSpeed));
            }

            transform.position = newPosition;

            // Rotation: LookAt을 사용할지 혹은 고정 회전을 사용할지 선택
            if (useLookAt)
            {
                Vector3 lookAtPoint = target.transform.position + Vector3.up * (offset.y * 0.25f);
                transform.LookAt(lookAtPoint);
            }
            else
            {
                Quaternion targetRot = Quaternion.Euler(fixedRotation);
                if (smoothRotation)
                {
                    float dt = Application.isPlaying ? Time.deltaTime : 0.02f;
                    float t = Mathf.Clamp01(rotationSmoothSpeed * dt);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
                }
                else
                {
                    transform.rotation = targetRot;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (target == null) return;

            // 원하는 위치를 시각화
            Vector3 desiredPosition = target.transform.position + offset;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(target.transform.position, desiredPosition);
            Gizmos.DrawSphere(desiredPosition, 0.2f);

            // 카메라가 바라보는 지점을 점으로 표시
            Vector3 lookAtPoint = target.transform.position + Vector3.up * (offset.y * 0.25f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(lookAtPoint, 0.1f);
        }

        // 유틸: 태그로 타겟 찾기
        private void FindTargetByTag(string tag)
        {
            try
            {
                var go = GameObject.FindWithTag(tag);
                if (go != null)
                {
                    target = go;
                }
            }
            catch
            {
                // 태그가 없거나 예외가 발생하면 무시
            }
        }
    }
}