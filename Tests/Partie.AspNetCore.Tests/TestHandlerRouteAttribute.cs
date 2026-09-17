using Brigade.Net.Partie.Engines.AspNetCore;

namespace Brigade.Net.Partie.AspNetCore.Tests;

internal sealed class TestHandlerRouteAttribute(string path = "")
    : HandlerRouteAttribute<TypeAttributeTests>(path, "POST");
