using AeFinder.Apps;
using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using AeFinder.Entities.Es;
using AeFinder.Etos;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Grains.State.Apps;
using AeFinder.Studio;
using AutoMapper;
using Volo.Abp.AutoMapper;
using Volo.Abp.Identity;

namespace AeFinder;

public class AeFinderApplicationAutoMapperProfile : Profile
{
    public AeFinderApplicationAutoMapperProfile()
    {
        CreateMap<BlockIndex, BlockDto>();
        CreateMap<Transaction, TransactionDto>();
        CreateMap<LogEvent, LogEventDto>();

        CreateMap<TransactionIndex, TransactionDto>();
        CreateMap<LogEventIndex, LogEventDto>();
        CreateMap<SummaryIndex, SummaryDto>();

        CreateMap<BlockDto, BlockWithTransactionDto>();
        CreateMap<NewBlockEto, BlockWithTransactionDto>();
        CreateMap<ConfirmBlockEto, BlockWithTransactionDto>();

        CreateMap<TransactionCondition, FilterTransactionInput>();
        CreateMap<LogEventCondition, FilterContractEventInput>();

        CreateMap<SubscriptionManifestDto, SubscriptionManifest>();
        CreateMap<SubscriptionManifest, SubscriptionManifestDto>();
        CreateMap<SubscriptionDto, Subscription>();
        CreateMap<TransactionConditionDto, TransactionCondition>();
        CreateMap<LogEventConditionDto, LogEventCondition>();

        CreateMap<Subscription, SubscriptionDto>();
        CreateMap<TransactionCondition, TransactionConditionDto>();
        CreateMap<LogEventCondition, LogEventConditionDto>();

        CreateMap<AllSubscription, AllSubscriptionDto>();
        CreateMap<SubscriptionDetail, SubscriptionDetailDto>();
        CreateMap<AddOrUpdateAeFinderAppInput, AeFinderAppInfo>();
        CreateMap<AeFinderAppInfo, AeFinderAppInfoDto>();

        CreateMap<IdentityUser, IdentityUserDto>();

        CreateMap<AppState, AppDto>()
            .ForMember(destination => destination.CreateTime,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.CreateTime)))
            .ForMember(destination => destination.UpdateTime,
                opt => opt.MapFrom(source => DateTimeHelper.ToUnixTimeMilliseconds(source.UpdateTime)));
        CreateMap<CreateAppDto, AppState>();
    }
}