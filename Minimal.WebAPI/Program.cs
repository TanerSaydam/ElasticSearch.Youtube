using Bogus;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();

ElasticsearchClientSettings settings = new(new Uri("http://localhost:9200"));
settings.DefaultIndex("products");

ElasticsearchClient client = new(settings);

client.IndexAsync("products").GetAwaiter().GetResult();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();




app.MapPost("/products/create", async (CreateProductDto request, CancellationToken cancellationToken) =>
{
    Product product = new()
    {
        Name = request.Name,
        Price = request.Price,
        Stock = request.Stock,
        Description = request.Description
    };

    CreateRequest<Product> createRequest = new(product.Id.ToString())
    {
        Document = product,
    };

    CreateResponse createResponse = await client.CreateAsync(createRequest, cancellationToken);

    return Results.Ok(createResponse.Id);
});

app.MapPut("/products/update", async (UpdateProductDto request, CancellationToken cancellationToken) =>
{

    UpdateRequest<Product, UpdateProductDto> updateRequest = new("products", request.Id.ToString())
    {
        Doc = request
    };

    UpdateResponse<Product> updateResponse = await client.UpdateAsync(updateRequest, cancellationToken);

    return Results.Ok(new { message = "Update is successful" });
});


app.MapDelete("/products/deleteById", async (Guid id, CancellationToken cancellationToken) =>
{
    DeleteResponse deleteResponse = await client.DeleteAsync("products", id, cancellationToken);

    return Results.Ok(new { message = "Delete is successful" });
});

app.MapGet("/products/getall", async (CancellationToken cancellationToken) =>
{
    SearchRequest searchRequest = new("products")
    {
        Size = 100,
        Sort = new List<SortOptions>
        {
            SortOptions.Field(new Field("name.keyword"),new FieldSort(){Order = SortOrder.Asc}),
        },
        //Query = new MatchQuery(new Field("name"))
        //{
        //    Query = "domates"
        //},
        //Query = new WildcardQuery(new Field("name"))
        //{
        //   Value = "*domates*"
        //},
        //Query = new FuzzyQuery(new Field("name"))
        //{
        //    Value = "domatse"
        //},
        Query = new BoolQuery
        {
            Should = new Query[]
           {
               new MatchQuery(new Field("name"))
                {
                    Query = "domates"
                },
                new FuzzyQuery(new Field("description"))
                {
                    Value = "domatse"
                }
           }
        }
    };
    SearchResponse<Product> response = await client.SearchAsync<Product>(searchRequest, cancellationToken);

    return Results.Ok(response.Documents);
});


app.MapGet("/products/seeddata", async (CancellationToken cancellationToken) =>
{
    for (int i = 0; i < 100; i++)
    {
        Faker faker = new();
        Product product = new()
        {
            Name = faker.Commerce.ProductName(),
            Price = Convert.ToDecimal(faker.Commerce.Price()),
            Stock = faker.Commerce.Random.Int(1, 20),
            Description = faker.Commerce.ProductDescription()
        };

        CreateRequest<Product> createRequest = new(product.Id.ToString())
        {
            Document = product
        };
        await client.CreateAsync(createRequest, cancellationToken);
    }

    return Results.Created();
});




app.Run();


class Product
{
    public Product()
    {
        Id = Guid.NewGuid();
    }
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Description { get; set; } = default!;
}


record CreateProductDto(
    string Name,
    decimal Price,
    int Stock,
    string Description);

record UpdateProductDto(
    Guid Id,
    string Name,
    decimal Price,
    int Stock,
    string Description);