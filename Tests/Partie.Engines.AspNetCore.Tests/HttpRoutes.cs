using Brigade.Net.Core.Results;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Failure.SearchV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Optional.SearchV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Status.SearchV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Text;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Text.Provided.SearchV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Text.Request.SearchV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.DeleteV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.CreateV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.SearchV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.UpdateV1;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

[BrigadeGroup("/items")]
public static partial class HttpRoutes
{
    [ItemCreateV1HandlerRoute.Post("{itemId}"), ValidationPartie,
        UnitOfWorkPartie, ContextProvider]
    static partial void Post();
    [ItemUpdateV1HandlerRoute.Put("{itemId}"), ValidationPartie,
        UnitOfWorkPartie, ContextProvider]
    static partial void Put();
    [ItemUpdateV1HandlerRoute.Patch("{itemId}"), ValidationPartie,
        UnitOfWorkPartie, ContextProvider]
    static partial void Patch();
    [ItemDeleteV1HandlerRoute.Delete("{itemId}"), ValidationPartie,
        UnitOfWorkPartie, ContextProvider]
    static partial void Delete();
    [ItemSearchV1HandlerRoute.Get("search/{category}"), ValidationPartie, ContextProvider]
    static partial void Read();
    [FailureSearchV1HandlerRoute.Get("failure")]
    static partial void Failure();
    [StatusSearchV1HandlerRoute.Get("status")]
    static partial void Status();
    [OptionalSearchV1HandlerRoute.Get("optional"), ValidationPartie]
    static partial void Optional();
    [TextProvidedSearchV1HandlerRoute.Get("provider-first"), TextLengthProvider]
    static partial void ProviderFirst();
    [TextRequestSearchV1HandlerRoute.Get("input-first"), TextLengthProvider]
    static partial void InputFirst();
}
