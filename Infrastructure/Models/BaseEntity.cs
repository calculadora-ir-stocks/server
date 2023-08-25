using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FluentValidation;

namespace Infrastructure.Models
{
    public abstract class BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

        // Notification pattern validations.
        [NotMapped]
        public FluentValidation.Results.ValidationResult ValidationResult { get; private set; }

        [NotMapped]
        public bool IsValid { get; protected set; }

        [NotMapped]
        public bool IsInvalid => !IsValid;

        public bool Validate<Model>(Model model, AbstractValidator<Model> validator)
        {
            ValidationResult = validator.Validate(model);
            return IsValid = ValidationResult.IsValid;
        }
    }
}