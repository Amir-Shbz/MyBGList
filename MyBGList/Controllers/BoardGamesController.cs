using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MyBGList.DTO;
using MyBGList.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using MyBGList.Constants;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;



namespace MyBGList.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BoardGamesController : ControllerBase
    {
        private readonly ILogger<BoardGamesController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _memoryCache;
        public BoardGamesController(ILogger<BoardGamesController> logger, ApplicationDbContext context, IMemoryCache memoryCache)
        {
            _logger = logger;
            _context = context;
            _memoryCache = memoryCache;
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
        [ResponseCache(CacheProfileName = "Any-60")]
        public async Task<RestDTO<BoardGame[]>> Get([FromQuery] RequestDTO<BoardGameDTO> input)
        {
            _logger.LogInformation(CustomLogEvents.BoardGamesController_Get,
                                                "Get method started at {0}", DateTime.Now.ToString("HH:mm"));

            var query = _context.BoardGames.AsQueryable();
            if (!string.IsNullOrEmpty(input.FilterQuery))
                query = query.Where(b => b.Name.Contains(input.FilterQuery));

            var recordCount = await query.CountAsync();

            BoardGame[]? result = null;
            var cacheKey = $"{input.GetType()}-{JsonSerializer.Serialize(input)}";
            if (!_memoryCache.TryGetValue<BoardGame[]>(cacheKey, out result))
            {
                query = query
                        .OrderBy($"{input.SortColumn} {input.SortOrder}")
                        .Skip(input.PageIndex * input.PageSize)
                        .Take(input.PageSize);
                result = await query.ToArrayAsync();
                _memoryCache.Set(cacheKey, result, new TimeSpan(0, 0, 30));
            }

            return new RestDTO<BoardGame[]>()
            {
                Data = await query.ToArrayAsync(),
                PageIndex = input.PageIndex,
                PageSize = input.PageSize,
                RecordCount = recordCount,
                Links = new List<LinkDTO> {
                     new LinkDTO(
                     Url.Action(
                         null,
                         "BoardGames",
                         new { input.PageIndex, input.PageSize },
                         Request.Scheme)!,
                     "self",
                     "GET"),
                            }
            };
        }

        [Authorize(Roles = RoleNames.Moderator)]
        [HttpPost(Name = "UpdateBoardGame")]
        [ResponseCache(NoStore = true)]
        public async Task<RestDTO<BoardGame?>> Post(BoardGameDTO model)
        {
            var boardgame = await _context.BoardGames.Where(b => b.Id == model.Id).FirstOrDefaultAsync();
            if (boardgame != null)
            {
                if (!string.IsNullOrEmpty(model.Name))
                    boardgame.Name = model.Name;
                if (model.Year.HasValue && model.Year.Value > 0)
                    boardgame.Year = model.Year.Value;
                boardgame.LastModifiedDate = DateTime.Now;
                _context.BoardGames.Update(boardgame);
                await _context.SaveChangesAsync();
            }

            return new RestDTO<BoardGame?>
            {
                Data = boardgame,
                Links = new List<LinkDTO>
                    {
                        new LinkDTO(
                            Url.Action(
                                null,
                                "BoardGames",
                                model,
                                Request.Scheme)!,
                            "self",
                            "POST"),
                    }
            };
        }

        [Authorize(Roles = RoleNames.Administrator)]
        [HttpDelete(Name = "DeleteBoardGame")]
        [ResponseCache(NoStore = true)]
        public async Task<RestDTO<BoardGame?>> Delete(int id)
        {
            var boardgame = await _context.BoardGames.Where(b => b.Id == id).FirstOrDefaultAsync();
            if (boardgame != null)
            {
                _context.BoardGames.Remove(boardgame);
                await _context.SaveChangesAsync();
            };
            return new RestDTO<BoardGame?>()
            {
                Data = boardgame,
                Links = new List<LinkDTO>
                {
                        new LinkDTO(
                        Url.Action(
                            null,
                            "BoardGames",
                            id,
                            Request.Scheme)!,
                        "self",
                        "DELETE"),
                }
            };
        }

        [HttpGet("{id}")]
        [ResponseCache(CacheProfileName = "Any-60")]
        public async Task<RestDTO<BoardGame?>> GetBoardGame(int id)
        {
            _logger.LogInformation(CustomLogEvents.BoardGamesController_Get, "GetBoardGame method started.");
            BoardGame? result = null;
            var cacheKey = $"GetBoardGame-{id}";
            if (!_memoryCache.TryGetValue<BoardGame>(cacheKey, out result))
            {
                result = await _context.BoardGames.FirstOrDefaultAsync(bg => bg.Id == id);
                _memoryCache.Set(cacheKey, result, new TimeSpan(0, 0, 30));
            }
            return new RestDTO<BoardGame?>()
            {
                Data = result,
                PageIndex = 0,
                PageSize = 1,
                RecordCount = result != null ? 1 : 0,
                Links = new List<LinkDTO> {
                    new LinkDTO(
                        Url.Action(
                            null,
                            "BoardGames",
                            new { id },
                            Request.Scheme)!,
                        "self",
                        "GET"),
                }
            };
        }
    }
}
