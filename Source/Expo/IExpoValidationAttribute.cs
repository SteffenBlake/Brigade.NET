namespace Brigade.Net.Expo;

public interface IExpoValidationAttribute
{
    static abstract bool IsValid(object? value);
}
