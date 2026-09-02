using System;
using System.Collections.Generic;
using Work.Dispatch.Code.Data;

namespace Work.Dispatch.Code.Runtime
{
    public sealed class DispatchValidator
    {
        public DispatchValidationResult Validate(
            DispatchDraft draft,
            DispatchCatalogSO catalog,
            DispatchNpcEligibility eligibility,
            bool hasActiveDispatch)
        {
            if (catalog == null || draft == null)
            {
                return Fail(DispatchValidationError.ConfigurationMissing, "파견 설정을 불러오지 못했습니다.");
            }

            if (hasActiveDispatch)
            {
                return Fail(DispatchValidationError.ActiveDispatchExists, "현재 다른 파견이 진행 중입니다.");
            }

            if (eligibility.NpcExists == false || string.IsNullOrWhiteSpace(draft.NpcId))
            {
                return Fail(DispatchValidationError.NpcMissing, "선택한 NPC 정보를 찾을 수 없습니다.");
            }

            if (catalog.TryFindNpcRule(draft.NpcId, out DispatchNpcRule npcRule) == false)
            {
                return Fail(DispatchValidationError.NpcRuleMissing, "이 NPC는 아직 파견을 보낼 수 없습니다.");
            }

            if (eligibility.Affinity < npcRule.RequiredAffinity)
            {
                return Fail(
                    DispatchValidationError.AffinityTooLow,
                    $"친밀도가 부족합니다. 필요 친밀도: {npcRule.RequiredAffinity}");
            }

            if (catalog.TryFindRegion(draft.RegionId, out DispatchRegionSO region) == false)
            {
                return Fail(DispatchValidationError.RegionMissing, "선택한 파견 지역을 찾을 수 없습니다.");
            }

            if (eligibility.CanVisitRegion(region.RegionId) == false)
            {
                return Fail(DispatchValidationError.NpcCannotVisitRegion, "이 NPC가 출몰하는 지역이 아닙니다.");
            }

            if (draft.Requests == null || draft.Requests.Count == 0)
            {
                return Fail(DispatchValidationError.RequestMissing, "요청할 재료를 선택해 주세요.");
            }

            if (draft.Requests.Count > catalog.MaxMaterialTypes)
            {
                return Fail(
                    DispatchValidationError.TooManyMaterialTypes,
                    $"재료는 최대 {catalog.MaxMaterialTypes}종까지 요청할 수 있습니다.");
            }

            HashSet<string> requestedItemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < draft.Requests.Count; i++)
            {
                DispatchDraftRequest request = draft.Requests[i];
                if (request == null || string.IsNullOrWhiteSpace(request.ItemId))
                {
                    return Fail(DispatchValidationError.MaterialUnavailable, "선택한 재료 정보가 올바르지 않습니다.");
                }

                if (requestedItemIds.Add(request.ItemId) == false)
                {
                    return Fail(DispatchValidationError.DuplicateMaterial, "같은 재료를 중복으로 요청할 수 없습니다.");
                }

                if (region.TryFindMaterial(request.ItemId, out DispatchMaterialRule materialRule) == false)
                {
                    return Fail(DispatchValidationError.MaterialUnavailable, "이 지역에서는 해당 재료를 채집할 수 없습니다.");
                }

                if (request.Amount <= 0 || request.Amount > materialRule.MaxRequestAmount)
                {
                    return Fail(
                        DispatchValidationError.InvalidAmount,
                        $"{materialRule.Item.DisplayName} 요청량은 1~{materialRule.MaxRequestAmount}개여야 합니다.");
                }
            }

            return DispatchValidationResult.Success;
        }

        private static DispatchValidationResult Fail(DispatchValidationError error, string message)
        {
            return new DispatchValidationResult(error, message);
        }
    }
}
