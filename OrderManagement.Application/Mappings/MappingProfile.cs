using AutoMapper;
using OrderManagement.Application.DTOs;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Entity to DTO mappings
        CreateMap<Customer, CustomerDto>();
        CreateMap<Product, ProductDto>();
        CreateMap<Order, OrderDto>();
        CreateMap<OrderItem, OrderItemDto>();

        CreateMap<Customer, CreateCustomerDto>().ReverseMap();

        CreateMap<Product, CreateProductDto>().ReverseMap();
     
        CreateMap<CreateOrderDto, Order>()
            .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems));

        CreateMap<CreateOrderItemDto, OrderItem>();

        // Entity to DTO mappings


    }
}