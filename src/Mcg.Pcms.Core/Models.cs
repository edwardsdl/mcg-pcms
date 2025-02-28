using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mcg.Pcms.Core;

public record PatientDto(
    string Name,
    int Age,
    string PhoneNumber,
    string EmailAddress,
    string Address,
    string MedicalHistory
);

public class Patient
{
    [Key] public Guid Id { get; init; }

    [Required] public required string Address { get; set; }

    [Range(0, 150)] public int Age { get; set; }

    [NotMapped] public IEnumerable<string> ClinicalAttachments { get; set; } = [];

    [Required] [EmailAddress] public required string EmailAddress { get; set; }

    public string MedicalHistory { get; set; } = "";

    [Required] public required string Name { get; set; }

    [Required] [Phone] public required string PhoneNumber { get; set; }
}

public record ClinicalAttachment(string FileName, string ContentType, byte[] FileContents);