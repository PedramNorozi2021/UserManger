using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using UserManagement.Api.DTOs;
using UserManagement.Api.Models;
using Z.Dapper.Plus;

namespace UserManagement.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IDbConnection _connection;
    public UsersController(IConfiguration configur)
    {
        _connection = new SqlConnection(configur.GetConnectionString("default"));
    }

    /// <summary>
    /// ثبت گروهی کاربران
    /// </summary>
    [HttpPost("users")]
    public async Task<IActionResult> InsertUsers(
        [FromBody] IEnumerable<User> users
        )
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        using (IDbTransaction trann = _connection.BeginTransaction())
        {
            try
            {
                trann.BulkInsert(users);
                trann.Commit();
                _connection.Close();
            }
            catch
            {
                trann.Rollback();
                _connection.Close();
                /// log error
                return Problem();
            }
        }
        return Ok();
    }


    /// <summary>
    /// دریافت لبست کاربران
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetUsers(int pageid = 1)
    {
        int take = 10;
        int skip = (pageid - 1) * take;

        string sql = @"select u.Id,u.[Name],u.Family,
	                          r.Id,r.[Name]
                       From dbo.Users u inner join dbo.Roles r
	                        on u.RoleId=r.Id
                       order by u.RegisterDate desc
		               Offset @skip Rows
		               FETCH Next @take ROWS ONLY";

        var users = await _connection.QueryAsync<UserDto, RoleDto, UserDto>(sql,
                     (user, role) =>
                     {
                         user.Role = role;
                         return user;
                     }, new { skip, take }, splitOn: "Id");

        if (!users.Any())
            return NotFound();

        return Ok(users);
    }


    /// <summary>
    /// سرچ کاربران
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchFilter(string q = "", int pageId = 1)
    {
        int take = 10;
        int skip = (pageId - 1) * take;

        DynamicParameters parameters = new DynamicParameters();
        parameters.Add("param", q);
        parameters.Add("skip", skip);
        parameters.Add("take", take);

        var users =
            await _connection.QueryAsync("usp_SearchUser", parameters, commandType: CommandType.StoredProcedure);

        if (!users?.Any() ?? true)
            return NotFound();

        return Ok(users);
    }


    /// <summary>
    /// ثبت نام کاربر
    /// </summary>
    /// <param name="model"></param>
    /// <param name="configur"></param>
    /// <returns></returns>
    [HttpPost("Register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserDto model,
        [FromServices] IConfiguration configur)
    {
        if (!ModelState.IsValid || User.Identity.IsAuthenticated)
            return BadRequest();

        string sql = @"Insert into dbo.[Users]([Name],[Family],[RoleId])
                             		output [inserted].Id
                                 Values(@name,@family,1)";
        string? userId =
            await _connection.ExecuteScalarAsync<string>(sql, new { model.Name, model.Family });

        var issuer = configur["Jwt:Issuer"];
        var audience = configur["Jwt:Audience"];
        var key = Encoding.UTF8.GetBytes(configur["Jwt:Key"]);
        var signinCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            algorithm: SecurityAlgorithms.HmacSha256
            );

        var subject = new ClaimsIdentity(
            new Claim[] {
                 new Claim(ClaimTypes.NameIdentifier, userId!),
                 new Claim(ClaimTypes.Name, model.Name + " " + model.Family)
            });

        var expire = DateTime.Now.AddDays(2);
        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Expires = expire,
            Audience = audience,
            Issuer = issuer,
            SigningCredentials = signinCredentials,
            Subject = subject,
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Ok(new
        {
            msg = "عملیات با موفقیت انجام شد",
            Token = tokenHandler.WriteToken(token)
        });
    }
}
