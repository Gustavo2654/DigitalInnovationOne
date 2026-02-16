using FluentValidation;
using MinimalApi.DTOs;

namespace MinimalApi.Dominio.Validations;

public class AdministradorDTOValidator : AbstractValidator<AdministradorDTO>
{
    public AdministradorDTOValidator()
    {
        RuleFor(adm => adm.Email)
            .NotEmpty().WithMessage("O email é obrigatório.")
            .EmailAddress().WithMessage("O email deve ser um endereço válido.");

        RuleFor(adm => adm.Senha)
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(6).WithMessage("A senha deve ter pelo menos 6 caracteres.");

            RuleFor(adm => adm.Perfil)
            .NotEmpty().WithMessage("O perfil é obrigatório.");
    }
}