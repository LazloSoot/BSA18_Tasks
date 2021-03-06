using Newtonsoft.Json;
using System.Collections.Generic;


namespace ProjectStructure.Infrastructure.Shared
{
    public class CrewExtendedDTO
    {
        public long Id { get; set; }

        [JsonProperty("Pilot")]
        public IEnumerable<PilotDTO> PilotDTO { get; set; }
        //public PilotDTO PilotDto { get; set; }

        [JsonProperty("Stewardess")]
        public IEnumerable<StewardessDTO> StewardessesDtos { get; set; }

        public CrewExtendedDTO()
        {
            StewardessesDtos = new List<StewardessDTO>();
        }
    }
}
