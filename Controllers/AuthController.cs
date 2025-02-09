using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using ILogger = Serilog.ILogger;



[ApiController]
[Route("api/auth")]
public class AuthContoller : ControllerBase
{
    private readonly AppDbContext _context;
    public readonly IConfiguration _configuration; 
    private readonly ILogger _logger;
    public AuthContoller(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _logger = Log.ForContext<AuthContoller>();
    }
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        if (await _context.Users.AnyAsync(e => e.Email == user.Email))
            return BadRequest("User already Exist");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
        _context.Users.Add(user); 
        await _context.SaveChangesAsync();           

        return Ok("User registered successfully.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        _logger.Information("Login attempt for user: {Email}", loginRequest.Email);
        var user = await _context.Users.FirstOrDefaultAsync(e => e.Email == loginRequest.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash)){
            _logger.Warning("Invalid login attempt for user: {Email}", loginRequest.Email);
            return Unauthorized("Invalid Email or Password");
        }
        var token = GenerateJwtToken(user);

        return Ok(new { token });
    }
    
    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

}