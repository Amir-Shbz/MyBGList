using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using MyBGList.Models;

namespace MyBGList.gRPC
{
    public class GrpcService : Grpc.GrpcBase
    {
        private readonly ApplicationDbContext _context;
        public GrpcService(ApplicationDbContext context)
        {
            _context = context;
        }
        public override async Task<BoardGameResponse> GetBoardGame(
            BoardGameRequest request,
            ServerCallContext scc)
        {
            var bg = await _context.BoardGames
                .Where(bg => bg.Id == request.Id)
                .FirstOrDefaultAsync();
            var response = new BoardGameResponse();
            if (bg != null)
            {
                response.Id = bg.Id;
                response.Name = bg.Name;
                response.Year = bg.Year;
            }
            return response;
        }
    }
}
