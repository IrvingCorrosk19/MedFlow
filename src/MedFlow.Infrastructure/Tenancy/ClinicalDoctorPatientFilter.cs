using MedFlow.Application.Interfaces;
using MedFlow.Domain.Entities;

namespace MedFlow.Infrastructure.Tenancy;

/// <summary>
/// Pacientes "del médico": al menos una cita o una historia clínica con ese <see cref="Doctor"/>.
/// </summary>
public static class ClinicalDoctorPatientFilter
{
    public static IQueryable<Patient> Apply(IApplicationDbContext db, Guid doctorId, IQueryable<Patient> patients) =>
        patients.Where(p =>
            db.Appointments.Any(a => !a.IsDeleted && a.DoctorId == doctorId && a.PatientId == p.Id) ||
            db.MedicalRecords.Any(m => !m.IsDeleted && m.DoctorId == doctorId && m.PatientId == p.Id));
}
