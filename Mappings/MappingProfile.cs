using AutoMapper;
using InboxIntelligence.Models;
using InboxIntelligence.Models.DTOs;

namespace InboxIntelligence.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile() {
            CreateMap<BulkEmailImportDTO, Email>()
                .ForMember(
                    dest => dest.SentAt,
                    opt => opt.MapFrom(src => src.Timestamp))
                .ForMember(
                     dest => dest.Recipients,
                     opt => opt.MapFrom(src => string.Join(" ,", src.Recipients)));
        }
    }
}
