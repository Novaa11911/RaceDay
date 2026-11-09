
using RaceDay.Part3.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".RaceDay.Mvc.Session";
});

// Configure the typed HTTP client for the Part 2 API
builder.Services.AddHttpClient<RaceDayApiClient>(client =>
{
    var baseUrl = builder.Configuration["ApiSettings:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl) ||
        !Uri.TryCreate(baseUrl, UriKind.Absolute, out var apiUri) ||
        (apiUri.Scheme != Uri.UriSchemeHttp &&
         apiUri.Scheme != Uri.UriSchemeHttps))
    {
        throw new InvalidOperationException(
            "ApiSettings:BaseUrl is missing or invalid. " +
            "Set it to the root URL of the Part 2 API in appsettings.json.");
    }

    client.BaseAddress = new Uri(
        apiUri.AbsoluteUri.EndsWith('/')
            ? apiUri.AbsoluteUri
            : apiUri.AbsoluteUri + "/");

    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Error handling and security
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Session must be enabled before MVC endpoints execute.
app.UseSession();

// Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
