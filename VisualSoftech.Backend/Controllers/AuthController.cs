using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using VisualSoftech.Backend.Models;

namespace VisualSoftech.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

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
                    return Ok(new { success = true });
                }
                else
                {
                    return Unauthorized(new { message = "Invalid Username or Password" });
                }
            }
        }
    }
}
