namespace Brigade.Net.Mise;

/// <summary>Compiles a write command into a fresh SQL snapshot.</summary>
public interface ICommandBuilder
{
    /// <summary>Compiles the command and its parameters.</summary>
    CompiledSql Compile();
}
