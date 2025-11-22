using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using VisualSoftech.Backend.Models;

namespace VisualSoftech.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public StudentsController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("states")]
        public IActionResult GetStates()
        {
            string cs = _config.GetConnectionString("DefaultConnection");
            var list = new List<Dictionary<string, object>>();

            using (var conn = new SqlConnection(cs))
            using (var cmd = new SqlCommand("SELECT id, state_name FROM state_name ORDER BY state_name", conn))
            {
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new Dictionary<string, object>
                        {
                            ["id"] = rd["id"],
                            ["state_name"] = rd["state_name"]
                        });
                    }
                }
            }

            return Ok(list);
        }

        [HttpGet("list")]
        public IActionResult GetStudents(int page = 1, int pageSize = 10)
        {
            string cs = _config.GetConnectionString("DefaultConnection");
            var list = new List<Dictionary<string, object>>();

            string sql = @"
                SELECT sm.id, sm.name, sm.age, sm.address, sn.state_name,
                       sm.phone_number, sm.photos_json
                FROM student_master sm
                LEFT JOIN state_name sn ON sm.state_id = sn.id
                ORDER BY sm.id
                OFFSET @offset ROWS FETCH NEXT @ps ROWS ONLY;
            ";

            using (var conn = new SqlConnection(cs))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                cmd.Parameters.AddWithValue("@ps", pageSize);

                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        list.Add(new Dictionary<string, object>
                        {
                            ["id"] = rd["id"],
                            ["name"] = rd["name"],
                            ["age"] = rd["age"],
                            ["address"] = rd["address"],
                            ["state_name"] = rd["state_name"],
                            ["phone_number"] = rd["phone_number"],
                            ["photos_json"] = rd["photos_json"]
                        });
                    }
                }
            }

            return Ok(list);
        }

        [HttpGet("{id}")]
        public IActionResult GetStudent(int id)
        {
            string cs = _config.GetConnectionString("DefaultConnection");

            var master = new List<Dictionary<string, object>>();
            var details = new List<Dictionary<string, object>>();

            using (var conn = new SqlConnection(cs))
            {
                conn.Open();

                using (var cmd = new SqlCommand("SELECT * FROM student_master WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            master.Add(new Dictionary<string, object>
                            {
                                ["id"] = rd["id"],
                                ["name"] = rd["name"],
                                ["age"] = rd["age"],
                                ["dob"] = rd["dob"],
                                ["address"] = rd["address"],
                                ["state_id"] = rd["state_id"],
                                ["phone_number"] = rd["phone_number"],
                                ["photos_json"] = rd["photos_json"]
                            });
                        }
                    }
                }

                using (var cmd = new SqlCommand("SELECT * FROM student_detail WHERE student_master_id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            details.Add(new Dictionary<string, object>
                            {
                                ["id"] = rd["id"],
                                ["student_master_id"] = rd["student_master_id"],
                                ["subject_name"] = rd["subject_name"]
                            });
                        }
                    }
                }
            }

            return Ok(new object[] { master, details });
        }

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
                        string sql = @"
                            INSERT INTO student_master 
                            (name, age, dob, address, state_id, phone_number, photos_json)
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
                        return Ok(new { message = "Student created", id = newId });
                    }
                    catch (Exception ex)
                    {
                        tr.Rollback();
                        return StatusCode(500, new { error = ex.Message });
                    }
                }
            }
        }

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
                        string updateSql = @"
                            UPDATE student_master SET 
                                name=@name, age=@age, dob=@dob, address=@addr,
                                state_id=@state, phone_number=@phone, photos_json=@photos
                            WHERE id=@id;
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

                        using (var del = new SqlCommand("DELETE FROM student_detail WHERE student_master_id=@id", conn, tr))
                        {
                            del.Parameters.AddWithValue("@id", id);
                            del.ExecuteNonQuery();
                        }

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
                        using (var cmd = new SqlCommand("DELETE FROM student_detail WHERE student_master_id=@id", conn, tr))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            cmd.ExecuteNonQuery();
                        }

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
    }
}
