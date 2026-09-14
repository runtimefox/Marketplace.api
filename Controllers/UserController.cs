using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Models.Dtos.Users;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Users;

namespace MyApi.Controllers;

[Route("api/user")]
[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]

public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> Get()
    {
        return await _userService.GetAllUsersAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> Get(Guid id)
    {
        return await _userService.GetUserByIdAsync(id);
    }
}
