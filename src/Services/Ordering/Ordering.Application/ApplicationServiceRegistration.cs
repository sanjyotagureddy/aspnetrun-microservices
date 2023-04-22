using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Behaviors;

namespace Ordering.Application;

public static class ApplicationServiceRegistration
{
  public static IServiceCollection AddApplicationServices(this IServiceCollection service)
  {
    service.AddAutoMapper(Assembly.GetExecutingAssembly());
    service.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    service.AddMediatR(Assembly.GetExecutingAssembly());

    service.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
    service.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    return service;
  }
}