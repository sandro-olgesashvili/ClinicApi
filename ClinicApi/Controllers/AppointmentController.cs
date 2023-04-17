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

    }
}

