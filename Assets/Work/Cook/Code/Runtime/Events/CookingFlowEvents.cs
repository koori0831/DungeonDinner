using Work.Cook.Code.Data;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.Events
{
    /// <summary>
    /// 요리 화면 변경 알림
    /// </summary>
    /// <param name="Source">이벤트를 발행한 요리 패널</param>
    /// <param name="Screen">변경된 화면 상태</param>
    public readonly record struct CookingGameScreenChangedEvent(
        CookingGamePanel Source,
        CookingGameScreenState Screen
    ) : IEvent;

    /// <summary>
    /// 요리 화면 스냅샷 변경 알림
    /// </summary>
    /// <param name="Source">이벤트를 발행한 요리 패널</param>
    /// <param name="Snapshot">변경된 요리 스냅샷</param>
    public readonly record struct CookingGameSnapshotChangedEvent(
        CookingGamePanel Source,
        CookingGameSnapshot Snapshot
    ) : IEvent;

    /// <summary>
    /// 요리 결과 준비 알림
    /// </summary>
    /// <param name="Source">이벤트를 발행한 요리 패널</param>
    /// <param name="Result">준비된 요리 결과</param>
    public readonly record struct CookingDishResultReadyEvent(
        CookingGamePanel Source,
        DishResult Result
    ) : IEvent;

    /// <summary>
    /// 요리 결과 NPC 전달 알림
    /// </summary>
    /// <param name="Source">이벤트를 발행한 요리 패널</param>
    /// <param name="Result">전달한 요리 결과</param>
    public readonly record struct CookingDishHandedToNpcEvent(
        CookingGamePanel Source,
        DishResult Result
    ) : IEvent;

    /// <summary>
    /// 요리 보상 지급 알림
    /// </summary>
    /// <param name="Source">이벤트를 발행한 요리 패널</param>
    /// <param name="Grant">지급된 보상 정보</param>
    public readonly record struct CookingRewardGrantedEvent(
        CookingGamePanel Source,
        CookingRewardGrant Grant
    ) : IEvent;

    /// <summary>
    /// 요리 플로우 상태 변경 알림
    /// </summary>
    /// <param name="Source">상태가 변경된 플로우 러너</param>
    /// <param name="State">변경된 플로우 상태</param>
    public readonly record struct CookingFlowStateChangedEvent(
        CookingFlowRunner Source,
        CookingFlowState State
    ) : IEvent;

    /// <summary>
    /// 요리 완료 알림
    /// </summary>
    /// <param name="Source">요리를 완료한 플로우 러너</param>
    /// <param name="Result">완성된 요리 결과</param>
    public readonly record struct CookingFlowCompletedEvent(
        CookingFlowRunner Source,
        DishResult Result
    ) : IEvent;

    /// <summary>
    /// 요리 지식 변경 알림
    /// </summary>
    /// <param name="Source">변경된 지식 저장소</param>
    public readonly record struct CookingKnowledgeChangedEvent(
        CookingKnowledgeStore Source,
        string VariantId = null
    ) : IEvent;

    /// <summary>
    /// 요리 지식 업데이트 대기열 추가 알림
    /// </summary>
    /// <param name="Source">업데이트를 추가한 지식 저장소</param>
    /// <param name="Update">추가된 지식 업데이트</param>
    public readonly record struct CookingKnowledgeUpdateQueuedEvent(
        CookingKnowledgeStore Source,
        CookingKnowledgeUpdate Update
    ) : IEvent;

    /// <summary>
    /// 요리 보상 재화 변경 알림
    /// </summary>
    /// <param name="Source">변경된 보상 지갑</param>
    /// <param name="Balance">변경 후 재화량</param>
    public readonly record struct CookingRewardBalanceChangedEvent(
        CookingRewardWallet Source,
        int Balance
    ) : IEvent;

    /// <summary>
    /// 요리 재료 공급원 변경 알림
    /// </summary>
    /// <param name="Source">변경된 재료 공급원</param>
    public readonly record struct CookingIngredientSourceChangedEvent(
        ICookingIngredientSource Source
    ) : IEvent;

    /// <summary>
    /// 레시피 선택 화면 열기 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    public readonly record struct CookingRecipeSelectionOpenRequestedEvent(
        CookingGamePanel Source
    ) : IEvent;

    /// <summary>
    /// 직접 재료 선택 화면 열기 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    public readonly record struct CookingDirectIngredientSelectionOpenRequestedEvent(
        CookingGamePanel Source
    ) : IEvent;

    /// <summary>
    /// 재료 선택 확정 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    public readonly record struct CookingIngredientSelectionConfirmRequestedEvent(
        CookingGamePanel Source
    ) : IEvent;

    /// <summary>
    /// 재료 선택 초기화 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    public readonly record struct CookingIngredientSelectionClearRequestedEvent(
        CookingGamePanel Source
    ) : IEvent;

    /// <summary>
    /// 레시피 확정 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    /// <param name="Recipe">확정할 레시피</param>
    public readonly record struct CookingRecipeConfirmRequestedEvent(
        CookingGamePanel Source,
        RecipeSO Recipe,
        string VariantId = null
    ) : IEvent;

    /// <summary>
    /// 재료 선택 토글 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    /// <param name="Ingredient">토글할 재료</param>
    public readonly record struct CookingIngredientSelectionToggleRequestedEvent(
        CookingGamePanel Source,
        IngredientSO Ingredient
    ) : IEvent;

    /// <summary>
    /// 재료 선택 제거 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    /// <param name="Ingredient">제거할 재료</param>
    public readonly record struct CookingIngredientSelectionRemoveRequestedEvent(
        CookingGamePanel Source,
        IngredientSO Ingredient
    ) : IEvent;

    /// <summary>
    /// 재료 검색어 변경 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    /// <param name="Query">적용할 검색어</param>
    public readonly record struct CookingIngredientSearchQueryChangeRequestedEvent(
        CookingGamePanel Source,
        string Query
    ) : IEvent;

    /// <summary>
    /// 현재 손질 옵션 인덱스 선택 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    /// <param name="OptionIndex">선택할 옵션 인덱스</param>
    public readonly record struct CookingPreparationSelectCurrentByIndexRequestedEvent(
        CookingGamePanel Source,
        int OptionIndex
    ) : IEvent;

    /// <summary>
    /// 현재 손질 옵션 선택 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    /// <param name="Option">선택할 손질 옵션</param>
    public readonly record struct CookingPreparationSelectCurrentRequestedEvent(
        CookingGamePanel Source,
        IngredientPreparationOption Option
    ) : IEvent;

    /// <summary>
    /// 손질 옵션 선택 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    /// <param name="Ingredient">손질할 재료</param>
    /// <param name="Option">선택할 손질 옵션</param>
    public readonly record struct CookingPreparationSelectRequestedEvent(
        CookingGamePanel Source,
        IngredientSO Ingredient,
        IngredientPreparationOption Option
    ) : IEvent;

    /// <summary>
    /// 손질 상호작용 완료 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    /// <param name="Ingredient">손질한 재료</param>
    /// <param name="Option">적용한 손질 옵션</param>
    /// <param name="MiniGameResult">미니게임 결과</param>
    public readonly record struct CookingPreparationInteractionCompleteRequestedEvent(
        CookingGamePanel Source,
        IngredientSO Ingredient,
        IngredientPreparationOption Option,
        CookingMiniGameResult MiniGameResult
    ) : IEvent;

    /// <summary>
    /// 요리 완료 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    public readonly record struct CookingCompleteRequestedEvent(
        CookingGamePanel Source
    ) : IEvent;

    /// <summary>
    /// 결과 화면 진행 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    public readonly record struct CookingResultAdvanceRequestedEvent(
        CookingGamePanel Source
    ) : IEvent;

    /// <summary>
    /// 요리 결과 NPC 전달 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    public readonly record struct CookingDishHandToNpcRequestedEvent(
        CookingGamePanel Source
    ) : IEvent;

    /// <summary>
    /// NPC 대화 화면 복귀 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    public readonly record struct CookingNpcConversationReturnRequestedEvent(
        CookingGamePanel Source
    ) : IEvent;

    /// <summary>
    /// 요리 화면 닫기 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    public readonly record struct CookingViewsCloseRequestedEvent(
        CookingGamePanel Source
    ) : IEvent;

    /// <summary>
    /// 요리 화면 새로고침 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    public readonly record struct CookingViewsRefreshRequestedEvent(
        CookingGamePanel Source
    ) : IEvent;

    /// <summary>
    /// 손질 화면 열기 요청
    /// </summary>
    /// <param name="Source">요청 대상 요리 패널</param>
    public readonly record struct CookingPreparationOpenRequestedEvent(
        CookingGamePanel Source
    ) : IEvent;
}
