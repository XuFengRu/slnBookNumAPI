namespace BookNumAPI.Match.DTO
{
    public class MatchResultDto
    {
        public int Count { get; set; }
        public List<MatchCandidateDto> Candidates { get; set; } = new();

    }
}
