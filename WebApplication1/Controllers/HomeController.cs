﻿using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    [Route("[controller]")] // Route template for this controller
    public class HomeController : ControllerBase
    {
        private readonly IEmailSender _emailSender;
        private MySqlConnection connection;

        public HomeController(IEmailSender emailSender)
        {
            this._emailSender = emailSender;
            // Initialize your database connection here
            connection = new MySqlConnection("server=localhost;uid=root;pwd=;database=UserManagement");
        }

        [HttpGet("CreateAcc")]
        public async Task<IActionResult> CreateUser(User user)
        {
            if (string.IsNullOrEmpty(user.username) || string.IsNullOrEmpty(user.mail) || string.IsNullOrEmpty(user.password))
            {
                return BadRequest("All fields must be filled out.");
            }

            if (!IsValidEmail(user.mail))
            {
                return BadRequest("Invalid email format.");
            }

            // Generate confirmation token
            var token = Guid.NewGuid().ToString();
            var tokenExpiry = DateTime.UtcNow.AddMinutes(10);

            try
            {
                connection.Open();
                MySqlCommand query = connection.CreateCommand();
                query.Prepare();
                query.CommandText =
                    "INSERT INTO `PendingUser` (`Role`, `username`, `passwordHash`, `mail`, `address`, `Token`, `TokenExpiry`) " +
                    "VALUES(1, @username, @password, @mail, @address, @token, @tokenExpiry)";

                // Hash the password using BCrypt
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.password);
                query.Parameters.AddWithValue("@username", user.username);
                query.Parameters.AddWithValue("@password", hashedPassword);
                query.Parameters.AddWithValue("@mail", user.mail);
                query.Parameters.AddWithValue("@address", user.address);
                query.Parameters.AddWithValue("@token", token);
                query.Parameters.AddWithValue("@tokenExpiry", tokenExpiry);

                query.ExecuteNonQuery();

                // Send confirmation email
                var receiver = user.mail;
                var subject = "Confirm Your Account Creation";
                var confirmationLink = Url.Action("ConfirmUser", "Home", new { token = token }, Request.Scheme); // Generate a confirmation link
                var bodyMessage = $@"
                    <p>Dear {user.username},</p>
                    <p>Thank you for signing up! To complete your registration and activate your account, please verify your email by clicking the button below</p>
                    <a href='{confirmationLink}' style='color:blue;text-decoration:none;font-weight:bold;'>Create Account</a>.
                    <p> If you did not create an account with us, you can safely ignore this email. Thank you</p>
                    <p>This link will expire in 10 minutes</p>";

                await _emailSender.SendEmailAsync(receiver, subject, bodyMessage);

                return Ok("Confirmation email sent. Please confirm within 10 minutes.");
            }
            catch (Exception ex)
            {
                connection.Close();
                return StatusCode(500);
            }
            finally
            {
                connection.Close();
            }
        }
     
        [HttpGet("ConfirmUser")]
        public async Task<IActionResult> ConfirmUser(string token)
        {
            try
            {
                connection.Open();
                MySqlCommand query = connection.CreateCommand();
                query.CommandText = "SELECT * FROM PendingUser WHERE Token = @token AND TokenExpiry > @currentDate";
                query.Parameters.AddWithValue("@token", token);
                query.Parameters.AddWithValue("@currentDate", DateTime.UtcNow);

                var reader = query.ExecuteReader();
                if (reader.Read())
                {
                    // Token is valid, proceed with account creation
                    string username = reader["Username"].ToString();
                    string passwordHash = reader["PasswordHash"].ToString();
                    string mail = reader["mail"].ToString();
                    string address = reader["Address"].ToString();

                    reader.Close();

                    // Create user in the actual user table
                    query.CommandText = "INSERT INTO `user` (`Role`, `username`, `password`, `mail`, `address`) " +
                                        "VALUES(1, @username, @password, @mail, @address)";
                    query.Parameters.Clear();
                    query.Parameters.AddWithValue("@username", username);
                    query.Parameters.AddWithValue("@password", passwordHash);
                    query.Parameters.AddWithValue("@mail", mail);
                    query.Parameters.AddWithValue("@address", address);

                    query.ExecuteNonQuery();

                    // Delete pending user
                    query.CommandText = "DELETE FROM PendingUser WHERE Token = @token";
                    query.Parameters.AddWithValue("@token", token);
                    query.ExecuteNonQuery();

                    // Return a success message with auto-close functionality
                    return Content(@"
                <html>
                    <head>
                        <title>Account Confirmed</title>
                    </head>
                    <body>
                        <p>Your account has been confirmed and created successfully.</p>
                        <p>This window will close automatically in 1 seconds.</p>
                        <script>
                            setTimeout(function() {
                                window.close();
                            }, 1000); // Close the window after 1 seconds
                        </script>
                    </body>
                </html>", "text/html");
                }
                else
                {
                    return BadRequest("Invalid or expired token.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }




        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }


}