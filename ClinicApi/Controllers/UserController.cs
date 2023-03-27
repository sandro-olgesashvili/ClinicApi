using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ClinicApi.Data;
using ClinicApi.Dto;
using ClinicApi.Interfaces;
using ClinicApi.Models;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;

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
        public async Task<ActionResult> Register([FromBody] RegisterDto req)
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


            //req.ImageName = await SaveImage(req.ImageFile);


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
                //ImageName = req.ImageName,
                //ImageSrc = String.Format("{0}://{1}{2}/Images/{3}", Request.Scheme, Request.Host, Request.PathBase, req.ImageName),
            };

            _dbContext.Users.Add(user);

            await _dbContext.SaveChangesAsync();


            var confirmationToken = _dbContext.Users.Where(x => x.Email == req.Email).FirstOrDefault().ConfirmationToken;


            await SendConfirmationEmail(user, confirmationToken);

            return Ok(true);

        }


        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmEmail([FromBody] string token)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.ConfirmationToken == token);

            var nowDate = DateTime.Now;

            int result = DateTime.Compare(nowDate, user.EmailConfirmationTokenExpiration);

            if (result >= 0) return Ok(false);


            if (user == null)
            {
                return Ok(false);
            }

            user.IsConfirmed = true;
            user.ConfirmationToken = string.Empty;

            await _dbContext.SaveChangesAsync();

            var res = new { token = _tokenService.GenerateToken(user), role = user.Role };

            return Ok(res);
        }


        [HttpPut("update-confirm-send")]
        public async Task<IActionResult> UpdateConfirm([FromBody] string req)
        {
            var user = _dbContext.Users.Where(x => x.Email == req).FirstOrDefault();

            if (user == null) return Ok(false);

            user.ConfirmationToken = GenerateConfirmationToken();

            user.EmailConfirmationTokenExpiration = DateTime.Now.AddMinutes(5);


            var confirmationTokenn = _dbContext.Users.Where(x => x.Email == req).FirstOrDefault().ConfirmationToken;

            await SendConfirmationEmail(user, confirmationTokenn);

            await _dbContext.SaveChangesAsync();


            return Ok(true);

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


            var res = new { token = _tokenService.GenerateToken(user), role = user.Role };

            return Ok(res);
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



        private async Task SendConfirmationEmail(User user, string token)
        {
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress("Info", "example@gmail.com"));
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
                smtpClient.Authenticate("example@gmail.com", "password");
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

