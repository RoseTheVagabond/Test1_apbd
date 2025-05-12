namespace DoctorAppointments.Models.DTOs;

public class NewAppointmentDTO
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string PWZ { get; set; }
    public List<AppointmentServiceDTO> AppointmentServices { get; set; }
}