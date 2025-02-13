using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MyBGList.DTO;
using MyBGList.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace MyBGList.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BoardGamesController : ControllerBase
    {
        private readonly ILogger<BoardGamesController> _logger;
        private readonly ApplicationDbContext _context;
        public BoardGamesController(ILogger<BoardGamesController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // The JSON Structure of our Response
        //[
        //    {
        //    "id": <int>,
        //    "name": <string>,
        //    "year": <int>
        //    }
        //]

        [HttpGet(Name = "GetBoardGames")]
        [EnableCors("AnyOrigin")]
        [ResponseCache(Location = ResponseCacheLocation.Any, Duration = 60)]
        public async Task<RestDTO<BoardGame[]>> Get(int pageIndex=0, 
                                                    int pageSize=10, 
                                                    string? sortColumn = "Name", 
                                                    string? sortOrder = "ASC",
                                                    string? filterQuery = null)
        {
            var query = _context.BoardGames.AsQueryable();
            if (!string.IsNullOrEmpty(filterQuery))
                query = query.Where(b => b.Name.Contains(filterQuery));
            var recordCount = await query.CountAsync();
            query = query.OrderBy($"{sortColumn} {sortOrder}")
                                           .Skip(pageIndex * pageSize)
                                           .Take(pageSize);

            return new RestDTO<BoardGame[]>()
            {
                Data = await query.ToArrayAsync(),
                PageIndex = pageIndex,
                PageSize = pageSize,
                RecordCount = recordCount,
                Links = new List<LinkDTO> {
                     new LinkDTO(
                     Url.Action(
                         null,
                         "BoardGames",
                         new { pageIndex, pageSize },
                         Request.Scheme)!,
                     "self",
                     "GET"),
                            }
            };
        }

    }
}
