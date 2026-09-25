namespace Brigade.Net.Mise;

/// <summary>Base class for faults detected by Mise itself.</summary>
/// <param name="message">A safe message containing no parameter values or credentials.</param>
public abstract class DatabaseException(string message) : Exception(message)
{
}
