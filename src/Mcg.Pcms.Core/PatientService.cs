using Microsoft.Extensions.Logging;

namespace Mcg.Pcms.Core;

public class PatientService(IPatientRepository repository, ILogger<PatientService> logger)
{
    private IPatientRepository Repository { get; } = repository;
    private ILogger<PatientService> Logger { get; } = logger;

    public async Task<Patient> CreatePatientAsync(CreatePatientRequest createPatientRequest)
    {
        Logger.LogInformation("Creating patient");

        var patient = new Patient
        {
            Address = createPatientRequest.Address,
            Age = createPatientRequest.Age,
            EmailAddress = createPatientRequest.EmailAddress,
            MedicalHistory = createPatientRequest.MedicalHistory,
            Name = createPatientRequest.Name,
            PhoneNumber = createPatientRequest.PhoneNumber
        };

        return await Repository.AddPatientAsync(patient);
    }

    public async Task<IEnumerable<Patient>> FindPatientsAsync(string? query = null)
    {
        Logger.LogInformation("Finding patient with query {query}", query);

        return await Repository.FindPatientsAsync(query);
    }

    public async Task<Patient> GetPatientAsync(Guid patientId)
    {
        Logger.LogInformation("Getting patient with id {patientId}", patientId);

        return await Repository.GetPatientAsync(patientId);
    }

    public async Task UpdatePatientAsync(Guid patientId, UpdatePatientRequest updatePatientRequest)
    {
        Logger.LogInformation("Updating patient with id {patientId}", patientId);

        var patient = await Repository.GetPatientAsync(patientId);
        patient.Address = updatePatientRequest.Address;
        patient.Age = updatePatientRequest.Age;
        patient.EmailAddress = updatePatientRequest.EmailAddress;
        patient.MedicalHistory = updatePatientRequest.MedicalHistory;
        patient.Name = updatePatientRequest.Name;
        patient.PhoneNumber = updatePatientRequest.PhoneNumber;

        await Repository.UpdatePatientAsync(patient);
    }

    public async Task DeletePatientAsync(Guid patientId)
    {
        Logger.LogInformation("Deleting patient with id {patientId}", patientId);

        await Repository.RemovePatientAsync(patientId);
    }

    public async Task CreateAttachmentAsync(Guid patientId, string fileName, string contentType, byte[] fileContents)
    {
        Logger.LogInformation("Creating attachment with patient with id {id}", patientId);

        var patient = await GetPatientAsync(patientId);
        await Repository.AddClinicalAttachmentAsync(patient, fileName, contentType, fileContents);
    }

    public async Task<ClinicalAttachment> GetAttachmentAsync(Guid patientId, string fileName)
    {
        Logger.LogInformation("Getting attachment for patient with id {patientId} and file name {fileName}", patientId,
            fileName);

        var patient = await GetPatientAsync(patientId);
        return await Repository.GetClinicalAttachmentAsync(patient, fileName);
    }

    public async Task DeleteAttachmentAsync(Guid patientId, string fileName)
    {
        Logger.LogInformation("Deleting attachment for patient with id {patientId} and file name {fileName}", patientId,
            fileName);

        var patient = await GetPatientAsync(patientId);
        await Repository.RemoveClinicalAttachmentAsync(patient, fileName);
    }
}