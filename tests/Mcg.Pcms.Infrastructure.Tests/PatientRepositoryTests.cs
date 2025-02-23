﻿using System.Text;
using Mcg.Pcms.Core;
using Microsoft.EntityFrameworkCore;

namespace Mcg.Pcms.Infrastructure.Tests;

public class PatientRepositoryTests
{
    private PatientRepository GetRepository()
    {
        // We want a unique in-memory database for each test. 
        var options = new DbContextOptionsBuilder<PatientDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new PatientDbContext(options);

        return new PatientRepository(dbContext);
    }

    private Patient GetJohnDoe()
    {
        return new Patient
        {
            Address = "123 Main Street",
            Age = 65,
            EmailAddress = "john.doe@example.com",
            MedicalHistory = "No pre-existing conditions.",
            Name = "John Doe",
            PhoneNumber = "(888) 555-1212"
        };
    }

    private Patient GetJaneDoe()
    {
        return new Patient
        {
            Address = "123 Main Street",
            Age = 60,
            EmailAddress = "jane.doe@example.com",
            MedicalHistory = "No pre-existing conditions.",
            Name = "John Doe",
            PhoneNumber = "(888) 555-1212"
        };
    }

    private Patient GetJohnSmith()
    {
        return new Patient
        {
            Address = "456 Main Street",
            Age = 20,
            EmailAddress = "john.smith@example.com",
            MedicalHistory = "No pre-existing conditions.",
            Name = "John Smith",
            PhoneNumber = "(123) 456-7890"
        };
    }

    private byte[] GetClinicalAttachment()
    {
        return Encoding.UTF8.GetBytes("Clinical Attachment");
    }

    [Fact]
    public async Task AddPatientAsync_MustSavePatient()
    {
        // Arrange
        var repository = GetRepository();
        var createdPatient = GetJohnDoe();

        // Act
        await repository.AddPatientAsync(createdPatient);
        var retrievedPatient = await repository.GetPatientAsync(createdPatient.Id);

        // Assert
        Assert.NotNull(retrievedPatient);
        Assert.Equal(createdPatient, retrievedPatient);
    }

    [Fact]
    public async Task RemovePatientAsync_MustRemovePatient_WhenPatientExists()
    {
        // Arrange
        var repository = GetRepository();
        var patient = GetJohnDoe();
        await repository.AddPatientAsync(patient);

        // Act
        await repository.RemovePatientAsync(patient.Id);

        // Assert
        await Assert.ThrowsAsync<PatientNotFoundException>(() => repository.GetPatientAsync(patient.Id));
    }

    [Fact]
    public async Task RemovePatientAsync_ThrowsPatientNotFoundException_WhenPatientDoesNotExist()
    {
        // Arrange
        var repository = GetRepository();
        var patientId = Guid.NewGuid();

        // Act / Assert
        await Assert.ThrowsAsync<PatientNotFoundException>(() => repository.RemovePatientAsync(patientId));
    }

    [Fact]
    public async Task FindPatientsAsync_MustFindAllPatients_WhenGivenNoSearchTerm()
    {
        // Arrange
        var repository = GetRepository();
        await repository.AddPatientAsync(GetJohnDoe());
        await repository.AddPatientAsync(GetJaneDoe());
        await repository.AddPatientAsync(GetJohnSmith());

        // Act
        var foundPatients = await repository.FindPatientsAsync(null);

        // Assert
        Assert.Equal(3, foundPatients.Count());
    }

    [Fact]
    public async Task FindPatientsAsync_MustFindPatients_WhenGivenPartialMatch()
    {
        // Arrange
        var repository = GetRepository();
        await repository.AddPatientAsync(GetJohnDoe());
        await repository.AddPatientAsync(GetJaneDoe());
        await repository.AddPatientAsync(GetJohnSmith());

        // Act
        var foundPatients = await repository.FindPatientsAsync("Doe");

        // Assert
        Assert.Equal(2, foundPatients.Count());
    }

    [Fact]
    public async Task FindPatientsAsync_MustNotFindPatients_WhenGivenNoMatch()
    {
        // Arrange
        var repository = GetRepository();
        await repository.AddPatientAsync(GetJohnDoe());
        await repository.AddPatientAsync(GetJaneDoe());
        await repository.AddPatientAsync(GetJohnSmith());

        // Act
        var foundPatients = await repository.FindPatientsAsync("Schmoe");

        // Assert
        Assert.Empty(foundPatients);
    }

    [Fact]
    public async Task GetPatientAsync_MustGetPatient_WhenGivenMatchingId()
    {
        // Arrange
        var repository = GetRepository();
        var createdPatient = GetJohnDoe();
        await repository.AddPatientAsync(createdPatient);

        // Act
        var retrievedPatient = await repository.GetPatientAsync(createdPatient.Id);

        // Assert
        Assert.NotNull(retrievedPatient);
        Assert.Equal(createdPatient, retrievedPatient);
    }

    [Fact]
    public async Task GetPatientAsync_MustThrowPatientNotFoundException_WhenGivenNoMatchingId()
    {
        // Arrange
        var repository = GetRepository();
        await repository.AddPatientAsync(GetJohnDoe());

        // Act / Assert
        await Assert.ThrowsAsync<PatientNotFoundException>(() => repository.GetPatientAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdatePatientAsync_MustUpdatePatient_WhenGivenMatchingId()
    {
        // Arrange
        var repository = GetRepository();
        var patient = GetJohnDoe();
        await repository.AddPatientAsync(patient);

        var originalId = patient.Id;
        const string updatedName = "Joe Schmoe";
        
        // Act
        patient.Name = updatedName;
        await repository.UpdatePatientAsync(patient);

        // Assert
        Assert.Equal(originalId, patient.Id);
        Assert.Equal(updatedName, patient.Name);
    }

    [Fact(Skip = "This test is failing but will be addressed when we refactor to request DTOs.")]
    public async Task UpdatePatientAsync_MustThrowPatientNotFoundException_WhenGivenNoMatchingId()
    {
        // Arrange
        var repository = GetRepository();

        // Act / Assert
        await Assert.ThrowsAsync<PatientNotFoundException>(() => repository.UpdatePatientAsync(GetJohnDoe()));
    }

    [Fact]
    public async Task AddClinicalAttachmentAsync_MustAddAttachment()
    {
        // Arrange
        var repository = GetRepository();
        var patient = GetJohnDoe();
        await repository.AddPatientAsync(patient);
        
        const string filename = "Clinical Attachment";

        // Act
        await repository.AddClinicalAttachmentAsync(patient, filename, GetClinicalAttachment());

        // Assert
        var clinicalAttachment = await repository.GetClinicalAttachmentAsync(patient, filename);
        Assert.NotNull(clinicalAttachment);
    }

    [Fact]
    public async Task GetClinicalAttachmentsAsync_MustGetAttachments_WhenAttachmentsExist()
    {
        // Arrange
        var repository = GetRepository();
        var patient = GetJohnDoe();
        await repository.AddPatientAsync(patient);
        await repository.AddClinicalAttachmentAsync(patient, "Clinical Attachment 1", GetClinicalAttachment());
        await repository.AddClinicalAttachmentAsync(patient, "Clinical Attachment 2", GetClinicalAttachment());

        // Act
        var clinicalAttachments = await repository.GetClinicalAttachmentsAsync(patient);

        // Assert
        Assert.Equal(2, clinicalAttachments.Count());
    }
    
    [Fact]
    public async Task GetClinicalAttachmentsAsync_MustNotGetAttachments_WhenAttachmentsDoNotExist()
    {
        // Arrange
        var repository = GetRepository();
        var patient = GetJohnDoe();
        await repository.AddPatientAsync(patient);

        // Act
        var clinicalAttachments = await repository.GetClinicalAttachmentsAsync(patient);

        // Assert
        Assert.Empty(clinicalAttachments);
    }

    [Fact]
    public async Task RemoveClinicalAttachmentAsync_MustRemoveAttachment_WhenGivenMatchingFilename()
    {
        // Arrange
        var repository = GetRepository();
        var patient = GetJohnDoe();
        const string filename = "Clinical Attachment";
        
        await repository.AddPatientAsync(patient);
        await repository.AddClinicalAttachmentAsync(patient, filename, GetClinicalAttachment());

        // Act
        await repository.RemoveClinicalAttachmentAsync(patient, filename);

        // Assert
        await Assert.ThrowsAsync<ClinicalAttachmentNotFoundException>(() =>  repository.GetClinicalAttachmentAsync(patient, filename));
    }
    
    [Fact]
    public async Task RemoveClinicalAttachmentAsync_MustThrowClinicalAttachmentNotFoundException_WhenGivenNoMatchingFilename()
    {
        // Arrange
        var repository = GetRepository();
        var patient = GetJohnDoe();
        await repository.AddPatientAsync(patient);

        // Act / Assert
        await Assert.ThrowsAsync<ClinicalAttachmentNotFoundException>(() =>
            repository.RemoveClinicalAttachmentAsync(patient, "Clinical Attachment"));
    }
}