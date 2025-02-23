namespace Mcg.Pcms.Core;

public interface IPatientRepository
{
    Task<Patient> AddPatientAsync(Patient patient);
    Task RemovePatientAsync(Guid patientId);
    Task<IEnumerable<Patient>> FindPatientsAsync(string? name);
    Task<Patient> GetPatientAsync(Guid patientId);
    Task UpdatePatientAsync(Patient patient);
    
    Task AddClinicalAttachmentAsync(Patient patient, string filename, byte[] bytes);
    Task<IEnumerable<ClinicalAttachment>> GetClinicalAttachmentsAsync(Patient patient);
    Task<ClinicalAttachment> GetClinicalAttachmentAsync(Patient patient, string filename);
    Task RemoveClinicalAttachmentAsync(Patient patient, string filename);
}