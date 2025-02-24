namespace Mcg.Pcms.Core;

public interface IPatientRepository
{
    Task<Patient> AddPatientAsync(Patient patient);
    Task RemovePatientAsync(Guid patientId);
    Task<IEnumerable<Patient>> FindPatientsAsync(string? name);
    Task<Patient> GetPatientAsync(Guid patientId);
    Task UpdatePatientAsync(Patient patient);
    
    Task AddClinicalAttachmentAsync(Patient patient, string fileName, string contentType, byte[] fileContents);
    Task<ClinicalAttachment> GetClinicalAttachmentAsync(Patient patient, string fileName);
    Task RemoveClinicalAttachmentAsync(Patient patient, string fileName);
}