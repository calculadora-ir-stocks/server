using System.ComponentModel;

namespace Common.Enums
{
    public enum TaxesStatus
    {
        [Description("unpaid")]
        Unpaid,
        [Description("paid")]
        Paid,
        /// <summary>
        /// É utilizado no mês atual pois é impossível pagar o imposto do mês atual.
        /// É necessário, no entanto, esperar o mês seguinte para pagar o imposto do mês atual.
        /// </summary>
        [Description("pending")]
        Pending
    }
}
