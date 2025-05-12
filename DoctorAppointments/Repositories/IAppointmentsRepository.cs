using DoctorAppointments.Models.DTOs;

namespace DoctorAppointments.Services;

public interface IAppointmentsRepository
{
    public Task<bool> DoesAppointmentExist(int id);

    public Task<bool> DoesPatientExist(int id);
    public Task<bool> DoesDoctorExist(string pwz);
    
    public Task<bool> DoesServiceExist(string name);
    public Task<AppointmentDTO> GetAppointment(int id);
    public Task AddNewAppointment(NewAppointmentDTO newAppointment);
}