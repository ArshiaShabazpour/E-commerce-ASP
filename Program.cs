using ECommerceApp.Data;
using ECommerceApp.Services;
using ECommerece.Services;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped<CustomerService>();
            builder.Services.AddScoped<AddressService>();
            builder.Services.AddScoped<CategoryService>();
            builder.Services.AddScoped<ProductService>();
            builder.Services.AddScoped<ShoppingCartService>();
            builder.Services.AddScoped<ProductService>();
            builder.Services.AddScoped<PaymentService>();
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<OrderService>();
            builder.Services.AddTransient<TemplateService>();
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}