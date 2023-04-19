using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClinicApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicApi.Interfaces;
using ClinicApi.Dto;
using ClinicApi.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ClinicApi.Controllers
{
    [Route("api/[controller]")]
    public class AppointmentController : Controller
    {
        private readonly DataContext _dbContext;

        private readonly IUserService _userService;
        public AppointmentController(DataContext dbContext, IUserService userService)
        {
            _dbContext = dbContext;

            _userService = userService;
        }


        [HttpGet, Authorize(Roles = "doctor")]
        public async Task<IActionResult> Get()
        {
            var username = _userService.GetMyName();

            var user = _dbContext.Users.Where(x => x.Email == username).FirstOrDefault();

            if (user == null) return Ok(false);

            var appointment = await _dbContext.Appointments.Where(x => x.UserId == user.Id)
                .Select(x => new { x.StartTime, x.EndTime, x.PatientId, x.Id, x.UserId }).ToListAsync();

            return Ok(appointment);
        }


        [HttpPost("addAppointment"), Authorize(Roles = "doctor")]
        public async Task<IActionResult> addAppointment([FromBody] AppointmentDto req)
        {
            var username = _userService.GetMyName();

            var doctor = await _dbContext.Users.Where(x => x.Email == username).FirstOrDefaultAsync();

            if (doctor == null) return Ok(false);

            var appoitment = new Appointment
            {
                StartTime = req.StartTime,
                EndTime = req.EndTime,
                UserId = doctor.Id,
                PatientId = null
            };

            _dbContext.Appointments.Add(appoitment);

            await _dbContext.SaveChangesAsync();



            var sendData = await _dbContext.Appointments.Where(x => x.UserId == doctor.Id)
                .Select(x => new {x.StartTime, x.EndTime, x.PatientId, x.Id, x.UserId }).ToListAsync();

            return Ok(sendData);
        }


        [HttpDelete("deleteAppointment"), Authorize(Roles = "doctor")]
        public async Task<IActionResult> deleteAppointment([FromQuery] GetDoctorProfileDto req)
        {
            var username = _userService.GetMyName();

            var user = _dbContext.Users.Where(x => x.Email == username).FirstOrDefault();

            if (user == null) return Ok(false);

            var appointment = _dbContext.Appointments.Where(x => x.Id == req.Id && x.UserId == user.Id).FirstOrDefault();

            if (appointment == null) return Ok(false);

            _dbContext.Appointments.Remove(appointment);

            await _dbContext.SaveChangesAsync();
            
            return Ok(true);
        }


        [HttpGet("getAppointment")]
        public async Task<IActionResult> getAppointment(GetDoctorProfileDto req)
        {
            var user = _dbContext.Users.Where(x => x.Id == req.Id).FirstOrDefault();

            if (user == null) return Ok(false);

            var appointments = await _dbContext.Appointments.Where(x => x.UserId == user.Id).ToListAsync();

            if (appointments == null) return Ok(false);

            return Ok(appointments);
        }


        [HttpGet("userAppointment"), Authorize]
        public async Task<IActionResult> userAppointment()
        {
            var username = _userService.GetMyName();

            var user = _dbContext.Users.Where(x => x.Email == username).FirstOrDefault();

            if (user == null) return Ok(false);

            var userAppointment = await _dbContext.Appointments.Where(x => x.PatientId == user.Id).ToListAsync();

            if (userAppointment == null) return Ok(false);

            return Ok(userAppointment);
        }

        [HttpPut("userAppointmentRemove"), Authorize]
        public async Task<IActionResult> userAppointmentRemove([FromBody] GetDoctorProfileDto req)
        {
            var username = _userService.GetMyName();

            var user = _dbContext.Users.Where(x => x.Email == username).FirstOrDefault();

            if (user == null) return Ok(false);

            var userAppointment = _dbContext.Appointments.Where(x => x.Id == req.Id && user.Id == x.PatientId).FirstOrDefault();

            if (userAppointment == null) return Ok(false);

            userAppointment.PatientId = null;

            await _dbContext.SaveChangesAsync();

            return Ok(true);
        }
    }
}

