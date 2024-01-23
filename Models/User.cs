using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace UserManagement.Api.Models;

public class User
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; }
    [StringLength(50)]
    [Required]
    public string Family { get; set; }
    [Required]
    public int RoleId { get; set; }
    [JsonIgnore]
    public DateTime RegisterDate { get; } = DateTime.Now;
}