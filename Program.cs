using Microsoft.EntityFrameworkCore;
using ContestApi.Data;
using ContestApi.Models;
using ContestApi.Services;
using Azure.Storage.Blobs;
using Azure.Identity;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

/* ─────────────────────────────────────────────────────────────
   1) Load Key Vault (if configured)
   - Reads KeyVault__Url from App Service settings
   - Uses the App Service's Managed Identity (DefaultAzureCredential)
   ───────────────────────────────────────────────────────────── */
var kvUrl = builder.Configuration["KeyVault__Url"];


if (!string.IsNullOrWhiteSpace(kvUrl))
{
    builder.Configuration.AddAzureKeyVault(new Uri(kvUrl), new DefaultAzureCredential());
}


builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowStarbucks", p => p
        .SetIsOriginAllowed(origin =>
            origin.Equals("http://localhost:5173", StringComparison.OrdinalIgnoreCase) ||
            origin.Equals("https://ganaconstarbucks.com", StringComparison.OrdinalIgnoreCase) ||
            origin.Equals("https://www.ganaconstarbucks.com", StringComparison.OrdinalIgnoreCase) ||
            origin.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)   // previews & prod on Vercel
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("Content-Disposition"));
});


// --------------------
// 2️⃣ Configure Authentication with Cookies
// --------------------
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        // Make cookies cross-site compatible
        options.Cookie.Name = "ContestAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.Path = "/";
        options.SlidingExpiration = true;

        // Optional: redirect paths
        options.LoginPath = "/api/auth/login";
        options.LogoutPath = "/api/auth/logout";
        options.AccessDeniedPath = "/api/auth/denied";
    });


/* ─────────────────────────────────────────────────────────────
   2) EF Core: SQL connection from Configuration
   - With KV provider: secret name "ConnectionStrings--Sql" maps to "ConnectionStrings:Sql"
   - GetConnectionString("Sql") works as-is
   ───────────────────────────────────────────────────────────── */
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("Sql"))
);

/* ─────────────────────────────────────────────────────────────
   3) Swagger/OpenAPI (show UI only in Development)
   ───────────────────────────────────────────────────────────── */
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

/* ─────────────────────────────────────────────────────────────
   4) Upload/Container settings (non-secrets from App Settings)
   ───────────────────────────────────────────────────────────── */
var containerName = builder.Configuration["Storage__Container"] ?? "contest-photos";
var maxBytes = long.TryParse(builder.Configuration["Uploads__MaxBytes"], out var b)
    ? b
    : 5L * 1024 * 1024;
var allowedTypes = (builder.Configuration["Uploads__AllowedTypes"] ?? "image/webp,image/jpeg,image/png")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

/* ─────────────────────────────────────────────────────────────
   5) CORS: allow one or more origins (comma-separated)
   - App setting: Cors__AllowedOrigin, e.g. "https://contest-web.azurewebsites.net"
   - You can include multiple origins separated by commas
   ───────────────────────────────────────────────────────────── */
var originsSetting = builder.Configuration["Cors__AllowedOrigin"] ?? "http://localhost:5173";

/* ─────────────────────────────────────────────────────────────
   6) Options and Blob client
   - Prefer a Storage connection string from KV/app settings:
       KV secret "Storage--ConnectionString" => config key "Storage:ConnectionString"
       or app setting "Storage__ConnectionString"
   - Fallback: account name + MSI (if you choose that model)
   ───────────────────────────────────────────────────────────── */
builder.Services.AddSingleton(new StoreOptions
{
    ContainerName = containerName,
    MaxBytes = maxBytes,
    AllowedContentTypes = allowedTypes
});

// // Bind EmailOptions from configuration
// builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

// // Register the email service
// builder.Services.AddSingleton<AcsEmailService>();

var adminKey = builder.Configuration["Admin:Key"]
    ?? builder.Configuration["Admin__Key"];
var storageConn =
    builder.Configuration["Storage:ConnectionString"]         // from KV provider mapping (ConnectionString under "Storage")
    ?? builder.Configuration["Storage__ConnectionString"];    // from regular app settings (double-underscore)

/* If you only use connection string (recommended), this branch is enough */
if (!string.IsNullOrWhiteSpace(storageConn))
{
    builder.Services.AddSingleton(new BlobServiceClient(storageConn));
}
else
{
    // Optional fallback if you store only account name and use MSI
    var account = builder.Configuration["Storage:AccountName"]  // KV-style ("Storage--AccountName")
               ?? builder.Configuration["Storage__AccountName"]; // appsetting-style
    if (string.IsNullOrWhiteSpace(account))
        throw new InvalidOperationException("Provide Storage:ConnectionString (preferred) or Storage:AccountName for MSI flow.");

    builder.Services.AddSingleton(new BlobServiceClient(
        new Uri($"https://{account}.blob.core.windows.net"),
        new DefaultAzureCredential()
    ));
}

builder.Services.AddScoped<BlobUploadService>();

var app = builder.Build();

/* ─────────────────────────────────────────────────────────────
   7) Middleware order
   ───────────────────────────────────────────────────────────── */
// app.UseCors("frontend");
app.UseCors("AllowStarbucks");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

/* ─────────────────────────────────────────────────────────────
   8) Health
   ───────────────────────────────────────────────────────────── */
app.MapGet("/health", () => Results.Ok(new { ok = true, time = DateTime.UtcNow }));

/* ─────────────────────────────────────────────────────────────
   9) Dev seed (optional)
   ───────────────────────────────────────────────────────────── */
app.MapPost("/dev/seed", async (AppDbContext db) =>
{
    if (!await db.Contests.AnyAsync())
    {
        db.Contests.Add(new Contest
        {
            Name = "Photo Contest 2025",
            Slug = "photo-contest-2025",
            StartsAtUtc = DateTime.UtcNow,
            EndsAtUtc = DateTime.UtcNow.AddDays(30),
            IsActive = true
        });
        await db.SaveChangesAsync();
    }
    return Results.Ok("Seeded.");
});

/* ─────────────────────────────────────────────────────────────
   10) Endpoints (unchanged)
   ───────────────────────────────────────────────────────────── */
app.MapPost("/api/contests", async (Contest contest, AppDbContext db) =>
{
    var exists = await db.Contests.AnyAsync(c => c.Slug == contest.Slug);
    if (exists) return Results.Conflict("Contest slug already exists.");

    contest.StartsAtUtc = contest.StartsAtUtc.ToUniversalTime();
    contest.EndsAtUtc   = contest.EndsAtUtc.ToUniversalTime();

    db.Contests.Add(contest);
    await db.SaveChangesAsync();

    return Results.Created($"/api/contests/{contest.ContestId}", contest);
});

app.MapGet("/api/contests/{slug}", async (string slug, AppDbContext db) =>
{
    var contest = await db.Contests
        .Where(c => c.Slug == slug)
        .Select(c => new { c.ContestId, c.Name, c.Slug, c.StartsAtUtc, c.EndsAtUtc, c.IsActive })
        .FirstOrDefaultAsync();

    return contest is null ? Results.NotFound() : Results.Ok(contest);
});

app.MapPost("/api/auth/login", async (HttpContext ctx) =>
{
    // validate user credentials here...

    var claims = new[] { new System.Security.Claims.Claim("user", "demo") };
    var identity = new System.Security.Claims.ClaimsIdentity(claims, "Cookies");
    var principal = new System.Security.Claims.ClaimsPrincipal(identity);

    await ctx.SignInAsync("Cookies", principal);
    return Results.Ok(new { message = "Logged in successfully" });
});

app.MapPost("/api/submissions", async (SubmissionDto dto, AppDbContext db,  CancellationToken ct) =>
{
    var ctx = new ValidationContext(dto);
    var results = new List<ValidationResult>();
    if (!Validator.TryValidateObject(dto, ctx, results, true))
        return Results.BadRequest(new { errors = results.Select(r => r.ErrorMessage) });

    var contest = await db.Contests.FirstOrDefaultAsync(c => c.Slug == dto.ContestSlug && c.IsActive);
    if (contest is null) return Results.BadRequest("Contest not found or inactive.");

    var exists = await db.Submissions.AnyAsync(s => s.ContestId == contest.ContestId && s.Email == dto.Email);
    if (exists) return Results.Conflict("This email already submitted.");

    var submission = new Submission
    {
        ContestId = contest.ContestId,
        FirstName = dto.FirstName,
        LastName  = dto.LastName,
        Email     = dto.Email,
        Phone     = dto.Phone,
        ConsentGiven   = dto.ConsentGiven,
        ConsentVersion = dto.ConsentVersion,
        BlobName    = dto.BlobName,
        ContentType = dto.ContentType,
        SizeBytes   = dto.SizeBytes,
        CreatedAtUtc = DateTime.UtcNow
    };

    db.Submissions.Add(submission);
    await db.SaveChangesAsync(ct);

   
    return Results.Created($"/api/submissions/{submission.SubmissionId}", new
    {
        submission.SubmissionId,
        submission.CreatedAtUtc
    });
});

app.MapGet("/api/submissions", async (string contestSlug, int page, int pageSize, HttpRequest request,
 AppDbContext db) =>
{
    var providedKey = request.Headers["x-admin-key"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(providedKey) || providedKey != adminKey)
    {
        return Results.Unauthorized();
    }

    page = page <= 0 ? 1 : page;
    pageSize = pageSize <= 0 || pageSize > 500 ? 50 : pageSize;

    var baseQuery = db.Submissions
        .Where(s => s.Contest.Slug == contestSlug)
        .OrderByDescending(s => s.CreatedAtUtc);

    var total = await baseQuery.CountAsync();
    var items = await baseQuery.Skip((page - 1) * pageSize).Take(pageSize)
        .Select(s => new { s.FirstName, s.LastName, s.Email, s.Phone, s.ConsentGiven, s.ConsentVersion, s.CreatedAtUtc })
        .ToListAsync();

    return Results.Ok(new { total, page, pageSize, items });
});

app.MapGet("/api/submissions/export", async (
    string contestSlug,
    HttpRequest request,
    AppDbContext db) =>
{
    // 1) Check admin key – allow header or query string
    var providedKey =
        request.Headers["x-admin-key"].FirstOrDefault()
        ?? request.Query["adminKey"].FirstOrDefault();

    if (string.IsNullOrWhiteSpace(providedKey) || providedKey != adminKey)
    {
        return Results.Unauthorized();
    }

    // 2) Query data
    var data = await db.Submissions
        .Where(s => s.Contest.Slug == contestSlug)
        .OrderByDescending(s => s.CreatedAtUtc)
        .Select(s => new
        {
            // If you want to KEEP the ID in the CSV, leave this:
            // s.SubmissionId,
            s.FirstName,
            s.LastName,
            s.Email,
            s.Phone,
            s.ConsentGiven,
            s.ConsentVersion,
            s.CreatedAtUtc
        })
        .ToListAsync();

    var sb = new StringBuilder();

    // Header row – remove SubmissionId if you don't want it
    sb.AppendLine("FirstName,LastName,Email,Phone,ConsentGiven,ConsentVersion,CreatedAtUtc");

    static string esc(string? v) => $"\"{(v ?? "").Replace("\"", "\"\"")}\"";

    foreach (var r in data)
    {
        sb.AppendLine(string.Join(",",
            // If you keep ID:
            // r.SubmissionId,
            esc(r.FirstName),
            esc(r.LastName),
            esc(r.Email),
            esc(r.Phone),
            r.ConsentGiven,
            esc(r.ConsentVersion ?? ""),
            r.CreatedAtUtc.ToString("u")
        ));
    }

    var bytes = Encoding.UTF8.GetBytes(sb.ToString());
    var fileName = $"submissions_{contestSlug}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

    return Results.File(bytes, "text/csv; charset=utf-8", fileName);
});

app.MapGet("/dbcheck", async (AppDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { status = "connected" })
            : Results.Problem("Cannot connect to database.");
    }
    catch (Exception ex)
    {
        return Results.Problem($"DB connection failed: {ex.Message}");
    }
});

app.MapPost("/api/uploads/presign", async (PresignRequest req, BlobUploadService uploader) =>
{
    try
    {
        var res = await uploader.GetWriteSasAsync(req.FileName, req.ContentType, req.Bytes);
        return Results.Ok(new PresignResponse(res.BlobName, res.UploadUrl, res.ExpiresAtUtc));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

/* ─────────────────────────────────────────────────────────────
   11) Auto-migrate on startup (prod-safe if your migrations are correct)
   ───────────────────────────────────────────────────────────── */
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
