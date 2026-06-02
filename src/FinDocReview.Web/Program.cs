using FinDocReview.Infrastructure.Data;
using FinDocReview.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010

var builder = WebApplication.CreateBuilder(args);

// EF Core + SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<AppDbContext>();

// Semantic Kernel
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"]!;

var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion("gpt-4o-mini", openAiApiKey)
    .AddOpenAITextEmbeddingGeneration("text-embedding-3-small", openAiApiKey)
    .Build();

builder.Services.AddSingleton(kernel);
builder.Services.AddSingleton<IChatCompletionService>(sp =>
    sp.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>());
builder.Services.AddSingleton<ITextEmbeddingGenerationService>(sp =>
    sp.GetRequiredService<Kernel>().GetRequiredService<ITextEmbeddingGenerationService>());

// Infrastructure services
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<TextExtractionService>();
builder.Services.AddScoped<ChunkingService>();
builder.Services.AddScoped<EmbeddingService>();
builder.Services.AddScoped<AiSummarizationService>();
builder.Services.AddScoped<SemanticSearchService>();
builder.Services.AddScoped<QaService>();
builder.Services.AddSingleton<DocumentProcessingService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DocumentProcessingService>());

// Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();