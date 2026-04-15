using Chat.Components;
using Chat.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient("LMStudio", client =>
{
    client.BaseAddress = new Uri("http://172.23.20.107:1234/");
});

builder.Services.AddScoped<LlmChatServices>();

builder.Services.AddScoped<DocumentService>();

builder.Services.AddScoped<EmbeddingService>();

builder.Services.AddScoped<VectorSearchService>();

builder.Services.AddScoped<UserVectorStore>();

builder.Services.AddScoped<UserDocumentService>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();


//Shared config / read-only data	    Singleton
//User-specific session data	        Scoped (IN blazor sever = per connection, Web assembly = per browser session)
//Temporary utility / stateless service	Transient
