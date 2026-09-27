namespace Brigade.Net.Benchmarks.Validation.Common;

public sealed record ValidationCase(
    ExpoValidationModel Expo,
    ValidlyValidationModel Validly,
    AnnotationValidationModel Standard
);
