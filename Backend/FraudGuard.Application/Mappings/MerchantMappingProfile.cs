using System;
using AutoMapper;
using FraudGuard.Application.DTOs.Merchant;
using FraudGuard.Domain.Entities;

namespace FraudGuard.Application.Mappings
{
    public class MerchantMappingProfile : Profile
    {
        public MerchantMappingProfile()
        {
            CreateMap<EMerchant, GetMerchantsResponse>()
                .ForMember(d => d.PosAgeDays,
                    o => o.MapFrom(s => (int)Math.Max(0, (DateTime.Now - s.PosAssignmentDate).TotalDays)));
        }
    }
}
