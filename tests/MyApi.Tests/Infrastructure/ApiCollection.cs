namespace MyApi.Tests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<MyApiFactory>
{
    public const string Name = "api";
}
