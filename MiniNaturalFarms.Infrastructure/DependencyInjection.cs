using MiniNaturalFarms.Application.Catalog;
using MiniNaturalFarms.Application.Images;
using MiniNaturalFarms.Infrastructure.Catalog;
using MiniNaturalFarms.Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniNaturalFarms.Application.Auth;
using MiniNaturalFarms.Infrastructure.Auth;
using MiniNaturalFarms.Infrastructure.Identity;
using MiniNaturalFarms.Infrastructure.Persistence;
using MiniNaturalFarms.Application.Customer;
using MiniNaturalFarms.Infrastructure.Customer;
using MiniNaturalFarms.Application.Orders;
using MiniNaturalFarms.Infrastructure.Orders;
using MiniNaturalFarms.Infrastructure.Payments;
using MiniNaturalFarms.Application.Admin;
using MiniNaturalFarms.Infrastructure.Admin;
namespace MiniNaturalFarms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
                o.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();

        services.Configure<CloudinarySettings>(config.GetSection("Cloudinary"));
        services.AddScoped<IImageService, CloudinaryImageService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.Configure<RazorpaySettings>(config.GetSection("Razorpay"));
        services.Configure<ShippingSettings>(config.GetSection("Shipping"));
        services.Configure<BusinessSettings>(config.GetSection("Business"));
        services.AddHttpClient<IPaymentGateway, RazorpayGateway>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IAdminOrderService, AdminOrderService>();
        services.AddScoped<IAdminInventoryService, AdminInventoryService>();
        services.AddScoped<IAdminCustomerService, AdminCustomerService>();
        services.AddScoped<IAdminReportService, AdminReportService>();


        return services;
    }
}
