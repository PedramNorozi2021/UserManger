using System.ComponentModel.DataAnnotations;

namespace UserManagement.Api.DTOs;

public class RegisterUserDto
{
    [Required]
    public string Name { get; set; }
    [Required]
    public string Family { get; set; }
}
