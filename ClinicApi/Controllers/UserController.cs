﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ClinicApi.Data;
using ClinicApi.Dto;
using ClinicApi.Interfaces;
using ClinicApi.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Org.BouncyCastle.Ocsp;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ClinicApi.Controllers
{
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly DataContext _dbContext;

        private readonly ITokenService _tokenService;

        private readonly IUserService _userService;

        private readonly IWebHostEnvironment _env;



        public UserController(DataContext dbContext, IUserService userService, ITokenService tokenService, IWebHostEnvironment env)
        {
            _dbContext = dbContext;

            _tokenService = tokenService;

            _userService = userService;

            _env = env;

        }



        [HttpPost("register")]
        public async Task<ActionResult> Register([FromForm] RegisterDto req)
        {
            var check = _dbContext.Users.Where(x => x.Email == req.Email).FirstOrDefault();

            if (check?.IsConfirmed == false && check != null) {
                check.ConfirmationToken = GenerateConfirmationToken();

                var confirmationTokenn = _dbContext.Users.Where(x => x.Email == req.Email).FirstOrDefault().ConfirmationToken;

                await SendConfirmationEmail(check, confirmationTokenn);

                await _dbContext.SaveChangesAsync();


                return Ok(true);

            }


            if (check != null) return Ok(false);

            if (req.ImageFile != null)
            {
                req.ImageName = await SaveImage(req.ImageFile);
            }

            var user = new User
            {
                Name = req.Name,
                Surname = req.Surname,
                Email = req.Email,
                IdNumber = req.IdNumber,
                Password = req.Password,
                IsConfirmed = false,
                ConfirmationToken = GenerateConfirmationToken(),
                EmailConfirmationTokenExpiration = DateTime.Now.AddMinutes(30),
                ImageName = req.ImageName,
            };

            _dbContext.Users.Add(user);

            await _dbContext.SaveChangesAsync();


            var confirmationToken = _dbContext.Users.Where(x => x.Email == req.Email).FirstOrDefault().ConfirmationToken;


            await SendConfirmationEmail(user, confirmationToken);

            return Ok(true);

        }


        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmationTokenDto req)
        {
            var user = _dbContext.Users.Where(u => u.ConfirmationToken == req.ConfirmationToken).FirstOrDefault();

            if (user == null) return Ok(false);

            var nowDate = DateTime.Now;

            int result = DateTime.Compare(nowDate, user.EmailConfirmationTokenExpiration);

            if (result >= 0) return Ok("ბმულის მოქედების ვადა ამოიწურა");

            user.IsConfirmed = true;
            user.ConfirmationToken = null;

            await _dbContext.SaveChangesAsync();

            var res = new {
                token = _tokenService.GenerateToken(user),
                role = user.Role,
                name = user.Name,
                surname = user.Surname,
                image = String.Format("{0}://{1}{2}/Images/{3}", Request.Scheme, Request.Host, Request.PathBase, user.ImageName)
            };

            return Ok(res);
        }


        [HttpPut("update-confirm-send")]
        public async Task<IActionResult> UpdateConfirm([FromBody] UpdateConfrimDto req)
        {
            var user = _dbContext.Users.Where(x => x.Email == req.Email).FirstOrDefault();

            if (user == null) return Ok(false);

            user.ConfirmationToken = GenerateConfirmationToken();

            user.EmailConfirmationTokenExpiration = DateTime.Now.AddMinutes(5);

            var confirmationTokenn = _dbContext.Users.Where(x => x.Email == req.Email).FirstOrDefault().ConfirmationToken;

            await SendConfirmationEmail(user, confirmationTokenn);

            await _dbContext.SaveChangesAsync();


            return Ok(true);

        }

        [HttpPut("passwrodRestore")]
        public async Task<IActionResult> passwrodRestore([FromBody] PasswrodRestoreDto req)
        {
            var user = _dbContext.Users.Where(x => x.Email == req.Email).FirstOrDefault();

            if (user == null) return Ok(false);

            if (user.ConfirmationToken == req.ConfirmationToken) return Ok(true);

            return Ok(false);
        }


        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] UpdateDto req)
        {
            var user = _dbContext.Users.Where(x => x.Email == req.Email).FirstOrDefault();

            if (user == null) return Ok(false);


            if (user.ConfirmationToken != req.ConfirmationToken) return Ok(false);


            var nowDate = DateTime.Now;

            int result = DateTime.Compare(nowDate, user.EmailConfirmationTokenExpiration);

            if (result >= 0) return Ok(false);

            user.IsConfirmed = true;
            user.ConfirmationToken = string.Empty;
            user.Password = req.Password;

            await _dbContext.SaveChangesAsync();

            return Ok(true);

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto req)
        {
            var user = _dbContext.Users.Where(x => x.Email == req.Email).FirstOrDefault();

            if (user == null) return Ok(false);

            if (user.Password != req.Password) return Ok(false);


            if (user.TwoFactor == false)
            {
                var res = new
                {
                    token = _tokenService.GenerateToken(user),
                    role = user.Role,
                    name = user.Name,
                    surname = user.Surname,
                    image = user.ImageName != null ? String.Format("{0}://{1}{2}/Images/{3}", Request.Scheme, Request.Host, Request.PathBase, user.ImageName) : null
                };

                return Ok(res);

            }
            else {

                user.TwoFactorStr = TwoFactorGenerateToken();

                var twoFactorStrToken = _dbContext.Users.Where(x => x.Email == user.Email).FirstOrDefault().TwoFactorStr;

                user.EmailConfirmationTokenExpiration = DateTime.Now.AddMinutes(5);

                await SendConfirmationEmail(user, twoFactorStrToken);

                await _dbContext.SaveChangesAsync();

                return Ok(true);
            }
        }


        [HttpPost("twoFactorLogin")]
        public async Task<IActionResult> twoFactorLogin([FromBody] LoginTwoFactorDto req)
        {
            var user = _dbContext.Users.Where(x => x.Email == req.Email).FirstOrDefault();

            if (user == null) return Ok(false);

            var nowDate = DateTime.Now;

            int result = DateTime.Compare(nowDate, user.EmailConfirmationTokenExpiration);

            if (user.Password != req.Password) return Ok(false);

            if (result >= 0) return Ok("ბმულის მოქედების ვადა ამოიწურა");

            if (user.TwoFactorStr != req.TwoFactorStr) return Ok(false);


            var res = new
            {
                token = _tokenService.GenerateToken(user),
                role = user.Role,
                name = user.Name,
                surname = user.Surname,
                image = user.ImageName != null ? String.Format("{0}://{1}{2}/Images/{3}", Request.Scheme, Request.Host, Request.PathBase, user.ImageName) : null
            };

            user.TwoFactorStr = null;

            await _dbContext.SaveChangesAsync();

            return Ok(res);

        }



        [HttpGet("doctor")]
        public async Task<IActionResult> getDoctor()
        {

            var doctor = await _dbContext.Users.Where(x => x.Role == "doctor").GroupJoin(
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
                        ImageSrc = String.Format("{0}://{1}{2}/Images/{3}", Request.Scheme, Request.Host, Request.PathBase, x.User.ImageName),
                        pdfSrc = String.Format("{0}://{1}{2}/Pdf/{3}", Request.Scheme, Request.Host, Request.PathBase, x.User.PdfName),
                        Description = x.User.Description,
                        Views = x.User.Views != null ? x.User.Views : null
                    }
                 ).OrderByDescending(x => x.IsPinned).ToListAsync();

            return Ok(doctor);
        }


        [HttpGet("doctorCategory")]
        public async Task<IActionResult> getDoctorCategory()
        {
            var doctor = await _dbContext.Users.Where(x => x.Role == "doctor").GroupJoin(
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
                    }
                 ).ToListAsync();



            var doctorByCategory = doctor.GroupBy(c => c.CategoryName);

            var byCategory = doctorByCategory.Select(x => new {
                CategoryName = x.Key,
                Total = x.Sum(x => 1)
            });

            return Ok(byCategory);
        }


        [HttpGet("getDoctorProfile"), Authorize(Roles = "admin")]
        public async Task<IActionResult> getDoctorProfile([FromQuery] GetDoctorProfileDto req)
        {
            var doctor = await _dbContext.Users.Where(x => x.Id == req.Id).GroupJoin(
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
                        ImageSrc = x.User.ImageName != null ? String.Format("{0}://{1}{2}/Images/{3}", Request.Scheme, Request.Host, Request.PathBase, x.User.ImageName) : null,
                        pdfSrc = x.User.PdfName != null ? String.Format("{0}://{1}{2}/Pdf/{3}", Request.Scheme, Request.Host, Request.PathBase, x.User.PdfName) : null
                    }
                 ).FirstOrDefaultAsync();


            return Ok(doctor);
        }


        [HttpGet("getDoctorProfileForAppointment")]
        public async Task<IActionResult> getDoctorProfileForAppointment([FromQuery] GetDoctorProfileDto req)
        {
            var doctor = await _dbContext.Users.Where(x => x.Id == req.Id && x.Role == "doctor").GroupJoin(
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
                        ImageSrc = x.User.ImageName != null ? String.Format("{0}://{1}{2}/Images/{3}", Request.Scheme, Request.Host, Request.PathBase, x.User.ImageName) : null,
                        pdfSrc = x.User.PdfName != null ? String.Format("{0}://{1}{2}/Pdf/{3}", Request.Scheme, Request.Host, Request.PathBase, x.User.PdfName) : null,
                        Description = x.User.Description != null ? x.User.Description : null,
                        Views = x.User.Views != null ? x.User.Views : null
                    }
                 ).FirstOrDefaultAsync();


            return Ok(doctor);
        }

        [HttpGet("getDocotrAppointments")]
        public async Task<IActionResult> getDocotrAppointments([FromQuery] GetDoctorProfileDto req)
        {
            var appointments = _dbContext.Appointments.Where(x => x.UserId == req.Id).ToList();

            if (appointments == null) return Ok(false);

            return Ok(appointments);
        }



        [HttpGet("getMyProfile"), Authorize]
        public async Task<IActionResult> getMyProfile()
        {
            var username = _userService.GetMyName();


            var user = await _dbContext.Users.Where(x => x.Email == username).GroupJoin(
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
                       ImageSrc = x.User.ImageName != null ? String.Format("{0}://{1}{2}/Images/{3}", Request.Scheme, Request.Host, Request.PathBase, x.User.ImageName) : null,
                       pdfSrc = x.User.PdfName != null ? String.Format("{0}://{1}{2}/Pdf/{3}", Request.Scheme, Request.Host, Request.PathBase, x.User.PdfName) : null,
                       TwoFactor = x.User.TwoFactor
                    }
                   ).FirstOrDefaultAsync();

            return Ok(user);
        }


        [HttpPut("changeTwoFactor"), Authorize]
        public async Task<IActionResult> changeTwoFactor([FromBody] TwoFactorDto req)
        {
            var username = _userService.GetMyName();

            var user = _dbContext.Users.Where(x => x.Email == username).FirstOrDefault();

            if (user == null) return Ok(false);

            user.TwoFactor = req.TwoFactor;

            await _dbContext.SaveChangesAsync();

            return Ok(true);
        }



        [HttpPut("passwordChange"), Authorize]
        public async Task<IActionResult> passwordChange([FromQuery] UpdatePasswordDoctorDto req)
        {
            var user = _dbContext.Users.Where(x => x.Id == req.Id).FirstOrDefault();

            if (user == null) return Ok(false);

            user.Password = req.Password;

            await _dbContext.SaveChangesAsync();

            return Ok(true);
        }


        [HttpPut("updateDoctorEmailSend"), Authorize]
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

            var firstEmail = new User {Id =check.Id , Email = check.Email, Name = check.Name };

            var secondEmail = new User { Id = check.Id, Email = req.Email, Name = check.Name };

            await SendConfirmationEmail(firstEmail, check.ConfirmationToken);

            await SendConfirmationEmail(secondEmail, check.ConfirmationTokenEmail);

            return Ok(true);
        }


        [HttpPut("updateDoctorEmailNew"), Authorize]
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


        [HttpPost("reservation"), Authorize]
        public async Task<IActionResult> reservation([FromBody] ReservationDto req)
        {
            var username = _userService.GetMyName();

            var user = _dbContext.Users.Where(x => x.Email == username).FirstOrDefault();

            if (user == null) return Ok(false);

            var appointment = _dbContext.Appointments.Where(x => x.Id == req.Id).FirstOrDefault();

            if (appointment == null || appointment.PatientId != null) return Ok(false);

            var doctor = _dbContext.Users.Where(x => x.Id == appointment.UserId).FirstOrDefault();

            if (doctor.Id == user.Id) return Ok(false);

            appointment.PatientId = user.Id;

            appointment.Description = req.Description;

            await _dbContext.SaveChangesAsync();

            var sendData = await _dbContext.Appointments.Where(x => x.UserId == appointment.UserId).ToListAsync();

            return Ok(sendData);
        }

        [HttpGet("getMoreDetail"), Authorize(Roles = "admin, doctor")]
        public async Task<IActionResult> getMoreDetail([FromQuery] GetDoctorProfileDto req)
        {
            var appointment = _dbContext.Appointments.Where(x => x.Id == req.Id).FirstOrDefault();

            if (appointment == null) return Ok(false);

            var user = _dbContext.Users.Where(x => x.Id == appointment.PatientId).FirstOrDefault();

            if (user == null) return Ok(false);

            var sendData = new
            {
                name = user.Name,
                surname = user.Surname,
                image = String.Format("{0}://{1}{2}/Images/{3}", Request.Scheme, Request.Host, Request.PathBase, user.ImageName),
                description = appointment.Description
            };


            return Ok(sendData);
        }

        [HttpGet("getMore"), Authorize]
        public async Task<IActionResult> getMore([FromQuery] GetDoctorProfileDto req)
        {
            var appointment = _dbContext.Appointments.Where(x => x.Id == req.Id).FirstOrDefault();

            if (appointment == null) return Ok(false);

            var doctor = _dbContext.Users.Where(x => x.Id == appointment.UserId).FirstOrDefault();

            if (doctor == null) return Ok(false);

            var sendData = new
            {
                name = doctor.Name,
                surname = doctor.Surname,
                image = String.Format("{0}://{1}{2}/Images/{3}", Request.Scheme, Request.Host, Request.PathBase, doctor.ImageName),
            };

            return Ok(sendData);
        }


        [HttpPost("views")]
        public async Task<IActionResult> views([FromBody] GetDoctorProfileDto req)
        {
            var user = _dbContext.Users.Where(x => x.Id == req.Id).FirstOrDefault();

            if (user == null) return Ok(false);

            user.Views += 1;

            await _dbContext.SaveChangesAsync();

            return Ok(true);
        }

        
        


        private string GenerateConfirmationToken()
        {
            const int tokenLength = 7;
            var tokenBytes = new byte[tokenLength];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(tokenBytes);
            }
            return Convert.ToBase64String(tokenBytes);
        }


        private static string TwoFactorGenerateToken()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var tokenBuilder = new StringBuilder(4);

            for (int i = 0; i < 4; i++)
            {
                tokenBuilder.Append(chars[random.Next(chars.Length)]);
            }

            return tokenBuilder.ToString();
        }


        private async Task SendConfirmationEmail(User user, string token)
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

