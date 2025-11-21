//using Microsoft.AspNetCore.Mvc;

//namespace VisualSoftech.Backend.Controllers
//{
//    public class StudentsController : Controller
//    {
//        public IActionResult Index()
//        {
//            return View();
//        }
//    }
//}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Data;
using VisualSoftech.Backend.Models;

namespace VisualSoftech.Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]   // all APIs protected
    public class StudentsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public StudentsController(IConfiguration config)
        {
            _config = config;
        }

        // -------------------------------------------------------------
        // GET: api/students/states    -> dynamic dropdown
        // -------------------------------------------------------------
        [AllowAnonymous]
        [HttpGet("states")]
        public IActionResult GetStates()
        {
            string cs = _config.GetConnectionString("DefaultConnection");
            var dt = new DataTable();

            using (var da = new SqlDataAdapter("SELECT id, state_name FROM state_name ORDER BY state_name", cs))
            {
                da.Fill(dt);
            }

            return Ok(dt);
        }

        // -------------------------------------------------------------
        // GET: api/students/list?page=1&pageSize=10 (pagination)
        // -------------------------------------------------------------
        [HttpGet("list")]
        public IActionResult GetStudents(int page = 1, int pageSize = 10)
        {
            string cs = _config.GetConnectionString("DefaultConnection");
            var dt = new DataTable();

            string sql = @"
                SELECT sm.id, sm.name, sm.age, sm.address, sn.state_name, sm.phone_number, sm.photos_json
                FROM student_master sm
                LEFT JOIN state_name sn ON sm.state_id = sn.id
                ORDER BY sm.id
                OFFSET @offset ROWS FETCH NEXT @ps ROWS ONLY
            ";

            using (var conn = new SqlConnection(cs))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@ps", pageSize);

                using (var da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            return Ok(dt);
        }


        // -------------------------------------------------------------
        // GET BY ID → JSON ARRAY (required in assignment)
        // -------------------------------------------------------------
        [HttpGet("{id}")]
        public IActionResult GetStudent(int id)
        {
            string cs = _config.GetConnectionString("DefaultConnection");

            var master = new DataTable();
            var detail = new DataTable();

            using (var conn = new SqlConnection(cs))
            {
                conn.Open();

                // master
                using (var cmd = new SqlCommand("SELECT * FROM student_master WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(master);
                    }
                }

                // detail (subjects)
                using (var cmd2 = new SqlCommand("SELECT * FROM student_detail WHERE student_master_id=@id", conn))
                {
                    cmd2.Parameters.AddWithValue("@id", id);
                    using (var da2 = new SqlDataAdapter(cmd2))
                    {
                        da2.Fill(detail);
                    }
                }
            }

            // assignment requirement: return JSON array
            return Ok(new object[] { master, detail });
        }


        // -------------------------------------------------------------
        // POST: api/students/create
        // -------------------------------------------------------------
        [HttpPost("create")]
        public IActionResult Create([FromBody] StudentMaster model)
        {
            string cs = _config.GetConnectionString("DefaultConnection");

            using (var conn = new SqlConnection(cs))
            {
                conn.Open();
                using (var tr = conn.BeginTransaction())
                {
                    try
                    {
                        // insert master
                        string sql = @"
                            INSERT INTO student_master (name, age, dob, address, state_id, phone_number, photos_json)
                            VALUES (@name, @age, @dob, @addr, @state, @phone, @photos);
                            SELECT SCOPE_IDENTITY();
                        ";

                        int newId;

                        using (var cmd = new SqlCommand(sql, conn, tr))
                        {
                            cmd.Parameters.AddWithValue("@name", model.Name);
                            cmd.Parameters.AddWithValue("@age", model.Age);
                            cmd.Parameters.AddWithValue("@dob", model.Dob);
                            cmd.Parameters.AddWithValue("@addr", (object)model.Address ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@state", model.StateId);
                            cmd.Parameters.AddWithValue("@phone", (object)model.PhoneNumber ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@photos", (object)model.PhotosJson ?? DBNull.Value);

                            newId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // insert subjects
                        if (model.Subjects != null)
                        {
                            foreach (var sub in model.Subjects)
                            {
                                using (var cmd = new SqlCommand(
                                    "INSERT INTO student_detail (student_master_id, subject_name) VALUES (@sid, @sub)",
                                    conn, tr))
                                {
                                    cmd.Parameters.AddWithValue("@sid", newId);
                                    cmd.Parameters.AddWithValue("@sub", sub.SubjectName);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        tr.Commit();
                        return Ok(new { message = "Student created successfully", id = newId });
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        return StatusCode(500, new { error = ex.Message });
                    }
                }
            }
        }


        // -------------------------------------------------------------
        // PUT: api/students/update/{id}
        // -------------------------------------------------------------
        [HttpPut("update/{id}")]
        public IActionResult Update(int id, [FromBody] StudentMaster model)
        {
            string cs = _config.GetConnectionString("DefaultConnection");

            using (var conn = new SqlConnection(cs))
            {
                conn.Open();
                using (var tr = conn.BeginTransaction())
                {
                    try
                    {
                        // update master
                        string updateSql = @"
                            UPDATE student_master 
                            SET name=@name, age=@age, dob=@dob, address=@addr, state_id=@state, phone_number=@phone, photos_json=@photos
                            WHERE id=@id
                        ";

                        using (var cmd = new SqlCommand(updateSql, conn, tr))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.Parameters.AddWithValue("@name", model.Name);
                            cmd.Parameters.AddWithValue("@age", model.Age);
                            cmd.Parameters.AddWithValue("@dob", model.Dob);
                            cmd.Parameters.AddWithValue("@addr", (object)model.Address ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@state", model.StateId);
                            cmd.Parameters.AddWithValue("@phone", (object)model.PhoneNumber ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@photos", (object)model.PhotosJson ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }

                        // delete old subjects
                        using (var del = new SqlCommand("DELETE FROM student_detail WHERE student_master_id=@id", conn, tr))
                        {
                            del.Parameters.AddWithValue("@id", id);
                            del.ExecuteNonQuery();
                        }

                        // insert new subjects
                        if (model.Subjects != null)
                        {
                            foreach (var sub in model.Subjects)
                            {
                                using (var cmd = new SqlCommand(
                                    "INSERT INTO student_detail (student_master_id, subject_name) VALUES (@sid, @sub)",
                                    conn, tr))
                                {
                                    cmd.Parameters.AddWithValue("@sid", id);
                                    cmd.Parameters.AddWithValue("@sub", sub.SubjectName);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        tr.Commit();
                        return Ok(new { message = "Record updated" });
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        return StatusCode(500, new { error = ex.Message });
                    }
                }
            }
        }


        // -------------------------------------------------------------
        // DELETE: api/students/{id}
        // -------------------------------------------------------------
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            string cs = _config.GetConnectionString("DefaultConnection");

            using (var conn = new SqlConnection(cs))
            {
                conn.Open();
                using (var tr = conn.BeginTransaction())
                {
                    try
                    {
                        // delete detail table
                        using (var cmd = new SqlCommand("DELETE FROM student_detail WHERE student_master_id=@id", conn, tr))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        // delete master table
                        using (var cmd = new SqlCommand("DELETE FROM student_master WHERE id=@id", conn, tr))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

                        tr.Commit();
                        return Ok(new { message = "Record deleted" });
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        return StatusCode(500, new { error = ex.Message });
                    }
                }
            }
        }


        // -------------------------------------------------------------
        // POST: api/students/add-state
        // -------------------------------------------------------------
        [HttpPost("add-state")]
        public IActionResult AddState([FromBody] dynamic data)
        {
            string stateName = data?.state;

            if (string.IsNullOrWhiteSpace(stateName))
            {
                return BadRequest(new { message = "State name required" });
            }

            string cs = _config.GetConnectionString("DefaultConnection");

            using (var conn = new SqlConnection(cs))
            using (var cmd = new SqlCommand("INSERT INTO state_name (state_name) VALUES (@s)", conn))
            {
                cmd.Parameters.AddWithValue("@s", stateName);
                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return Ok(new { message = "State saved" });
        }
    }
}
