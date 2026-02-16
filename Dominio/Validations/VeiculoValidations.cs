using FluentValidation;
using MinimalApi.DTOs;

namespace MinimalApi.Dominio.Validations;

public class VeiculoValidations : AbstractValidator<VeiculoDTO>
{
    public VeiculoValidations()
    {
        RuleFor(vc => vc.Nome)
            .NotEmpty().WithMessage("O nome do veículo é obrigatório.");

        RuleFor(vc => vc.Ano)
            .LessThanOrEqualTo(DateTime.Now.Year).WithMessage("O ano do veículo não pode ser maior que o ano atual.")
            .GreaterThanOrEqualTo(1886).WithMessage("O ano do veículo não pode ser menor que 1886.");

            RuleFor(vc => vc.Marca)
            .NotEmpty().WithMessage("A marca é obrigatória.");
    }
}