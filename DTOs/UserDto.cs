namespace UserManagement.Api.DTOs
{
    public class UserDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Family { get; set; }
        public RoleDto Role { get; set; }
    }
    public class RoleDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
