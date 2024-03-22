using Audit.Core;

namespace Common
{
    public class Auditor
    {
        /// <summary>
        /// Cria um novo <c>Audit</c> no banco de dados.
        /// </summary>
        /// <param name="event">O tipo do evento no formato Objeto:AuditOperation</param>
        /// <param name="value">O valor a ser auditado</param>
        public static void Audit(string @event, string? value, string? comment = null)
        {
            using var audit = AuditScope.Create(@event, () => value);
            if (comment is not null)
                audit.Comment(comment);
        }
    }
}
