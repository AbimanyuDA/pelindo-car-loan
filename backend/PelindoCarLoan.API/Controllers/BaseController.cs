using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PelindoCarLoan.API.Controllers
{
    /// <summary>
    /// Base controller with common functionality
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController : ControllerBase
    {
        /// <summary>
        /// Gets the current user ID from the JWT token
        /// </summary>
        protected int CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
            }
        }

        /// <summary>
        /// Gets the current user's role from the JWT token
        /// </summary>
        protected string CurrentUserRole
        {
            get
            {
                var roleClaim = User.FindFirst(ClaimTypes.Role);
                return roleClaim?.Value ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the current user's name from the JWT token
        /// </summary>
        protected string CurrentUserName
        {
            get
            {
                var nameClaim = User.FindFirst(ClaimTypes.Name);
                return nameClaim?.Value ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets the current user's email from the JWT token
        /// </summary>
        protected string CurrentUserEmail
        {
            get
            {
                var emailClaim = User.FindFirst(ClaimTypes.Email);
                return emailClaim?.Value ?? string.Empty;
            }
        }
    }
}
