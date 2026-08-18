using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FraudGuard.Application.DTOs;
using FraudGuard.Application.DTOs.Merchant;
using FraudGuard.Application.Interfaces;
using FraudGuard.Domain.Interfaces.Repositories;

namespace FraudGuard.Application.Services
{
    public class MerchantAppService : IMerchantAppService
    {
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMapper _mapper;

        public MerchantAppService(IMerchantRepository merchantRepository, IMapper mapper)
        {
            _merchantRepository = merchantRepository;
            _mapper = mapper;
        }

        public async Task<ResponseDTO<List<GetMerchantsResponse>>> GetActiveMerchantsAsync()
        {
            var merchants = await _merchantRepository.GetAllActiveAsync();
            return ResponseDTO<List<GetMerchantsResponse>>.Success(
                _mapper.Map<List<GetMerchantsResponse>>(merchants));
        }
    }
}
