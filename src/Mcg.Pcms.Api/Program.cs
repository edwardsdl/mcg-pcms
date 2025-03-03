using Mcg.Pcms.Core;
using Mcg.Pcms.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateLogger();
builder.Services.AddSerilog();

// Configure HTTP server
builder.WebHost.ConfigureKestrel(options => { options.ListenAnyIP(5016); });

// Configure authentication and authorization
builder.Services
    .AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddAuthorization();

// Configure Open API
builder.Services.AddOpenApi();

// Configure Scalar
builder.Services.Configure<ScalarOptions>(options =>
{
    options.HideModels = true;
    options.HideClientButton = true;
    options.AddServer(new ScalarServer("http://localhost:5016"));
});

// Configure persistence
builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("pcms"));
builder.Services.AddScoped<IPatientRepository, PatientRepository>();

// Configure other services
builder.Services.AddScoped<PatientService>();

try
{
    Log.Information("Application started");

    var app = builder.Build();
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapIdentityApi<IdentityUser>();
    app.MapOpenApi();
    app.MapScalarApiReference("/");
    app.UseDeveloperExceptionPage();

    // For more information about configuring authorization policies, see
    // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security?view=aspnetcore-9.0#configuring-authorization-policies-in-minimal-apps

    app.MapPost("/patients",
            async ([FromBody] PatientDto patientDto, PatientService patientService) =>
            {
                var createdPatient = await patientService.CreatePatientAsync(patientDto);
                return Results.Created($"/patients/{createdPatient.Id}", createdPatient);
            })
        .RequireAuthorization();

    app.MapGet("/patients", async (string? query, PatientService patientService) =>
        {
            var patients = await patientService.FindPatientsAsync(query);
            return Results.Ok(patients);
        })
        .RequireAuthorization();

    app.MapGet("/patients/{patientId:guid}", async (Guid patientId, PatientService patientService) =>
        {
            try
            {
                var patient = await patientService.GetPatientAsync(patientId);
                return Results.Ok(patient);
            }
            catch (PatientNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization();

    app.MapPut("/patients/{patientId:guid}",
            async (Guid patientId, [FromBody] PatientDto patientDto, PatientService patientService) =>
            {
                try
                {
                    await patientService.UpdatePatientAsync(patientId, patientDto);
                    return Results.NoContent();
                }
                catch (PatientNotFoundException)
                {
                    return Results.NotFound();
                }
            })
        .RequireAuthorization();

    app.MapDelete("/patients/{patientId:guid}", async (Guid patientId, PatientService patientService) =>
        {
            try
            {
                await patientService.DeletePatientAsync(patientId);
                return Results.NoContent();
            }
            catch (PatientNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization();

    // By default, CSRF protection is enabled for endpoints that accept IFormFile. Since we're using bearer tokens and
    // not session cookies, we don't need this protection, and it can be disabled.
    app.MapPost("/patients/{patientId:guid}/attachments",
            async (Guid patientId, [FromForm] IFormFile file, PatientService patientService) =>
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileContents = memoryStream.ToArray();

                await patientService.CreateAttachmentAsync(patientId, file.FileName, file.ContentType, fileContents);

                return Results.Created($"/patients/{patientId}/attachments/{file.FileName}", null);
            })
        .DisableAntiforgery()
        .RequireAuthorization();

    app.MapGet("/patients/{patientId:guid}/attachments/{fileName}",
            async (Guid patientId, string fileName, PatientService patientService) =>
            {
                try
                {
                    var clinicalAttachment = await patientService.GetAttachmentAsync(patientId, fileName);
                    return Results.File(clinicalAttachment.FileContents, clinicalAttachment.ContentType, fileName);
                }
                catch (ClinicalAttachmentNotFoundException)
                {
                    return Results.NotFound();
                }
                catch (PatientNotFoundException)
                {
                    return Results.NotFound();
                }
            })
        .RequireAuthorization();

    app.MapDelete("/patients/{patientId:guid}/attachments/{fileName}",
            async (Guid patientId, string fileName, PatientService patientService) =>
            {
                try
                {
                    await patientService.DeleteAttachmentAsync(patientId, fileName);
                    return Results.NoContent();
                }
                catch (ClinicalAttachmentNotFoundException)
                {
                    return Results.NotFound();
                }
                catch (PatientNotFoundException)
                {
                    return Results.NotFound();
                }
            })
        .RequireAuthorization();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}