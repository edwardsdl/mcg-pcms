namespace Mcg.Pcms.Core;

public class PatientNotFoundException(Guid patientId) : Exception($"Patient with id '{patientId}' was not found.");

public class ClinicalAttachmentNotFoundException(string filename)
    : Exception($"Clinical attachment with filename '{filename}' was not found.");