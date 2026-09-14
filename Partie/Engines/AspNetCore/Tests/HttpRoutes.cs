using Brigade.Net.Core.Results;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Optional.SearchV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Failure.SearchV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Status.SearchV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Text;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Text.Provided.SearchV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Text.Request.SearchV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.CreateV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.UpdateV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.DeleteV1;
using Brigade.Net.Partie.Engines.AspNetCore.Tests.Fixtures.Items.SearchV1;
using Brigade.Net.Core.Transactions;
using Brigade.Net.Partie;
using Microsoft.AspNetCore.Http;

namespace Brigade.Net.Partie.Engines.AspNetCore.Tests;

[BrigadeGroup("/items"), Provider(typeof(ContextProvider))]
public static partial class HttpRoutes
{
    [Post("{id}"), Partie(typeof(ValidationPartie)), Partie(typeof(UnitOfWorkPartie)), Handler(typeof(ItemCreateV1Handler))]
    static partial void Post();
    [Put("{id}"), Partie(typeof(ValidationPartie)), Partie(typeof(UnitOfWorkPartie)), Handler(typeof(ItemUpdateV1Handler))]
    static partial void Put();
    [Patch("{id}"), Partie(typeof(ValidationPartie)), Partie(typeof(UnitOfWorkPartie)), Handler(typeof(ItemUpdateV1Handler))]
    static partial void Patch();
    [Delete("{id}"), Partie(typeof(ValidationPartie)), Partie(typeof(UnitOfWorkPartie)), Handler(typeof(ItemDeleteV1Handler))]
    static partial void Delete();
    [Get("search/{category}"), Partie(typeof(ValidationPartie)), Handler(typeof(ItemSearchV1Handler))]
    static partial void Read();
    [Get("failure"), Handler(typeof(FailureSearchV1Handler))]
    static partial void Failure();
    [Get("status"), Handler(typeof(StatusSearchV1Handler))]
    static partial void Status();
    [Get("optional"), Partie(typeof(ValidationPartie)), Handler(typeof(OptionalSearchV1Handler))]
    static partial void Optional();
    [Get("provider-first"), Handler(typeof(TextProvidedSearchV1Handler)), Provider(typeof(TextLengthProvider))]
    static partial void ProviderFirst();
    [Get("input-first"), Handler(typeof(TextRequestSearchV1Handler)), Provider(typeof(TextLengthProvider))]
    static partial void InputFirst();
}
