﻿namespace Core.Services.Account
{
    public interface IAccountService
    {
        /// <summary>
        /// Atualiza a senha do usuário caso a mesma obedeça todos os critérios de validação.
        /// </summary>
        void UpdatePassword(Guid accountId, string password);

        /// <summary>
        /// Deleta fisicamente o usuário da base e desvincula sua conta com a B3.
        /// </summary>
        void Delete(Guid accountId);
        Task SendEmailVerification(Guid accountId);
        bool IsEmailVerificationCodeValid(Guid accountId, string code);
    }
}
