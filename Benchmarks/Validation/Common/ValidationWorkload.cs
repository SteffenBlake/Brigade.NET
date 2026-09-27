using System.ComponentModel.DataAnnotations;

namespace Brigade.Net.Benchmarks.Validation.Common;

public static class ValidationWorkload
{
    private static readonly FluentValidationModelValidator FluentValidator = new();

    public static int Expo(ExpoValidationModel model)
    {
        model.TryValidate(out var errors);
        return errors.Count();
    }

    public static int Validly(ValidlyValidationModel model)
    {
        using var result = model.Validate();
        return result.Properties.Count(property => !property.IsSuccess);
    }

    public static int FluentValidation(AnnotationValidationModel model)
    {
        return FluentValidator.Validate(model).Errors.Count;
    }

    public static int DataAnnotations(AnnotationValidationModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results.Count;
    }
}
