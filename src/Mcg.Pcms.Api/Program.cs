using Mcg.Pcms.Core;
using Mcg.Pcms.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseDeveloperExceptionPage();
}

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

app.MapGet("/patients/{id:guid}", async (Guid id, PatientService patientService) =>
{
    try
    {
        var patient = await patientService.GetPatientAsync(id);
        return Results.Ok(patient);
    }
    catch (PatientNotFoundException)
    {
        return Results.NotFound();
    }
});

app.MapPut("/patients/{id:guid}",
    async (Guid id, [FromBody] UpdatePatientRequest updatePatientRequest, PatientService patientService) =>
    {
        try
        {
            await patientService.UpdatePatientAsync(id, updatePatientRequest);
            return Results.NoContent();
        }
        catch (PatientNotFoundException)
        {
            return Results.NotFound();
        }
    });

app.MapDelete("/patients/{id:guid}", async (Guid id, PatientService patientService) =>
{
    try
    {
        await patientService.DeletePatientAsync(id);
        return Results.NoContent();
    }
    catch (PatientNotFoundException)
    {
        return Results.NotFound();
    }
});

app.MapPost("/patients/{patientId}/attachments",
    async (Guid patientId, HttpRequest request, PatientService patientService) =>
    {
        var formFile = request.Form.Files.FirstOrDefault();
        if (formFile is null || formFile.Length == 0)
        {
            return Results.BadRequest("No file uploaded.");
        }

        using var memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        await patientService.CreateAttachmentAsync(patientId, formFile.FileName, fileBytes);
        return Results.Created($"/patients/{patientId}/attachments/{formFile.FileName}", null);
    });

app.MapGet("/patients/{patientId:guid}/attachments/{filename}",
    async (Guid patientId, string filename, PatientService patientService) =>
    {
        try
        {
            var clinicalAttachment = await patientService.GetAttachmentAsync(patientId, filename);
            return Results.File(clinicalAttachment.Data, "application/octet-stream", filename);
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

app.MapDelete("/patients/{patientId:guid}/attachments/{filename}",
    async (Guid patientId, string filename, PatientService patientService) =>
    {
        try
        {
            await patientService.DeleteAttachmentAsync(patientId, filename);
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