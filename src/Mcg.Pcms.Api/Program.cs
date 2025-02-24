using Mcg.Pcms.Core;
using Mcg.Pcms.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5016); // Listen on all network interfaces
});

// Configure authentication and authorization
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<PatientDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.RequireHttpsMetadata = true);
builder.Services.AddAuthorization();

// Configure Open API
builder.Services.AddOpenApi();

// Configure persistence
builder.Services.AddDbContext<PatientDbContext>(options => options.UseInMemoryDatabase("pcms"));
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
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseDeveloperExceptionPage();

    app.MapPost("/patients",
        async ([FromBody] CreatePatientRequest createPatientRequest, PatientService patientService) =>
        {
            var createdPatient = await patientService.CreatePatientAsync(createPatientRequest);
            return Results.Created($"/patients/{createdPatient.Id}", createdPatient);
        });

    app.MapGet("/patients", async (string? query, PatientService patientService) =>
    {
        var patients = await patientService.FindPatientsAsync(query);
        return Results.Ok(patients);
    });

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
    });

    app.MapPut("/patients/{patientId:guid}",
        async (Guid patientId, [FromBody] UpdatePatientRequest updatePatientRequest, PatientService patientService) =>
        {
            try
            {
                await patientService.UpdatePatientAsync(patientId, updatePatientRequest);
                return Results.NoContent();
            }
            catch (PatientNotFoundException)
            {
                return Results.NotFound();
            }
        });

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
    });

    app.MapPost("/patients/{patientId:guid}/attachments",
            async (Guid patientId, [FromForm] IFormFile file, PatientService patientService) =>
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                var fileContents = memoryStream.ToArray();

                await patientService.CreateAttachmentAsync(patientId, file.FileName, file.ContentType, fileContents);

                return Results.Created($"/patients/{patientId}/attachments/{file.FileName}", null);
            })
        .DisableAntiforgery();

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
        });

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
        });

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