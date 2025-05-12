using DoctorAppointments.Models.DTOs;
using DoctorAppointments.Services;
using Microsoft.AspNetCore.Mvc;

namespace DoctorAppointments.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentsRepository _appointmentsRepository;

    public AppointmentsController(IAppointmentsRepository appointmentsRepository)
    {
        this._appointmentsRepository = appointmentsRepository;
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetAppointment(int id)
    {
        if (!await _appointmentsRepository.DoesAppointmentExist(id))
            return NotFound($"Appointment with given ID - {id} doesn't exist");

        var appointment = await _appointmentsRepository.GetAppointment(id);
            
        return Ok(appointment);
    }

    [HttpPost]
    public async Task<IActionResult> PostAppointment(NewAppointmentDTO newAppointment)
    {
        if (await _appointmentsRepository.DoesAppointmentExist(newAppointment.AppointmentId))
            return Conflict($"Appointment with given ID - {newAppointment.AppointmentId} already exists");
        
        if (!await _appointmentsRepository.DoesPatientExist(newAppointment.PatientId))
            return NotFound($"Patient with given ID - {newAppointment.PatientId} doesn't exist");
        
        if (!await _appointmentsRepository.DoesDoctorExist(newAppointment.PWZ))
            return NotFound($"Doctor with given PWZ - {newAppointment.PWZ} doesn't exist");

        foreach (var appointmentService in newAppointment.AppointmentServices)
        {
            if (!await _appointmentsRepository.DoesServiceExist(appointmentService.Name))
                return NotFound($"Service with the given name - {appointmentService.Name} doesn't exist");
        }

        await _appointmentsRepository.AddNewAppointment(newAppointment);

        return Created(Request.Path.Value ?? "api/animals", newAppointment);
    }
}