namespace Common.Models.Handlers
{
    public abstract class BaseRequirement
    {
        protected BaseRequirement(string scope)
        {
            Scope = scope;
        }

        /// <summary>
        /// O nome da <c>Policy</c> a ser configurada no contâiner de DI.
        /// </summary>
        public string Scope { get; init; }
    }
}
