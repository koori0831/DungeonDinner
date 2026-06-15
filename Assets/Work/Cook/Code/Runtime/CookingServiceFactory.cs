using Work.Cook.Code.Data;

namespace Work.Cook.Code.Runtime
{
    public static class CookingServiceFactory
    {
        public static ICookingDataProvider CreateDataProvider(CookingDataCatalogSO catalog)
        {
            return new CookingDataProvider(catalog);
        }

        public static IDishResultBuilder CreateDishResultBuilder(CookingDataCatalogSO catalog)
        {
            ICookingDataProvider dataProvider = CreateDataProvider(catalog);
            return new DishResultBuilder(new RecipeMatcher(dataProvider));
        }

        public static CookingFlowController CreateFlowController(CookingDataCatalogSO catalog)
        {
            ICookingDataProvider dataProvider = CreateDataProvider(catalog);
            IDishResultBuilder resultBuilder = new DishResultBuilder(new RecipeMatcher(dataProvider));
            return new CookingFlowController(dataProvider, resultBuilder);
        }
    }
}
