using System.Collections.Concurrent;
using Mcg.Pcms.Core;
using Microsoft.EntityFrameworkCore;

namespace Mcg.Pcms.Infrastructure;

public class PatientRepository(PatientDbContext dbContext) : IPatientRepository
{
    private static ConcurrentDictionary<Guid, List<ClinicalAttachment>> BlobStorage { get; } = new();
    private PatientDbContext DbContext { get; } = dbContext;

    public async Task<Patient> AddPatientAsync(Patient patient)
    {
        await DbContext.Patients.AddAsync(patient);
        await DbContext.SaveChangesAsync();

        return patient;
    }

    public async Task RemovePatientAsync(Guid patientId)
    {
        var patient = await DbContext.Patients.FindAsync(patientId);
        if (patient == null) throw new PatientNotFoundException(patientId);

        DbContext.Patients.Remove(patient);
        await DbContext.SaveChangesAsync();

        BlobStorage.TryRemove(patientId, out _);
    }

    public async Task<IEnumerable<Patient>> FindPatientsAsync(string? name)
    {
        var patients = string.IsNullOrWhiteSpace(name)
            ? await DbContext.Patients.ToListAsync()
            : await DbContext.Patients.Where(p => p.Name.Contains(name)).ToListAsync();

        PopulateClinicalAttachments(patients);

        return patients;
    }

    public async Task<Patient> GetPatientAsync(Guid patientId)
    {
        var patient = await DbContext.Patients.FindAsync(patientId);
        if (patient == null) throw new PatientNotFoundException(patientId);

        PopulateClinicalAttachments([patient]);

        return patient;
    }

    public async Task UpdatePatientAsync(Patient patient)
    {
        DbContext.Patients.Update(patient);
        await DbContext.SaveChangesAsync();
    }

    public async Task AddClinicalAttachmentAsync(Patient patient, string fileName, string contentType, byte[] fileContents)
    {
        var clinicalAttachments = BlobStorage.GetOrAdd(patient.Id, []);
        clinicalAttachments.Add(new ClinicalAttachment(fileName, contentType, fileContents));

        PopulateClinicalAttachments([patient]);

        // Any implementation of this method written against real blob storage will be handled asynchronously. If we
        // fake it now, we can avoid changing the method signature and dealing with the ripple effects it would cause.
        await Task.CompletedTask;
    }

    public async Task<ClinicalAttachment> GetClinicalAttachmentAsync(Patient patient, string fileName)
    {
        var clinicalAttachments = BlobStorage.GetOrAdd(patient.Id, []);
        var clinicalAttachment = clinicalAttachments.FirstOrDefault(ca => ca.FileName == fileName);
        if (clinicalAttachment == null) throw new ClinicalAttachmentNotFoundException(fileName);

        // Any implementation of this method written against real blob storage will be handled asynchronously. If we
        // fake it now, we can avoid changing the method signature and dealing with the ripple effects it would cause.
        return await Task.FromResult(clinicalAttachment);
    }

    public async Task RemoveClinicalAttachmentAsync(Patient patient, string fileName)
    {
        var clinicalAttachments = BlobStorage.GetOrAdd(patient.Id, []);
        var numRemovedClinicalAttachments = clinicalAttachments.RemoveAll(ca => ca.FileName == fileName);
        if (numRemovedClinicalAttachments == 0) throw new ClinicalAttachmentNotFoundException(fileName);

        // Any implementation of this method written against real blob storage will be handled asynchronously. If we
        // fake it now, we can avoid changing the method signature and dealing with the ripple effects it would cause.
        await Task.CompletedTask;
    }

    private void PopulateClinicalAttachments(IEnumerable<Patient> patients)
    {
        foreach (var patient in patients)
        {
            if (BlobStorage.TryGetValue(patient.Id, out var clinicalAttachments))
            {
                patient.ClinicalAttachments = clinicalAttachments.Select(ca => ca.FileName);
            }
        }
    }
}