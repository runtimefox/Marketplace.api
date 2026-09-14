using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MyApi.GraphQL;
using MyApi.GraphQL.Cart;
using MyApi.GraphQL.Categories;
using MyApi.GraphQL.Orders;
using MyApi.GraphQL.Products;
using MyApi.GraphQL.Sellers;
using MyApi.GraphQL.Users;
using MyApi.Services;
using MyApi.Services.Interfaces.Auth;
using MyApi.Services.Interfaces.Cart;
using MyApi.Services.Interfaces.Categories;
using MyApi.Services.Interfaces.Orders;
using MyApi.Services.Interfaces.Products;
using MyApi.Services.Interfaces.Sellers;
using MyApi.Services.Interfaces.Users;
using MyApi.Shared.Auth;
using MyApi.Shared.Configuration;
using MyApi.Shared.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DbConnection"))
);


builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                 ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = AuthClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (!context.Request.Headers.ContainsKey("Authorization")
                    && context.Request.Cookies.TryGetValue(AuthCookies.AccessToken, out var token))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISellerService, SellerService>();
builder.Services.AddScoped<ISellerAccessService, SellerAccessService>();
builder.Services.AddScoped<ISellerMemberService, SellerMemberService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT access token without the \"Bearer\" prefix. Browser requests are authenticated by the access_token cookie set by /api/auth/login."
    });

    options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() }
    });
});

builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType()
    .AddMutationType()
    .AddTypeExtension<UserQueries>()
    .AddTypeExtension<CategoryQueries>()
    .AddTypeExtension<CategoryMutations>()
    .AddTypeExtension<CartQueries>()
    .AddTypeExtension<CartMutations>()
    .AddTypeExtension<OrderQueries>()
    .AddTypeExtension<OrderMutations>()
    .AddTypeExtension<ProductQueries>()
    .AddTypeExtension<ProductMutations>()
    .AddTypeExtension<SellerQueries>()
    .AddTypeExtension<SellerMutations>()
    .AddFiltering()
    .AddSorting()
    .AddErrorFilter<DomainErrorFilter>();

var app = builder.Build();

if(builder.Environment.IsDevelopment()){
    await Seed.RunAsync(app.Services);
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGraphQL();

app.Run();
