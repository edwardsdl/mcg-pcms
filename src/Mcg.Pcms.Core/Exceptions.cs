namespace Mcg.Pcms.Core;

public class PatientNotFoundException(Guid patientId) : Exception($"Patient with id '{patientId}' was not found.");

public class ClinicalAttachmentNotFoundException(string fileName)
    : Exception($"Clinical attachment with file name '{fileName}' was not found.");