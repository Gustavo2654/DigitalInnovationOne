using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Enuns;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Servicos;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.Db;

public class Startup
{
    // Construtor da classe Startup, onde a configuração é injetada e a chave para JWT é obtida do arquivo de configuração
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
        key = Configuration["Jwt:Key"] ?? "deuerrodeveestarassimmesmotestando";

    }
    public IConfiguration Configuration { get; set; } = default!;
    private string key = "";

    // Configurando os serviços da aplicação, incluindo autenticação JWT, autorização
    // injeção de dependências para os serviços de administradores e veículos, validação de DTOs, Swagger e DbContext para MySQL
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication(option =>
        {
            option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(option =>
        {
            option.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
            };
        });

        services.AddAuthorization();
        services.AddControllers();
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend",
            builder =>
            {
                builder.WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
            });
        });

        services.AddScoped<IAdministradorServico, AdministradorServico>();
        services.AddScoped<IVeiculoServico, VeiculoServico>();

        services.AddValidatorsFromAssembly(typeof(Startup).Assembly, includeInternalTypes: true);
        services.AddEndpointsApiExplorer();
        // Configurando o Swagger para incluir a definição de segurança JWT e permitir a autenticação via token
        services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Insira o token JWT aqui"
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
            },
            new List<string>()
        }
    });
        });

        // Configurando o DbContext para usar MySQL, com a string de conexão obtida do arquivo de configuração
        services.AddDbContext<DbContexto>(options =>
        {
            options.UseMySql(
                Configuration.GetConnectionString("MySql"),
                ServerVersion.AutoDetect(Configuration.GetConnectionString("MySql")
                )
            );
        });
    }
    // Configurando o pipeline de middleware da aplicação
    // incluindo tratamento de erros, redirecionamento para HTTPS, autenticação, roteamento e autorização
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseCors("AllowFrontend");
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            // Mapeando os endpoints da API
            endpoints.MapControllers();
            #region Home
            endpoints.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
            #endregion

            // Mapeando endpoints dos administradores e veículos
            #region Administradores
            string GerarTokenJwt(Administrador adm)
            {
                if (string.IsNullOrEmpty(key)) return string.Empty;
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                // Criando as claims para o token JWT
                var claims = new List<Claim>(){
        new Claim("Email", adm.Email),
        new Claim(ClaimTypes.Role, adm.Perfil.ToLower()),
        new Claim("Perfil", adm.Perfil)
    };
                var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: credentials
                );
                return new JwtSecurityTokenHandler().WriteToken(token);
            }

            // Endpoint para login de administradores, que gera um token JWT válido por 1 dia
            endpoints.MapPost("/administradores/login", ([FromBody] LoginDTO loginDTO, IAdministradorServico administradorServico) =>
            {
                var adm = administradorServico.Login(loginDTO);
                if (adm != null)
                {
                    string token = GerarTokenJwt(adm);
                    return Results.Ok(new AdministradorLogado
                    {
                        Email = adm.Email,
                        Perfil = adm.Perfil,
                        Token = token
                    });
                }
                else
                    return Results.Unauthorized();
            }).AllowAnonymous().WithTags("Administradores");

            // *Endpoint para criar um novo administrador*, protegido por autorização
            //  onde apenas administradores com perfil "adm" podem acessar
            endpoints.MapPost("/administradores", ([FromBody] AdministradorDTO administradorDTO, IAdministradorServico administradorServico, IValidator<AdministradorDTO> validator) =>
            {
                var validationResult = validator.Validate(administradorDTO);

                if (!validationResult.IsValid)
                {
                    var problemDetails = new HttpValidationProblemDetails(validationResult.ToDictionary())
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Um ou mais erros de validação ocorreram.",
                        Detail = "Veja a lista de erros para mais detalhes.",
                        Instance = "/administradores"
                    };
                    return Results.Problem(problemDetails);
                }

                // Criando um novo administrador com base no DTO recebido e incluindo-o no serviço de administradores
                var adm = new Administrador
                {
                    Email = administradorDTO.Email,
                    Senha = administradorDTO.Senha,
                    Perfil = administradorDTO.Perfil.ToString() ?? Perfil.editor.ToString()
                };
                administradorServico.Incluir(adm);

                return Results.Created($"/administradores/{adm.Id}", new AdministradorModelView
                {
                    Id = adm.Id,
                    Email = adm.Email,
                    Perfil = adm.Perfil
                });
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "adm" })
            .WithTags("Administradores");

            // *Endpoint para listar todos os administradores*, protegido por autorização
            //  onde apenas administradores com perfil "adm" podem acessar
            endpoints.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) =>
            {
                var adms = new List<AdministradorModelView>();
                var administradores = administradorServico.Todos(pagina);
                foreach (var adm in administradores)
                {
                    adms.Add(new AdministradorModelView
                    {
                        Id = adm.Id,
                        Email = adm.Email,
                        Perfil = adm.Perfil
                    });
                }
                return Results.Ok(adms);
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "adm" })
            .WithTags("Administradores");

            // *Endpoint para buscar um administrador por ID*, protegido por autorização
            //  onde apenas administradores com perfil "adm" podem acessar
            endpoints.MapGet("/administrador/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) =>
            {
                var adm = administradorServico.BuscaPorId(id);
                if (adm == null) return Results.NotFound();
                return Results.Ok(new AdministradorModelView
                {
                    Id = adm.Id,
                    Email = adm.Email,
                    Perfil = adm.Perfil
                });
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "adm" })
            .WithTags("Administradores");
            #endregion

            // *Mapeando os endpoints relacionados aos veículos*, protegidos por autorização
            //  onde apenas administradores com perfil "adm" ou "editor" podem acessar
            #region Veiculos
            static IResult validaDTO(VeiculoDTO veiculoDTO, IValidator<VeiculoDTO> validator)
            {
                if (veiculoDTO is null)
                {
                    return Results.BadRequest(new { Erro = "Payload de veículo não pode ser nulo." });
                }

                var validationResult = validator.Validate(veiculoDTO);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        );

                    return Results.ValidationProblem(errors);
                }

                return Results.Ok();
            }

            // *Endpoint para criar um novo veículo*, protegido por autorização
            //  onde apenas administradores com perfil "adm" ou "editor" podem acessar
            endpoints.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IValidator<VeiculoDTO> validator, IVeiculoServico veiculoServico) =>
            {
                var result = validator.Validate(veiculoDTO);

                if (!result.IsValid)
                    return Results.BadRequest(result.Errors);

                var veiculo = new Veiculo
                {
                    Nome = veiculoDTO.Nome,
                    Marca = veiculoDTO.Marca,
                    Ano = veiculoDTO.Ano
                };
                veiculoServico.Incluir(veiculo);

                return Results.Created($"/veiculos/{veiculo.Id}", veiculo);

            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "adm,editor" })
            .WithTags("Veiculos");

            // *Endpoint para listar todos os veículos*, protegido por autorização
            //  onde apenas administradores com perfil "adm" ou "editor" podem acessar
            endpoints.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculoServico veiculoServico) =>
            {
                var veiculos = veiculoServico.Todos(pagina);
                return Results.Ok(veiculos);
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "adm,editor" })
            .WithTags("Veiculos");

            // *Endpoint para buscar um veículo por ID*, protegido por autorização
            //  onde apenas administradores com perfil "adm" ou "editor" podem acessar
            endpoints.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
            {
                var veiculo = veiculoServico.BuscaPorId(id);
                if (veiculo == null) return Results.NotFound();
                return Results.Ok(veiculo);
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "adm,editor" })
            .WithTags("Veiculos");

            // *Endpoint para atualizar um veículo por ID*, protegido por autorização
            //  onde apenas administradores com perfil "adm" podem acessar
            endpoints.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IValidator<VeiculoDTO> validator, IVeiculoServico veiculoServico) =>
            {
                var veiculo = veiculoServico.BuscaPorId(id);
                if (veiculo == null) return Results.NotFound();

                var result = validator.Validate(veiculoDTO);

                if (!result.IsValid)
                    return Results.BadRequest(result.Errors);

                veiculo.Nome = veiculoDTO.Nome;
                veiculo.Marca = veiculoDTO.Marca;
                veiculo.Ano = veiculoDTO.Ano;

                veiculoServico.Atualizar(veiculo);

                return Results.Ok(veiculo);
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "adm" })
            .WithTags("Veiculos");

            // *Endpoint para apagar um veículo por ID*, protegido por autorização
            //  onde apenas administradores com perfil "adm" podem acessar
            endpoints.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculoServico veiculoServico) =>
            {
                var veiculo = veiculoServico.BuscaPorId(id);
                if (veiculo == null) return Results.NotFound();

                veiculoServico.Apagar(veiculo);

                return Results.NoContent();
            })
            .RequireAuthorization(new AuthorizeAttribute { Roles = "adm" })
            .WithTags("Veiculos");
            #endregion
        });
    }
};