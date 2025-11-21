
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using VisualSoftech.Backend.Models;
using Services;

namespace VisualSoftech.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly JwtService _jwt;

        public AuthController(IConfiguration config, JwtService jwt)
        {
            _config = config;
            _jwt = jwt;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] UserModel user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Password))
            {
                return BadRequest(new { message = "Username and password are required." });
            }

            string cs = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(cs))
            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(1) FROM users WHERE username=@u AND password=@p", conn))
            {
                cmd.Parameters.AddWithValue("@u", user.Username);
                cmd.Parameters.AddWithValue("@p", user.Password);

                conn.Open();
                int isValid = Convert.ToInt32(cmd.ExecuteScalar());

                if (isValid == 1)
                {
                    string token = _jwt.GenerateToken(user.Username);
                    return Ok(new { token });
                }
                else
                {
                    return Unauthorized(new { message = "Invalid Username or Password" });
                }
            }
        }
    }
}
