using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ClinicApi.Data;
using ClinicApi.Dto;
using ClinicApi.Interfaces;
using ClinicApi.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using MimeKit;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ClinicApi.Controllers
{
    [Route("api/[controller]")]
    public class AdminController : Controller
    {
        private readonly DataContext _dbContext;

        private readonly IUserService _userService;

        private readonly IWebHostEnvironment _env;


        public AdminController(DataContext dbContext, IUserService userService, IWebHostEnvironment env)
        {
            _dbContext = dbContext;

            _userService = userService;


            _env = env;

        }


        [HttpGet("admin"), Authorize(Roles = "admin")]
        public async Task<IActionResult> GetSingle([FromQuery] int req)
        {
            var user = _dbContext.Users.Where(x => x.Id == req).FirstOrDefault();

            if (user == null) return Ok(false);

            return Ok(user);
        }


        [HttpPost("addUser"), Authorize(Roles = "admin")]
        public async Task<IActionResult> PostUser([FromForm] RegisterDto req)
        {
            var userCheck = _dbContext.Users.Where(x => x.Email == req.Email).FirstOrDefault();

            var category = _dbContext.Categories.Where(x => x.CategoryName == req.Category).FirstOrDefault();

            if (userCheck != null) return Ok(false);

            if (req.ImageFile != null)
            {
                req.ImageName = await SaveImage(req.ImageFile);
            }

            if (req.PdfFile != null)
            {
                req.PdfName = await SavePdf(req.PdfFile);
            }

            var user = new User
            {
                Name = req.Name,
                Surname = req.Surname,
                Email = req.Email,
                IdNumber = req.IdNumber,
                Password = req.Password,
                Role = req.Role,
                IsConfirmed = true,
                CategoryId = category?.Id,
                ConfirmationToken = null,
                EmailConfirmationTokenExpiration = DateTime.Now,
                ImageName = req.ImageName,
                PdfName = req.PdfName,
                Description = req.Description,
                Views = req.Role == "doctor" ? 0 : null
               
            };

            _dbContext.Users.Add(user);

            await _dbContext.SaveChangesAsync();

            var createdUser = _dbContext.Users.Where(x => x.Email == req.Email).FirstOrDefault();

            return Ok(createdUser);
        }


        [HttpPut("updateUser"), Authorize(Roles = "admin")]
        public async Task<IActionResult> updateDoctorPassword ([FromQuery] UpdatePasswordDoctorDto req)
        {
            var doctor = _dbContext.Users.Where(x => x.Id == req.Id).FirstOrDefault();

            if (doctor == null) return Ok(false);

            doctor.Password = req.Password;

            await _dbContext.SaveChangesAsync();

            return Ok(true);
        }


        [HttpPut("updateDoctorEmailSend"), Authorize(Roles = "admin")]
        public async Task<IActionResult> updateDoctorEmailSend([FromBody] UpdateDoctorEmailDto req)
        {
            var check = _dbContext.Users.Where(x => x.Id == req.Id).FirstOrDefault();

            var checkEmail = _dbContext.Users.Where(x => x.Email == req.Email).FirstOrDefault();

            if (checkEmail != null) return Ok(false);

            if (check == null) return Ok(false);

            check.ConfirmationToken = GenerateConfirmationToken();

            check.ConfirmationTokenEmail = GenerateConfirmationToken();

            await _dbContext.SaveChangesAsync();

            var confirm = _dbContext.Users.Where(x => x.Id == req.Id).FirstOrDefault().ConfirmationToken;

            var confirmNew = _dbContext.Users.Where(x => x.Id == req.Id).FirstOrDefault().ConfirmationTokenEmail;

            UpdateDoctorEmailDto firstEmail = new UpdateDoctorEmailDto {Id =check.Id , Email = check.Email, Name = check.Name };

            UpdateDoctorEmailDto secondEmail = new UpdateDoctorEmailDto { Id = check.Id, Email = req.Email, Name = check.Name };

            await SendConfirmationEmail(firstEmail, check.ConfirmationToken);

            await SendConfirmationEmail(secondEmail, check.ConfirmationTokenEmail);

            return Ok(true);
        }


        [HttpPut("updateDoctorEmailNew"), Authorize(Roles = "admin")]
        public async Task<IActionResult> updateDoctorEmailNew([FromBody] UpdateDoctorEmailConfirmDto req)
        {
            var user = _dbContext.Users.Where(x => x.Id == req.Id).FirstOrDefault();

            var check = _dbContext.Users.Where(x => x.Email == req.Email).FirstOrDefault();

            if (check != null) return Ok(false);

            if (user == null) return Ok(false);

            if (user.ConfirmationToken != req.ConfirmationToken || user.ConfirmationTokenEmail != req.ConfirmationTokenEmail)
                return Ok(false);

            user.ConfirmationToken = null;
            user.ConfirmationTokenEmail = null;

            user.Email = req.Email;

            await _dbContext.SaveChangesAsync();

            return Ok(true);
        }


        [HttpGet("getCategorys"), Authorize(Roles = "admin")]
        public async Task<IActionResult> getCategorys()
        {
            var categorys = await _dbContext.Categories.ToListAsync();

            return Ok(categorys);
        }


        [HttpPost("createCategory"), Authorize(Roles = "admin")]
        public async Task<IActionResult> createCategory([FromBody] CategoryDto req)
        {
            var categoryCheck = _dbContext.Categories.Where(x => x.CategoryName == req.CategoryName).FirstOrDefault();

            if (categoryCheck != null) return Ok(false);

            var category = new Category { CategoryName = req.CategoryName };

            _dbContext.Categories.Add(category);

            await _dbContext.SaveChangesAsync();

            var resCategory = _dbContext.Categories.Where(x => x.CategoryName == req.CategoryName).FirstOrDefault(); 

            return Ok(resCategory);
        }


        [HttpDelete("removeCategory"), Authorize(Roles = "admin")]
        public async Task<IActionResult> removeCategory([FromQuery] CategoryDto req)
        {
            var category = _dbContext.Categories.Where(x => x.CategoryName == req.CategoryName).FirstOrDefault();

            if (category == null) return Ok(false);

            var user = _dbContext.Users.Where(x => x.CategoryId == category.Id).FirstOrDefault();

            if (user != null) return Ok(false);

            _dbContext.Categories.Remove(category);

            await _dbContext.SaveChangesAsync();

            return Ok(true);
        }


        [HttpPut("updateCategory"), Authorize(Roles = "admin")]
        public async Task<IActionResult> updateCategory([FromBody] Category req)
        {
            var category = _dbContext.Categories.Where(x => x.Id == req.Id).FirstOrDefault();

            var category2 = _dbContext.Categories.Where(x => x.CategoryName == req.CategoryName).FirstOrDefault();

            if (category == null) return Ok(false);
            if (category2 != null) return Ok(false);

            category.CategoryName = req.CategoryName;

            await _dbContext.SaveChangesAsync();

            return Ok(category);
        }



        [HttpGet("GetAllUsers"), Authorize(Roles ="admin")]
        public async Task<IActionResult> getAllUsers()
        {
            var username = _userService.GetMyName();

            var getAllUsers = await _dbContext.Users.Where(x => x.Email != username).GroupJoin(
                    _dbContext.Categories,
                    u => u.Id,
                    c => c.Id,
                    (u, c) => new { User = u, Category = c }
                    ).SelectMany(
                    x => x.Category.DefaultIfEmpty(),
                    (x, c) => new DoctorDto
                    {
                        Id = x.User.Id,
                        Name = x.User.Name,
                        Surname = x.User.Surname,
                        Email = x.User.Email,
                        IdNumber = x.User.IdNumber,
                        CategoryName = x.User.Category.CategoryName,
                        Role = x.User.Role,
                        IsPinned = x.User.IsPinned,
                        ImageSrc = String.Format("{0}://{1}{2}/Images/{3}",
                        Request.Scheme,
                        Request.Host,
                        Request.PathBase,
                        x.User.ImageName)

                    }
                 ).OrderByDescending(x => x.IsPinned).ToListAsync();

            return Ok(getAllUsers);
        }

        [HttpDelete("deleteUser"), Authorize(Roles = "admin")]
        public async Task<IActionResult> deleteUser([FromQuery] GetDoctorProfileDto req)
        {
            var user = _dbContext.Users.Where(x => x.Id == req.Id).FirstOrDefault();

            if (user.ImageName != null) {
                DeleteImage(user.ImageName);
            }

            if (user.PdfName != null) {
                DeletePdf(user.PdfName);
            }

            var delApp = _dbContext.Appointments.Where(x => x.PatientId == req.Id).ToList();

            if (delApp != null) {
                foreach (var item in delApp)
                {
                    item.PatientId = null;
                }

            }

            if (user == null) return Ok(false);

            _dbContext.Remove(user);

            await _dbContext.SaveChangesAsync();

            return Ok(true);
        }


        [HttpPut("changePin"), Authorize(Roles = "admin")]
        public async Task<IActionResult> changePinned([FromBody] UpdatePinDto req)
        {
            var user = _dbContext.Users.Where(x => x.Id == req.Id).FirstOrDefault();

            if (user == null) return Ok(false);

            user.IsPinned = !user.IsPinned;

            await _dbContext.SaveChangesAsync();

            var data = new { Id = user.Id, isPinned = user.IsPinned };

            return Ok(data);
        }


        [HttpGet("doctorAppointments"), Authorize(Roles = "admin")]
        public async Task<IActionResult> doctorAppointments([FromQuery] GetDoctorProfileDto req)
        {
            var user = _dbContext.Users.Where(x => x.Id == req.Id).FirstOrDefault();

            if (user == null) return Ok(false);

            var appointments = await _dbContext.Appointments.Where(x => x.UserId == user.Id).ToListAsync();

            if (appointments == null) return Ok(false);

            return Ok(appointments);
        }

        [HttpPost("doctorAppointments"), Authorize(Roles = "admin")]
        public async Task<IActionResult> doctorAppointmentsPost([FromBody] AppointmentAdminDto req)
        {
            var user = _dbContext.Users.Where(x => x.Id == req.Id).FirstOrDefault();

            if (user == null) return Ok(false);

            var appointment = new Appointment {
                StartTime = req.StartTime,
                EndTime = req.EndTime,
                UserId = req.Id,
                PatientId = null
            };

            _dbContext.Appointments.Add(appointment);

            await _dbContext.SaveChangesAsync();

            var sendData = await _dbContext.Appointments.Where(x => x.UserId == req.Id).
                Select(x => new { x.StartTime, x.EndTime, x.PatientId, x.Id, x.UserId }).ToListAsync();

            return Ok(sendData);
        }

        [HttpDelete("doctorAppointmentDelete"), Authorize(Roles = "admin")]
        public async Task<IActionResult> doctorAppointmentDelete([FromQuery] AppointmentDelAdminDto req)
        {
            var app = _dbContext.Appointments.Where(x => x.Id == req.AppointmentId).FirstOrDefault();

            if (app == null) return Ok(false);

            _dbContext.Appointments.Remove(app);

            await _dbContext.SaveChangesAsync();

            return Ok(true);
        }


        [HttpGet("userAppointment"), Authorize(Roles = "admin")]
        public async Task<IActionResult> userAppointment([FromQuery] GetDoctorProfileDto req)
        {
            var user = _dbContext.Users.Where(x => x.Id == req.Id).FirstOrDefault();

            if (user == null) return Ok(false);

            var appointment = await _dbContext.Appointments.Where(x => x.PatientId == user.Id).ToListAsync();

            return Ok(appointment);
        }


        [HttpDelete("userAppointmentDel"), Authorize(Roles = "admin")]
        public async Task<IActionResult> userAppointmentDel([FromQuery] GetDoctorProfileDto req)
        {
            var appointment = _dbContext.Appointments.Where(x => x.Id == req.Id).FirstOrDefault();

            if (appointment == null) return Ok(false);

            appointment.PatientId = null;

            await _dbContext.SaveChangesAsync();

            return Ok(true);
        }




        private string GenerateConfirmationToken()
        {
            const int tokenLength = 8;
            var tokenBytes = new byte[tokenLength];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(tokenBytes);
            }
            return Convert.ToBase64String(tokenBytes);
        }



        [NonAction]
        public void DeleteImage(string imageName)
        {
            var imagePath = Path.Combine(_env.ContentRootPath, "Images", imageName);
            if (System.IO.File.Exists(imagePath))
                System.IO.File.Delete(imagePath);
        }

        [NonAction]
        public void DeletePdf(string pdfName)
        {
            var pdfPath = Path.Combine(_env.ContentRootPath, "Pdf", pdfName);
            if (System.IO.File.Exists(pdfPath))
                System.IO.File.Delete(pdfPath);
        }

        private async Task SendConfirmationEmail(UpdateDoctorEmailDto user, string token)
        {
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress("Info", "sandrikaa97@gmail.com"));
            emailMessage.To.Add(new MailboxAddress(user.Name, user.Email));
            emailMessage.Subject = "Confirm";

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = $@"
            <p>Hello {user.Name},</p>
            <p>Thank you for registering with our service. Please Copy the following Token to confirm your email address:</p>
            <h1>{token}</h1>
            <p>If you did not register with our service, please ignore this email.</p>";

            emailMessage.Body = bodyBuilder.ToMessageBody();

            using (var smtpClient = new SmtpClient())
            {
                smtpClient.Connect("smtp.gmail.com", 587, useSsl: false);
                smtpClient.Authenticate("sandrikaa97@gmail.com", "");
                await smtpClient.SendAsync(emailMessage);
                smtpClient.Disconnect(true);
            }
        }



        [NonAction]
        public async Task<string> SaveImage(IFormFile imageFile)
        {
            string imageName = new String(Path.GetFileNameWithoutExtension(imageFile.FileName).Take(10).ToArray()).Replace(' ', '-');
            imageName = imageName + DateTime.Now.ToString("yymmssfff") + Path.GetExtension(imageFile.FileName);
            var imagePath = Path.Combine(_env.ContentRootPath, "Images", imageName);
            using (var fileStream = new FileStream(imagePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }
            return imageName;
        }


        [NonAction]
        public async Task<string> SavePdf(IFormFile pdfFile)
        {
            string imageName = new String(Path.GetFileNameWithoutExtension(pdfFile.FileName).Take(10).ToArray()).Replace(' ', '-');
            imageName = imageName + DateTime.Now.ToString("yymmssfff") + Path.GetExtension(pdfFile.FileName);
            var imagePath = Path.Combine(_env.ContentRootPath, "Pdf", imageName);
            using (var fileStream = new FileStream(imagePath, FileMode.Create))
            {
                await pdfFile.CopyToAsync(fileStream);
            }
            return imageName;
        }
    }
}

