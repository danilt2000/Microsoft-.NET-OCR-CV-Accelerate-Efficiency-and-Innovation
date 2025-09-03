using System.ComponentModel;

namespace Microsoft_.NET_OCR_CV_Accelerate_Efficiency_and_Innovation.Models
{
    public class BankAccount
    {
        [Description("Celé číslo bankovního účtu bez předčíslí. U vyplněných dotazníků toto bývá v sekci s názvem: Způsob výplaty prostředků, ale hledej kdekoliv.")]
        public required string AccountNumber { get; set; }
    }
}
