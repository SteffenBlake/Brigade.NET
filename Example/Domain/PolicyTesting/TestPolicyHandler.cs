using Brigade.Net.Core.Results;
using Brigade.Net.Partie;

namespace Brigade.Net.Example.Domain.PolicyTesting;

/// <summary>Simple test handler for policy testing.</summary>
public static class TestPolicyHandler
{
    public static Result<string> InvokeAsync() => "success";
}
