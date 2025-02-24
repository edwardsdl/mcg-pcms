namespace Mcg.Pcms.Core;

public class PatientService(IPatientRepository repository)
{
    private IPatientRepository Repository { get; } = repository;

    public async Task<Patient> CreatePatientAsync(CreatePatientRequest createPatientRequest)
    {
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
        return await Repository.FindPatientsAsync(query);
    }
    
    public async Task<Patient> GetPatientAsync(Guid id)
    {
        return await Repository.GetPatientAsync(id);
    }

    public async Task UpdatePatientAsync(Guid id, UpdatePatientRequest updatePatientRequest)
    {
        var patient = await Repository.GetPatientAsync(id);
        patient.Address = updatePatientRequest.Address;
        patient.Age = updatePatientRequest.Age;
        patient.EmailAddress = updatePatientRequest.EmailAddress;
        patient.MedicalHistory = updatePatientRequest.MedicalHistory;
        patient.Name = updatePatientRequest.Name;
        patient.PhoneNumber = updatePatientRequest.PhoneNumber;
        
        await Repository.UpdatePatientAsync(patient);
    }

    public async Task DeletePatientAsync(Guid id)
    {
        await Repository.RemovePatientAsync(id);
    }

    public async Task CreateAttachmentAsync(Guid patientId, string filename, byte[] bytes)
    {
        var patient = await GetPatientAsync(patientId);
        await Repository.AddClinicalAttachmentAsync(patient, filename, bytes);
    }
    
    public async Task<ClinicalAttachment> GetAttachmentAsync(Guid patientId, string filename)
    {
        var patient = await GetPatientAsync(patientId);
        return await Repository.GetClinicalAttachmentAsync(patient, filename);
    }

    public async Task DeleteAttachmentAsync(Guid patientId, string filename)
    {
        var patient = await GetPatientAsync(patientId);
        await Repository.RemoveClinicalAttachmentAsync(patient, filename);
    }
}