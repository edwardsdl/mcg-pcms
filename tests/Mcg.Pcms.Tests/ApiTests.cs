using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Mcg.Pcms.Core;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Mcg.Pcms.Tests;

public class PatientApiTests
{
    private readonly HttpClient _client = new WebApplicationFactory<Program>().CreateClient();

    [Fact]
    public async Task CreatePatient_MustReturnOk_WhenGivenValidRequest()
    {
        // Arrange
        var createPatientDto = GetJohnDoe();

        // Act
        var createResponse = await _client.PostAsync("/patients", ToJsonContent(createPatientDto));

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
    }

    [Fact]
    public async Task GetPatient_MustReturnOk_WhenPatientExists()
    {
        // Arrange
        var createPatientDto = GetJohnDoe();
        var createResponse = await _client.PostAsync("/patients", ToJsonContent(createPatientDto));
        var createdPatient = await FromHttpResponse<Patient>(createResponse);

        // Act
        var getResponse = await _client.GetAsync($"/patients/{createdPatient!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task UpdatePatient_MustReturnNoContent_WhenPatientExists()
    {
        // Arrange
        var createPatientDto = GetJohnDoe();
        var createResponse = await _client.PostAsync("/patients", ToJsonContent(createPatientDto));
        var createdPatient = await FromHttpResponse<Patient>(createResponse);

        // Act
        var updatePatientDto = GetJaneDoe();
        var updateResponse =
            await _client.PutAsync($"/patients/{createdPatient!.Id}", ToJsonContent(updatePatientDto));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    }

    [Fact]
    public async Task DeletePatient_MustReturnNoContent_WhenPatientExists()
    {
        // Arrange
        var createPatientDto = GetJohnDoe();
        var createResponse = await _client.PostAsync("/patients", ToJsonContent(createPatientDto));
        var createdPatient = await FromHttpResponse<Patient>(createResponse);

        // Act
        var deleteResponse = await _client.DeleteAsync($"/patients/{createdPatient!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task FindPatients_MustReturnPatients_WhenGivenPartialMatch()
    {
        // Arrange
        await _client.PostAsync("/patients", ToJsonContent(GetJohnDoe()));
        await _client.PostAsync("/patients", ToJsonContent(GetJaneDoe()));

        // Act
        var response = await _client.GetAsync("/patients?name=Test");
        var patients = await FromHttpResponse<List<Patient>>(response);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(patients);
        Assert.NotEmpty(patients);
    }

    [Fact]
    public async Task UploadAttachment_ReturnsCreated()
    {
        // Arrange
        var createPatientDto = GetJohnDoe();
        var createResponse = await _client.PostAsync("/patients", ToJsonContent(createPatientDto));
        var createdPatient = await FromHttpResponse<Patient>(createResponse);

        // Act
        var addClinicalAttachmentResponse =
            await _client.PostAsync($"/patients/{createdPatient!.Id}/attachments", GetClinicalAttachment());

        // Assert
        Assert.Equal(HttpStatusCode.Created, addClinicalAttachmentResponse.StatusCode);
    }

    private PatientDto GetJohnDoe()
    {
        return new PatientDto
        (
            Address: "123 Main Street",
            Age: 65,
            EmailAddress: "john.doe@example.com",
            MedicalHistory: "No pre-existing conditions.",
            Name: "John Doe",
            PhoneNumber: "(888) 555-1212"
        );
    }

    private PatientDto GetJaneDoe()
    {
        return new PatientDto
        (
            Address: "123 Main Street",
            Age: 60,
            EmailAddress: "jane.doe@example.com",
            MedicalHistory: "No pre-existing conditions.",
            Name: "John Doe",
            PhoneNumber: "(888) 555-1212"
        );
    }

    private MultipartFormDataContent GetClinicalAttachment()
    {
        return new MultipartFormDataContent
        {
            {
                new ByteArrayContent("Clinical Attachment"u8.ToArray())
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/octet-stream")
                    }
                },
                "file",
                "clinical_attachment.txt"
            }
        };
    }

    private static StringContent ToJsonContent(PatientDto patientDto)
    {
        return new StringContent(JsonSerializer.Serialize(patientDto), Encoding.UTF8, "application/json");
    }

    private static async Task<T?> FromHttpResponse<T>(HttpResponseMessage response)
    {
        return JsonSerializer.Deserialize<T>(
            await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
    }
}