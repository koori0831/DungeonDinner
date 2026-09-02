namespace Work.Dispatch.Code.Runtime
{
    public enum DispatchValidationError
    {
        None = 0,
        ConfigurationMissing,
        ActiveDispatchExists,
        NpcMissing,
        NpcRuleMissing,
        AffinityTooLow,
        RegionMissing,
        NpcCannotVisitRegion,
        RequestMissing,
        TooManyMaterialTypes,
        DuplicateMaterial,
        MaterialUnavailable,
        InvalidAmount
    }

    public readonly struct DispatchValidationResult
    {
        public static readonly DispatchValidationResult Success =
            new DispatchValidationResult(DispatchValidationError.None, string.Empty);

        public DispatchValidationError Error { get; }
        public string Message { get; }
        public bool IsValid => Error == DispatchValidationError.None;

        public DispatchValidationResult(DispatchValidationError error, string message)
        {
            Error = error;
            Message = message ?? string.Empty;
        }
    }
}
