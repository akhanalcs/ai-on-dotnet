using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using RagChat.Web.Components;
using RagChat.Web.Services;
using RagChat.Web.Services.Security;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Sign-in with Entra ID (OpenID Connect). The token's "roles" claim carries the user's access groups (COC-4, COC-6).
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
// Every page and endpoint requires a signed-in user unless it opts out
builder.Services.AddAuthorization(options => options.FallbackPolicy = options.DefaultPolicy);
// Lets Blazor components read the signed-in user
builder.Services.AddCascadingAuthenticationState();

// Documents live outside wwwroot, so they're never served as public static files
builder.AddRagChat(ingestionDirectory: Path.Combine(builder.Environment.ContentRootPath, "Data"));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // wwwroot (CSS, JS, PDF viewer) stays public
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapDocumentEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
