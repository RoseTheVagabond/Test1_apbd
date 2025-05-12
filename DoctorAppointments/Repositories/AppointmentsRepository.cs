using DoctorAppointments.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace DoctorAppointments.Services;

public class AppointmentsRepository : IAppointmentsRepository
{
    private readonly string _connectionString;

    public AppointmentsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default");
    }


    public async Task<bool> DoesAppointmentExist(int id)
    {
        var query = "SELECT 1 FROM Appointment WHERE appointment_id = @ID";

        await using SqlConnection connection = new SqlConnection(_connectionString);
        await using SqlCommand command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = query;
        command.Parameters.AddWithValue("@ID", id);

        await connection.OpenAsync();

        var res = await command.ExecuteScalarAsync();

        return res is not null;
    }

    public async Task<bool> DoesPatientExist(int id)
    {
	    var query = "SELECT 1 FROM Patient WHERE patient_id = @ID";

	    await using SqlConnection connection = new SqlConnection(_connectionString);
	    await using SqlCommand command = new SqlCommand();

	    command.Connection = connection;
	    command.CommandText = query;
	    command.Parameters.AddWithValue("@ID", id);

	    await connection.OpenAsync();

	    var res = await command.ExecuteScalarAsync();

	    return res is not null;
    }

    public async Task<bool> DoesDoctorExist(string pwz)
    {
	    var query = "SELECT 1 FROM Doctor WHERE pwz = @PWZ";

	    await using SqlConnection connection = new SqlConnection(_connectionString);
	    await using SqlCommand command = new SqlCommand();

	    command.Connection = connection;
	    command.CommandText = query;
	    command.Parameters.AddWithValue("@PWZ", pwz);

	    await connection.OpenAsync();

	    var res = await command.ExecuteScalarAsync();

	    return res is not null;
    }

    public async Task<bool> DoesServiceExist(string name)
    {
	    var query = "SELECT 1 FROM Service WHERE Name = @Name";

	    await using SqlConnection connection = new SqlConnection(_connectionString);
	    await using SqlCommand command = new SqlCommand();

	    command.Connection = connection;
	    command.CommandText = query;
	    command.Parameters.AddWithValue("@Name", name);

	    await connection.OpenAsync();

	    var res = await command.ExecuteScalarAsync();

	    return res is not null;
    }

    public async Task<AppointmentDTO> GetAppointment(int id)
    {
        var query = @"SELECT 
							Appointment.Date AS AppointmentDate,
							Patient.First_Name AS FirstName,
							Patient.Last_Name AS LastName,
							Patient.Date_of_Birth AS DateOfBirth,
							Doctor.doctor_id AS DoctorId,
							Doctor.PWZ AS PWZ,
							Service.Name AS Name,
							Appointment_Service.Service_fee AS ServiceFee
						FROM Patient JOIN Appointment ON Patient.Patient_id = Appointment.Patient_id
						    JOIN Doctor ON Appointment.Doctor_id = Doctor.Doctor_id
						    JOIN Appointment_Service ON Appointment.Appointment_id = Appointment_Service.Appointment_id
						    JOIN Service ON Appointment_Service.Service_id = Service.Service_id
						WHERE Appointment.Appointment_ID = @ID";
	    
	    await using SqlConnection connection = new SqlConnection(_connectionString);
	    await using SqlCommand command = new SqlCommand();

	    command.Connection = connection;
	    command.CommandText = query;
	    command.Parameters.AddWithValue("@ID", id);
	    
	    await connection.OpenAsync();

	    var reader = await command.ExecuteReaderAsync();
	    
	    AppointmentDTO appointmentDto = null;

	    while (await reader.ReadAsync())
	    {
		    if (appointmentDto is not null)
		    {
			    appointmentDto.AppointmentServices.Add(new AppointmentServiceDTO()
			    {
				    Name = reader.GetString(6),
				    ServiceFee = reader.GetDecimal(7)
			    });
		    }
		    else
		    {
			    appointmentDto = new AppointmentDTO()
			    {
				    Date = reader.GetDateTime(0),
				    Patient = new PatientDTO()
				    {
					    FirstName = reader.GetString(1),
					    LastName = reader.GetString(2),
					    DateOfBirth = reader.GetDateTime(3)
				    },
				    Doctor = new DoctorDTO()
				    {
					    DoctorId = reader.GetInt32(4),
					    pwz = reader.GetString(5)
				    },
				    AppointmentServices = new List<AppointmentServiceDTO>()
				    {
					    new AppointmentServiceDTO()
					    {
						    Name = reader.GetString(6),
						    ServiceFee = reader.GetDecimal(7)
					    }
				    }
			    };
		    }
	    }

	    if (appointmentDto is null) throw new Exception();
        
        return appointmentDto;
    }

    public async Task AddNewAppointment(NewAppointmentDTO newAppointment)
    {
	    await using SqlConnection connection = new SqlConnection(_connectionString);
	    await using SqlCommand command = new SqlCommand();
	    
	    command.Connection = connection;
	    command.CommandText = "SELECT doctor_id FROM Doctor WHERE PWZ = @PWZ";
	    command.Parameters.AddWithValue("@PWZ", newAppointment.PWZ);
	    int doctorId = (int) await command.ExecuteScalarAsync();
	    command.Parameters.Clear();
	    
	    var insert = @"INSERT INTO Appointment VALUES(@AppointmentId, @PatientId, @DoctorId, CURRENT_DATE);";
	    command.CommandText = insert;
	    
	    command.Parameters.AddWithValue("@AppointmentId", newAppointment.AppointmentId);
	    command.Parameters.AddWithValue("@PatientId", newAppointment.PatientId);
	    command.Parameters.AddWithValue("@DoctorId", doctorId);
	    
	    await connection.OpenAsync();

	    var transaction = await connection.BeginTransactionAsync();
	    command.Transaction = transaction as SqlTransaction;
	    
	    try
	    {
		    var id = await command.ExecuteScalarAsync();
    
		    foreach (var appointmentService in newAppointment.AppointmentServices)
		    {
			    command.Parameters.Clear();
			    command.CommandText = "INSERT INTO Appointment_Service VALUES(@AppointmentId, @Name, @ServiceFee)";
			    command.Parameters.AddWithValue("@AppointmentId", newAppointment.AppointmentId);
			    command.Parameters.AddWithValue("@Name", appointmentService.Name);
			    command.Parameters.AddWithValue("@ServiceFee", appointmentService.ServiceFee);

			    await command.ExecuteNonQueryAsync();
		    }

		    await transaction.CommitAsync();
	    }
	    catch (Exception)
	    {
		    await transaction.RollbackAsync();
		    throw;
	    }
    }
}